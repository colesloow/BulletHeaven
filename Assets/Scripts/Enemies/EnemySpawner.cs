using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Handles WHERE and HOW enemies spawn.
// Spawning rhythm (WHEN) is controlled by WaveManager, which calls TrySpawnOne and ForceSpawnOne.
//
// Spawn logic:
//   - Picks a random room within [_minSpawnDistance, _maxSpawnDistance] from the player.
//   - Finds a NavMesh-valid point on that room's floor.
//   - Retrieves an enemy instance from the pool.
public class EnemySpawner : MonoBehaviour
{
    // Default prefab used when no prefab is specified by the caller.
    [SerializeField] private GameObject _enemyPrefab;

    // Spawn distance range relative to the player.
    // Min avoids spawning on top of the player; max keeps enemies off-screen.
    [SerializeField] private float _minSpawnDistance = 10f;
    [SerializeField] private float _maxSpawnDistance = 30f;

    // Search radius used by NavMesh.SamplePosition to find a valid point on the floor.
    [SerializeField] private float _navMeshSampleRadius = 2f;

    private DungeonGenerator _dungeonGenerator;
    private Transform _player;

    // Tracks active enemies to enforce the MaxEnemies cap in TrySpawnOne.
    private readonly List<GameObject> _activeEnemies = new();

    private void Start()
    {
        _dungeonGenerator = FindFirstObjectByType<DungeonGenerator>();

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            _player = playerObj.transform;
    }

    // Called by WaveManager at base spawn rate. Respects the global MaxEnemies cap.
    public void TrySpawnOne(GameObject prefab = null)
    {
        _activeEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
        if (_player == null || _dungeonGenerator == null) return;
        if (WaveManager.Instance == null) return;
        if (_activeEnemies.Count >= WaveManager.Instance.MaxEnemies) return;

        SpawnAtRandomRoom(prefab != null ? prefab : _enemyPrefab);
    }

    // Called by WaveManager during timed waves. Ignores the global MaxEnemies cap.
    // The per-wave cap (WaveConfig.MaxEnemies) is enforced by WaveManager before calling this.
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

    // Returns a random room whose center is within the spawn distance range from the player.
    private Room GetEligibleRoom()
    {
        var eligible = new List<Room>();

        foreach (DungeonPiece piece in _dungeonGenerator.PlacedPieces)
        {
            if (piece is not Room room) continue;
            float dist = Vector3.Distance(_player.position, room.transform.position);
            if (dist < _minSpawnDistance || dist > _maxSpawnDistance) continue;
            eligible.Add(room);
        }

        if (eligible.Count == 0) return null;
        return eligible[Random.Range(0, eligible.Count)];
    }

    // Samples up to 10 random points inside the room's floor bounds until a NavMesh-valid one is found.
    // Returns Vector3.zero if no valid point is found.
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
