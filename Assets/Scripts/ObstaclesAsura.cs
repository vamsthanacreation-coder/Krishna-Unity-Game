using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Obstacle (Asura) behaviour: falls with configurable speed, notifies on hit, damages player on collision,
/// and destroys itself when it goes below the screen.
/// Configure sprite, speed multiplier and optional gravity-like smoothing.
/// </summary>
[RequireComponent(typeof(SpriteRenderer), typeof(Collider2D))]
public class ObstaclesAsura : MonoBehaviour
    // Integration notes:
    // - Attach this script to your Asura obstacle prefab.
    // - Add a SpriteRenderer and Collider2D (set as Trigger).
    // - Assign sprites, speed, gravity, and bounds in inspector.
    // - Player object must have tag "Player" and a PlayerHealth script.
    // - GameManager script should have GameOver(string reason) method if hitEndsGame is enabled.
    // - Use UnityEvents for custom effects on spawn/hit.
{
    [Header("Appearance")]
    public Sprite[] possibleSprites;
    public bool randomizeSpriteOnSpawn = true;

    [Header("Movement")]
    [Tooltip("Base falling speed in units per second")] public float baseSpeed = 2f;
    [Tooltip("Additional downward acceleration to make fall smoother (positive value increases downward speed)")] public float gravity = 0.5f;
    [Tooltip("Optional horizontal drift range applied each spawn")]
    public float horizontalDrift = 0.0f;

    [Header("Gameplay")]
    [Tooltip("Damage to apply to the player on collision (usually 1)")] public int damage = 1;
    [Tooltip("If true, hitting the player ends the game instantly as well as dealing damage")]
    public bool hitEndsGame = false;

    [Header("Bounds")]
    [Tooltip("Y position below which the obstacle is destroyed automatically")] public float destroyBelowY = -10f;
    [Tooltip("X bounds - if obstacle goes outside these bounds, it gets destroyed")] public float destroyOutsideX = 15f;

    [Header("Events")]
    public UnityEvent onSpawned;
    public UnityEvent onHitPlayer;

    [Header("Audio")]
    [Tooltip("Audio clip to play when hitting the player")]
    public AudioClip hitPlayerSound;
    [Tooltip("Audio clip to play when spawning (optional)")]
    public AudioClip spawnSound;

    // internal state
    Vector2 velocity;

    SpriteRenderer sr;
    AudioSource audioSource;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        audioSource = GetComponent<AudioSource>();
        
        // If no AudioSource exists, add one
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
        
        // Configure AudioSource to prevent being disabled
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D audio
    }

    void OnEnable()
    {
        SetupOnSpawn();
    }

    /// <summary>
    /// Reset obstacle for reuse from object pool
    /// </summary>
    public void ResetObstacle()
    {
        // Reset velocity to prevent bouncing
        velocity = Vector2.zero;
        SetupOnSpawn();
    }

    void SetupOnSpawn()
    {
        // choose sprite
        if (possibleSprites != null && possibleSprites.Length > 0 && randomizeSpriteOnSpawn)
        {
            sr.sprite = possibleSprites[UnityEngine.Random.Range(0, possibleSprites.Length)];
        }

        // Reset velocity to initial downward movement (prevent bouncing)
        velocity = new Vector2(UnityEngine.Random.Range(-horizontalDrift, horizontalDrift), -baseSpeed);

        // Play spawn sound if available
        if (spawnSound != null)
        {
            AudioSource.PlayClipAtPoint(spawnSound, transform.position);
        }

        onSpawned?.Invoke();
    }

    void Update()
    {
        // integrate velocity with optional gravity for smooth falling
        velocity.y -= gravity * Time.deltaTime; // Make gravity pull downward (negative)
        transform.Translate(velocity * Time.deltaTime);

        // Check boundaries and destroy if outside
        Vector3 pos = transform.position;
        if (pos.y < destroyBelowY || Mathf.Abs(pos.x) > destroyOutsideX)
        {
            gameObject.SetActive(false);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        HandlePlayerCollision(other);
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        HandlePlayerCollision(collision.collider);
    }

    void HandlePlayerCollision(Collider2D other)
    {
        // Debug to check if collision is detected
        Debug.Log($"Asura collision detected with: {other.name}, tag: '{other.tag}'");
        
        // Check if the other object is the player (try multiple ways)
        bool isPlayer = other.CompareTag("Player") || 
                       other.name.ToLower().Contains("player") ||
                       other.GetComponent<PlayerHealth>() != null;
        
        if (isPlayer)
        {
            Debug.Log("Player collision confirmed - applying damage and playing sound");
            
            // Force play audio with multiple methods
            if (hitPlayerSound != null)
            {
                try
                {
                    // Method 1: PlayClipAtPoint
                    AudioSource.PlayClipAtPoint(hitPlayerSound, transform.position, 1.0f);
                    Debug.Log("Audio played using PlayClipAtPoint");
                    
                    // Method 2: Also try with the AudioSource if available
                    if (audioSource != null && audioSource.enabled)
                    {
                        audioSource.clip = hitPlayerSound;
                        audioSource.Play();
                        Debug.Log("Audio also played using AudioSource.Play");
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Audio playback failed: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning("No hit player sound assigned!");
            }

            // try to call health on player
            var healthComp = other.GetComponent<PlayerHealth>();
            if (healthComp != null)
            {
                Debug.Log($"Found PlayerHealth component. Calling TakeDamage({damage})");
                healthComp.TakeDamage(damage);
                Debug.Log("TakeDamage called successfully");
            }
            else
            {
                Debug.LogWarning("PlayerHealth component not found on player!");
                // Try to find it in parent or children
                var parentHealth = other.GetComponentInParent<PlayerHealth>();
                var childHealth = other.GetComponentInChildren<PlayerHealth>();
                if (parentHealth != null)
                {
                    Debug.Log("Found PlayerHealth in parent, calling TakeDamage");
                    parentHealth.TakeDamage(damage);
                }
                else if (childHealth != null)
                {
                    Debug.Log("Found PlayerHealth in children, calling TakeDamage");
                    childHealth.TakeDamage(damage);
                }
            }

            onHitPlayer?.Invoke();

            if (hitEndsGame)
            {
                // Try to find a GameManager and notify game over if available
                var gm = FindFirstObjectByType<GameManager>();
                if (gm != null)
                {
                    gm.GameOver("You were defeated by an Asura!");
                }
            }

            // Delay deactivation slightly to ensure audio starts
            StartCoroutine(DeactivateAfterSound());
        }
        else
        {
            Debug.Log($"Not a player collision. Object: {other.name}, Tag: '{other.tag}'");
        }
    }

    System.Collections.IEnumerator DeactivateAfterSound()
    {
        yield return new WaitForSeconds(0.2f); // Increased delay to ensure audio starts
        gameObject.SetActive(false);
    }

    /// <summary>
    /// Test method to manually trigger audio - call this from inspector or other scripts
    /// </summary>
    [ContextMenu("Test Hit Sound")]
    public void TestHitSound()
    {
        Debug.Log("Testing hit sound manually");
        if (hitPlayerSound != null)
        {
            AudioSource.PlayClipAtPoint(hitPlayerSound, transform.position, 1.0f);
            Debug.Log("Test audio played");
        }
        else
        {
            Debug.LogWarning("No hit sound assigned for testing");
        }
    }
}
