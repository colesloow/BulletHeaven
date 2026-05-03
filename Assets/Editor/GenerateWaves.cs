using UnityEditor;
using UnityEngine;

public static class GenerateWaves
{
    private const string OutputFolder = "Assets/Data/Waves";
    private const string FlyingEnemyPath = "Assets/Prefabs/Enemy_FlyingRobot.prefab";
    private const string SpiderEnemyPath = "Assets/Prefabs/Enemy_SpiderRobot.prefab";

    [MenuItem("BulletHeaven/Generate Waves")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(OutputFolder))
            AssetDatabase.CreateFolder("Assets/Data", "Waves");

        string[] existing = AssetDatabase.FindAssets("Wave_", new[] { OutputFolder });
        foreach (string guid in existing)
            AssetDatabase.DeleteAsset(AssetDatabase.GUIDToAssetPath(guid));

        GameObject flyingEnemy = AssetDatabase.LoadAssetAtPath<GameObject>(FlyingEnemyPath);
        GameObject spiderEnemy = AssetDatabase.LoadAssetAtPath<GameObject>(SpiderEnemyPath);

        if (flyingEnemy == null) Debug.LogWarning($"Flying enemy prefab not found at {FlyingEnemyPath}");
        if (spiderEnemy == null) Debug.LogWarning($"Spider enemy prefab not found at {SpiderEnemyPath}");

        EnemySpawnEntry Flying(float w) => new() { Prefab = flyingEnemy, Weight = w };
        EnemySpawnEntry Spider(float w) => new() { Prefab = spiderEnemy, Weight = w };

        // (fileName, triggerTime, duration, spawnInterval, maxEnemies, enemyTypes[])
        // Game duration: 10 minutes (600s). Spider introduced at 3m30 to ensure both types
        // are present for most of the session. MaxEnemies=0 disables the per-wave cap.
        var definitions = new (string name, float trigger, float duration, float interval, int max, EnemySpawnEntry[] enemies)[]
        {
            ("Wave_00m30s",  30f, 20f, 1.5f, 15, new[] { Flying(1f) }),
            ("Wave_01m00s",  60f, 25f, 1.2f, 20, new[] { Flying(1f) }),
            ("Wave_01m30s",  90f, 25f, 1.0f, 25, new[] { Flying(1f) }),
            ("Wave_02m30s", 150f, 30f, 0.9f, 30, new[] { Flying(1f) }),
            ("Wave_03m30s", 210f, 30f, 0.7f, 35, new[] { Flying(2f), Spider(1f) }),
            ("Wave_05m00s", 300f, 35f, 0.6f, 45, new[] { Flying(1f), Spider(1f) }),
            ("Wave_06m30s", 390f, 35f, 0.5f, 55, new[] { Flying(1f), Spider(2f) }),
            ("Wave_08m00s", 480f, 40f, 0.4f, 70, new[] { Flying(1f), Spider(3f) }),
            ("Wave_09m00s", 540f, 40f, 0.35f, 80, new[] { Flying(1f), Spider(4f) }),
            ("Wave_09m30s", 570f, 30f, 0.3f,   0, new[] { Flying(1f), Spider(5f) }),
        };

        int count = 0;
        foreach (var (name, trigger, duration, interval, max, enemies) in definitions)
        {
            string assetPath = $"{OutputFolder}/{name}.asset";

            WaveConfig asset = ScriptableObject.CreateInstance<WaveConfig>();
            asset.TriggerTime = trigger;
            asset.Duration = duration;
            asset.SpawnInterval = interval;
            asset.MaxEnemies = max;
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
