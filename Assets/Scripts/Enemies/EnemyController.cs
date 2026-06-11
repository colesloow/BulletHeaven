using UnityEngine;
using UnityEngine.AI;

// Controls a single enemy: chases the player via NavMeshAgent and deals contact damage.
// Damage scales with player level via WaveManager.OnEnemiesLevelUp.
// No physics collision — damage is detected by XZ distance check each frame.
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private float _speed = 3.5f;

    [Header("Damage")]
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _damageRange = 0.8f;
    [SerializeField] private float _damageInterval = 1f;

    private float _baseDamage;
    private NavMeshAgent _agent;
    private Health _ownHealth;
    private Transform _player;
    private Health _playerHealth;
    private float _nextHitTime;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _speed;
        _baseDamage = _damage;
        _ownHealth = GetComponent<Health>();

        GameObject playerObj = GameObject.FindWithTag(Tags.Player);
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerHealth = playerObj.GetComponent<Health>();
        }
    }

    private void OnEnable()
    {
        _damage = _baseDamage;
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnEnemiesLevelUp += ScaleDamage;
    }

    private void OnDisable()
    {
        if (WaveManager.Instance != null)
            WaveManager.Instance.OnEnemiesLevelUp -= ScaleDamage;
    }

    // Recomputes damage from base each level-up to avoid compounding.
    // Formula: baseDamage * (1 + (level - 1) * scalingPerLevel)
    private void ScaleDamage(float healthScalingPerLevel, float damageScalingPerLevel, int level)
    {
        _damage = _baseDamage * (1f + (level - 1) * damageScalingPerLevel);
    }

    private void Update()
    {
        if (_player == null || !_agent.isOnNavMesh) return;
        _agent.SetDestination(_player.position);
        CheckPlayerHit();
    }

    // XZ-only distance check: ignores Y so flying enemies at different heights still deal damage.
    // Gated by _damageInterval to avoid damage every frame.
    private void CheckPlayerHit()
    {
        if (_ownHealth != null && _ownHealth.IsDead) return;
        if (_playerHealth == null || Time.time < _nextHitTime) return;
        if (WaveManager.Instance != null && !WaveManager.Instance.EnemyDamageEnabled) return;

        Vector2 enemyXZ = new(transform.position.x, transform.position.z);
        Vector2 playerXZ = new(_player.position.x, _player.position.z);

        if (Vector2.Distance(enemyXZ, playerXZ) < _damageRange)
        {
            _playerHealth.LoseHealth(_damage);
            _nextHitTime = Time.time + _damageInterval;
        }
    }

    // Called by PooledObject when returned to pool
    public void OnRelease()
    {
        _agent.ResetPath();
        _nextHitTime = 0f;
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        // Damage range disc (XZ)
        UnityEditor.Handles.color = new Color(1f, 0.3f, 0.3f, 0.4f);
        UnityEditor.Handles.DrawSolidDisc(transform.position, Vector3.up, _damageRange);
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.DrawWireDisc(transform.position, Vector3.up, _damageRange);

        if (_player == null) return;

        // Line to player with current XZ distance label
        Vector3 playerFlat = new(_player.position.x, transform.position.y, _player.position.z);
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(transform.position, playerFlat);

        float xzDist = Vector2.Distance(
            new Vector2(transform.position.x, transform.position.z),
            new Vector2(_player.position.x, _player.position.z));
        UnityEditor.Handles.Label(
            Vector3.Lerp(transform.position, playerFlat, 0.5f) + Vector3.up * 0.3f,
            $"XZ: {xzDist:F2}");

        // NavMesh status
        if (!_agent.isOnNavMesh)
        {
            UnityEditor.Handles.color = Color.magenta;
            UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, "NOT ON NAVMESH");
        }
    }
#endif
}
