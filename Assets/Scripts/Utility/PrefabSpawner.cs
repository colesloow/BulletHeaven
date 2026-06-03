using System.Collections;
using UnityEngine;

public class PrefabSpawner : MonoBehaviour
{
    [SerializeField] private GameObject prefab;
    [SerializeField] private bool destroyWithOwner = true;
    private GameObject instance;

    private void OnEnable()
    {
        StartCoroutine(SpawnNextFrame());
    }

    private IEnumerator SpawnNextFrame()
    {
        yield return null;
        if (prefab != null && instance == null)
            instance = Instantiate(prefab, transform.position, Quaternion.identity);
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
            Destroy(instance);
    }
}
