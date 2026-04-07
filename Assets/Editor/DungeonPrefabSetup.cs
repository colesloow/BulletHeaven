using UnityEngine;
using UnityEditor;
using System.IO;

public static class DungeonPrefabSetup
{
    private const string PrefabsPath = "Assets/Prefabs/Rooms";
    private const string MeshesPath = "Assets/Meshes";

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

        // Find FBX by name (case-insensitive on Windows)
        string[] fbxGuids = AssetDatabase.FindAssets(fbxBaseName + " t:Model", new[] { MeshesPath });
        if (fbxGuids.Length == 0)
        {
            Debug.LogWarning($"[DungeonPrefabSetup] No FBX found for '{prefabName}' (searched: {fbxBaseName})");
            return false;
        }

        string fbxPath = AssetDatabase.GUIDToAssetPath(fbxGuids[0]);

        // CorridorWideCorner -> corridor-wide-corner
        string kebabName = PascalToKebab(Path.GetFileNameWithoutExtension(fbxPath));

        Mesh wallsMesh = LoadMesh(fbxPath, kebabName);
        Mesh floorMesh = LoadMesh(fbxPath, kebabName + "-floor");
        Mesh navMesh = LoadMesh(fbxPath, kebabName + "-navmesh");

        if (wallsMesh == null)
        {
            Debug.LogWarning($"[DungeonPrefabSetup] Walls mesh '{kebabName}' not found in {fbxPath}");
            return false;
        }

        using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
        GameObject root = scope.prefabContentsRoot;

        // Root: walls mesh + collider
        SetMesh(root, wallsMesh, wallMat);
        MeshCollider col = root.GetComponent<MeshCollider>();
        if (col == null) col = root.AddComponent<MeshCollider>();
        col.sharedMesh = wallsMesh;

        // Floor child
        bool isRoom = prefabName.StartsWith("Room_");
        if (floorMesh != null)
        {
            GameObject floorGO = GetOrCreateChild(root, "Floor");
            SetMesh(floorGO, floorMesh, floorMat);
        }

        // Navmesh child (no renderer, invisible)
        // Always created for rooms; created for corridors only if mesh exists.
        if (navMesh != null || isRoom)
        {
            GameObject navGO = GetOrCreateChild(root, "Navmesh");
            MeshFilter navMF = navGO.GetComponent<MeshFilter>();
            if (navMF == null) navMF = navGO.AddComponent<MeshFilter>();
            navMF.sharedMesh = navMesh; // null is acceptable if mesh not yet in FBX
        }

        // For Room prefabs: add Room component and assign NavFloorMesh
        if (isRoom)
        {
            Room room = root.GetComponent<Room>();
            if (room == null) room = root.AddComponent<Room>();
            room.NavFloorMesh = navMesh; // null if not yet in FBX
        }

        // Add DoorSocket component to each child of the "Doors" GameObject
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
