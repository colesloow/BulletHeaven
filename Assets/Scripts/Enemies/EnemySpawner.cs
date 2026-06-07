using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

// Handles WHERE and HOW enemies spawn.
// Spawning rhythm (WHEN) is controlled by WaveManager, which calls TrySpawnOne and ForceSpawnOne.
public class EnemySpawner : MonoBehaviour
{
    [SerializeField] private float spawnRadius = 6f;
    [SerializeField] private float navMeshSampleRadius = 2f;
    [SerializeField] private float despawnDistance = 40f;
    // If an enemy hasn't been within engagementRange for longer than maxChaseTime, it despawns.
    [SerializeField] private float engagementRange = 8f;
    [SerializeField] private float maxChaseTime = 4f;

    private Transform player;

    private readonly List<GameObject> activeEnemies = new();
    private readonly Dictionary<GameObject, float> lastCloseTime = new();

    private void Start()
    {
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

    private bool Relocate(GameObject enemy)
    {
        Vector3 spawnPoint = GetSpawnPoint();
        if (spawnPoint == Vector3.zero) return false;

        if (enemy.TryGetComponent(out NavMeshAgent agent))
            agent.Warp(spawnPoint);
        else
            enemy.transform.position = spawnPoint;

        if (enemy.TryGetComponent(out HologramEffect hologram))
            hologram.StartEffect();

        return true;
    }

    public void TrySpawnOne(GameObject prefab)
    {
        activeEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
        if (player == null) return;
        if (WaveManager.Instance == null) return;
        if (activeEnemies.Count >= WaveManager.Instance.MaxEnemies) return;

        SpawnEnemy(prefab);
    }

    public void ForceSpawnOne(GameObject prefab)
    {
        activeEnemies.RemoveAll(e => e == null || !e.activeInHierarchy);
        if (player == null) return;

        SpawnEnemy(prefab);
    }

    private void SpawnEnemy(GameObject prefab)
    {
        Vector3 spawnPoint = GetSpawnPoint();
        if (spawnPoint == Vector3.zero) return;
        if (PoolManager.Instance == null) return;

        GameObject enemy = PoolManager.Instance.Get(prefab);
        enemy.transform.position = spawnPoint;
        FacePlayer(enemy.transform);
        activeEnemies.Add(enemy);
    }

    private void FacePlayer(Transform t)
    {
        Vector3 dir = player.position - t.position;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.001f)
            t.rotation = Quaternion.LookRotation(dir.normalized);
    }

    private Vector3 GetSpawnPoint()
    {
        for (int attempt = 0; attempt < 20; attempt++)
        {
            float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = Random.Range(2f, spawnRadius);
            Vector3 candidate = player.position + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * dist;

            if (NavMesh.SamplePosition(candidate, out NavMeshHit hit, navMeshSampleRadius, NavMesh.AllAreas))
                return hit.position;
        }
        return Vector3.zero;
    }
}
