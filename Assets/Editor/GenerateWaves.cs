using UnityEditor;
using UnityEngine;

public static class GenerateWaves
{
    private const string OutputFolder = "Assets/ScriptableObjetcts/Waves";
    private const string BasicEnemyPath = "Assets/Prefabs/Enemy.prefab";
    private const string FlyingEnemyPath = "Assets/Prefabs/Enemy_FlyingRobot.prefab";

    [MenuItem("BulletHeaven/Generate Waves")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder("Assets/ScriptableObjetcts", "Waves");

        // Delete existing Wave_*.asset files before regenerating
        string[] existing = AssetDatabase.FindAssets("Wave_", new[] { OutputFolder });
        foreach (string guid in existing)
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

        GameObject basicEnemy = AssetDatabase.LoadAssetAtPath<GameObject>(BasicEnemyPath);
        GameObject flyingEnemy = AssetDatabase.LoadAssetAtPath<GameObject>(FlyingEnemyPath);

        if (basicEnemy == null)
            Debug.LogWarning($"Basic enemy prefab not found at {BasicEnemyPath}");
        if (flyingEnemy == null)
            Debug.LogWarning($"Flying enemy prefab not found at {FlyingEnemyPath}");

        // Entry helpers
        EnemySpawnEntry Basic(float w) => new() { Prefab = basicEnemy, Weight = w };
        EnemySpawnEntry Flying(float w) => new() { Prefab = flyingEnemy, Weight = w };

        // (fileName, triggerTime, duration, spawnInterval, maxEnemies, enemyTypes[])
        var definitions = new (string fileName, float triggerTime, float duration, float spawnInterval, int maxEnemies, EnemySpawnEntry[] enemies)[]
        {
            // Early waves: only basic enemies
            ("Wave_00m30s",  30f, 20f, 1.5f, 15, new[] { Basic(1f) }),
            ("Wave_01m00s",  60f, 25f, 1.2f, 20, new[] { Basic(1f) }),
            ("Wave_01m30s",  90f, 30f, 1.0f, 25, new[] { Basic(1f) }),

            // Mid waves: mix basic + flying, flying increases over time
            ("Wave_02m00s", 120f, 30f, 1.0f, 30, new[] { Basic(3f), Flying(1f) }),
            ("Wave_03m00s", 180f, 35f, 0.8f, 35, new[] { Basic(2f), Flying(1f) }),
            ("Wave_04m00s", 240f, 40f, 0.7f, 40, new[] { Basic(1f), Flying(1f) }),

            // Late waves: flying enemies dominant
            ("Wave_05m00s", 300f, 45f, 0.6f, 50, new[] { Basic(1f), Flying(2f) }),
            ("Wave_07m00s", 420f, 50f, 0.5f, 60, new[] { Basic(1f), Flying(3f) }),
            ("Wave_10m00s", 600f, 60f, 0.4f, 80, new[] { Basic(1f), Flying(4f) }),
        };

        int count = 0;
        foreach (var (fileName, triggerTime, duration, spawnInterval, maxEnemies, enemies) in definitions)
        {
            string assetPath = $"{OutputFolder}/{fileName}.asset";

            WaveConfig asset = ScriptableObject.CreateInstance<WaveConfig>();
            asset.TriggerTime = triggerTime;
            asset.Duration = duration;
            asset.SpawnInterval = spawnInterval;
            asset.MaxEnemies = maxEnemies;
            asset.EnemyTypes = enemies;

            AssetDatabase.CreateAsset(asset, assetPath);
            EditorUtility.SetDirty(asset);
            count++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"Generated {count} wave configs in {OutputFolder}.");
    }
}
