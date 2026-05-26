using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Manages enemy spawning over time. Two parallel systems run simultaneously:
//
//   1. Base continuous spawn: one enemy every baseSpawnInterval seconds, respects MaxEnemies cap.
//   2. Timed waves: each WaveConfig triggers at a set time, spawns enemies for a duration,
//      bypasses the global MaxEnemies cap but respects the wave's own MaxEnemies cap.
//
// Enemy stats (health, damage) scale on player level-up via OnEnemiesLevelUp.
[DefaultExecutionOrder(-10)]
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Test")]
    [SerializeField] private bool enemyDamageEnabled = true;
    public bool EnemyDamageEnabled => enemyDamageEnabled;

    [Header("Base Spawn Rate")]
    // One enemy spawns every baseSpawnInterval seconds, independently of waves.
    [SerializeField] private float baseSpawnInterval = 2f;

    [Header("Max Enemies Progression")]
    // Global cap for the base continuous spawn. Grows linearly over time.
    // Formula: initialMaxEnemies + (minutesElapsed * increasePerMinute)
    [SerializeField] private int initialMaxEnemies = 10;
    [SerializeField] private float maxEnemiesIncreasePerMinute = 5f;
    public int MaxEnemies => Mathf.RoundToInt(initialMaxEnemies + (elapsedTime / 60f) * maxEnemiesIncreasePerMinute);

    [Header("Waves")]
    [SerializeField] private GameObject defaultEnemyPrefab;
    [SerializeField] private List<WaveConfig> waves;

    [Header("Enemy Scaling")]
    // Linear scaling per player level. Formula: base * (1 + (level - 1) * scalingPerLevel)
    // Example at 0.15: level 5 = x1.6, level 10 = x2.35
    [SerializeField] private float enemyHealthScalingPerLevel = 0.15f;
    [SerializeField] private float enemyDamageScalingPerLevel = 0.1f;
    // Passes (healthScaling, damageScaling, playerLevel) to subscribers.
    public event Action<float, float, int> OnEnemiesLevelUp;

    private EnemySpawner enemySpawner;
    private float elapsedTime;
    private float spawnTimer;
    private int nextWaveIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        enemySpawner = FindFirstObjectByType<EnemySpawner>();
        waves.Sort((a, b) => a.TriggerTime.CompareTo(b.TriggerTime));

        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelUp += OnPlayerLevelUp;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelUp -= OnPlayerLevelUp;
    }

    public void CleanupEnemies()
    {
        StopAllCoroutines();
        enemySpawner?.DespawnAll();
        elapsedTime = 0f;
        spawnTimer = 0f;
        nextWaveIndex = 0;
    }

    private void Update()
    {
        if (GameManager.Instance == null || GameManager.Instance.State != GameState.Playing) return;

        elapsedTime += Time.deltaTime;

        // Base continuous spawn: one enemy per interval, capped by MaxEnemies.
        spawnTimer += Time.deltaTime;
        if (spawnTimer >= baseSpawnInterval)
        {
            enemySpawner.TrySpawnOne(defaultEnemyPrefab);
            spawnTimer = 0f;
        }

        // Check if the next wave should trigger.
        if (nextWaveIndex < waves.Count && elapsedTime >= waves[nextWaveIndex].TriggerTime)
        {
            StartCoroutine(SpawnWave(waves[nextWaveIndex]));
            nextWaveIndex++;
        }
    }

    // Spawns enemies for wave.Duration at wave.SpawnInterval.
    // Skips a tick (but does not stop the wave) if the per-wave MaxEnemies cap is reached.
    private IEnumerator SpawnWave(WaveConfig wave)
    {
        float elapsed = 0f;
        WaitForSeconds interval = new(wave.SpawnInterval);
        while (elapsed < wave.Duration)
        {
            bool atCap = wave.MaxEnemies > 0 && Health.ActiveEnemies.Count >= wave.MaxEnemies;
            if (!atCap)
                enemySpawner.ForceSpawnOne(wave.PickRandomPrefab());

            yield return interval;
            elapsed += wave.SpawnInterval;
        }
    }

    // When the player levels up, all active enemies get stronger.
    private void OnPlayerLevelUp(int level)
    {
        OnEnemiesLevelUp?.Invoke(enemyHealthScalingPerLevel, enemyDamageScalingPerLevel, level);
    }
}
