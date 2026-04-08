using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

public static class DungeonPrefabSetup
{
    private const string PrefabsPath = "Assets/Prefabs/Rooms";
    private const string MeshesPath  = "Assets/Meshes";

    // -------------------------------------------------------------------------
    // Setup Dungeon Prefabs
    // -------------------------------------------------------------------------

    [MenuItem("BulletHeaven/Setup Dungeon Prefabs")]
    public static void Execute()
    {
        Material wallMat  = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MAT_WallCutout.mat");
        Material floorMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/MAT_Colormap.mat");

        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsPath });
        int processed = 0;

        foreach (string guid in guids)
        {
            string prefabPath = AssetDatabase.GUIDToAssetPath(guid);
            string prefabName = Path.GetFileNameWithoutExtension(prefabPath);

            if (!prefabName.StartsWith("Corridor_") && !prefabName.StartsWith("Room_") && prefabName != "Corridor")
                continue;

            if (SetupPrefab(prefabPath, prefabName, wallMat, floorMat))
                processed++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[DungeonPrefabSetup] Done. {processed} prefabs processed.");
    }

    static bool SetupPrefab(string prefabPath, string prefabName, Material wallMat, Material floorMat)
    {
        // Corridor_Wide_Corner -> CorridorWideCorner  /  Room_Large -> RoomLarge
        string fbxBaseName = prefabName.Replace("_", "");

        string[] fbxGuids = AssetDatabase.FindAssets(fbxBaseName + " t:Model", new[] { MeshesPath });
        if (fbxGuids.Length == 0)
        {
            Debug.LogWarning($"[DungeonPrefabSetup] No FBX found for '{prefabName}' (searched: {fbxBaseName})");
            return false;
        }

        string fbxPath = AssetDatabase.GUIDToAssetPath(fbxGuids[0]);
        string kebabName = PascalToKebab(Path.GetFileNameWithoutExtension(fbxPath));

        Mesh wallsMesh = LoadMesh(fbxPath, kebabName);
        Mesh floorMesh = LoadMesh(fbxPath, kebabName + "-floor");
        Mesh navMesh  = LoadMesh(fbxPath, kebabName + "-navmesh");

        if (wallsMesh == null)
        {
            Debug.LogWarning($"[DungeonPrefabSetup] Walls mesh '{kebabName}' not found in {fbxPath}");
            return false;
        }

        using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
        GameObject root = scope.prefabContentsRoot;

        bool isRoom = prefabName.StartsWith("Room_");
        bool isCorridor = prefabName.StartsWith("Corridor_") || prefabName == "Corridor";

        // Root: walls mesh + collider
        SetMesh(root, wallsMesh, wallMat);
        MeshCollider col = root.GetComponent<MeshCollider>();
        if (col == null) col = root.AddComponent<MeshCollider>();
        col.sharedMesh = wallsMesh;

        // Floor child
        if (floorMesh != null)
            SetMesh(GetOrCreateChild(root, "Floor"), floorMesh, floorMat);

        // Navmesh child (no renderer, invisible) — always for rooms, only if mesh exists for corridors
        if (navMesh != null || isRoom)
        {
            MeshFilter navMF = GetOrCreateChild(root, "Navmesh").GetComponent<MeshFilter>();
            if (navMF == null) navMF = GetOrCreateChild(root, "Navmesh").AddComponent<MeshFilter>();
            navMF.sharedMesh = navMesh;
        }

        // Room component
        if (isRoom)
        {
            Room room = root.GetComponent<Room>();
            if (room == null) room = root.AddComponent<Room>();
            room.NavFloorMesh = navMesh;
        }

        // Corridor component with auto-detected CorridorType
        if (isCorridor)
        {
            Corridor corridor = root.GetComponent<Corridor>();
            if (corridor == null) corridor = root.AddComponent<Corridor>();
            corridor.Type = DetectCorridorType(prefabName);
        }

        // DoorSocket component on each child of the "Doors" GameObject
        Transform doorsParent = root.transform.Find("Doors");
        if (doorsParent != null)
        {
            foreach (Transform doorChild in doorsParent)
            {
                if (doorChild.GetComponent<DoorSocket>() == null)
                    doorChild.gameObject.AddComponent<DoorSocket>();
            }
        }

        Debug.Log($"[DungeonPrefabSetup] {prefabName} -> {kebabName} (walls: {wallsMesh.name}, floor: {floorMesh?.name ?? "none"}, navmesh: {navMesh?.name ?? "none"})");
        return true;
    }

    // -------------------------------------------------------------------------
    // Fill Corridor Rules
    // Populates CorridorRules on the first DungeonRules asset found.
    // One rule per CorridorType, weight = 1, MaxCount = 0 (unlimited).
    // CorridorProbability is left unchanged so the user controls it.
    // -------------------------------------------------------------------------

    [MenuItem("BulletHeaven/Fill Corridor Prefabs")]
    public static void FillCorridorPrefabs()
    {
        // Find the DungeonRules ScriptableObject
        string[] rulesGuids = AssetDatabase.FindAssets("t:DungeonRules");
        if (rulesGuids.Length == 0)
        {
            Debug.LogError("[DungeonPrefabSetup] No DungeonRules asset found in the project.");
            return;
        }

        string rulesPath = AssetDatabase.GUIDToAssetPath(rulesGuids[0]);
        DungeonRules rules = AssetDatabase.LoadAssetAtPath<DungeonRules>(rulesPath);

        // Collect all Corridor prefabs grouped by CorridorType
        var byType = new Dictionary<CorridorType, List<Corridor>>();
        foreach (CorridorType t in System.Enum.GetValues(typeof(CorridorType)))
            byType[t] = new List<Corridor>();

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsPath });
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            if (!name.StartsWith("Corridor_") && name != "Corridor") continue;

            Corridor corridor = AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<Corridor>();
            if (corridor == null) continue;

            byType[corridor.Type].Add(corridor);
        }

        // Build one CorridorPrefabSet per type that has at least one prefab
        var newSets = new List<CorridorPrefabSet>();
        foreach (var kvp in byType)
        {
            if (kvp.Value.Count == 0) continue;
            newSets.Add(new CorridorPrefabSet
            {
                Type = kvp.Key,
                Prefabs = kvp.Value.ToArray()
            });
        }

        rules.CorridorPrefabs = newSets.ToArray();
        EditorUtility.SetDirty(rules);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DungeonPrefabSetup] Filled {newSets.Count} corridor prefab sets in '{rulesPath}'.");
    }

    // -------------------------------------------------------------------------
    // Fill Corridor Sequences
    // Populates CorridorSequences with sensible default patterns.
    // -------------------------------------------------------------------------

    [MenuItem("BulletHeaven/Fill Corridor Sequences")]
    public static void FillCorridorSequences()
    {
        string[] rulesGuids = AssetDatabase.FindAssets("t:DungeonRules");
        if (rulesGuids.Length == 0)
        {
            Debug.LogError("[DungeonPrefabSetup] No DungeonRules asset found in the project.");
            return;
        }

        string rulesPath = AssetDatabase.GUIDToAssetPath(rulesGuids[0]);
        DungeonRules rules = AssetDatabase.LoadAssetAtPath<DungeonRules>(rulesPath);

        rules.CorridorSequences = new[]
        {
            // Short straight passage
            new CorridorSequence { Pattern = new[] { CorridorType.Straight }, Weight = 3f },

            // Medium straight passage
            new CorridorSequence { Pattern = new[] { CorridorType.Straight, CorridorType.Straight }, Weight = 2f },

            // L-shaped hallway: straight in, corner, straight out
            new CorridorSequence { Pattern = new[] { CorridorType.Straight, CorridorType.Corner, CorridorType.Straight }, Weight = 2f },

            // Branching hallway: straight then a junction (one branch goes elsewhere)
            new CorridorSequence { Pattern = new[] { CorridorType.Straight, CorridorType.Junction }, Weight = 1f },
        };

        EditorUtility.SetDirty(rules);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DungeonPrefabSetup] Filled {rules.CorridorSequences.Length} corridor sequences in '{rulesPath}'.");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    static CorridorType DetectCorridorType(string prefabName)
    {
        string lower = prefabName.ToLower();
        if (lower.Contains("corner")) return CorridorType.Corner;
        if (lower.Contains("intersection")) return CorridorType.Intersection;
        if (lower.Contains("junction")) return CorridorType.Junction;
        if (lower.Contains("end")) return CorridorType.End;
        if (lower.Contains("transition")) return CorridorType.Transition;
        return CorridorType.Straight;
    }

    static void SetMesh(GameObject go, Mesh mesh, Material mat)
    {
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null) mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr == null) mr = go.AddComponent<MeshRenderer>();
        if (mat != null) mr.sharedMaterial = mat;
    }

    static GameObject GetOrCreateChild(GameObject parent, string childName)
    {
        Transform existing = parent.transform.Find(childName);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(childName);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    static Mesh LoadMesh(string fbxPath, string meshName)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (asset is Mesh m && m.name == meshName) return m;
        return null;
    }

    // CorridorWideCorner -> corridor-wide-corner  /  RoomLarge2 -> room-large-2
    static string PascalToKebab(string pascal)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < pascal.Length; i++)
        {
            char c = pascal[i];
            bool insertHyphen = i > 0 && (char.IsUpper(c) || (char.IsDigit(c) && !char.IsDigit(pascal[i - 1])));
            if (insertHyphen) sb.Append('-');
            sb.Append(char.ToLower(c));
        }
        return sb.ToString();
    }
}
