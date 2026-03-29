using UnityEngine;

// Defines what an enemy drops and grants on death.
// Add this component to enemy prefabs and configure drops in the Inspector.
// Called by Health.EnemyDeathSequence when the enemy dies.
public class EnemyRewards : MonoBehaviour
{
    [SerializeField] private int _scoreValue = 10;

    [SerializeField] private DropEntry[] _drops;

    public void GrantRewards(Vector3 position)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.TotalScore += _scoreValue;

        if (PoolManager.Instance == null) return;

        foreach (var drop in _drops)
        {
            if (drop.Prefab == null || drop.Chance <= 0f) continue;
            if (Random.value <= drop.Chance)
                PoolManager.Instance.Get(drop.Prefab).transform.position = position;
        }
    }
}

// One collectable type this enemy can drop, with a probability.
// Chance = 0: never drops. Chance = 1: always drops.
[System.Serializable]
public struct DropEntry
{
    public GameObject Prefab;
    [Range(0f, 1f)] public float Chance;
}
