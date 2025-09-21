# Obstacles (Asuras) Integration Guide

This guide explains how to set up and use the `ObstaclesAsura` and `AsuraSpawner` scripts in your Unity project.

## 1. Create the Asura Obstacle Prefab
- Create an empty GameObject in your scene.
- Add a `SpriteRenderer` and assign your Asura sprite(s).
- Add a `Collider2D` (e.g., CircleCollider2D) and set it as Trigger.
- Add the `ObstaclesAsura` script.
- In the inspector:
  - Assign one or more sprites to `possibleSprites`.
  - Set `baseSpeed` (e.g., 2-5), `gravity` (e.g., 0.5 for smooth acceleration), and `horizontalDrift` (optional).
  - Set `damage` (usually 1).
  - Set `destroyBelowY` to a value below the bottom of your camera view (e.g., -10).
  - Enable `randomizeSpriteOnSpawn` if you want random sprite selection.
  - Enable `hitEndsGame` if hitting an Asura should end the game instantly.
- Save this GameObject as a prefab (e.g., `AsuraObstacle.prefab`).

## 2. Set Up the Spawner
- Create an empty GameObject in your scene (e.g., `AsuraSpawner`).
- Add the `AsuraSpawner` script.
- In the inspector:
  - Assign your Asura prefab to `asuraPrefab`.
  - Set `poolSize` (e.g., 20-50 for most games).
  - Set `areaCenterX` and `areaHalfWidth` to match your playable area.
  - Set `spawnY` to a value above the top of your camera view (e.g., 6).
  - Set `baseSpawnDelay`, `minSpawnDelay`, and `baseSpawnCount` for spawn frequency.
  - Set difficulty parameters (`difficultyThresholdPoints`, `speedMultiplierPerStep`, `spawnDelayReductionPerStep`, `extraSpawnsPerStep`).

## 3. Player Health Integration
- Ensure your player GameObject has the tag `Player`.
- Add the `PlayerHealth` script to your player.
- The obstacle script will call `TakeDamage(int)` on collision.

## 4. Score & Difficulty Integration
- When the player scores points, call `AddScore(int)` on the spawner:
  - Example: `FindObjectOfType<AsuraSpawner>().AddScore(1);`
- The spawner will automatically increase difficulty when the score crosses thresholds.

## 5. Game Over Integration
- The obstacle script will call `GameManager.GameOver()` if `hitEndsGame` is enabled.
- Ensure you have a `GameManager` script with a `GameOver(string reason)` method.

## 6. Inspector Tuning
- All key parameters are exposed in the inspector for easy tuning.
- Use UnityEvents (`onSpawned`, `onHitPlayer`) for custom effects (e.g., sound, VFX).

## 7. Pooling
- The spawner uses pooling for performance. Increase `poolSize` if you see missing obstacles.

## 8. Responsive Bounds
- Adjust `areaHalfWidth` and `areaCenterX` to match your screen/camera size.
- For camera-relative spawning, parent the spawner to the camera or update its position in code.

## 9. Customization
- You can change sprites, speed, difficulty, and spawn rate at runtime via inspector or script.

---
For further customization or integration, see inline comments in the scripts.
