using UnityEngine;

// Added automatically by PoolManager to every pooled instance.
// Allows any object to release itself back to the pool without
// needing a reference to its prefab or the PoolManager.
public class PooledObject : MonoBehaviour
{
    private GameObject _prefab;
    private PoolManager _manager;

    public void Setup(GameObject prefab, PoolManager manager)
    {
        _prefab = prefab;
        _manager = manager;
    }

    public void Release()
    {
        _manager.Release(_prefab, gameObject);
    }
}
