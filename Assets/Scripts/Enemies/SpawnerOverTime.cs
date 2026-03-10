using System.Collections;
using System.Collections.Generic; // Pour la liste des spawners
using UnityEngine;

public class SpawnerOverTime : MonoBehaviour
{
    [SerializeField]
    private Transform _player;
    [Header("Spawn Settings")]
    [SerializeField]
    private GameObject _spawner;
    [SerializeField]
    private float _startSpawningDelay = 2f;
    [SerializeField]
    private float _spawnFrequency = 10f;
    [SerializeField]
    private float _spawnRadius = 10f;
    [SerializeField]
    private float _minSpawnRadius = 2f;
    [Header("Destruction Settings")]
    [SerializeField]
    private float _destroyDistance = 15f;

    private Coroutine _spawnCoroutine;
    private List<GameObject> _activeSpawners = new List<GameObject>();

    private void Start()
    {
        _spawnCoroutine = StartCoroutine(CreateSpawnerCoroutine());
    }

    private void Update()
    {
        CheckAndDestroySpawners();
    }

    private void StopSpawning()
    {
        if (_spawnCoroutine == null) return;
        StopCoroutine(_spawnCoroutine);
    }

    private void CreateSpawner()
    {
        Vector3 randomPosition = GetRandomPositionNearPlayer();
        Quaternion spawnRotation = Quaternion.Euler(0, 45, 0); // y rotation
        GameObject spawnerInstance = Instantiate(_spawner, randomPosition, spawnRotation);
        _activeSpawners.Add(spawnerInstance);
    }

    private IEnumerator CreateSpawnerCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(_startSpawningDelay);
            CreateSpawner();
            yield return new WaitForSeconds(_spawnFrequency);
        }
    }

    private Vector3 GetRandomPositionNearPlayer()
    {
        float angle = Random.Range(0f, Mathf.PI * 2); // Angle aléatoire
        float distance = Random.Range(_minSpawnRadius, _spawnRadius);

        // calculate x and z offset
        float xOffset = Mathf.Cos(angle) * distance;
        float zOffset = Mathf.Sin(angle) * distance;

        Vector3 spawnPosition = new Vector3(
            _player.position.x + xOffset,
            _player.position.y,
            _player.position.z + zOffset
        );

        return spawnPosition;
    }

    private void CheckAndDestroySpawners()
    {
        for (int i = _activeSpawners.Count - 1; i >= 0; i--)
        {
            GameObject spawner = _activeSpawners[i];
            if (spawner == null) continue;

            float distanceToPlayer = Vector3.Distance(spawner.transform.position, _player.position);

            // destroy spawner if too far from player
            if (distanceToPlayer > _destroyDistance)
            {
                Destroy(spawner);
                _activeSpawners.RemoveAt(i);
            }
        }
    }
}
