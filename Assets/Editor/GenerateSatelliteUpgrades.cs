using UnityEditor;
using UnityEngine;

public static class GenerateSatelliteUpgrades
{
    private const string SatelliteDataPath = "Assets/ScriptableObjetcts/Weapons/Satellite_WeaponData.asset";
    private const string LaserDataPath     = "Assets/ScriptableObjetcts/Weapons/Laser_WeaponData.asset";
    private const string OutputFolder      = "Assets/ScriptableObjetcts/Weapons";

    [MenuItem("BulletHeaven/Generate Satellite Upgrades")]
    public static void Generate()
    {
        WeaponData satelliteData = AssetDatabase.LoadAssetAtPath<WeaponData>(SatelliteDataPath);
        if (satelliteData == null)
        {
            Debug.LogError($"WeaponData not found at {SatelliteDataPath}");
            return;
        }

        WeaponData laserData = AssetDatabase.LoadAssetAtPath<WeaponData>(LaserDataPath);
        if (laserData == null)
        {
            Debug.LogError($"WeaponData not found at {LaserDataPath}. Create it first in the Unity editor.");
            return;
        }

        // Delete all existing Upgrade_*.asset files before regenerating
        string[] existing = AssetDatabase.FindAssets("Upgrade_", new[] { OutputFolder });
        foreach (string guid in existing)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            AssetDatabase.DeleteAsset(path);
        }

        // (fileName, display name, target weapon, type, value, rarity)
        var definitions = new (string fileName, string displayName, WeaponData target, UpgradeType type, float value, UpgradeRarity rarity)[]
        {
            // Satellite upgrades
            ("Upgrade_Satellite_Count_1",  "+1 Satellite",               satelliteData, UpgradeType.SatelliteCount,  1f,   UpgradeRarity.Common),
            ("Upgrade_Satellite_Count_2",  "+2 Satellites",              satelliteData, UpgradeType.SatelliteCount,  2f,   UpgradeRarity.Uncommon),
            ("Upgrade_Satellite_Count_3",  "+3 Satellites",              satelliteData, UpgradeType.SatelliteCount,  3f,   UpgradeRarity.Rare),
            ("Upgrade_Satellite_Radius_S", "+0.5 Orbit Radius",          satelliteData, UpgradeType.SatelliteRadius, 0.5f, UpgradeRarity.Common),
            ("Upgrade_Satellite_Radius_M", "+1 Orbit Radius",            satelliteData, UpgradeType.SatelliteRadius, 1f,   UpgradeRarity.Uncommon),
            ("Upgrade_Satellite_Speed_S",  "+20 Orbit Speed",            satelliteData, UpgradeType.SatelliteSpeed,  20f,  UpgradeRarity.Common),
            ("Upgrade_Satellite_Speed_M",  "+50 Orbit Speed",            satelliteData, UpgradeType.SatelliteSpeed,  50f,  UpgradeRarity.Uncommon),
            ("Upgrade_Satellite_Damage_S", "+5 Damage",                  satelliteData, UpgradeType.SatelliteDamage, 5f,   UpgradeRarity.Common),
            ("Upgrade_Satellite_Damage_M", "+15 Damage",                 satelliteData, UpgradeType.SatelliteDamage, 15f,  UpgradeRarity.Uncommon),
            ("Upgrade_Satellite_Damage_L", "+30 Damage",                 satelliteData, UpgradeType.SatelliteDamage, 30f,  UpgradeRarity.Rare),

            // Laser upgrades (require LaserWeapon to be active)
            ("Upgrade_Laser_Interval_S",   "-1s Laser Interval",  laserData, UpgradeType.LaserInterval, -1f,  UpgradeRarity.Common),
            ("Upgrade_Laser_Interval_M",   "-2s Laser Interval",  laserData, UpgradeType.LaserInterval, -2f,  UpgradeRarity.Uncommon),
            ("Upgrade_Laser_Duration_S",   "+1.5s Laser Duration",laserData, UpgradeType.LaserDuration,  1.5f, UpgradeRarity.Common),
            ("Upgrade_Laser_Duration_M",   "+3s Laser Duration",  laserData, UpgradeType.LaserDuration,  3f,   UpgradeRarity.Uncommon),
            ("Upgrade_Laser_Length_S",     "+2 Laser Length",     laserData, UpgradeType.LaserLength,    2f,   UpgradeRarity.Common),
            ("Upgrade_Laser_Length_M",     "+5 Laser Length",     laserData, UpgradeType.LaserLength,    5f,   UpgradeRarity.Uncommon),
        };

        var satelliteUpgrades = new System.Collections.Generic.List<WeaponUpgrade>();
        var laserUpgrades     = new System.Collections.Generic.List<WeaponUpgrade>();

        foreach (var (fileName, displayName, target, type, value, rarity) in definitions)
        {
            string assetPath = $"{OutputFolder}/{fileName}.asset";

            WeaponUpgrade asset = AssetDatabase.LoadAssetAtPath<WeaponUpgrade>(assetPath);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<WeaponUpgrade>();
                AssetDatabase.CreateAsset(asset, assetPath);
            }

            asset.UpgradeName  = displayName;
            asset.Description  = displayName;
            asset.TargetWeapon = target;
            asset.Type         = type;
            asset.Value        = value;
            asset.Rarity       = rarity;

            EditorUtility.SetDirty(asset);

            if (target == satelliteData)
                satelliteUpgrades.Add(asset);
            else
                laserUpgrades.Add(asset);
        }

        satelliteData.AvailableUpgrades = satelliteUpgrades.ToArray();
        laserData.AvailableUpgrades     = laserUpgrades.ToArray();

        EditorUtility.SetDirty(satelliteData);
        EditorUtility.SetDirty(laserData);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Generated {satelliteUpgrades.Count} satellite upgrades and {laserUpgrades.Count} laser upgrades.");
    }
}
