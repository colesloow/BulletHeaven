using System.Collections.Generic;
using UnityEngine;

public class SatelliteWeapon : Weapon
{
    [SerializeField] private GameObject satellitePrefab;
    // Euler offset applied after the orbit yaw, to compensate for the model's local orientation.
    [SerializeField] private Vector3 modelRotationOffset = Vector3.zero;
    [SerializeField] private float orbitRadius = 1.5f;
    [SerializeField] private float orbitSpeed = 100f;
    [SerializeField] private int satelliteCount = 1;

    [Header("Damage")]
    [SerializeField] private float damage = 10f;
    [SerializeField] private float contactRadius = 0.3f;
    [SerializeField] private float damageInterval = 0.5f;

    [Header("Caps")]
    [SerializeField] private int maxSatellites = 10;
    [SerializeField] private float maxOrbitRadius = 3f;
    [SerializeField] private float maxOrbitSpeed = 300f;
    [SerializeField] private float maxDamageBonus = 50f;

    private float damageBonus = 0f;
    private int laserCount = 0;
    private float angle = 0f;
    private GameObject[] satellites;
    private readonly Dictionary<Health, float> nextHitTime = new();

    private void OnEnable() => Health.OnEnemyDisabled += OnEnemyRemoved;
    private void OnDisable() => Health.OnEnemyDisabled -= OnEnemyRemoved;
    private void OnEnemyRemoved(Health enemy) => nextHitTime.Remove(enemy);

    protected override void OnInitialize()
    {
        SpawnSatellites();
    }

    private void Update()
    {
        angle -= orbitSpeed * Time.deltaTime;
        UpdateSatellitePositions();
        CheckHits();
    }

    private void UpdateSatellitePositions()
    {
        if (satellites == null) return;
        Vector3 center = transform.position;

        for (int i = 0; i < satelliteCount; i++)
        {
            if (satellites[i] == null) continue;
            float rad = (angle + i * 360f / satelliteCount) * Mathf.Deg2Rad;
            Vector3 outward = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
            satellites[i].transform.position = center + outward * orbitRadius;
            // Yaw faces outward; modelRotationOffset corrects the model's local orientation.
            satellites[i].transform.rotation =
                Quaternion.LookRotation(outward, Vector3.up) * Quaternion.Euler(modelRotationOffset);
        }
    }

    private void CheckHits()
    {
        if (satellites == null) return;

        foreach (GameObject sat in satellites)
        {
            if (sat == null) continue;
            Vector2 satXZ = new(sat.transform.position.x, sat.transform.position.z);

            foreach (Health enemy in Health.ActiveEnemies)
            {
                if (nextHitTime.TryGetValue(enemy, out float next) && Time.time < next) continue;

                Vector2 enemyXZ = new(enemy.transform.position.x, enemy.transform.position.z);
                if (Vector2.Distance(satXZ, enemyXZ) < contactRadius)
                {
                    enemy.LoseHealth(damage + damageBonus);
                    nextHitTime[enemy] = Time.time + damageInterval;
                }
            }
        }
    }

    public override bool IsUpgradeAvailable(WeaponUpgrade upgrade)
    {
        return upgrade.Type switch
        {
            UpgradeType.SatelliteCount => satelliteCount < maxSatellites,
            UpgradeType.SatelliteRadius => orbitRadius < maxOrbitRadius,
            UpgradeType.SatelliteSpeed => orbitSpeed < maxOrbitSpeed,
            UpgradeType.SatelliteDamage => damageBonus < maxDamageBonus,
            UpgradeType.SatelliteLaserCount => laserCount > 0 && laserCount < satelliteCount,
            _ => true,
        };
    }

    public override void ApplyUpgrade(WeaponUpgrade upgrade)
    {
        switch (upgrade.Type)
        {
            case UpgradeType.SatelliteCount:
                satelliteCount = Mathf.Clamp(satelliteCount + (int)upgrade.Value, 1, maxSatellites);
                SpawnSatellites();
                break;
            case UpgradeType.SatelliteRadius:
                orbitRadius += upgrade.Value;
                SpawnSatellites();
                break;
            case UpgradeType.SatelliteSpeed:
                orbitSpeed += upgrade.Value;
                break;
            case UpgradeType.SatelliteDamage:
                damageBonus = Mathf.Min(damageBonus + upgrade.Value, maxDamageBonus);
                break;
            case UpgradeType.SatelliteLaserCount:
                if (laserCount < satelliteCount)
                {
                    laserCount++;
                    ApplyLaserState();
                }
                break;
        }
    }

    public void UnlockLasers()
    {
        if (laserCount > 0) return;
        laserCount = 1;
        ApplyLaserState();
    }

    public void ModifyLaserInterval(float delta) => ForEachLaser(l => l.ModifyInterval(delta));
    public void ModifyLaserDuration(float delta) => ForEachLaser(l => l.ModifyDuration(delta));
    public void ModifyLaserLength(float delta) => ForEachLaser(l => l.ModifyLength(delta));

    private void ForEachLaser(System.Action<LaserBeamController> action)
    {
        if (satellites == null) return;
        foreach (var sat in satellites)
        {
            if (sat == null) continue;
            var laser = sat.GetComponentInChildren<LaserBeamController>();
            if (laser != null) action(laser);
        }
    }

    public override void OnPlayerDeath()
    {
        if (satellites == null) return;
        foreach (var sat in satellites)
            if (sat != null) sat.SetActive(false);
    }

    private void OnDrawGizmos()
    {
        if (satellites == null) return;
        Gizmos.color = new Color(1f, 0.3f, 0f, 0.4f);
        foreach (GameObject sat in satellites)
        {
            if (sat == null) continue;
            Gizmos.DrawSphere(sat.transform.position, contactRadius);
        }
    }

    private void ApplyLaserState()
    {
        if (satellites == null || laserCount == 0) return;
        for (int slot = 0; slot < laserCount; slot++)
        {
            int idx = LaserPlacementIndex(slot);
            if (idx >= satelliteCount || satellites[idx] == null) continue;
            satellites[idx].GetComponentInChildren<LaserBeamController>()?.Unlock();
        }
    }

    // Even count: interleave 0, N/2, 1, N/2+1, ... to maximize angular spread.
    // Odd count: sequential 0, 1, 2, ... (asymmetry accepted).
    private int LaserPlacementIndex(int laserSlot)
    {
        if (satelliteCount % 2 == 1) return laserSlot;
        int half = satelliteCount / 2;
        return laserSlot % 2 == 0 ? laserSlot / 2 : half + laserSlot / 2;
    }

    private void SpawnSatellites()
    {
        if (satellites != null)
            foreach (var sat in satellites)
                if (sat != null) Destroy(sat);

        satellites = new GameObject[satelliteCount];
        for (int i = 0; i < satelliteCount; i++)
            satellites[i] = Instantiate(satellitePrefab, transform);

        ApplyLaserState();
    }
}
