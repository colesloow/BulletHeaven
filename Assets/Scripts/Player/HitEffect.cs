using UnityEngine;

public class HitEffect : MonoBehaviour
{
    [SerializeField] private GameObject vfxPrefab;

    private Health _health;

    private void Awake()
    {
        _health = GetComponent<Health>();
    }

    private void OnEnable()
    {
        if (_health != null) _health.OnDamaged += SpawnVfx;
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnDamaged -= SpawnVfx;
    }

    private void SpawnVfx(float _)
    {
        if (vfxPrefab == null || PoolManager.Instance == null) return;
        PoolManager.Instance.Get(vfxPrefab).transform.position = transform.position;
    }
}
