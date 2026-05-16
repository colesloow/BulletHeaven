using UnityEngine;

// Defines what an enemy drops and grants on death.
// Add this component to enemy prefabs and configure drops in the Inspector.
// Called by Health.EnemyDeathSequence when the enemy dies.
public class EnemyRewards : MonoBehaviour
{
    [SerializeField] private int _scoreValue = 10;
    [SerializeField] private float _dropHeight = 0.3f;

    [SerializeField] private DropEntry[] _drops;

    public void GrantRewards(Vector3 position)
    {
        if (GameManager.Instance != null)
            GameManager.Instance.TotalScore += _scoreValue;

        if (PoolManager.Instance == null || _drops == null || _drops.Length == 0) return;

        float total = 0f;
        foreach (var drop in _drops)
            if (drop.Prefab != null && drop.Weight > 0f)
                total += drop.Weight;

        if (total <= 0f) return;

        float roll = Random.value * total;
        float cumulative = 0f;
        Vector3 dropPosition = new Vector3(position.x, _dropHeight, position.z);
        foreach (var drop in _drops)
        {
            if (drop.Prefab == null || drop.Weight <= 0f) continue;
            cumulative += drop.Weight;
            if (roll <= cumulative)
            {
                PoolManager.Instance.Get(drop.Prefab).transform.position = dropPosition;
                return;
            }
        }
    }
}

// One collectable type this enemy can drop.
// Weight is relative: XP=60, Health=30, Screw=10 means 60/40/10% chance respectively.
[System.Serializable]
public struct DropEntry
{
    public GameObject Prefab;
    public float Weight;
}
