using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject _enemyPrefab;
    [SerializeField] private float _minSpawnDistance = 10f;
    [SerializeField] private float _maxSpawnDistance = 30f;
    [SerializeField] private float _navMeshSampleRadius = 2f;

    private DungeonGenerator _dungeonGenerator;
    private Transform _player;
    private readonly List<GameObject> _activeEnemies = new();

    private void Start()
    {
        _dungeonGenerator = FindFirstObjectByType<DungeonGenerator>();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
    }

    // Called by WaveManager at base spawn rate, respects MaxEnemies cap
    public void TrySpawnOne(GameObject prefab = null)
    {
        _activeEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
        if (_player == null || _dungeonGenerator == null) return;
        if (WaveManager.Instance == null) return;
        if (_activeEnemies.Count >= WaveManager.Instance.MaxEnemies) return;

        SpawnAtRandomRoom(prefab != null ? prefab : _enemyPrefab);
    }

    // Called by WaveManager for wave bursts, ignores MaxEnemies cap
    public void ForceSpawnOne(GameObject prefab = null)
    {
        _activeEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
        if (_player == null || _dungeonGenerator == null) return;

        SpawnAtRandomRoom(prefab != null ? prefab : _enemyPrefab);
    }

    private void SpawnAtRandomRoom(GameObject prefab)
    {
        Room room = GetEligibleRoom();
        if (room == null) return;

        Vector3 spawnPoint = GetSpawnPoint(room);
        if (spawnPoint == Vector3.zero) return;

        if (PoolManager.Instance == null) return;

        GameObject enemy = PoolManager.Instance.Get(prefab);
        enemy.transform.position = spawnPoint;
        _activeEnemies.Add(enemy);
    }

    // Returns a random room within spawn distance range from the player
    private Room GetEligibleRoom()
    {
        var eligible = new List<Room>();

        foreach (Room room in _dungeonGenerator.PlacedRooms)
        {
            float dist = Vector3.Distance(_player.position, room.transform.position);
            if (dist < _minSpawnDistance || dist > _maxSpawnDistance) continue;
            eligible.Add(room);
        }

        if (eligible.Count == 0) return null;
        return eligible[Random.Range(0, eligible.Count)];
    }

    // Returns a random NavMesh-valid point on the room's floor
    private Vector3 GetSpawnPoint(Room room)
    {
        Bounds floor = room.GetFloorBounds();

        for (int attempt = 0; attempt < 10; attempt++)
        {
            Vector3 candidate = new(
                Random.Range(floor.min.x, floor.max.x),
                0f,
                Random.Range(floor.min.z, floor.max.z)
            );

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, _navMeshSampleRadius, NavMesh.AllAreas))
                return hit.position;
        }

        return Vector3.zero;
    }
}
