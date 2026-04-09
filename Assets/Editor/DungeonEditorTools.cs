using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;

// Editor tools for the dungeon system, accessible via the BulletHeaven menu.
// All functions are idempotent: safe to re-run without duplicating data.
public static class DungeonEditorTools
{
    private const string PrefabsPath = "Assets/Prefabs/Rooms";
    private const string MeshesPath  = "Assets/Meshes";

    // -------------------------------------------------------------------------
    // Setup Dungeon Prefabs
    // Assigns meshes, materials, components, and NavFloorMesh on all Room and
    // Corridor prefabs found in PrefabsPath.
    // -------------------------------------------------------------------------

    [MenuItem("BulletHeaven/Setup Dungeon Prefabs")]
    public static void SetupDungeonPrefabs()
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
        Debug.Log($"[DungeonEditorTools] Done. {processed} prefabs processed.");
    }

    static bool SetupPrefab(string prefabPath, string prefabName, Material wallMat, Material floorMat)
    {
        // Corridor_Wide_Corner -> CorridorWideCorner  /  Room_Large -> RoomLarge
        string fbxBaseName = prefabName.Replace("_", "");

        string[] fbxGuids = AssetDatabase.FindAssets(fbxBaseName + " t:Model", new[] { MeshesPath });
        if (fbxGuids.Length == 0)
        {
            Debug.LogWarning($"[DungeonEditorTools] No FBX found for '{prefabName}' (searched: {fbxBaseName})");
            return false;
        }

        string fbxPath   = AssetDatabase.GUIDToAssetPath(fbxGuids[0]);
        string kebabName = PascalToKebab(Path.GetFileNameWithoutExtension(fbxPath));

        Mesh wallsMesh = LoadMesh(fbxPath, kebabName);
        Mesh floorMesh = LoadMesh(fbxPath, kebabName + "-floor");
        Mesh navMesh   = LoadMesh(fbxPath, kebabName + "-navmesh");

        if (wallsMesh == null)
        {
            Debug.LogWarning($"[DungeonEditorTools] Walls mesh '{kebabName}' not found in {fbxPath}");
            return false;
        }

        using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
        GameObject root = scope.prefabContentsRoot;

        bool isRoom     = prefabName.StartsWith("Room_");
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
            corridor.NavFloorMesh = navMesh;
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

        Debug.Log($"[DungeonEditorTools] {prefabName} -> {kebabName} (walls: {wallsMesh.name}, floor: {floorMesh?.name ?? "none"}, navmesh: {navMesh?.name ?? "none"})");
        return true;
    }

    // -------------------------------------------------------------------------
    // Fill Room Prefabs
    // Populates RoomRules on the DungeonRules asset with all Room prefabs found,
    // grouped by RoomType. Weight = 1, MaxCount = 0 (unlimited) by default.
    // -------------------------------------------------------------------------

    [MenuItem("BulletHeaven/Fill Room Prefabs")]
    public static void FillRoomPrefabs()
    {
        DungeonRules rules = LoadDungeonRules();
        if (rules == null) return;

        // Collect all Room prefabs grouped by RoomType
        var byType = new Dictionary<RoomType, List<Room>>();
        foreach (RoomType t in System.Enum.GetValues(typeof(RoomType)))
            byType[t] = new List<Room>();

        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabsPath });
        foreach (string guid in prefabGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string name = Path.GetFileNameWithoutExtension(path);
            if (!name.StartsWith("Room_")) continue;

            Room room = AssetDatabase.LoadAssetAtPath<GameObject>(path)?.GetComponent<Room>();
            if (room == null) continue;

            byType[DetectRoomType(name)].Add(room);
        }

        // Build one RoomRule per type that has at least one prefab
        var newRules = new List<RoomRule>();
        foreach (var kvp in byType)
        {
            if (kvp.Value.Count == 0) continue;
            newRules.Add(new RoomRule
            {
                Type     = kvp.Key,
                Prefabs  = kvp.Value.ToArray(),
                Weight   = 1f,
                MaxCount = 0
            });
        }

        rules.RoomRules = newRules.ToArray();
        EditorUtility.SetDirty(rules);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DungeonEditorTools] Filled {newRules.Count} room rules in '{AssetDatabase.GetAssetPath(rules)}'.");
    }

    // -------------------------------------------------------------------------
    // Fill Corridor Prefabs
    // Populates CorridorPrefabs on the DungeonRules asset, one set per CorridorType.
    // -------------------------------------------------------------------------

    [MenuItem("BulletHeaven/Fill Corridor Prefabs")]
    public static void FillCorridorPrefabs()
    {
        DungeonRules rules = LoadDungeonRules();
        if (rules == null) return;

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
                Type    = kvp.Key,
                Prefabs = kvp.Value.ToArray()
            });
        }

        rules.CorridorPrefabs = newSets.ToArray();
        EditorUtility.SetDirty(rules);
        AssetDatabase.SaveAssets();

        Debug.Log($"[DungeonEditorTools] Filled {newSets.Count} corridor prefab sets in '{AssetDatabase.GetAssetPath(rules)}'.");
    }

    // -------------------------------------------------------------------------
    // Fill Corridor Sequences
    // Populates CorridorSequences with sensible default patterns.
    // -------------------------------------------------------------------------

    [MenuItem("BulletHeaven/Fill Corridor Sequences")]
    public static void FillCorridorSequences()
    {
        DungeonRules rules = LoadDungeonRules();
        if (rules == null) return;

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

        Debug.Log($"[DungeonEditorTools] Filled {rules.CorridorSequences.Length} corridor sequences in '{AssetDatabase.GetAssetPath(rules)}'.");
    }

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    // Loads the first DungeonRules asset found in the project.
    // Logs an error and returns null if none is found.
    static DungeonRules LoadDungeonRules()
    {
        string[] guids = AssetDatabase.FindAssets("t:DungeonRules");
        if (guids.Length == 0)
        {
            Debug.LogError("[DungeonEditorTools] No DungeonRules asset found in the project.");
            return null;
        }
        return AssetDatabase.LoadAssetAtPath<DungeonRules>(AssetDatabase.GUIDToAssetPath(guids[0]));
    }

    // Detects RoomType from a prefab name by looking for keywords.
    // Defaults to Normal if no keyword matches (covers all geometry-only variants:
    // Room_Large, Room_Large_2, Room_Small, Room_Wide, etc.).
    static RoomType DetectRoomType(string prefabName)
    {
        string lower = prefabName.ToLower();
        if (lower.Contains("treasure")) return RoomType.Treasure;
        if (lower.Contains("arena"))    return RoomType.Arena;
        if (lower.Contains("trap"))     return RoomType.Trap;
        if (lower.Contains("event"))    return RoomType.Event;
        return RoomType.Normal;
    }

    // Detects CorridorType from a prefab name by looking for keywords.
    // Defaults to Straight if no keyword matches.
    static CorridorType DetectCorridorType(string prefabName)
    {
        string lower = prefabName.ToLower();
        if (lower.Contains("corner"))       return CorridorType.Corner;
        if (lower.Contains("intersection")) return CorridorType.Intersection;
        if (lower.Contains("junction"))     return CorridorType.Junction;
        if (lower.Contains("end"))          return CorridorType.End;
        if (lower.Contains("transition"))   return CorridorType.Transition;
        return CorridorType.Straight;
    }

    // Assigns a mesh and material to a GameObject's MeshFilter and MeshRenderer.
    // Adds the components if they don't already exist.
    static void SetMesh(GameObject go, Mesh mesh, Material mat)
    {
        MeshFilter mf = go.GetComponent<MeshFilter>();
        if (mf == null) mf = go.AddComponent<MeshFilter>();
        mf.sharedMesh = mesh;

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr == null) mr = go.AddComponent<MeshRenderer>();
        if (mat != null) mr.sharedMaterial = mat;
    }

    // Returns an existing child GameObject by name, or creates it under parent.
    static GameObject GetOrCreateChild(GameObject parent, string childName)
    {
        Transform existing = parent.transform.Find(childName);
        if (existing != null) return existing.gameObject;
        var go = new GameObject(childName);
        go.transform.SetParent(parent.transform, false);
        return go;
    }

    // Loads a sub-asset Mesh by name from an FBX file.
    // Returns null if no mesh with that name exists in the asset.
    static Mesh LoadMesh(string fbxPath, string meshName)
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
            if (asset is Mesh m && m.name == meshName) return m;
        return null;
    }

    // Converts a PascalCase name to kebab-case.
    // Inserts a hyphen before uppercase letters and before digits preceded by a non-digit.
    // Examples: CorridorWideCorner -> corridor-wide-corner  /  RoomLarge2 -> room-large-2
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
