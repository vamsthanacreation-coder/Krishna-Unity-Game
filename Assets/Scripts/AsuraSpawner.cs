using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns Obstacles (Asuras) above the screen at random X positions within a responsive boundary.
/// Adjusts spawn rate, count and speed based on score thresholds to increase difficulty.
/// </summary>
public class AsuraSpawner : MonoBehaviour
{
    [Header("Spawn Overlap Prevention")]
    [Tooltip("Minimum horizontal spacing between spawned obstacles")]
    public float minHorizontalSpacing = 2.0f;
    [Tooltip("Minimum vertical spacing between spawned obstacles in a wave")]
    public float minVerticalSpacing = 1.5f;

    [Header("Responsive Area")]
    [Tooltip("If true, automatically set areaCenterX and areaHalfWidth from main camera viewport")] 
    public bool autoResponsiveArea = true;

    // Integration notes:
    // - Attach this script to an empty GameObject in your scene.
    // - Assign your Asura obstacle prefab to asuraPrefab.
    // - Set poolSize, spawn area, and difficulty parameters in inspector.
    // - Call AddScore(int) when player scores points to scale difficulty.
    // - Adjust areaHalfWidth and areaCenterX for responsive horizontal bounds.
    // - Increase poolSize if you see missing obstacles at high difficulty.

    [Header("Prefab & Pooling")]
    public ObstaclesAsura asuraPrefab;
    public int poolSize = 20;

    [Header("Spawn Area")]
    [Tooltip("Center X of the spawn area (local or world depending on spawner placement)")]
    public float areaCenterX = 0f;
    [Tooltip("Half-width of spawn area in units, responsive to screen width")]
    public float areaHalfWidth = 5f;
    [Tooltip("Y offset above the camera where asuras spawn")] public float spawnY = 6f;
    

    [Header("Spawn Rate")]
    [Tooltip("Base spawn delay in seconds")]
    public float baseSpawnDelay = 1.0f;
    [Tooltip("Minimum spawn delay allowed when difficulty increases")]
    public float minSpawnDelay = 0.2f;
    [Tooltip("How many asuras to spawn per wave (base)")]
    public int baseSpawnCount = 1;

    [Header("Difficulty")]
    [Tooltip("Score threshold to increase difficulty (e.g., every X points)")]
    public int difficultyThresholdPoints = 20;
    [Tooltip("Multiplier applied to speed when difficulty step happens")]
    public float speedMultiplierPerStep = 1.2f;
    [Tooltip("How much to reduce spawn delay each step (seconds)")]
    public float spawnDelayReductionPerStep = 0.1f;
    [Tooltip("Extra spawns added per step")]
    public int extraSpawnsPerStep = 1;

    [Header("Runtime State")]
    public int currentScore = 0; // update via AddScore or externally

    List<ObstaclesAsura> pool;
    float spawnTimer = 0f;
    float currentSpawnDelay;
    int currentStep = 0;
    int currentSpawnCount;

    void Awake()
    {
        // Responsive area setup
        if (autoResponsiveArea)
        {
            var cam = Camera.main;
            if (cam != null)
            {
                float camHeight = 2f * cam.orthographicSize;
                float camWidth = camHeight * cam.aspect;
                areaCenterX = cam.transform.position.x;
                areaHalfWidth = camWidth / 2f;
            }
        }

        // init pool
        pool = new List<ObstaclesAsura>(poolSize);
        for (int i = 0; i < poolSize; i++)
        {
            var go = Instantiate(asuraPrefab, transform);
            go.gameObject.SetActive(false);
            pool.Add(go);
        }

        currentSpawnDelay = baseSpawnDelay;
        currentSpawnCount = baseSpawnCount;
    }

    void Start()
    {
        spawnTimer = currentSpawnDelay;
    }

    void Update()
    {
        spawnTimer -= Time.deltaTime;
        if (spawnTimer <= 0f)
        {
            SpawnWave(currentSpawnCount);
            spawnTimer = currentSpawnDelay;
        }
    }

    void SpawnWave(int count)
    {
        List<Vector3> usedPositions = new List<Vector3>();
        
        for (int i = 0; i < count; i++)
        {
            var obj = GetFromPool();
            if (obj == null) return; // pool exhausted

            Vector3 spawnPos = Vector3.zero;
            bool validPos = false;
            int attempts = 0;
            
            // Try to find a non-overlapping position
            while (!validPos && attempts < 20)
            {
                spawnPos = GetRandomSpawnPosition();
                validPos = true;
                
                // Check against all previously used positions in this wave
                foreach (var pos in usedPositions)
                {
                    float horizontalDistance = Mathf.Abs(spawnPos.x - pos.x);
                    float verticalDistance = Mathf.Abs(spawnPos.y - pos.y);
                    
                    if (horizontalDistance < minHorizontalSpacing && verticalDistance < minVerticalSpacing)
                    {
                        validPos = false;
                        break;
                    }
                }
                attempts++;
            }

            // Store this position to avoid future overlaps in this wave
            usedPositions.Add(spawnPos);
            
            // Reset the obstacle's properties for reuse
            obj.transform.position = spawnPos;
            obj.ResetObstacle(); // Reset velocity and setup spawn properties
            // Apply difficulty scaling to speed
            obj.baseSpeed = 2f * Mathf.Pow(speedMultiplierPerStep, currentStep);
            obj.gameObject.SetActive(true);
        }
    }

    Vector3 GetRandomSpawnPosition()
    {
        // If the spawner is parented to camera or world space, areaCenterX is used as-is. Otherwise, use transform.position.x
        float centerX = transform.position.x + areaCenterX;
        float x = Random.Range(centerX - areaHalfWidth, centerX + areaHalfWidth);
        float y = spawnY + transform.position.y;
        return new Vector3(x, y, 0f);
    }

    ObstaclesAsura GetFromPool()
    {
        for (int i = 0; i < pool.Count; i++)
        {
            if (!pool[i].gameObject.activeInHierarchy)
            {
                return pool[i];
            }
        }
        return null;
    }

    /// <summary>
    /// Call this to add to the score. Spawner will increase difficulty when thresholds are crossed.
    /// </summary>
    public void AddScore(int points)
    {
        currentScore += points;
        int newStep = currentScore / difficultyThresholdPoints;
        if (newStep > currentStep)
        {
            // apply step increases for each newly crossed step
            for (int s = currentStep + 1; s <= newStep; s++)
            {
                ApplyDifficultyStep();
            }
            currentStep = newStep;
        }
    }

    void ApplyDifficultyStep()
    {
        // increase spawn count
        currentSpawnCount += extraSpawnsPerStep;

        // reduce spawn delay
        currentSpawnDelay = Mathf.Max(minSpawnDelay, currentSpawnDelay - spawnDelayReductionPerStep);

        // increase speed of remaining pooled asuras slightly
        foreach (var a in pool)
        {
            if (a != null)
            {
                a.baseSpeed *= speedMultiplierPerStep;
            }
        }
    }

    /// <summary>
    /// Optional: call to directly set difficulty parameters at runtime.
    /// </summary>
    public void SetDifficultyParams(float speedMultiplier, float spawnDelayReduction, int extraSpawns)
    {
        speedMultiplierPerStep = speedMultiplier;
        spawnDelayReductionPerStep = spawnDelayReduction;
        extraSpawnsPerStep = extraSpawns;
    }
}
