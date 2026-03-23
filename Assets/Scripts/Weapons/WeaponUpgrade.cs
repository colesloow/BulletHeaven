using UnityEngine;

public enum UpgradeType
{
    SatelliteCount,
    SatelliteRadius,
    SatelliteSpeed,
    SatelliteDamage,
    LaserUnlock,
    LaserInterval,
    LaserDuration,
    LaserLength,
}

public enum UpgradeRarity
{
    Common,
    Uncommon,
    Rare,
}

[CreateAssetMenu(fileName = "WeaponUpgrade", menuName = "BulletHeaven/Weapon Upgrade")]
public class WeaponUpgrade : ScriptableObject
{
    public string UpgradeName;
    [TextArea] public string Description;
    public WeaponData TargetWeapon; // null means player-level upgrade
    public UpgradeType Type;
    public float Value;
    public UpgradeRarity Rarity;
}
