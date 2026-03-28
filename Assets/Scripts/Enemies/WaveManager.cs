using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Manages enemy spawning over time. Two parallel systems run simultaneously:
//
//   1. Base continuous spawn: one enemy every _baseSpawnInterval seconds, respects MaxEnemies cap.
//   2. Timed waves: each WaveConfig triggers at a set time, spawns enemies for a duration,
//      bypasses the global MaxEnemies cap but respects the wave's own MaxEnemies cap.
//
// Enemy stats (health, damage) scale on player level-up via OnEnemiesLevelUp.
public class WaveManager : MonoBehaviour
{
    public static WaveManager Instance { get; private set; }

    [Header("Test")]
    [SerializeField] private bool _enemyDamageEnabled = true;
    // Set to false in the Inspector to disable enemy contact damage during testing.
    public bool EnemyDamageEnabled => _enemyDamageEnabled;

    [Header("Base Spawn Rate")]
    // One enemy spawns every _baseSpawnInterval seconds, independently of waves.
    [SerializeField] private float _baseSpawnInterval = 2f;

    [Header("Max Enemies Progression")]
    // Global cap for the base continuous spawn. Grows linearly over time.
    // Formula: initialMaxEnemies + (minutesElapsed * increasePerMinute)
    [SerializeField] private int _initialMaxEnemies = 10;
    [SerializeField] private float _maxEnemiesIncreasePerMinute = 5f;
    public int MaxEnemies => Mathf.RoundToInt(_initialMaxEnemies + (_elapsedTime / 60f) * _maxEnemiesIncreasePerMinute);

    [Header("Waves")]
    // List of WaveConfig assets sorted by TriggerTime at Start.
    // Add WaveConfig assets here via the Generate Waves editor tool or manually.
    [SerializeField] private List<WaveConfig> _waves;

    [Header("Enemy Scaling")]
    // Multipliers applied to all active enemies each time the player levels up.
    [SerializeField] private float _enemyHealthMultiplier = 1.5f;
    [SerializeField] private float _enemyDamageMultiplier = 1.2f;
    public event Action<float, float> OnEnemiesLevelUp;

    private EnemySpawner _enemySpawner;
    private float _elapsedTime;
    private float _spawnTimer;
    private int _nextWaveIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private void Start()
    {
        _enemySpawner = FindFirstObjectByType<EnemySpawner>();
        _waves.Sort((a, b) => a.TriggerTime.CompareTo(b.TriggerTime));

        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelUp += OnPlayerLevelUp;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnLevelUp -= OnPlayerLevelUp;
    }

    private void Update()
    {
        _elapsedTime += Time.deltaTime;

        // Base continuous spawn: one enemy per interval, capped by MaxEnemies.
        _spawnTimer += Time.deltaTime;
        if (_spawnTimer >= _baseSpawnInterval)
        {
            _enemySpawner.TrySpawnOne();
            _spawnTimer = 0f;
        }

        // Check if the next wave should trigger.
        if (_nextWaveIndex < _waves.Count && _elapsedTime >= _waves[_nextWaveIndex].TriggerTime)
        {
            StartCoroutine(SpawnWave(_waves[_nextWaveIndex]));
            _nextWaveIndex++;
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
                _enemySpawner.ForceSpawnOne(wave.PickRandomPrefab());

            yield return interval;
            elapsed += wave.SpawnInterval;
        }
    }

    // When the player levels up, all active enemies get stronger.
    private void OnPlayerLevelUp(int level)
    {
        OnEnemiesLevelUp?.Invoke(_enemyHealthMultiplier, _enemyDamageMultiplier);
    }
}
