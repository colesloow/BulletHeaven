using System.Collections.Generic;
using UnityEngine;

public class SatelliteWeapon : Weapon
{
    [SerializeField] private GameObject _satellitePrefab;
    // Euler offset applied after the orbit yaw, to compensate for the model's local orientation.
    [SerializeField] private Vector3 _modelRotationOffset = Vector3.zero;
    [SerializeField] private float _orbitRadius = 1.5f;
    [SerializeField] private float _orbitSpeed = 100f;
    [SerializeField] private int _satelliteCount = 1;

    [Header("Damage")]
    [SerializeField] private float _damage = 10f;
    [SerializeField] private float _contactRadius = 0.3f;
    [SerializeField] private float _damageInterval = 0.5f;

    [Header("Caps")]
    [SerializeField] private int _maxSatellites = 10;
    [SerializeField] private float _maxOrbitRadius = 3f;
    [SerializeField] private float _maxOrbitSpeed = 300f;
    [SerializeField] private float _maxDamageBonus = 50f;

    private float _damageBonus = 0f;
    private bool _laserUnlocked = false;
    private float _angle = 0f;
    private GameObject[] _satellites;
    private readonly Dictionary<Health, float> _nextHitTime = new();

    protected override void OnInitialize()
    {
        SpawnSatellites();
    }

    private void Update()
    {
        _angle += _orbitSpeed * Time.deltaTime;
        UpdateSatellitePositions();
        CheckHits();
    }

    private void UpdateSatellitePositions()
    {
        if (_satellites == null) return;
        Vector3 center = transform.position;

        for (int i = 0; i < _satelliteCount; i++)
        {
            if (_satellites[i] == null) continue;
            float rad = (_angle + i * 360f / _satelliteCount) * Mathf.Deg2Rad;
            Vector3 outward = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
            _satellites[i].transform.position = center + outward * _orbitRadius;
            // Yaw faces outward; _modelRotationOffset corrects the model's local orientation.
            _satellites[i].transform.rotation =
                Quaternion.LookRotation(outward, Vector3.up) * Quaternion.Euler(_modelRotationOffset);
        }
    }

    private void CheckHits()
    {
        if (_satellites == null) return;

        foreach (GameObject sat in _satellites)
        {
            if (sat == null) continue;
            Vector2 satXZ = new(sat.transform.position.x, sat.transform.position.z);

            foreach (Health enemy in Health.ActiveEnemies)
            {
                if (_nextHitTime.TryGetValue(enemy, out float next) && Time.time < next) continue;

                Vector2 enemyXZ = new(enemy.transform.position.x, enemy.transform.position.z);
                if (Vector2.Distance(satXZ, enemyXZ) < _contactRadius)
                {
                    enemy.LoseHealth(_damage + _damageBonus);
                    _nextHitTime[enemy] = Time.time + _damageInterval;
                }
            }
        }
    }

    public override bool IsUpgradeAvailable(WeaponUpgrade upgrade)
    {
        return upgrade.Type switch
        {
            UpgradeType.SatelliteCount => _satelliteCount < _maxSatellites,
            UpgradeType.SatelliteRadius => _orbitRadius < _maxOrbitRadius,
            UpgradeType.SatelliteSpeed => _orbitSpeed < _maxOrbitSpeed,
            UpgradeType.SatelliteDamage => _damageBonus < _maxDamageBonus,
            _ => true,
        };
    }

    public override void ApplyUpgrade(WeaponUpgrade upgrade)
    {
        switch (upgrade.Type)
        {
            case UpgradeType.SatelliteCount:
                _satelliteCount = Mathf.Clamp(_satelliteCount + (int)upgrade.Value, 1, _maxSatellites);
                SpawnSatellites();
                break;
            case UpgradeType.SatelliteRadius:
                _orbitRadius += upgrade.Value;
                SpawnSatellites();
                break;
            case UpgradeType.SatelliteSpeed:
                _orbitSpeed += upgrade.Value;
                break;
            case UpgradeType.SatelliteDamage:
                _damageBonus = Mathf.Min(_damageBonus + upgrade.Value, _maxDamageBonus);
                break;
        }
    }

    public void UnlockLasers()
    {
        _laserUnlocked = true;
        if (_satellites == null) return;
        foreach (var sat in _satellites)
        {
            if (sat == null) continue;
            sat.GetComponentInChildren<LaserBeamController>()?.Unlock();
        }
    }

    public void ModifyLaserInterval(float delta)
    {
        if (_satellites == null) return;
        foreach (var sat in _satellites)
            sat?.GetComponentInChildren<LaserBeamController>()?.ModifyInterval(delta);
    }

    public void ModifyLaserDuration(float delta)
    {
        if (_satellites == null) return;
        foreach (var sat in _satellites)
            sat?.GetComponentInChildren<LaserBeamController>()?.ModifyDuration(delta);
    }

    public void ModifyLaserLength(float delta)
    {
        if (_satellites == null) return;
        foreach (var sat in _satellites)
            sat?.GetComponentInChildren<LaserBeamController>()?.ModifyLength(delta);
    }

    public override void OnPlayerDeath()
    {
        if (_satellites == null) return;
        foreach (var sat in _satellites)
        {
            if (sat == null) continue;
            sat.transform.SetParent(null);
            var rb = sat.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
        }
    }

    private void OnDrawGizmos()
    {
        if (_satellites == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        foreach (GameObject sat in _satellites)
        {
            if (sat == null) continue;
            Gizmos.DrawSphere(sat.transform.position, _contactRadius);
        }
    }

    private void SpawnSatellites()
    {
        if (_satellites != null)
            foreach (var sat in _satellites)
                if (sat != null) Destroy(sat);

        _satellites = new GameObject[_satelliteCount];
        for (int i = 0; i < _satelliteCount; i++)
        {
            _satellites[i] = Instantiate(_satellitePrefab, transform);
            if (_laserUnlocked)
                _satellites[i].GetComponentInChildren<LaserBeamController>()?.Unlock();
        }
    }
}
