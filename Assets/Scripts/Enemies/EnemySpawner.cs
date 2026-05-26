using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Handles WHERE and HOW enemies spawn.
// Spawning rhythm (WHEN) is controlled by WaveManager, which calls TrySpawnOne and ForceSpawnOne.
//
// Spawn logic:
//   - Picks a random room within [minSpawnDistance, maxSpawnDistance] from the player,
//     that is NOT currently visible to the camera.
//   - Finds a NavMesh-valid point on that room's floor.
//   - Retrieves an enemy instance from the pool.
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private float minSpawnDistance = 10f;
    [SerializeField] private float maxSpawnDistance = 30f;
    [SerializeField] private float navMeshSampleRadius = 2f;
    [SerializeField] private float despawnDistance = 40f;
    // If an enemy hasn't been within engagementRange for longer than maxChaseTime, it despawns.
    [SerializeField] private float engagementRange = 8f;
    [SerializeField] private float maxChaseTime = 4f;

    private DungeonGenerator dungeonGenerator;
    private Transform player;
    private Camera mainCamera;

    private readonly List<GameObject> activeEnemies = new();
    private readonly Dictionary<GameObject, float> lastCloseTime = new();

    private void Start()
    {
        dungeonGenerator = FindFirstObjectByType<DungeonGenerator>();
        mainCamera = Camera.main;

        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged += OnGameStateChanged;
    }

    private void OnDestroy()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.OnGameStateChanged -= OnGameStateChanged;
    }

    private void OnGameStateChanged(GameState state)
    {
        if (state != GameState.Playing) return;

        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
            player = playerObj.transform;
    }

    private void Update()
    {
        if (player == null) return;

        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            GameObject enemy = activeEnemies[i];
            if (enemy == null || !enemy.activeInHierarchy)
            {
                lastCloseTime.Remove(enemy);
                activeEnemies.RemoveAt(i);
                continue;
            }

            float dist = Vector3.Distance(player.position, enemy.transform.position);

            if (dist <= engagementRange)
            {
                lastCloseTime[enemy] = Time.time;
            }
            else if (dist > despawnDistance || Time.time - lastCloseTime.GetValueOrDefault(enemy, Time.time) > maxChaseTime)
            {
                if (!Relocate(enemy))
                {
                    Despawn(enemy);
                    activeEnemies.RemoveAt(i);
                }
                else
                {
                    lastCloseTime.Remove(enemy);
                }
            }
        }
    }

    public void DespawnAll()
    {
        for (int i = activeEnemies.Count - 1; i >= 0; i--)
        {
            if (activeEnemies[i] != null)
                Despawn(activeEnemies[i]);
        }
        activeEnemies.Clear();
        lastCloseTime.Clear();
    }

    private void Despawn(GameObject enemy)
    {
        lastCloseTime.Remove(enemy);
        if (enemy.TryGetComponent(out PooledObject pooled)) pooled.Release();
        else enemy.SetActive(false);
    }

    // Moves an existing enemy to a new off-screen spawn point near the player.
    // Returns false if no valid room is found (caller should despawn instead).
    private bool Relocate(GameObject enemy)
    {
        Room room = GetEligibleRoom();
        if (room == null) return false;

        Vector3 spawnPoint = GetSpawnPoint(room);
        if (spawnPoint == Vector3.zero) return false;

        if (enemy.TryGetComponent(out NavMeshAgent agent))
            agent.Warp(spawnPoint);
        else
            enemy.transform.position = spawnPoint;

        return true;
    }

    public void TrySpawnOne(GameObject prefab)
    {
        activeEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
        if (player == null || dungeonGenerator == null) return;
        if (WaveManager.Instance == null) return;
        if (activeEnemies.Count >= WaveManager.Instance.MaxEnemies) return;

        SpawnAtRandomRoom(prefab);
    }

    public void ForceSpawnOne(GameObject prefab)
    {
        activeEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
        if (player == null || dungeonGenerator == null) return;

        SpawnAtRandomRoom(prefab);
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
        activeEnemies.Add(enemy);
    }

    // Returns a random room within spawn distance range that is not visible to the camera.
    // Falls back to any in-range room if all eligible rooms are visible.
    private Room GetEligibleRoom()
    {
        Plane[] frustumPlanes = GeometryUtility.CalculateFrustumPlanes(mainCamera);
        var offScreen = new List<Room>();
        var fallback = new List<Room>();

        foreach (DungeonPiece piece in dungeonGenerator.PlacedPieces)
        {
            if (piece is not Room room) continue;
            float dist = Vector3.Distance(player.position, room.transform.position);
            if (dist < minSpawnDistance || dist > maxSpawnDistance) continue;

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

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                return hit.position;
        }

        return Vector3.zero;
    }
}
