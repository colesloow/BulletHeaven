using UnityEngine;

public class Scoring : MonoBehaviour
{
    [SerializeField]
    private int _objectScoring;
    [SerializeField]
    private GameObject _xpCollectablePrefab;
    [SerializeField]
    private GameObject _healthCollectablePrefab;
    [SerializeField]
    [Range(0f, 100f)]
    private float _healthDropChance = 20f; 

    public void GrantRewards()
    {
        GameManager.Instance.TotalScore += _objectScoring;

        if (PoolManager.Instance == null) return;

        GameObject prefab = (Random.value * 100f <= _healthDropChance)
            ? _healthCollectablePrefab
            : _xpCollectablePrefab;

        if (prefab != null)
            PoolManager.Instance.Get(prefab).transform.position = transform.position;
    }
}
