using UnityEngine;

public class LaserWeapon : Weapon
{
    private SatelliteWeapon _satelliteWeapon;

    protected override void OnInitialize()
    {
        _satelliteWeapon = transform.parent.GetComponentInChildren<SatelliteWeapon>();
        if (_satelliteWeapon == null)
        {
            Debug.LogError("LaserWeapon: SatelliteWeapon not found among siblings.");
            return;
        }
        _satelliteWeapon.UnlockLasers();
    }

    public override void ApplyUpgrade(WeaponUpgrade upgrade)
    {
        switch (upgrade.Type)
        {
            case UpgradeType.LaserInterval: _satelliteWeapon?.ModifyLaserInterval(upgrade.Value); break;
            case UpgradeType.LaserDuration: _satelliteWeapon?.ModifyLaserDuration(upgrade.Value); break;
            case UpgradeType.LaserLength:   _satelliteWeapon?.ModifyLaserLength(upgrade.Value);   break;
        }
    }
}
