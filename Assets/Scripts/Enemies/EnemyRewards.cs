using UnityEngine;

// Defines what an enemy drops and grants on death.
// Add this component to enemy prefabs and configure drops in the Inspector.
// Called by Health.EnemyDeathSequence when the enemy dies.
public class EnemyRewards : MonoBehaviour
{
    [SerializeField] private int scoreValue = 10;
    [SerializeField] private float dropHeight = 0.3f;

    [SerializeField] private DropEntry[] drops;

    public void GrantRewards(Vector3 position)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.TotalScore += scoreValue;
            GameManager.Instance.RegisterKill();
        }

        if (PoolManager.Instance == null || drops == null || drops.Length == 0) return;

        var drop = WeightedRandom.Pick(drops, d => d.Prefab != null ? d.Weight : 0f);
        if (drop.Prefab == null) return;

        Vector3 dropPosition = new Vector3(position.x, dropHeight, position.z);
        PoolManager.Instance.Get(drop.Prefab).transform.position = dropPosition;
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
