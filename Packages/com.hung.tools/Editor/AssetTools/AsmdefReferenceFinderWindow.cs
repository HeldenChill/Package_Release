#if UNITY_EDITOR
using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEditorInternal;

[Serializable]
public class AsmdefJson
{
    public string name;
    public string[] references;
}

[Serializable]
public class AsmrefJson
{
    public string reference;
}


public class AsmReferenceFinderWindow : EditorWindow
{
    [Serializable]
    private enum SearchMode
    {
        All,
        AsmdefOnly,
        AsmrefOnly
    }
    private AssemblyDefinitionAsset targetAsmdef;
    private Vector2 scroll;
    private SearchMode searchMode = SearchMode.All;
    private bool includeSearchAssembly = false;
    private GUIStyle scrollStyle;

    private readonly List<ResultItem> assemblyResults = new();
    private readonly List<UnityEngine.Object> parentFolders = new();
    private class ResultItem
    {
        public string type;   // asmdef / asmref
        public string display;
        public string path;
        public UnityEngine.Object asset;
        public UnityEngine.Object parentFolder;

    }
    [MenuItem("Tools/PetVsMonster/Maintenance/Assembly Definition/Find References Deep")]
    public static void Open()
    {
        GetWindow<AsmReferenceFinderWindow>("Asm Ref Finder");
    }

    private void OnGUI()
    {
        scrollStyle ??= new GUIStyle(GUI.skin.scrollView)
        {
            padding = new RectOffset(0, 0, 0, 0), // 👈 giảm top/bottom
            margin = new RectOffset(0, 0, 0, 0)   // 👈 giảm spacing giữa item
        };
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Find references to Assembly Definition", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        targetAsmdef = (AssemblyDefinitionAsset)EditorGUILayout.ObjectField(
            "Target Assembly",
            targetAsmdef,
            typeof(AssemblyDefinitionAsset),
            false);

        searchMode = (SearchMode)EditorGUILayout.EnumPopup("Search Mode", searchMode);
        includeSearchAssembly = EditorGUILayout.ToggleLeft(
                    "Include Base Assembly",
                    includeSearchAssembly);
        EditorGUILayout.Space();



        EditorGUILayout.Space();
        EditorGUILayout.LabelField($"Assembly Results ({assemblyResults.Count})", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll, scrollStyle);

        if (assemblyResults.Count == 0)
        {
            EditorGUILayout.HelpBox("No results.", MessageType.Info);
        }
        else
        {
            foreach (var r in assemblyResults)
            {
                using (new EditorGUILayout.HorizontalScope("box"))
                {
                    GUILayout.Label(r.type, GUILayout.Width(55));
                    using (new EditorGUILayout.VerticalScope())
                    {
                        DrawField(r.asset);
                        DrawField(r.parentFolder);
                    }
                }
            }
        }

        EditorGUILayout.EndScrollView();
        using (new EditorGUI.DisabledScope(targetAsmdef == null))
        {
            if (GUILayout.Button("Find References", GUILayout.Height(28)))
            {
                FindReferences();
            }
            if (GUILayout.Button("Find Parent Folder", GUILayout.Height(28)))
            {
                FindParentFolder();
            }
        }
    }
    private void DrawField(UnityEngine.Object obj)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUI.ObjectField(rect, obj, typeof(UnityEngine.Object), false);
        }

        HandleDoubleClick(rect, obj);
    }
    private void FindParentFolder()
    {
        parentFolders.Clear();
        HashSet<string> folders = GetImmediateParentFolders(assemblyResults);
        List<UnityEngine.Object> folderObjects = GetFolderObjects(folders);
        if (folderObjects.Count == 0)
        {
            EditorUtility.DisplayDialog("No Parent Folders", "No parent folders found for the results.", "OK");
            return;
        }
        else
        {
            Selection.objects = folderObjects.ToArray();
            for (int i = 0; i < assemblyResults.Count; i++)
            {
                assemblyResults[i].parentFolder = folderObjects[i];
            }

            foreach (var obj in folderObjects)
            {
                parentFolders.Add(obj);
            }
        }
    }

    private HashSet<string> GetImmediateParentFolders(List<ResultItem> results)
    {
        HashSet<string> folders = new HashSet<string>();

        foreach (var r in results)
        {
            if (string.IsNullOrEmpty(r.path))
                continue;

            string parent = Path.GetDirectoryName(r.path)?.Replace("\\", "/");

            if (!string.IsNullOrEmpty(parent) && parent.StartsWith("Assets"))
            {
                folders.Add(parent);
            }
        }

        return folders;
    }
    private List<UnityEngine.Object> GetFolderObjects(HashSet<string> folders)
    {
        List<UnityEngine.Object> objs = new List<UnityEngine.Object>();

        foreach (var path in folders)
        {
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj != null)
            {
                objs.Add(obj);
            }
        }

        return objs;
    }
    private void HandleDoubleClick(Rect rect, UnityEngine.Object obj)
    {
        Event e = Event.current;

        if (e.type == EventType.MouseDown &&
            e.button == 0 &&
            e.clickCount == 2 &&
            rect.Contains(e.mousePosition))
        {
            if (obj != null)
            {
                AssetDatabase.OpenAsset(obj);
                e.Use(); // 👈 rất quan trọng để tránh event leak
            }
        }
    }

    private void FindReferences()
    {
        parentFolders.Clear();
        assemblyResults.Clear();

        string targetPath = AssetDatabase.GetAssetPath(targetAsmdef);
        if (string.IsNullOrEmpty(targetPath))
        {
            Debug.LogError("Invalid target asmdef path.");
            return;
        }

        string targetGuid = AssetDatabase.AssetPathToGUID(targetPath);
        var targetAsmdefJson = ReadJson<AsmdefJson>(targetPath);

        if (targetAsmdefJson == null || string.IsNullOrEmpty(targetAsmdefJson.name))
        {
            Debug.LogError("Cannot read target asmdef name.");
            return;
        }
        if (includeSearchAssembly)
        {
            assemblyResults.Add(new ResultItem
            {
                type = "asmdef",
                display = targetAsmdefJson.name,
                path = targetPath,
                asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(targetPath)
            });
        }
        string targetName = targetAsmdefJson.name;
        string targetGuidRef = $"GUID:{targetGuid}";

        // Chỉ tìm asmdef khi checkbox không được bật
        switch (searchMode)
        {
            case SearchMode.All:
                FindAsrmdef(targetPath, targetName, targetGuidRef);
                FindAsmref(targetName, targetGuidRef);
                break; // Tìm cả 2
            case SearchMode.AsmdefOnly:
                FindAsrmdef(targetPath, targetName, targetGuidRef);
                break;
            case SearchMode.AsmrefOnly:
                FindAsmref(targetName, targetGuidRef);
                break;
            default:
                FindAsrmdef(targetPath, targetName, targetGuidRef);
                FindAsmref(targetName, targetGuidRef);
                break;
        }
        assemblyResults.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.Ordinal));
        Repaint();
    }

    private void FindAsrmdef(string targetPath, string targetName, string targetGuidRef)
    {
        string[] asmdefGuids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset");
        foreach (string guid in asmdefGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == targetPath)
                continue;

            var json = ReadJson<AsmdefJson>(path);
            if (json?.references == null || json.references.Length == 0)
                continue;

            bool match = json.references.Any(r =>
                string.Equals(r, targetName, StringComparison.Ordinal) ||
                string.Equals(r, targetGuidRef, StringComparison.Ordinal));

            if (match)
            {
                assemblyResults.Add(new ResultItem
                {
                    type = "asmdef",
                    display = string.IsNullOrEmpty(json.name)
                        ? Path.GetFileNameWithoutExtension(path)
                        : json.name,
                    path = path,
                    asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)
                });
            }
        }
    }

    private void FindAsmref(string targetName, string targetGuidRef)
    {
        string[] allPaths = AssetDatabase.GetAllAssetPaths();
        foreach (string path in allPaths)
        {
            if (!path.EndsWith(".asmref", StringComparison.OrdinalIgnoreCase))
                continue;

            var json = ReadJson<AsmrefJson>(path);
            if (json == null || string.IsNullOrEmpty(json.reference))
                continue;

            bool match =
                string.Equals(json.reference, targetName, StringComparison.Ordinal) ||
                string.Equals(json.reference, targetGuidRef, StringComparison.Ordinal);

            if (match)
            {
                assemblyResults.Add(new ResultItem
                {
                    type = "asmref",
                    display = Path.GetFileNameWithoutExtension(path),
                    path = path,
                    asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path)
                });
            }
        }
    }


    private static T ReadJson<T>(string assetPath) where T : class
    {
        try
        {
            string fullPath = Path.GetFullPath(assetPath);
            if (!File.Exists(fullPath))
                return null;

            string json = File.ReadAllText(fullPath);
            return JsonUtility.FromJson<T>(json);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed reading {assetPath}\n{e}");
            return null;
        }
    }
}
#endif
