using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class PoolManager : MonoBehaviour
{
    public static PoolManager Instance { get; private set; }

    [SerializeField] private int _defaultInitialSize = 10;
    [SerializeField] private int _defaultMaxSize = 50;

    private readonly Dictionary<GameObject, ObjectPool<GameObject>> _pools = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public GameObject Get(GameObject prefab)
    {
        if (!_pools.ContainsKey(prefab))
            RegisterPool(prefab);

        return _pools[prefab].Get();
    }

    public void Release(GameObject prefab, GameObject instance)
    {
        if (_pools.TryGetValue(prefab, out var pool))
            pool.Release(instance);
        else
            Destroy(instance);
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (!_pools.ContainsKey(prefab))
            RegisterPool(prefab);

        var instances = new GameObject[count];
        for (int i = 0; i < count; i++)
            instances[i] = _pools[prefab].Get();
        for (int i = 0; i < count; i++)
            _pools[prefab].Release(instances[i]);
    }

    private void RegisterPool(GameObject prefab)
    {
        _pools[prefab] = new ObjectPool<GameObject>(
            createFunc: () =>
            {
                GameObject go = Instantiate(prefab, transform);
                go.GetComponent<PooledObject>()?.Setup(prefab, this);
                // If the prefab has no PooledObject, add one automatically
                if (go.GetComponent<PooledObject>() == null)
                {
                    go.AddComponent<PooledObject>().Setup(prefab, this);
                }
                return go;
            },
            actionOnGet:     go => go.SetActive(true),
            actionOnRelease: go => go.SetActive(false),
            actionOnDestroy: go => Destroy(go),
            defaultCapacity: _defaultInitialSize,
            maxSize:         _defaultMaxSize
        );
    }
}
