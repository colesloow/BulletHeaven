using UnityEngine;

public class SatelliteWeapon : Weapon
{
    [SerializeField] private GameObject _satellitePrefab;
    [SerializeField] private float _orbitRadius = 1.5f;
    [SerializeField] private float _orbitSpeed = 100f;
    [SerializeField] private int _satelliteCount = 1;

    [Header("Caps")]
    [SerializeField] private int _maxSatellites = 10;
    [SerializeField] private float _maxOrbitRadius = 3f;
    [SerializeField] private float _maxOrbitSpeed = 300f;
    [SerializeField] private float _maxDamageBonus = 50f;

    private float _damageBonus = 0f;
    private Transform _orbitParent;
    private GameObject[] _satellites;
    private bool _laserUnlocked = false;

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
    }

    public override bool IsUpgradeAvailable(WeaponUpgrade upgrade)
    {
        return upgrade.Type switch
        {
            UpgradeType.SatelliteCount  => _satelliteCount < _maxSatellites,
            UpgradeType.SatelliteRadius => _orbitRadius < _maxOrbitRadius,
            UpgradeType.SatelliteSpeed  => _orbitSpeed < _maxOrbitSpeed,
            UpgradeType.SatelliteDamage => _damageBonus < _maxDamageBonus,
            _                           => true,
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
                foreach (var sat in _satellites)
                    sat.GetComponent<HitOtherOnCollision>()?.AddDamage(upgrade.Value);
                break;
        }
    }

    public void UnlockLasers()
    {
        _laserUnlocked = true;
        foreach (var sat in _satellites)
            sat.GetComponent<LaserBeamController>()?.Unlock();
    }

    public void ApplyLaserUpgrade(WeaponUpgrade upgrade)
    {
        switch (upgrade.Type)
        {
            case UpgradeType.LaserInterval:
                foreach (var sat in _satellites)
                    sat.GetComponent<LaserBeamController>()?.ModifyInterval(upgrade.Value);
                break;
            case UpgradeType.LaserDuration:
                foreach (var sat in _satellites)
                    sat.GetComponent<LaserBeamController>()?.ModifyDuration(upgrade.Value);
                break;
            case UpgradeType.LaserLength:
                foreach (var sat in _satellites)
                    sat.GetComponent<LaserBeamController>()?.ModifyLength(upgrade.Value);
                break;
        }
    }

    public override void OnPlayerDeath()
    {
        if (_orbitParent == null) return;

        foreach (var sat in _satellites)
        {
            if (sat == null) continue;
            sat.GetComponent<LaserBeamController>()?.StopLaser();
            var rb = sat.GetComponent<Rigidbody>();
            if (rb != null) rb.isKinematic = false;
        }

        // Detach orbit from player so satellites fly off naturally
        _orbitParent.SetParent(null);
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

            if (_damageBonus > 0f && sat.TryGetComponent(out HitOtherOnCollision hit))
                hit.AddDamage(_damageBonus);

            if (_laserUnlocked)
                sat.GetComponent<LaserBeamController>()?.Unlock();

            _satellites[i] = sat;
        }
    }
}
