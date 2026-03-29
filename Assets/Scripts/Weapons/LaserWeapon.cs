using UnityEngine;

public class LaserWeapon : Weapon
{
    private SatelliteWeapon _satelliteWeapon;

    protected override void OnInitialize()
    {
        _satelliteWeapon = GetComponentInParent<SatelliteWeapon>();

        if (_satelliteWeapon == null)
        {
            Debug.LogError("LaserWeapon requires a SatelliteWeapon on the player.");
            return;
        }

        _satelliteWeapon.UnlockLasers();
    }

    public override void ApplyUpgrade(WeaponUpgrade upgrade)
    {
        _satelliteWeapon?.ApplyLaserUpgrade(upgrade);
    }
}
