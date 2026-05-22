using UnityEngine;

// One entry in a wave: which enemy prefab and how likely it is to spawn.
// Weight is relative: Weight=3 and Weight=1 means 75% / 25%.
[System.Serializable]
public struct EnemySpawnEntry
{
    public GameObject Prefab;

    [Tooltip("Relative spawn weight. Higher = spawns more often.")]
    public float Weight;
}

// Defines a single timed wave event.
// Waves run in parallel with the base continuous spawn (WaveManager._baseSpawnInterval).
// Multiple waves can overlap if their TriggerTimes are close.
[CreateAssetMenu(fileName = "WaveConfig", menuName = "BulletHeaven/Wave Config")]
public class WaveConfig : ScriptableObject
{
    [Tooltip("Seconds from game start when this wave triggers")]
    public float TriggerTime;

    [Tooltip("How long the wave lasts in seconds")]
    public float Duration = 20f;

    [Tooltip("Time between enemy spawns during the wave")]
    public float SpawnInterval = 1f;

    [Tooltip("Maximum enemies alive simultaneously during this wave. 0 = no cap.")]
    public int MaxEnemies;

    [Tooltip("Enemy types and their relative spawn weights")]
    public EnemySpawnEntry[] EnemyTypes;

    // Returns null if EnemyTypes is empty (WaveManager falls back to default prefab).
    public GameObject PickRandomPrefab()
    {
        return WeightedRandom.Pick(EnemyTypes, e => e.Weight).Prefab;
    }
}
