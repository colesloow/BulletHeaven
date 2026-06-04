using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private bool destroyWithOwner = true;
    [SerializeField] private Vector3 spawnOffset;

    private GameObject instance;
    private bool started;

    private void Start()
    {
        started = true;
        if (prefab != null && instance == null)
            instance = Instantiate(prefab, transform.position + spawnOffset, Quaternion.identity);
    }

    private void OnEnable()
    {
        // Handles pool reuse: started is true, instance was cleared by the previous OnDisable.
        if (started && instance == null && prefab != null)
            instance = Instantiate(prefab, transform.position + spawnOffset, Quaternion.identity);
    }

    private void OnDisable()
    {
        if (destroyWithOwner && instance != null)
        {
            Destroy(instance);
            instance = null;
        }
    }

    private void OnDestroy()
    {
        if (instance != null)
        {
            Destroy(instance);
            instance = null;
        }
    }
}
