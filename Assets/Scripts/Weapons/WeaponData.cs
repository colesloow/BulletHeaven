using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "BulletHeaven/Weapon")]
public class WeaponData : ScriptableObject
{
    public string WeaponName;
    public GameObject Prefab;
    public WeaponData Prerequisite; // null = no requirement to unlock this weapon
    public WeaponUpgrade[] AvailableUpgrades;
}
