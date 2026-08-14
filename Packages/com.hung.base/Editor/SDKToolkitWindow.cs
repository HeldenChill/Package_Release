using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

public sealed class SDKToolkitWindow : EditorWindow
{
    private const string DefaultConfigPath =
        "Assets/_Game/_Base/Tools/SDKPackager/sdk-package-config.json";

    private const string DefaultJsonExportFolder =
        "Assets/_Game/_Base/Tools/SDKPackager";

    private string configPath = DefaultConfigPath;
    private TextAsset configJsonAsset;

    private SDKPackageConfig config;

    private readonly List<SDKFolderRow> folderRows = new List<SDKFolderRow>();
    private readonly List<AssetPathRow> asmdefReferenceRows = new List<AssetPathRow>();
    private readonly List<AssetPathRow> asmdefParentFolderRows = new List<AssetPathRow>();

    private Vector2 folderScroll;
    private Vector2 jsonPreviewScroll;
    private Vector2 asmdefReferenceScroll;
    private Vector2 asmdefParentScroll;

    private int selectedTab;

    private bool autoAddProjectSelection = true;
    private string lastAutoAddedSelectionSignature = string.Empty;

    private string jsonExportPath =
        DefaultJsonExportFolder + "/ThirdPartySDKs.json";

    private AssemblyDefinitionAsset targetAsmdef;
    private AsmdefSearchMode asmdefSearchMode = AsmdefSearchMode.All;
    private bool includeTargetAssembly;

    private bool suppressNamePathBinding;

    [MenuItem("Tools/SDK Tools/SDK Toolkit")]
    public static void Open()
    {
        GetWindow<SDKToolkitWindow>("SDK Toolkit");
    }

    private void OnEnable()
    {
        Selection.selectionChanged += OnUnitySelectionChanged;

        SyncConfigAssetFromPath();
        LoadConfig();
    }

    private void OnDisable()
    {
        Selection.selectionChanged -= OnUnitySelectionChanged;
    }

    private void OnInspectorUpdate()
    {
        if (!autoAddProjectSelection)
            return;

        TryAutoAddSelectedFoldersFromProjectWindow();
    }

    private void OnGUI()
    {
        DrawHeader();

        EditorGUILayout.Space(8);

        selectedTab = GUILayout.Toolbar(
            selectedTab,
            new[]
            {
                "Package Export",
                "JSON Export",
                "Asmdef Finder"
            }
        );

        EditorGUILayout.Space(8);

        switch (selectedTab)
        {
            case 0:
                DrawPackageExportTab();
                break;

            case 1:
                DrawJsonExportTab();
                break;

            case 2:
                DrawAsmdefFinderTab();
                break;
        }
    }

    private void DrawHeader()
    {
        EditorGUILayout.LabelField("SDK Toolkit", EditorStyles.boldLabel);

        EditorGUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();

        EditorGUI.BeginChangeCheck();

        TextAsset newConfigAsset = (TextAsset)EditorGUILayout.ObjectField(
            "Config JSON",
            configJsonAsset,
            typeof(TextAsset),
            false
        );

        if (EditorGUI.EndChangeCheck())
        {
            SetConfigAsset(newConfigAsset);
        }

        if (GUILayout.Button("Load", GUILayout.Width(70)))
        {
            LoadConfig();
        }

        if (GUILayout.Button("Create Default", GUILayout.Width(110)))
        {
            CreateDefaultConfig();
        }

        EditorGUILayout.EndHorizontal();

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Loaded Path", configPath);
        }

        if (config == null)
        {
            EditorGUILayout.HelpBox("Config is not loaded.", MessageType.Warning);
            return;
        }

        EditorGUILayout.Space(4);

        EditorGUI.BeginChangeCheck();

        string newPackageName = EditorGUILayout.TextField("Package Name", config.packageName);

        if (EditorGUI.EndChangeCheck())
        {
            SetPackageNameFromEditor(newPackageName);
        }

        EditorGUI.BeginChangeCheck();

        string newVersion = EditorGUILayout.TextField("Version", config.version);

        if (EditorGUI.EndChangeCheck())
        {
            config.version = NormalizeVersion(newVersion);
        }

        EditorGUI.BeginChangeCheck();

        string newOutputFolder = EditorGUILayout.TextField("Output Folder", config.outputFolder);

        if (EditorGUI.EndChangeCheck())
        {
            config.outputFolder = NormalizeUnityPath(newOutputFolder).TrimEnd('/');
        }

        EditorGUI.BeginChangeCheck();

        string newSdkRootFolder = EditorGUILayout.TextField("SDK Root Folder", config.sdkRootFolder);

        if (EditorGUI.EndChangeCheck())
        {
            config.sdkRootFolder = NormalizeUnityPath(newSdkRootFolder).TrimEnd('/');
        }

        config.includeDependencies = EditorGUILayout.Toggle(
            "Include Dependencies",
            config.includeDependencies
        );
    }

    private void DrawPackageExportTab()
    {
        if (config == null)
            return;

        EditorGUILayout.Space(6);

        autoAddProjectSelection = EditorGUILayout.ToggleLeft(
            "Auto add selected folders from Project Window",
            autoAddProjectSelection
        );

        EditorGUILayout.HelpBox(
            "Khi bật, nếu bạn chọn folder trong Project Window, folder đó sẽ tự nhảy vào danh sách bên dưới. Nếu folder cha đã tồn tại, folder con sẽ bị bỏ qua.",
            MessageType.Info
        );

        EditorGUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Scan From Config", GUILayout.Height(30)))
        {
            ScanFromConfig();
        }

        if (GUILayout.Button("Add Project Selection", GUILayout.Height(30)))
        {
            AddSelectedFoldersFromProjectWindow();
        }

        if (GUILayout.Button("Clean Duplicates", GUILayout.Height(30)))
        {
            CleanParentChildDuplicates();
        }

        if (GUILayout.Button("Clear", GUILayout.Height(30), GUILayout.Width(70)))
        {
            folderRows.Clear();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        using (new EditorGUI.DisabledScope(GetIncludedExistingFolderPaths().Count == 0))
        {
            if (GUILayout.Button("Export .unitypackage", GUILayout.Height(36)))
            {
                ExportUnityPackage();
            }
        }

        EditorGUILayout.Space(8);

        DrawFolderList();
    }

    private void DrawJsonExportTab()
    {
        if (config == null)
            return;

        EditorGUILayout.Space(6);

        EditorGUI.BeginChangeCheck();

        string newJsonExportPath = EditorGUILayout.TextField("JSON Output Path", jsonExportPath);

        if (EditorGUI.EndChangeCheck())
        {
            SetJsonExportPathFromEditor(newJsonExportPath);
        }

        EditorGUILayout.Space(6);

        EditorGUILayout.HelpBox(
            "Package Name và JSON Output Path được bind 2 chiều. Sửa Package Name sẽ đổi tên file JSON export. Sửa tên file JSON export sẽ cập nhật lại Package Name.",
            MessageType.Info
        );

        EditorGUILayout.BeginHorizontal();

        using (new EditorGUI.DisabledScope(GetIncludedExistingFolderPaths().Count == 0))
        {
            if (GUILayout.Button("Export Checked Folders To Config JSON", GUILayout.Height(30)))
            {
                ExportCheckedFoldersToConfigJson();
            }
        }

        using (new EditorGUI.DisabledScope(Selection.assetGUIDs == null || Selection.assetGUIDs.Length == 0))
        {
            if (GUILayout.Button("Export Current Project Selection To Config JSON", GUILayout.Height(30)))
            {
                ExportCurrentProjectSelectionToConfigJson();
            }
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        DrawJsonPreview();
    }

    private void DrawAsmdefFinderTab()
    {
        if (config == null)
            return;

        EditorGUILayout.Space(6);

        EditorGUILayout.LabelField("Find .asmdef / .asmref References", EditorStyles.boldLabel);

        targetAsmdef = (AssemblyDefinitionAsset)EditorGUILayout.ObjectField(
            "Target Assembly",
            targetAsmdef,
            typeof(AssemblyDefinitionAsset),
            false
        );

        asmdefSearchMode = (AsmdefSearchMode)EditorGUILayout.EnumPopup(
            "Search Mode",
            asmdefSearchMode
        );

        includeTargetAssembly = EditorGUILayout.ToggleLeft(
            "Include Target Assembly",
            includeTargetAssembly
        );

        string targetAssemblyName = GetAssemblyNameFromAsset(targetAsmdef);

        using (new EditorGUI.DisabledScope(true))
        {
            EditorGUILayout.TextField("Resolved Name", targetAssemblyName);
        }

        EditorGUILayout.Space(6);

        EditorGUILayout.BeginHorizontal();

        using (new EditorGUI.DisabledScope(targetAsmdef == null || string.IsNullOrWhiteSpace(targetAssemblyName)))
        {
            if (GUILayout.Button("Find References", GUILayout.Height(30)))
            {
                FindAsmdefReferences();
            }
        }

        using (new EditorGUI.DisabledScope(asmdefReferenceRows.Count == 0))
        {
            if (GUILayout.Button("Find Parent Folders", GUILayout.Height(30)))
            {
                FindParentFoldersFromAsmdefReferences();
            }
        }

        using (new EditorGUI.DisabledScope(asmdefParentFolderRows.Count == 0))
        {
            if (GUILayout.Button("Add Parent Folders To Package List", GUILayout.Height(30)))
            {
                AddAsmdefParentFoldersToPackageList();
            }
        }

        if (GUILayout.Button("Clear", GUILayout.Height(30), GUILayout.Width(70)))
        {
            asmdefReferenceRows.Clear();
            asmdefParentFolderRows.Clear();
        }

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        DrawAsmdefReferenceList();
        EditorGUILayout.Space(8);
        DrawAsmdefParentFolderList();
    }

    private void DrawFolderList()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Show Folders", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();

        int includedCount = folderRows.Count(x => x.exists && x.include);
        int totalExistingCount = folderRows.Count(x => x.exists);

        EditorGUILayout.LabelField(
            "Included: " + includedCount + "/" + totalExistingCount,
            GUILayout.Width(120)
        );

        EditorGUILayout.EndHorizontal();

        if (folderRows.Count == 0)
        {
            EditorGUILayout.HelpBox("No folders added yet.", MessageType.None);
            return;
        }

        folderScroll = EditorGUILayout.BeginScrollView(
            folderScroll,
            GUILayout.MinHeight(220),
            GUILayout.MaxHeight(420)
        );

        for (int i = 0; i < folderRows.Count; i++)
        {
            DrawFolderRow(folderRows[i], i);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawFolderRow(SDKFolderRow row, int index)
    {
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();

        row.include = EditorGUILayout.Toggle(row.include, GUILayout.Width(20));

        if (row.exists)
        {
            DefaultAsset currentAsset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(row.assetPath);

            EditorGUI.BeginChangeCheck();

            UnityEngine.Object newAsset = EditorGUILayout.ObjectField(
                currentAsset,
                typeof(DefaultAsset),
                false
            );

            if (EditorGUI.EndChangeCheck())
            {
                TryReplaceRowAsset(row, newAsset);
            }
        }
        else
        {
            EditorGUILayout.LabelField("Missing: " + row.folderName);
        }

        if (GUILayout.Button("X", GUILayout.Width(24)))
        {
            folderRows.RemoveAt(index);
            GUIUtility.ExitGUI();
        }

        EditorGUILayout.EndHorizontal();

        if (!row.exists)
        {
            EditorGUILayout.HelpBox(
                "Folder '" + row.folderName + "' was not found.",
                MessageType.Warning
            );
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawJsonPreview()
    {
        List<string> paths = GetIncludedExistingFolderPaths();

        EditorGUILayout.LabelField("Checked Folder Preview", EditorStyles.boldLabel);

        if (paths.Count == 0)
        {
            EditorGUILayout.HelpBox("No checked folders.", MessageType.None);
            return;
        }

        jsonPreviewScroll = EditorGUILayout.BeginScrollView(
            jsonPreviewScroll,
            GUILayout.MinHeight(180),
            GUILayout.MaxHeight(360)
        );

        foreach (string path in paths)
        {
            DefaultAsset asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(path);

            EditorGUILayout.ObjectField(
                asset,
                typeof(DefaultAsset),
                false
            );
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawAsmdefReferenceList()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Reference Results", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField(
            asmdefReferenceRows.Count.ToString(),
            GUILayout.Width(60)
        );

        EditorGUILayout.EndHorizontal();

        if (asmdefReferenceRows.Count == 0)
        {
            EditorGUILayout.HelpBox("No asmdef/asmref references found yet.", MessageType.None);
            return;
        }

        asmdefReferenceScroll = EditorGUILayout.BeginScrollView(
            asmdefReferenceScroll,
            GUILayout.MinHeight(160),
            GUILayout.MaxHeight(260)
        );

        foreach (AssetPathRow row in asmdefReferenceRows)
        {
            EditorGUILayout.BeginHorizontal();

            row.include = EditorGUILayout.Toggle(row.include, GUILayout.Width(20));

            UnityEngine.Object asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(row.assetPath);

            EditorGUILayout.ObjectField(
                asset,
                typeof(UnityEngine.Object),
                false
            );

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawAsmdefParentFolderList()
    {
        EditorGUILayout.BeginHorizontal();

        EditorGUILayout.LabelField("Parent Folders", EditorStyles.boldLabel);

        GUILayout.FlexibleSpace();

        EditorGUILayout.LabelField(
            asmdefParentFolderRows.Count.ToString(),
            GUILayout.Width(60)
        );

        EditorGUILayout.EndHorizontal();

        if (asmdefParentFolderRows.Count == 0)
        {
            EditorGUILayout.HelpBox("No parent folders found yet.", MessageType.None);
            return;
        }

        asmdefParentScroll = EditorGUILayout.BeginScrollView(
            asmdefParentScroll,
            GUILayout.MinHeight(140),
            GUILayout.MaxHeight(240)
        );

        foreach (AssetPathRow row in asmdefParentFolderRows)
        {
            EditorGUILayout.BeginHorizontal();

            row.include = EditorGUILayout.Toggle(row.include, GUILayout.Width(20));

            DefaultAsset asset = AssetDatabase.LoadAssetAtPath<DefaultAsset>(row.assetPath);

            EditorGUILayout.ObjectField(
                asset,
                typeof(DefaultAsset),
                false
            );

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void SetConfigAsset(TextAsset asset)
    {
        if (asset == null)
        {
            configJsonAsset = null;
            configPath = string.Empty;
            config = null;
            folderRows.Clear();
            return;
        }

        string path = AssetDatabase.GetAssetPath(asset);
        path = NormalizeUnityPath(path);

        if (!IsJsonAssetPath(path))
        {
            EditorUtility.DisplayDialog(
                "Invalid Config File",
                "Please assign a .json file.",
                "OK"
            );

            SyncConfigAssetFromPath();
            return;
        }

        configJsonAsset = asset;
        configPath = path;

        LoadConfig();
    }

    private void SyncConfigAssetFromPath()
    {
        if (string.IsNullOrWhiteSpace(configPath))
        {
            configJsonAsset = null;
            return;
        }

        configJsonAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(configPath);
    }

    private void LoadConfig()
    {
        folderRows.Clear();

        if (string.IsNullOrWhiteSpace(configPath))
        {
            config = null;
            Debug.LogWarning("[SDKToolkit] Config path is empty.");
            return;
        }

        if (!IsJsonAssetPath(configPath))
        {
            config = null;
            Debug.LogWarning("[SDKToolkit] Config is not a JSON file: " + configPath);
            return;
        }

        if (!File.Exists(configPath))
        {
            config = null;
            Debug.LogWarning("[SDKToolkit] Config not found: " + configPath);
            return;
        }

        string json = File.ReadAllText(configPath);
        config = JsonUtility.FromJson<SDKPackageConfig>(json);

        if (config == null)
        {
            Debug.LogError("[SDKToolkit] Failed to parse config.");
            return;
        }

        NormalizeConfig();
        SyncConfigAssetFromPath();
        BindJsonOutputPathFromPackageName();

        Debug.Log("[SDKToolkit] Loaded config: " + configPath);
    }

    private void NormalizeConfig()
    {
        if (config == null)
            return;

        config.packageName = string.IsNullOrWhiteSpace(config.packageName)
            ? "ThirdPartySDKs"
            : config.packageName.Trim();

        config.version = NormalizeVersion(config.version);

        config.outputFolder = string.IsNullOrWhiteSpace(config.outputFolder)
            ? "SDKBuilds"
            : NormalizeUnityPath(config.outputFolder.Trim()).TrimEnd('/');

        config.sdkRootFolder = NormalizeUnityPath(config.sdkRootFolder);

        if (!string.IsNullOrWhiteSpace(config.sdkRootFolder))
            config.sdkRootFolder = config.sdkRootFolder.TrimEnd('/');

        if (config.sdkFolderNames == null)
            config.sdkFolderNames = Array.Empty<string>();

        if (config.sdkFolderPaths == null)
            config.sdkFolderPaths = Array.Empty<string>();

        if (config.excludePathContains == null)
            config.excludePathContains = Array.Empty<string>();
    }

    private void CreateDefaultConfig()
    {
        string directory = Path.GetDirectoryName(DefaultConfigPath);

        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        SDKPackageConfig defaultConfig = new SDKPackageConfig
        {
            packageName = "ThirdPartySDKs",
            version = "1.0.0",
            outputFolder = "SDKBuilds",
            includeDependencies = false,
            sdkRootFolder = "Assets",
            sdkFolderNames = new[]
            {
                "LevelPlay",
                "MaxSdk",
                "IronSource",
                "AppsFlyer",
                "Firebase",
                "ExternalDependencyManager"
            },
            sdkFolderPaths = Array.Empty<string>(),
            excludePathContains = new[]
            {
                "/Demo/",
                "/Demos/",
                "/Example/",
                "/Examples/",
                "/Sample/",
                "/Samples/",
                "/Samples~/"
            }
        };

        string json = JsonUtility.ToJson(defaultConfig, true);
        File.WriteAllText(DefaultConfigPath, json);

        AssetDatabase.Refresh();

        configPath = DefaultConfigPath;
        SyncConfigAssetFromPath();
        LoadConfig();
    }

    private void SetPackageNameFromEditor(string newPackageName)
    {
        if (config == null)
            return;

        config.packageName = string.IsNullOrWhiteSpace(newPackageName)
            ? "ThirdPartySDKs"
            : newPackageName.Trim();

        BindJsonOutputPathFromPackageName();
    }

    private void SetJsonExportPathFromEditor(string newPath)
    {
        if (config == null)
            return;

        if (suppressNamePathBinding)
            return;

        suppressNamePathBinding = true;

        jsonExportPath = NormalizeUnityPath(newPath).Trim();

        if (!jsonExportPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            jsonExportPath += ".json";
        }

        string fileName = Path.GetFileNameWithoutExtension(jsonExportPath);

        if (!string.IsNullOrWhiteSpace(fileName))
        {
            config.packageName = fileName.Trim();
        }

        suppressNamePathBinding = false;
    }

    private void BindJsonOutputPathFromPackageName()
    {
        if (config == null)
            return;

        if (suppressNamePathBinding)
            return;

        suppressNamePathBinding = true;

        string directory = GetCurrentJsonExportDirectory();
        string safeName = MakeSafeFileName(config.packageName);

        if (string.IsNullOrWhiteSpace(safeName))
        {
            safeName = "ThirdPartySDKs";
        }

        jsonExportPath = NormalizeUnityPath(Path.Combine(directory, safeName + ".json"));

        suppressNamePathBinding = false;
    }

    private string GetCurrentJsonExportDirectory()
    {
        string directory = Path.GetDirectoryName(jsonExportPath);

        if (string.IsNullOrWhiteSpace(directory))
        {
            directory = DefaultJsonExportFolder;
        }

        directory = NormalizeUnityPath(directory);

        if (!directory.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
        {
            directory = DefaultJsonExportFolder;
        }

        return directory.TrimEnd('/');
    }

    private void ScanFromConfig()
    {
        folderRows.Clear();

        if (config == null)
        {
            Debug.LogError("[SDKToolkit] Config is null.");
            return;
        }

        AddExplicitFolderPathsFromConfig();
        AddFolderNamesFromConfig();

        CleanParentChildDuplicates();

        Debug.Log("[SDKToolkit] Scan completed. Existing folders: " + folderRows.Count(x => x.exists));
    }

    private void AddExplicitFolderPathsFromConfig()
    {
        if (config.sdkFolderPaths == null)
            return;

        foreach (string rawPath in config.sdkFolderPaths)
        {
            if (string.IsNullOrWhiteSpace(rawPath))
                continue;

            string path = NormalizeUnityPath(rawPath.Trim()).TrimEnd('/');

            if (IsExcluded(path))
                continue;

            if (AssetDatabase.IsValidFolder(path))
            {
                AddFolderRow(path, true);
            }
            else
            {
                folderRows.Add(new SDKFolderRow
                {
                    folderName = Path.GetFileName(path),
                    assetPath = path,
                    exists = false,
                    include = false
                });
            }
        }
    }

    private void AddFolderNamesFromConfig()
    {
        if (config.sdkFolderNames == null || config.sdkFolderNames.Length == 0)
            return;

        foreach (string rawName in config.sdkFolderNames)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                continue;

            string folderName = rawName.Trim();

            if (!string.IsNullOrWhiteSpace(config.sdkRootFolder))
            {
                string directPath = NormalizeUnityPath(config.sdkRootFolder + "/" + folderName).TrimEnd('/');

                if (IsExcluded(directPath))
                    continue;

                if (AssetDatabase.IsValidFolder(directPath))
                {
                    AddFolderRow(directPath, true);
                }
                else
                {
                    folderRows.Add(new SDKFolderRow
                    {
                        folderName = folderName,
                        assetPath = directPath,
                        exists = false,
                        include = false
                    });
                }

                continue;
            }

            AddFoldersBySearchingName(folderName);
        }
    }

    private void AddFoldersBySearchingName(string folderName)
    {
        string[] allDirectories = Directory.GetDirectories(
            Application.dataPath,
            "*",
            SearchOption.AllDirectories
        );

        bool found = false;

        foreach (string absolutePath in allDirectories)
        {
            string currentFolderName = Path.GetFileName(absolutePath);

            if (!string.Equals(currentFolderName, folderName, StringComparison.OrdinalIgnoreCase))
                continue;

            string assetPath = ToAssetPath(absolutePath);

            if (!IsValidAssetFolder(assetPath))
                continue;

            if (IsExcluded(assetPath))
                continue;

            AddFolderRow(assetPath, true);
            found = true;
        }

        if (!found)
        {
            folderRows.Add(new SDKFolderRow
            {
                folderName = folderName,
                assetPath = string.Empty,
                exists = false,
                include = false
            });
        }
    }

    private void OnUnitySelectionChanged()
    {
        if (!autoAddProjectSelection)
            return;

        TryAutoAddSelectedFoldersFromProjectWindow();
    }

    private void TryAutoAddSelectedFoldersFromProjectWindow()
    {
        string[] selectedFolderPaths = GetCurrentProjectSelectionFolderPaths();

        if (selectedFolderPaths.Length == 0)
            return;

        string selectionSignature = string.Join("|", selectedFolderPaths);

        if (string.Equals(selectionSignature, lastAutoAddedSelectionSignature, StringComparison.Ordinal))
            return;

        lastAutoAddedSelectionSignature = selectionSignature;

        string beforeSignature = BuildFolderRowsSignature();

        foreach (string path in selectedFolderPaths)
        {
            if (!AssetDatabase.IsValidFolder(path))
                continue;

            if (IsExcluded(path))
                continue;

            AddFolderRow(path, true);
        }

        CleanParentChildDuplicates();

        string afterSignature = BuildFolderRowsSignature();

        if (!string.Equals(beforeSignature, afterSignature, StringComparison.Ordinal))
        {
            Repaint();
        }
    }

    private void AddSelectedFoldersFromProjectWindow()
    {
        string[] selectedFolderPaths = GetCurrentProjectSelectionFolderPaths();

        foreach (string path in selectedFolderPaths)
        {
            if (!AssetDatabase.IsValidFolder(path))
                continue;

            if (IsExcluded(path))
                continue;

            AddFolderRow(path, true);
        }

        CleanParentChildDuplicates();
        Repaint();
    }

    private static string[] GetCurrentProjectSelectionFolderPaths()
    {
        HashSet<string> paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (Selection.assetGUIDs != null)
        {
            foreach (string guid in Selection.assetGUIDs)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                path = NormalizeUnityPath(path);

                if (AssetDatabase.IsValidFolder(path))
                {
                    paths.Add(path.TrimEnd('/'));
                }
            }
        }

        if (Selection.objects != null)
        {
            foreach (UnityEngine.Object obj in Selection.objects)
            {
                if (obj == null)
                    continue;

                string path = AssetDatabase.GetAssetPath(obj);
                path = NormalizeUnityPath(path);

                if (AssetDatabase.IsValidFolder(path))
                {
                    paths.Add(path.TrimEnd('/'));
                }
            }
        }

        return paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .OrderBy(path => path.Length)
            .ThenBy(path => path)
            .ToArray();
    }

    private string BuildFolderRowsSignature()
    {
        return string.Join(
            "|",
            folderRows
                .Where(x => x.exists)
                .Select(x => NormalizeUnityPath(x.assetPath).TrimEnd('/'))
                .OrderBy(x => x)
        );
    }

    private void TryReplaceRowAsset(SDKFolderRow row, UnityEngine.Object newAsset)
    {
        if (newAsset == null)
            return;

        string newPath = AssetDatabase.GetAssetPath(newAsset);
        newPath = NormalizeUnityPath(newPath).TrimEnd('/');

        if (!AssetDatabase.IsValidFolder(newPath))
        {
            EditorUtility.DisplayDialog(
                "Invalid Selection",
                "Only folders are allowed in this list.",
                "OK"
            );

            return;
        }

        if (IsExcluded(newPath))
        {
            EditorUtility.DisplayDialog(
                "Excluded Folder",
                "This folder is excluded by config:\n" + newPath,
                "OK"
            );

            return;
        }

        row.folderName = Path.GetFileName(newPath);
        row.assetPath = newPath;
        row.exists = true;
        row.include = true;

        CleanParentChildDuplicates();
    }

    private void AddFolderRow(string assetPath, bool include)
    {
        if (string.IsNullOrWhiteSpace(assetPath))
            return;

        assetPath = NormalizeUnityPath(assetPath.Trim()).TrimEnd('/');

        if (!IsValidAssetFolder(assetPath))
            return;

        SDKFolderRow sameRow = folderRows.FirstOrDefault(
            x => x.exists &&
                 string.Equals(x.assetPath, assetPath, StringComparison.OrdinalIgnoreCase)
        );

        if (sameRow != null)
        {
            sameRow.include = sameRow.include || include;
            return;
        }

        bool alreadyCoveredByParent = folderRows.Any(
            x => x.exists &&
                 IsSameOrChildPath(assetPath, x.assetPath)
        );

        if (alreadyCoveredByParent)
            return;

        folderRows.RemoveAll(
            x => x.exists &&
                 IsSameOrChildPath(x.assetPath, assetPath)
        );

        folderRows.Add(new SDKFolderRow
        {
            folderName = Path.GetFileName(assetPath),
            assetPath = assetPath,
            exists = true,
            include = include
        });

        folderRows.Sort((a, b) => string.Compare(a.assetPath, b.assetPath, StringComparison.OrdinalIgnoreCase));
    }

    private void CleanParentChildDuplicates()
    {
        List<SDKFolderRow> missingRows = folderRows
            .Where(x => !x.exists)
            .ToList();

        List<SDKFolderRow> existingRows = folderRows
            .Where(x => x.exists)
            .OrderBy(x => x.assetPath.Length)
            .ThenBy(x => x.assetPath)
            .ToList();

        List<SDKFolderRow> cleaned = new List<SDKFolderRow>();

        foreach (SDKFolderRow row in existingRows)
        {
            bool coveredByParent = cleaned.Any(
                parent => IsSameOrChildPath(row.assetPath, parent.assetPath)
            );

            if (coveredByParent)
                continue;

            cleaned.Add(row);
        }

        folderRows.Clear();
        folderRows.AddRange(cleaned);
        folderRows.AddRange(missingRows);
    }

    private void ExportUnityPackage()
    {
        List<string> assetPaths = GetIncludedExistingFolderPaths();

        if (assetPaths.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Export Failed",
                "No valid folders selected.",
                "OK"
            );

            return;
        }

        string outputFolder = string.IsNullOrWhiteSpace(config.outputFolder)
            ? "SDKBuilds"
            : config.outputFolder;

        if (!Directory.Exists(outputFolder))
        {
            Directory.CreateDirectory(outputFolder);
        }

        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string safePackageName = MakeSafeFileName(config.packageName);
        string safeVersion = MakeSafeFileName(config.version);

        if (string.IsNullOrWhiteSpace(safePackageName))
            safePackageName = "ThirdPartySDKs";

        if (string.IsNullOrWhiteSpace(safeVersion))
            safeVersion = "1.0.0";

        string outputPath = Path.Combine(
            outputFolder,
            safePackageName + "_v" + safeVersion + "_" + timestamp + ".unitypackage"
        );

        ExportPackageOptions options = ExportPackageOptions.Recurse;

        if (config.includeDependencies)
        {
            options |= ExportPackageOptions.IncludeDependencies;
        }

        AssetDatabase.ExportPackage(assetPaths.ToArray(), outputPath, options);

        EditorUtility.RevealInFinder(outputPath);

        Debug.Log("[SDKToolkit] Exported package: " + outputPath);
    }

    private void ExportCheckedFoldersToConfigJson()
    {
        List<string> paths = GetIncludedExistingFolderPaths();

        if (paths.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Export Failed",
                "No checked folders to export.",
                "OK"
            );

            return;
        }

        ExportPathsAsLoadableConfigJson(paths, jsonExportPath);
    }

    private void ExportCurrentProjectSelectionToConfigJson()
    {
        List<string> paths = GetCurrentProjectSelectionFolderPaths()
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Where(path => path.StartsWith("Assets", StringComparison.OrdinalIgnoreCase))
            .Where(path => AssetDatabase.IsValidFolder(path))
            .Where(path => !IsExcluded(path))
            .ToList();

        paths = RemoveChildPathsIfParentExists(paths);

        if (paths.Count == 0)
        {
            EditorUtility.DisplayDialog(
                "Export Failed",
                "No valid folder selection to export.",
                "OK"
            );

            return;
        }

        ExportPathsAsLoadableConfigJson(paths, jsonExportPath);
    }

    private void ExportPathsAsLoadableConfigJson(List<string> paths, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            BindJsonOutputPathFromPackageName();
            outputPath = jsonExportPath;
        }

        outputPath = NormalizeUnityPath(outputPath);

        if (!outputPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            outputPath += ".json";
        }

        string directory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        SDKPackageConfig exportConfig = new SDKPackageConfig
        {
            packageName = config.packageName,
            version = NormalizeVersion(config.version),
            outputFolder = config.outputFolder,
            includeDependencies = config.includeDependencies,

            sdkRootFolder = string.Empty,
            sdkFolderNames = Array.Empty<string>(),
            sdkFolderPaths = paths.ToArray(),

            excludePathContains = config.excludePathContains ?? Array.Empty<string>()
        };

        string json = JsonUtility.ToJson(exportConfig, true);
        File.WriteAllText(outputPath, json);

        AssetDatabase.Refresh();
        EditorUtility.RevealInFinder(outputPath);

        Debug.Log("[SDKToolkit] Exported loadable config JSON: " + outputPath);
    }

    private List<string> GetIncludedExistingFolderPaths()
    {
        List<string> paths = folderRows
            .Where(x => x.exists && x.include)
            .Select(x => NormalizeUnityPath(x.assetPath).TrimEnd('/'))
            .Where(IsValidAssetFolder)
            .Where(path => !IsExcluded(path))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return RemoveChildPathsIfParentExists(paths);
    }

    private static List<string> RemoveChildPathsIfParentExists(List<string> paths)
    {
        List<string> sortedPaths = paths
            .Where(path => !string.IsNullOrWhiteSpace(path))
            .Select(path => NormalizeUnityPath(path).TrimEnd('/'))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(path => path.Length)
            .ThenBy(path => path)
            .ToList();

        List<string> result = new List<string>();

        foreach (string path in sortedPaths)
        {
            bool coveredByParent = result.Any(parent => IsSameOrChildPath(path, parent));

            if (!coveredByParent)
            {
                result.Add(path);
            }
        }

        return result;
    }

    private void FindAsmdefReferences()
    {
        asmdefReferenceRows.Clear();
        asmdefParentFolderRows.Clear();

        if (targetAsmdef == null)
            return;

        string targetAssemblyName = GetAssemblyNameFromAsset(targetAsmdef);
        string targetAsmdefPath = AssetDatabase.GetAssetPath(targetAsmdef);
        string targetGuid = AssetDatabase.AssetPathToGUID(targetAsmdefPath);

        if (string.IsNullOrWhiteSpace(targetAssemblyName))
        {
            Debug.LogWarning("[SDKToolkit] Target asmdef has no valid assembly name.");
            return;
        }

        if (includeTargetAssembly && !string.IsNullOrWhiteSpace(targetAsmdefPath))
        {
            asmdefReferenceRows.Add(new AssetPathRow
            {
                assetPath = NormalizeUnityPath(targetAsmdefPath),
                include = true
            });
        }

        List<string> files = new List<string>();

        if (asmdefSearchMode == AsmdefSearchMode.All || asmdefSearchMode == AsmdefSearchMode.AsmdefOnly)
        {
            files.AddRange(Directory.GetFiles(
                Application.dataPath,
                "*.asmdef",
                SearchOption.AllDirectories
            ));
        }

        if (asmdefSearchMode == AsmdefSearchMode.All || asmdefSearchMode == AsmdefSearchMode.AsmrefOnly)
        {
            files.AddRange(Directory.GetFiles(
                Application.dataPath,
                "*.asmref",
                SearchOption.AllDirectories
            ));
        }

        foreach (string absolutePath in files)
        {
            string assetPath = ToAssetPath(absolutePath);

            if (string.IsNullOrWhiteSpace(assetPath))
                continue;

            if (!includeTargetAssembly && string.Equals(assetPath, targetAsmdefPath, StringComparison.OrdinalIgnoreCase))
                continue;

            string content = File.ReadAllText(absolutePath);

            bool matchByName = content.IndexOf(targetAssemblyName, StringComparison.OrdinalIgnoreCase) >= 0;
            bool matchByGuid = !string.IsNullOrWhiteSpace(targetGuid) &&
                               content.IndexOf("GUID:" + targetGuid, StringComparison.OrdinalIgnoreCase) >= 0;

            if (!matchByName && !matchByGuid)
                continue;

            if (asmdefReferenceRows.Any(x => string.Equals(x.assetPath, assetPath, StringComparison.OrdinalIgnoreCase)))
                continue;

            asmdefReferenceRows.Add(new AssetPathRow
            {
                assetPath = assetPath,
                include = true
            });
        }

        asmdefReferenceRows.Sort((a, b) => string.Compare(a.assetPath, b.assetPath, StringComparison.OrdinalIgnoreCase));

        Debug.Log("[SDKToolkit] Found asmdef/asmref references: " + asmdefReferenceRows.Count);
    }

    private void FindParentFoldersFromAsmdefReferences()
    {
        asmdefParentFolderRows.Clear();

        List<string> parentPaths = new List<string>();

        foreach (AssetPathRow row in asmdefReferenceRows)
        {
            if (!row.include)
                continue;

            string parentPath = ResolvePackageParentFolderFromAssetPath(row.assetPath);

            if (string.IsNullOrWhiteSpace(parentPath))
                continue;

            if (!AssetDatabase.IsValidFolder(parentPath))
                continue;

            if (IsExcluded(parentPath))
                continue;

            parentPaths.Add(parentPath);
        }

        parentPaths = RemoveChildPathsIfParentExists(parentPaths);

        foreach (string path in parentPaths)
        {
            asmdefParentFolderRows.Add(new AssetPathRow
            {
                assetPath = path,
                include = true
            });
        }

        Debug.Log("[SDKToolkit] Found parent folders: " + asmdefParentFolderRows.Count);
    }

    private string ResolvePackageParentFolderFromAssetPath(string assetPath)
    {
        assetPath = NormalizeUnityPath(assetPath).TrimEnd('/');

        if (string.IsNullOrWhiteSpace(assetPath))
            return string.Empty;

        string directory = NormalizeUnityPath(Path.GetDirectoryName(assetPath)).TrimEnd('/');

        if (string.IsNullOrWhiteSpace(directory))
            return string.Empty;

        string folderName = Path.GetFileName(directory);

        if (IsAssemblySubFolderName(folderName))
        {
            string parent = NormalizeUnityPath(Path.GetDirectoryName(directory)).TrimEnd('/');

            if (!string.IsNullOrWhiteSpace(parent) && AssetDatabase.IsValidFolder(parent))
                return parent;
        }

        return directory;
    }

    private static bool IsAssemblySubFolderName(string folderName)
    {
        if (string.IsNullOrWhiteSpace(folderName))
            return false;

        return string.Equals(folderName, "Runtime", StringComparison.OrdinalIgnoreCase)
               || string.Equals(folderName, "Editor", StringComparison.OrdinalIgnoreCase)
               || string.Equals(folderName, "Tests", StringComparison.OrdinalIgnoreCase)
               || string.Equals(folderName, "Tests.Editor", StringComparison.OrdinalIgnoreCase)
               || string.Equals(folderName, "Tests.Runtime", StringComparison.OrdinalIgnoreCase);
    }

    private void AddAsmdefParentFoldersToPackageList()
    {
        foreach (AssetPathRow row in asmdefParentFolderRows)
        {
            if (!row.include)
                continue;

            if (!AssetDatabase.IsValidFolder(row.assetPath))
                continue;

            if (IsExcluded(row.assetPath))
                continue;

            AddFolderRow(row.assetPath, true);
        }

        CleanParentChildDuplicates();
        selectedTab = 0;

        Repaint();
    }

    private string GetAssemblyNameFromAsset(AssemblyDefinitionAsset asmdefAsset)
    {
        if (asmdefAsset == null)
            return string.Empty;

        string path = AssetDatabase.GetAssetPath(asmdefAsset);

        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            return string.Empty;

        string json = File.ReadAllText(path);
        AssemblyDefinitionInfo info = JsonUtility.FromJson<AssemblyDefinitionInfo>(json);

        if (info == null || string.IsNullOrWhiteSpace(info.name))
            return Path.GetFileNameWithoutExtension(path);

        return info.name.Trim();
    }

    private bool IsExcluded(string assetPath)
    {
        if (config == null || config.excludePathContains == null)
            return false;

        string normalizedPath = NormalizeUnityPath(assetPath);

        foreach (string exclude in config.excludePathContains)
        {
            if (string.IsNullOrWhiteSpace(exclude))
                continue;

            string normalizedExclude = NormalizeUnityPath(exclude.Trim());

            if (normalizedPath.IndexOf(normalizedExclude, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
        }

        return false;
    }

    private static bool IsValidAssetFolder(string assetPath)
    {
        return !string.IsNullOrWhiteSpace(assetPath)
               && assetPath.StartsWith("Assets", StringComparison.OrdinalIgnoreCase)
               && AssetDatabase.IsValidFolder(assetPath);
    }

    private static bool IsSameOrChildPath(string possibleChild, string possibleParent)
    {
        possibleChild = NormalizeUnityPath(possibleChild).TrimEnd('/');
        possibleParent = NormalizeUnityPath(possibleParent).TrimEnd('/');

        if (string.Equals(possibleChild, possibleParent, StringComparison.OrdinalIgnoreCase))
            return true;

        return possibleChild.StartsWith(
            possibleParent + "/",
            StringComparison.OrdinalIgnoreCase
        );
    }

    private static string ToAssetPath(string absolutePath)
    {
        string normalizedAbsolutePath = NormalizeUnityPath(absolutePath);
        string normalizedDataPath = NormalizeUnityPath(Application.dataPath);

        if (!normalizedAbsolutePath.StartsWith(normalizedDataPath, StringComparison.OrdinalIgnoreCase))
            return string.Empty;

        string relativePath = normalizedAbsolutePath.Substring(normalizedDataPath.Length);

        if (relativePath.StartsWith("/"))
            relativePath = relativePath.Substring(1);

        return "Assets/" + relativePath;
    }

    private static string NormalizeUnityPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        return path.Replace("\\", "/");
    }

    private static string MakeSafeFileName(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        char[] invalidChars = Path.GetInvalidFileNameChars();

        foreach (char invalidChar in invalidChars)
        {
            value = value.Replace(invalidChar.ToString(), "");
        }

        value = value.Trim();

        value = value
            .Replace(" ", "_")
            .Replace("/", "_")
            .Replace("\\", "_")
            .Replace(":", "_")
            .Replace("*", "_")
            .Replace("?", "_")
            .Replace("\"", "_")
            .Replace("<", "_")
            .Replace(">", "_")
            .Replace("|", "_");

        return value;
    }

    private static string NormalizeVersion(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "1.0.0";

        return value.Trim();
    }

    private static bool IsJsonAssetPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return false;

        return path.StartsWith("Assets", StringComparison.OrdinalIgnoreCase)
               && path.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }

    private enum AsmdefSearchMode
    {
        All,
        AsmdefOnly,
        AsmrefOnly
    }

    [Serializable]
    private sealed class SDKPackageConfig
    {
        public string packageName;
        public string version;

        public string outputFolder;
        public bool includeDependencies;

        public string sdkRootFolder;

        public string[] sdkFolderNames;
        public string[] sdkFolderPaths;

        public string[] excludePathContains;
    }

    [Serializable]
    private sealed class AssemblyDefinitionInfo
    {
        public string name;
        public string[] references;
        public string[] optionalUnityReferences;
        public string[] includePlatforms;
        public string[] excludePlatforms;
        public bool allowUnsafeCode;
        public bool overrideReferences;
        public string[] precompiledReferences;
        public bool autoReferenced;
        public string[] defineConstraints;
        public string[] versionDefines;
        public bool noEngineReferences;
    }

    private sealed class SDKFolderRow
    {
        public string folderName;
        public string assetPath;
        public bool exists;
        public bool include;
    }

    private sealed class AssetPathRow
    {
        public string assetPath;
        public bool include;
    }
}
