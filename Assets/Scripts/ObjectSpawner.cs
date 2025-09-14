using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the spawning of fruits and bombs from the top of the screen.
/// </summary>
public class ObjectSpawner : MonoBehaviour
{
    [Header("Object Prefabs")]
    [Tooltip("A list of fruit prefabs to be spawned.")]
    public List<GameObject> fruitPrefabs;
    [Tooltip("The bomb prefab to be spawned.")]
    public GameObject bombPrefab;

    [Header("Spawning Settings")]
    [Tooltip("The center of the area where objects will be spawned.")]
    public Vector3 spawnAreaCenter = new Vector3(0, 10, 0);
    [Tooltip("The size of the area where objects will be spawned.")]
    public Vector3 spawnAreaSize = new Vector3(16, 0, 0);
    [Tooltip("The initial time interval between spawns.")]
    public float initialSpawnInterval = 2.0f;

    [Header("Difficulty Settings")]
    [Tooltip("The minimum time interval between spawns, representing the maximum difficulty.")]
    public float minSpawnInterval = 0.5f;
    [Tooltip("The rate at which the spawn interval decreases, making the game harder over time.")]
    public float difficultyIncreaseRate = 0.05f;
    [Tooltip("The percentage chance (0-1) that a bomb will spawn instead of a fruit.")]
    [Range(0, 1)]
    public float bombSpawnChance = 0.2f;

    // --- Private Fields ---
    private float currentSpawnInterval;
    private float timeSinceLastSpawn;

    /// <summary>
    /// Called when the script instance is being loaded.
    /// </summary>
    void Start()
    {
        currentSpawnInterval = initialSpawnInterval;
        timeSinceLastSpawn = 0f;
    }

    /// <summary>
    /// Called every frame.
    /// </summary>
    void Update()
    {
        timeSinceLastSpawn += Time.deltaTime;

        if (timeSinceLastSpawn >= currentSpawnInterval)
        {
            SpawnObject();
            timeSinceLastSpawn = 0f;
            IncreaseDifficulty();
        }
    }

    /// <summary>
    /// Spawns either a fruit or a bomb at a random position within the spawn area.
    /// </summary>
    private void SpawnObject()
    {
        // Determine a random horizontal position within the spawn area
        float randomX = Random.Range(-spawnAreaSize.x / 2, spawnAreaSize.x / 2);
        Vector3 spawnPosition = new Vector3(randomX, spawnAreaCenter.y, spawnAreaCenter.z);

        // Decide whether to spawn a bomb or a fruit
        if (Random.value < bombSpawnChance)
        {
            // Spawn a bomb
            Instantiate(bombPrefab, spawnPosition, Quaternion.identity);
        }
        else
            // Spawn a random fruit from the list
            if (fruitPrefabs.Count > 0)
            {
                int fruitIndex = Random.Range(0, fruitPrefabs.Count);
                Instantiate(fruitPrefabs[fruitIndex], spawnPosition, Quaternion.identity);
            }
    }

    /// <summary>
    /// Gradually decreases the spawn interval to make the game more challenging.
    /// </summary>
    private void IncreaseDifficulty()
    {
        if (currentSpawnInterval > minSpawnInterval)
        {
            currentSpawnInterval -= difficultyIncreaseRate;
        }
    }

    /// <summary>
    /// Resets the spawner to its initial state.
    /// </summary>
    public void ResetSpawner()
    {
        currentSpawnInterval = initialSpawnInterval;
        timeSinceLastSpawn = 0f;
    }

    /// <summary>
    /// Draws a visual representation of the spawn area in the Unity Editor.
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(spawnAreaCenter, spawnAreaSize);
    }
}
