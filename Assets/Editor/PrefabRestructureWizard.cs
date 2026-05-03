using System.Collections.Generic;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public class PrefabRestructureWizard : EditorWindow
{
    private string folderPath = "Assets/Prefabs/Environment";
    private bool dryRun = true;

    [MenuItem("Tools/Prefab Restructure Wizard")]
    public static void Open() => GetWindow<PrefabRestructureWizard>("Prefab Restructure");

    private void OnGUI()
    {
        EditorGUILayout.HelpBox("Restructures each prefab to: [Name] > RootTransform > Mesh", MessageType.Info);
        folderPath = EditorGUILayout.TextField("Folder", folderPath);
        dryRun = EditorGUILayout.Toggle("Dry Run", dryRun);

        if (GUILayout.Button(dryRun ? "Preview" : "Apply"))
            Run();
    }

    private void Run()
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { folderPath });
        int processed = 0;
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (ProcessPrefab(path))
                processed++;
        }
        Debug.Log($"[PrefabRestructure] {(dryRun ? "Would process" : "Processed")} {processed}/{guids.Length} prefab(s).");
    }

    private bool ProcessPrefab(string path)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            if (root.transform.Find("RootTransform") != null)
            {
                Debug.Log($"  Skip (already done): {path}");
                return false;
            }

            Debug.Log($"  {(dryRun ? "[DRY] " : "")}Restructure: {path}");
            if (dryRun)
                return true;

            // Snapshot existing children before we add new ones.
            var existingChildren = new List<Transform>();
            for (int i = 0; i < root.transform.childCount; i++)
                existingChildren.Add(root.transform.GetChild(i));

            // Build new hierarchy.
            var rootTransformGO = new GameObject("RootTransform");
            rootTransformGO.transform.SetParent(root.transform, false);

            var meshGO = new GameObject("Mesh");
            meshGO.transform.SetParent(rootTransformGO.transform, false);

            // Move mesh components from root to Mesh.
            TryMoveComponent<MeshFilter>(root, meshGO);
            TryMoveComponent<MeshRenderer>(root, meshGO);
            TryMoveComponent<MeshCollider>(root, meshGO);
            TryMoveComponent<SkinnedMeshRenderer>(root, meshGO);

            // Re-parent pre-existing children under Mesh.
            foreach (Transform child in existingChildren)
                child.SetParent(meshGO.transform, false);

            PrefabUtility.SaveAsPrefabAsset(root, path);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void TryMoveComponent<T>(GameObject from, GameObject to) where T : Component
    {
        T comp = from.GetComponent<T>();
        if (comp == null) return;
        ComponentUtility.CopyComponent(comp);
        ComponentUtility.PasteComponentAsNew(to);
        Object.DestroyImmediate(comp);
    }
}
