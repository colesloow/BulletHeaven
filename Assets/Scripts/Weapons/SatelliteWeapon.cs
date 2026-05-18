using System.Collections.Generic;
using UnityEngine;

public class SatelliteWeapon : Weapon
{
    [SerializeField] private GameObject _satellitePrefab;
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
    private Transform _orbitParent;
    private GameObject[] _satellites;
    private readonly Dictionary<Health, float> _nextHitTime = new();

    protected override void OnInitialize()
    {
        _orbitParent = new GameObject("SatelliteOrbit").transform;
        _orbitParent.SetParent(transform);
        _orbitParent.localPosition = Vector3.zero;

        SpawnSatellites();
    }

    private void Update()
    {
        if (_orbitParent != null)
            _orbitParent.Rotate(0, _orbitSpeed * Time.deltaTime, 0);

        CheckHits();
    }

    private void CheckHits()
    {
        if (_satellites == null) return;

        foreach (GameObject sat in _satellites)
        {
            if (sat == null) continue;
            Renderer satRenderer = sat.GetComponentInChildren<Renderer>();
            Vector3 satCenter = satRenderer != null ? satRenderer.bounds.center : sat.transform.position;
            Vector2 satXZ = new(satCenter.x, satCenter.z);

            foreach (Health enemy in Health.ActiveEnemies)
            {
                if (_nextHitTime.TryGetValue(enemy, out float next) && Time.time < next) continue;

                Vector2 enemyXZ = new(enemy.transform.position.x, enemy.transform.position.z);
                float dist = Vector2.Distance(satXZ, enemyXZ);
                if (dist < _contactRadius)
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
            _  => true,
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
        if (_orbitParent == null) return;

        foreach (var sat in _satellites)
        {
            if (sat == null) continue;
            var rb = sat.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
        }

        // Detach orbit from player so satellites fly off naturally
        _orbitParent.SetParent(null);
    }

    private void OnDrawGizmos()
    {
        if (_satellites == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        foreach (GameObject sat in _satellites)
        {
            if (sat == null) continue;
            Renderer r = sat.GetComponentInChildren<Renderer>();
            Vector3 center = r != null ? r.bounds.center : sat.transform.position;
            Gizmos.DrawSphere(center, _contactRadius);
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
            float angle = i * 360f / _satelliteCount;
            Vector3 localPos = new Vector3(
                Mathf.Cos(angle * Mathf.Deg2Rad) * _orbitRadius,
                0f,
                Mathf.Sin(angle * Mathf.Deg2Rad) * _orbitRadius
            );

            GameObject sat = Instantiate(_satellitePrefab, _orbitParent);
            sat.transform.localPosition = localPos;
            sat.transform.localRotation = Quaternion.Euler(0, -angle, 0) * Quaternion.Euler(90, 90, 0);

            _satellites[i] = sat;

            if (_laserUnlocked)
                sat.GetComponentInChildren<LaserBeamController>()?.Unlock();
        }
    }
}
