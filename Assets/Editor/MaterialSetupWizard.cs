using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using System.Linq;

// EditorWindow that automates material extraction and texture renaming for newly imported assets.
// Run via Tools > Material Setup Wizard.
// FBX files are detected by looking for a staging subfolder (default: "NEW") anywhere under fbxSearchRoot.
// After processing, Step 4 moves FBX out of the staging folder into its parent.
public class MaterialSetupWizard : EditorWindow
{
    private string texturesRoot = "Assets/Textures";
    private string fbxSearchRoot = "Assets/Meshes";
    private string stagingFolderName = "NEW";
    private string materialsRoot = "Assets/Materials";

    private Vector2 scroll;
    private readonly List<string> log = new();
    private bool dryRun = true;

    [MenuItem("Tools/Material Setup Wizard")]
    public static void Open() => GetWindow<MaterialSetupWizard>("Material Setup Wizard");

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Material Setup Wizard", EditorStyles.boldLabel);

        EditorGUILayout.LabelField("Folders", EditorStyles.boldLabel);
        texturesRoot = EditorGUILayout.TextField("Textures root", texturesRoot);
        fbxSearchRoot = EditorGUILayout.TextField("FBX search root", fbxSearchRoot);
        stagingFolderName = EditorGUILayout.TextField("Staging folder name", stagingFolderName);
        materialsRoot = EditorGUILayout.TextField("Materials output", materialsRoot);

        EditorGUILayout.HelpBox(
            "Processes FBX files found inside any '" + stagingFolderName + "' subfolder under FBX search root.\n" +
            "Step 1 — Renames textures (T_ prefix + normalized suffix). Skips files already starting with T_.\n" +
            "Step 2 — Extracts embedded materials from FBX into Materials output. FBX sharing a material all get remapped.\n" +
            "Step 3 — Assigns matching textures to each extracted material.\n" +
            "Step 4 — Moves processed FBX out of the staging folder into its parent.",
            MessageType.Info);

        dryRun = EditorGUILayout.Toggle("Dry Run (preview only)", dryRun);
        EditorGUILayout.Space();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Step 1\nRename Textures", GUILayout.Height(36)))
        {
            log.Clear();
            RenameTextures();
            Repaint();
        }
        if (GUILayout.Button("Step 2\nExtract Materials", GUILayout.Height(36)))
        {
            log.Clear();
            ProcessFBXFiles();
            if (!dryRun) { AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); }
            Repaint();
        }
        if (GUILayout.Button("Step 3\nAssign Textures", GUILayout.Height(36)))
        {
            log.Clear();
            AssignAllTextures();
            if (!dryRun) { AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); }
            Repaint();
        }
        if (GUILayout.Button("Step 4\nMove Out of Staging", GUILayout.Height(36)))
        {
            log.Clear();
            MoveOutOfStaging();
            if (!dryRun) { AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); }
            Repaint();
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Run All Steps", GUILayout.Height(32)))
        {
            log.Clear();
            RenameTextures();
            ProcessFBXFiles();
            AssignAllTextures();
            MoveOutOfStaging();
            if (!dryRun) { AssetDatabase.SaveAssets(); AssetDatabase.Refresh(); }
            Repaint();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Log ({log.Count} lines)", EditorStyles.boldLabel);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        foreach (string line in log)
            EditorGUILayout.LabelField(line, EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();
    }

    // ── Step 1: Rename textures ───────────────────────────────────────────────

    private void RenameTextures()
    {
        Log("=== Step 1: Rename Textures ===");
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { texturesRoot });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileNameNoExt = Path.GetFileNameWithoutExtension(path);

            // Strip all leading T_ prefixes so we can re-evaluate casing and suffix.
            string raw = fileNameNoExt;
            while (raw.StartsWith("T_"))
                raw = raw[2..];

            string cleaned = StripEmbeddedExtension(raw);
            var (baseName, suffix) = ParseTextureName(cleaned);

            string pascalBase = InsertDigitSeparators(ToPascalCase(baseName));
            string newName = suffix != null
                ? $"T_{pascalBase}_{suffix}"
                : $"T_{pascalBase}";

            if (fileNameNoExt == newName) continue;

            Log($"  {fileNameNoExt}  ->  {newName}");

            if (!dryRun)
            {
                string error = AssetDatabase.RenameAsset(path, newName);
                if (!string.IsNullOrEmpty(error))
                    Log($"  ERROR: {error}");
            }
        }

        Log(dryRun ? "(dry run — no changes made)" : "Done.");
    }

    // ── Step 2: Extract and rename materials from FBX ─────────────────────────

    private void ProcessFBXFiles()
    {
        Log("=== Step 2: Extract Materials ===");

        string[] fbxPaths = FindStagingFBX();
        if (fbxPaths.Length == 0)
        {
            Log($"No FBX found inside '{stagingFolderName}' subfolders under {fbxSearchRoot}.");
            return;
        }

        EnsureFolder(materialsRoot);

        foreach (string fbxPath in fbxPaths)
            ProcessFBX(fbxPath);

        Log(dryRun ? "(dry run — no changes made)" : "Done.");
    }

    private void ProcessFBX(string fbxPath)
    {
        var importer = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
        if (importer == null) return;

        var embeddedMaterials = AssetDatabase.LoadAllAssetsAtPath(fbxPath)
            .OfType<Material>()
            .ToArray();

        if (embeddedMaterials.Length == 0) return;

        string fbxName = Path.GetFileNameWithoutExtension(fbxPath);
        Log($"  [{fbxName}]");

        bool remapNeeded = false;

        foreach (Material embedded in embeddedMaterials)
        {
            string matBaseName = embedded.name;
            string matName = matBaseName.StartsWith("MAT_") ? matBaseName : "MAT_" + matBaseName;
            string matPath = $"{materialsRoot}/{matName}.mat";

            // Create the material if it doesn't exist yet.
            if (AssetDatabase.LoadAssetAtPath<Material>(matPath) == null)
            {
                Log($"    Extract: {embedded.name}  ->  {matName}");

                if (!dryRun)
                {
                    var newMat = new Material(embedded);
                    AssetDatabase.CreateAsset(newMat, matPath);
                    AssetDatabase.SaveAssets();
                }
            }
            else
            {
                Log($"    Remap to existing: {matName}");
            }

            // Always remap this FBX to use the external material, even if shared with another FBX.
            if (!dryRun)
            {
                var externalMat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
                if (externalMat != null)
                {
                    importer.AddRemap(new AssetImporter.SourceAssetIdentifier(embedded), externalMat);
                    remapNeeded = true;
                }
                else
                {
                    Log($"    ERROR: could not load {matPath}");
                }
            }
        }

        if (!dryRun && remapNeeded)
            importer.SaveAndReimport();
    }

    // ── Step 3: Assign textures to materials ─────────────────────────────────

    private void AssignAllTextures()
    {
        Log("=== Step 3: Assign Textures to Materials ===");

        if (!AssetDatabase.IsValidFolder(materialsRoot))
        {
            Log("No materials folder found — run Step 2 first.");
            return;
        }

        var textureMap = BuildTextureMap();
        string[] matGuids = AssetDatabase.FindAssets("t:Material", new[] { materialsRoot });

        foreach (string guid in matGuids)
        {
            string matPath = AssetDatabase.GUIDToAssetPath(guid);
            string matFileName = Path.GetFileNameWithoutExtension(matPath);

            string searchKey = matFileName.StartsWith("MAT_")
                ? matFileName[4..]
                : matFileName;

            Log($"  [{matFileName}]  key: {searchKey}");

            var matches = FindMatchingTextures(searchKey, textureMap);
            if (matches.Count == 0) { Log("    No matching textures found."); continue; }

            var mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            if (mat == null) continue;

            foreach (var (texPath, suffix) in matches)
            {
                string texName = Path.GetFileNameWithoutExtension(texPath);
                Log($"    {suffix}  <-  {texName}");

                if (dryRun) continue;

                var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
                if (tex == null) continue;

                if (suffix == "N") EnsureNormalMap(texPath);
                AssignTextureToSlot(mat, suffix, tex);
            }

            if (!dryRun) EditorUtility.SetDirty(mat);
        }

        Log(dryRun ? "(dry run — no changes made)" : "Done.");
    }

    // ── Step 4: Move FBX out of staging folder ────────────────────────────────

    private void MoveOutOfStaging()
    {
        Log("=== Step 4: Move Out of Staging ===");

        string[] fbxPaths = FindStagingFBX();
        if (fbxPaths.Length == 0)
        {
            Log($"No FBX found inside '{stagingFolderName}' subfolders — nothing to move.");
            return;
        }

        string stagingMarker = "/" + stagingFolderName + "/";

        foreach (string src in fbxPaths)
        {
            // Remove the /NEW/ component: Assets/Meshes/Env/NEW/Pipes/I.fbx -> Assets/Meshes/Env/Pipes/I.fbx
            string dst = src.Replace(stagingMarker, "/");

            if (src == dst)
            {
                Log($"  SKIP (path unchanged): {src}");
                continue;
            }

            Log($"  {src}  ->  {dst}");

            if (dryRun) continue;

            string dstDir = Path.GetDirectoryName(dst).Replace('\\', '/');
            EnsureFolder(dstDir);

            string error = AssetDatabase.MoveAsset(src, dst);
            if (!string.IsNullOrEmpty(error))
                Log($"  ERROR: {error}");
        }

        // Remove empty staging subfolders after moving.
        if (!dryRun)
            CleanEmptyStagingFolders();

        Log(dryRun ? "(dry run — no changes made)" : "Done.");
    }

    private void CleanEmptyStagingFolders()
    {
        string[] folderGuids = AssetDatabase.FindAssets("", new[] { fbxSearchRoot });
        var stagingFolders = new List<string>();

        foreach (string guid in folderGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (AssetDatabase.IsValidFolder(path) && Path.GetFileName(path) == stagingFolderName)
                stagingFolders.Add(path);
        }

        // Sort deepest first so subdirs are deleted before parents.
        stagingFolders.Sort((a, b) => b.Length.CompareTo(a.Length));

        foreach (string folder in stagingFolders)
        {
            string[] remaining = AssetDatabase.FindAssets("", new[] { folder });
            if (remaining.Length == 0)
            {
                AssetDatabase.DeleteAsset(folder);
                Log($"  Removed empty folder: {folder}");
            }
            else
            {
                Log($"  Folder not empty, kept: {folder}");
            }
        }
    }

    // ── Texture assignment ────────────────────────────────────────────────────

    private List<(string path, string suffix)> FindMatchingTextures(
        string matBase,
        Dictionary<string, List<(string path, string suffix)>> textureMap)
    {
        string normKey = Normalize(matBase);

        if (textureMap.TryGetValue(normKey, out var exact))
            return exact;

        var results = new List<(string, string)>();
        foreach (var (key, entries) in textureMap)
        {
            if (key.Contains(normKey) || normKey.Contains(key))
                results.AddRange(entries);
        }

        return results;
    }

    private Dictionary<string, List<(string path, string suffix)>> BuildTextureMap()
    {
        var map = new Dictionary<string, List<(string, string)>>();
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { texturesRoot });

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string fileName = Path.GetFileNameWithoutExtension(path);
            string cleaned = StripEmbeddedExtension(fileName);

            var (baseName, suffix) = ParseTextureName(cleaned);
            if (suffix == null || suffix == "MASK") continue;

            string key = Normalize(baseName);
            if (!map.ContainsKey(key))
                map[key] = new List<(string, string)>();

            map[key].Add((path, suffix));
        }

        return map;
    }

    private void AssignTextureToSlot(Material mat, string suffix, Texture2D tex)
    {
        switch (suffix)
        {
            case "BC":
                SetTex(mat, tex, "_BaseMap", "_MainTex");
                break;
            case "N":
                SetTex(mat, tex, "_BumpMap", "_NormalMap");
                break;
            case "M":
                SetTex(mat, tex, "_MetallicGlossMap");
                break;
            case "R":
                SetTex(mat, tex, "_SpecGlossMap");
                Log("    NOTE: roughness assigned to _SpecGlossMap — invert manually if needed.");
                break;
            case "AO":
                SetTex(mat, tex, "_OcclusionMap");
                break;
            case "E":
                SetTex(mat, tex, "_EmissionMap");
                mat.EnableKeyword("_EMISSION");
                mat.globalIlluminationFlags = MaterialGlobalIlluminationFlags.BakedEmissive;
                break;
            case "ORM":
                SetTex(mat, tex, "_MetallicGlossMap");
                Log("    NOTE: ORM packed texture (R=AO, G=Roughness, B=Metallic) assigned to MetallicGlossMap.");
                break;
            case "SPEC":
                SetTex(mat, tex, "_SpecGlossMap");
                break;
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    // Returns all FBX paths that sit inside a subfolder named stagingFolderName.
    private string[] FindStagingFBX()
    {
        if (!AssetDatabase.IsValidFolder(fbxSearchRoot)) return System.Array.Empty<string>();

        string stagingMarker = "/" + stagingFolderName + "/";
        string[] guids = AssetDatabase.FindAssets("t:Model", new[] { fbxSearchRoot });

        return guids
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => p.Contains(stagingMarker))
            .ToArray();
    }

    private static (string baseName, string suffix) ParseTextureName(string name)
    {
        var patterns = new (string[] ends, string suffix)[]
        {
            // Underscore-separated variants are checked first (more explicit).
            // Bare variants (no leading underscore) catch camelCase suffixes like "pipesMetallic".
            (new[] { "_occlusionroughnessmetallic", "_metallicroughness", "_orm",
                     "occlusionroughnessmetallic", "metallicroughness" }, "ORM"),
            (new[] { "_basecolor", "_base_color", "_albedo", "_diffuse", "_bc", "_d", "_color", "_col",
                     "basecolor", "albedo", "diffuse", "color" }, "BC"),
            (new[] { "_normalmap", "_normal", "_nrm", "_nor", "_n",
                     "normalmap", "normal" }, "N"),
            (new[] { "_roughness", "_rgh", "_rough", "_r",
                     "roughness" }, "R"),
            (new[] { "_metallic", "_metal", "_met", "_m",
                     "metallic", "metal" }, "M"),
            (new[] { "_ambient_occlusion", "_ambientocclusion", "_occlusion", "_ao",
                     "ambientocclusion", "occlusion" }, "AO"),
            (new[] { "_emissive", "_emission", "_emit", "_e",
                     "emissive", "emission" }, "E"),
            (new[] { "_specular", "_spec",
                     "specular" }, "SPEC"),
            (new[] { "_mask" }, "MASK"),
        };

        string lower = name.ToLower();
        foreach (var (ends, suf) in patterns)
            foreach (string end in ends)
                if (lower.EndsWith(end))
                    return (name[..^end.Length], suf);

        return (name, null);
    }

    // Strips embedded file extensions left in the filename, e.g. "Foo_D.TGA" -> "Foo_D".
    private static string StripEmbeddedExtension(string fileName)
    {
        string lower = fileName.ToLower();
        string[] extras = { ".tga", ".png", ".jpg", ".jpeg", ".bmp", ".exr", ".hdr", ".psd" };
        foreach (string ext in extras)
            if (lower.EndsWith(ext))
                return fileName[..^ext.Length];
        return fileName;
    }

    // Capitalizes the first letter of each _-/-/ -separated segment, preserves the rest.
    private static string ToPascalCase(string s)
    {
        var sb = new System.Text.StringBuilder();
        foreach (string part in s.Split('_', '-', ' '))
        {
            if (part.Length == 0) continue;
            sb.Append(char.ToUpper(part[0]));
            sb.Append(part[1..]);
        }
        return sb.ToString();
    }

    // Inserts _ before digit runs that directly follow a letter: "Room001" -> "Room_001".
    private static string InsertDigitSeparators(string s)
    {
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < s.Length; i++)
        {
            if (i > 0 && char.IsDigit(s[i]) && char.IsLetter(s[i - 1]))
                sb.Append('_');
            sb.Append(s[i]);
        }
        return sb.ToString();
    }

    private static string Normalize(string s) =>
        s.ToLower().Replace("_", "").Replace(" ", "").Replace("-", "");

    private static void SetTex(Material mat, Texture2D tex, params string[] propertyNames)
    {
        foreach (string prop in propertyNames)
            if (mat.HasProperty(prop)) { mat.SetTexture(prop, tex); return; }
    }

    private static void EnsureNormalMap(string texPath)
    {
        var imp = AssetImporter.GetAtPath(texPath) as TextureImporter;
        if (imp != null && imp.textureType != TextureImporterType.NormalMap)
        {
            imp.textureType = TextureImporterType.NormalMap;
            imp.SaveAndReimport();
        }
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string folder = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, folder);
    }

    private void Log(string message) => log.Add(message);
}
