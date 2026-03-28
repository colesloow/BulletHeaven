using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyController : MonoBehaviour
{
    [SerializeField] private float _speed = 3.5f;

    [Header("Damage")]
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _damageRange = 0.8f;
    [SerializeField] private float _damageInterval = 1f;

    private NavMeshAgent _agent;
    private Transform _player;
    private Health _playerHealth;
    private float _nextHitTime;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _agent.speed = _speed;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindWithTag("Player");
        if (playerObj != null)
        {
            _player = playerObj.transform;
            _playerHealth = playerObj.GetComponent<Health>();
        }
    }

    private void Update()
    {
        if (_player == null || !_agent.isOnNavMesh) return;
        _agent.SetDestination(_player.position);
        CheckPlayerHit();
    }

    private void CheckPlayerHit()
    {
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
}
