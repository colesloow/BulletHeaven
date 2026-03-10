using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [SerializeField]
    private GameObject _enemyPrefab;
    [SerializeField]
    private GameObject _enemyParent;
    [SerializeField]
    private BoxCollider _spawnArea;
    [SerializeField]
    private float _height = 1f;
    [SerializeField]
    [Range(0.1f, 2.0f)]
    private float _spawnDelay = 2.0f;
    [SerializeField]
    private int _maxEnemies;

    void Start()
    {
        _maxEnemies = GameManager.Instance.MaxEnemies;
        _enemyParent = FindFirstObjectByType<SpawnerOverTime>().gameObject; // enemies will be instantiated in SpawnManager
        InvokeRepeating(nameof(InstantiateEnemy), 2.0f, _spawnDelay);
    }

    private void InstantiateEnemy()
    {
        Bounds bounds = _spawnArea.bounds;

        // generate random positions in bounds limits
        float randomPosX = Random.Range(bounds.min.x, bounds.max.x);
        float randomPosZ = Random.Range(bounds.min.z, bounds.max.z);

        Vector3 randomPosition = new Vector3(randomPosX, _height, randomPosZ);

        // check max number of enemies
        EnemyController[] enemies = _enemyParent.GetComponentsInChildren<EnemyController>();
        if (enemies.Length < _maxEnemies)
        {
            Instantiate(_enemyPrefab, randomPosition, Quaternion.identity, _enemyParent.transform);
        }
    }


}
