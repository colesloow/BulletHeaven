using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Handles WHERE and HOW enemies spawn.
// Spawning rhythm (WHEN) is controlled by WaveManager, which calls TrySpawnOne and ForceSpawnOne.
//
// Spawn logic:
//   - Picks a random room within [_minSpawnDistance, _maxSpawnDistance] from the player,
//     that is NOT currently visible to the camera.
//   - Finds a NavMesh-valid point on that room's floor.
//   - Retrieves an enemy instance from the pool.
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private GameObject _enemyPrefab;

    [SerializeField] private float _minSpawnDistance = 10f;
    [SerializeField] private float _maxSpawnDistance = 30f;
    [SerializeField] private float _navMeshSampleRadius = 2f;

    private DungeonGenerator _dungeonGenerator;
    private Transform _player;
    private Camera _mainCamera;

    private readonly List<GameObject> _activeEnemies = new();

    private void Start()
    {
        _dungeonGenerator = FindFirstObjectByType<DungeonGenerator>();
        _mainCamera = Camera.main;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
    }

    public void TrySpawnOne(GameObject prefab = null)
    {
        _activeEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
        if (_player == null || _dungeonGenerator == null) return;
        if (WaveManager.Instance == null) return;
        if (_activeEnemies.Count >= WaveManager.Instance.MaxEnemies) return;

        SpawnAtRandomRoom(prefab != null ? prefab : _enemyPrefab);
    }

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

    // Returns a random room within spawn distance range that is not visible to the camera.
    // Falls back to any in-range room if all eligible rooms are off-screen check fails.
    private Room GetEligibleRoom()
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);
        var offScreen = new List<Room>();
        var fallback = new List<Room>();

        foreach (DungeonPiece piece in _dungeonGenerator.PlacedPieces)
        {
            if (piece is not Room room) continue;
            float dist = Vector3.Distance(_player.position, room.transform.position);
            if (dist < _minSpawnDistance || dist > _maxSpawnDistance) continue;

            if (GeometryUtility.TestPlanesAABB(frustumPlanes, room.GetFloorBounds()))
                fallback.Add(room);
            else
                offScreen.Add(room);
        }

        if (offScreen.Count > 0) return offScreen[Random.Range(0, offScreen.Count)];
        if (fallback.Count > 0) return fallback[Random.Range(0, fallback.Count)];
        return null;
    }

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
