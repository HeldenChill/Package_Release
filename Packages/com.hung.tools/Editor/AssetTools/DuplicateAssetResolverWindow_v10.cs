#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace DuplicateAssetResolver.EditorTool
{
    /// <summary>
    /// Editor-only tool to find exact duplicate asset files, analyze project references,
    /// remap all references to a chosen master asset, then safely remove the duplicate assets.
    ///
    /// Menu: Tools/PetVsMonster/Maintenance/Danger/Duplicate Asset Resolver
    /// </summary>
    public sealed class DuplicateAssetResolverWindow : EditorWindow
    {
        private const string DefaultScanFolder = "Assets";
        private const string BackupRootFolderName = "DuplicateAssetResolverBackups";
        private const string CacheFileRelativePath = "Library/DuplicateAssetResolverCache.json";
        private const int CacheVersion = 2;

        private static readonly Regex GuidRegex = new Regex(@"\b[0-9a-fA-F]{32}\b", RegexOptions.Compiled);
        private static readonly Regex GuidReferenceRegex = new Regex(@"guid:\s*([0-9a-fA-F]{32})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        private DefaultAsset scanFolderAsset;
        private string scanFolderPath = DefaultScanFolder;
        private Vector2 scroll;
        private readonly List<DuplicateGroup> groups = new List<DuplicateGroup>();

        private bool analyzeUsageAfterScan = true;
        private bool compareSelectedFolderAgainstAllAssets = true;
        private bool includeMetaFilesInUsage = false;
        private bool includeProjectSettings = true;
        private bool includeScripts = false;
        private bool includeScenes = false;
        private bool includePackages = false;
        private bool createBackups = true;
        private bool moveToTrash = true;
        private bool allowDeleteStillReferenced = false;
        private bool optimizedRemapUsingKnownReferences = true;
        private bool refreshUsageAfterRemap = false;
        private bool showUsedByFiles = false;
        private bool useCache = true;
        private ResolverCache cache;
        private bool cacheLoaded;
        private bool cacheDirty;
        private Dictionary<string, HashCacheEntry> hashCacheByPath;
        private Dictionary<string, ReferenceCacheEntry> referenceCacheByPath;
        private int lastHashCacheHits;
        private int lastHashCacheMisses;
        private int lastHashCandidateFiles;
        private int lastHashSkippedBySizeOrScope;
        private int lastReferenceCacheHits;
        private int lastReferenceCacheMisses;
        private bool isAnalyzingUsage;
        private bool cancelAnalyzeRequested;
        private UsageAnalysisJob activeUsageJob;
        private int maxFileSizeMb = 0;
        private string searchText = string.Empty;

        private string lastBackupFolder;
        private string lastStatus = "Ready.";

        [MenuItem("Tools/Universal/Maintenance/Danger/Duplicate Asset Resolver")]
        private static void Open()
        {
            var window = GetWindow<DuplicateAssetResolverWindow>("Duplicate Resolver");
            window.minSize = new Vector2(850f, 500f);
            window.Show();
        }

        private void OnEnable()
        {
            if (useCache)
                EnsureCacheLoaded();
        }

        private void OnDisable()
        {
            EditorApplication.update -= TickUsageAnalysis;
            EditorUtility.ClearProgressBar();
            activeUsageJob = null;
            isAnalyzingUsage = false;
            cancelAnalyzeRequested = false;
        }

        private void OnGUI()
        {
            DrawHeader();
            DrawOptions();
            DrawActionBar();
            DrawStatus();
            DrawGroups();
        }

        private void DrawHeader()
        {
            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("Duplicate Asset Resolver", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Tool scan duplicate theo exact file hash, kiểm tra file nào đang reference GUID của asset duplicate, " +
                "cho chọn 1 master asset, remap references sang master rồi xóa asset thừa. Nên bật Force Text Serialization trước khi remap.",
                MessageType.Info);
        }

        private void DrawOptions()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Scan Folder", GUILayout.Width(110f));
            var newFolder = (DefaultAsset)EditorGUILayout.ObjectField(scanFolderAsset, typeof(DefaultAsset), false);
            if (newFolder != scanFolderAsset)
            {
                scanFolderAsset = newFolder;
                scanFolderPath = ResolveFolderPath(scanFolderAsset, DefaultScanFolder);
            }

            if (GUILayout.Button("Use Assets", GUILayout.Width(90f)))
            {
                scanFolderAsset = null;
                scanFolderPath = DefaultScanFolder;
            }
            EditorGUILayout.LabelField(scanFolderPath, EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            analyzeUsageAfterScan = EditorGUILayout.ToggleLeft("Analyze usage after scan", analyzeUsageAfterScan, GUILayout.Width(190f));
            compareSelectedFolderAgainstAllAssets = EditorGUILayout.ToggleLeft("Compare selected folder against all Assets", compareSelectedFolderAgainstAllAssets, GUILayout.Width(270f));
            includeProjectSettings = EditorGUILayout.ToggleLeft("Include ProjectSettings refs", includeProjectSettings, GUILayout.Width(190f));
            showUsedByFiles = EditorGUILayout.ToggleLeft("Show used-by files", showUsedByFiles, GUILayout.Width(150f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            includePackages = EditorGUILayout.ToggleLeft("Include Packages refs", includePackages, GUILayout.Width(160f));
            includeMetaFilesInUsage = EditorGUILayout.ToggleLeft("Analyze .meta files as refs", includeMetaFilesInUsage, GUILayout.Width(185f));
            includeScripts = EditorGUILayout.ToggleLeft("Include .cs/.asmdef/.dll", includeScripts, GUILayout.Width(180f));
            includeScenes = EditorGUILayout.ToggleLeft("Include scenes", includeScenes, GUILayout.Width(130f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            createBackups = EditorGUILayout.ToggleLeft("Backup before remap", createBackups, GUILayout.Width(160f));
            moveToTrash = EditorGUILayout.ToggleLeft("Move deleted assets to Trash", moveToTrash, GUILayout.Width(190f));
            allowDeleteStillReferenced = EditorGUILayout.ToggleLeft("Allow delete even if still referenced", allowDeleteStillReferenced, GUILayout.Width(240f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            optimizedRemapUsingKnownReferences = EditorGUILayout.ToggleLeft("Fast remap: only known referenced files", optimizedRemapUsingKnownReferences, GUILayout.Width(260f));
            refreshUsageAfterRemap = EditorGUILayout.ToggleLeft("Full usage refresh after remap", refreshUsageAfterRemap, GUILayout.Width(230f));
            EditorGUILayout.LabelField("Tắt Full refresh để Remap + Delete nhanh hơn.", EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            bool previousUseCache = useCache;
            useCache = EditorGUILayout.ToggleLeft("Use cache", useCache, GUILayout.Width(100f));
            if (useCache && !previousUseCache)
                EnsureCacheLoaded();
            EditorGUILayout.LabelField(GetCacheSummary(), EditorStyles.miniLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Max file size MB", GUILayout.Width(120f));
            maxFileSizeMb = EditorGUILayout.IntField(maxFileSizeMb, GUILayout.Width(80f));
            EditorGUILayout.LabelField("0 = no limit", EditorStyles.miniLabel);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Filter", GUILayout.Width(40f));
            searchText = EditorGUILayout.TextField(searchText, GUILayout.Width(260f));
            EditorGUILayout.EndHorizontal();

            if (EditorSettings.serializationMode != SerializationMode.ForceText)
            {
                EditorGUILayout.HelpBox(
                    "Project hiện không ở Force Text Serialization. Tool vẫn scan được duplicate, nhưng remap reference sẽ chỉ sửa chắc chắn các file text-serialized. " +
                    "Vào Edit > Project Settings > Editor > Asset Serialization > Force Text để an toàn hơn.",
                    MessageType.Warning);
            }

            if (compareSelectedFolderAgainstAllAssets && !string.Equals(NormalizeProjectPath(scanFolderPath), DefaultScanFolder, StringComparison.OrdinalIgnoreCase))
            {
                EditorGUILayout.HelpBox(
                    "Compare selected folder against all Assets đang bật: tool sẽ hash toàn bộ Assets nhưng chỉ hiển thị duplicate group có ít nhất 1 asset nằm trong Scan Folder. " +
                    "Dùng mode này để tìm duplicate giữa 2 folder giống nhau như Sprites/Frame-bar và Sprites/ui/Frame-bar.",
                    MessageType.None);
            }

            if (includeMetaFilesInUsage)
            {
                EditorGUILayout.HelpBox(
                    "Analyze .meta files as refs đang bật. Mode này có thể tìm reference trong importer metadata, nhưng với PNG/Sprite dễ tạo false positive kiểu asset tự reference. " +
                    "Nên tắt nếu bạn đang dọn duplicate Sprite/UI.",
                    MessageType.Warning);
            }

            EditorGUILayout.EndVertical();
        }

        private void DrawActionBar()
        {
            EditorGUILayout.BeginHorizontal();

            using (new EditorGUI.DisabledScope(isAnalyzingUsage))
            {
                if (GUILayout.Button("Scan Duplicates", GUILayout.Height(28f)))
                    ScanDuplicates();
            }

            using (new EditorGUI.DisabledScope(groups.Count == 0 || isAnalyzingUsage))
            {
                if (GUILayout.Button("Refresh Usage", GUILayout.Height(28f)))
                    StartUsageAnalysis(groups, false);

                if (GUILayout.Button("Auto Pick Most Used As Master", GUILayout.Height(28f)))
                {
                    foreach (var group in groups)
                        group.PickMostUsedAsMaster();

                    lastStatus = "Master asset has been picked by highest usage count.";
                }
            }

            using (new EditorGUI.DisabledScope(!isAnalyzingUsage))
            {
                if (GUILayout.Button("Cancel Analyze", GUILayout.Height(28f), GUILayout.Width(120f)))
                {
                    cancelAnalyzeRequested = true;
                    if (activeUsageJob != null)
                        activeUsageJob.cancelRequested = true;
                    lastStatus = "Cancel requested. Finishing current batch safely...";
                }
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(!useCache || isAnalyzingUsage))
            {
                if (GUILayout.Button("Rebuild Cache + Scan", GUILayout.Height(22f)))
                    RebuildCacheAndScan();

                if (GUILayout.Button("Clear Cache", GUILayout.Height(22f), GUILayout.Width(120f)))
                    ClearCache();
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawStatus()
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Status", lastStatus, EditorStyles.wordWrappedMiniLabel);
            if (isAnalyzingUsage && activeUsageJob != null)
            {
                EditorGUILayout.LabelField(
                    string.Format("Analyzing usage: {0}/{1} files | Reference cache hit/miss: {2}/{3}. Cancel button is active in this window and in the Unity progress bar.",
                        activeUsageJob.index,
                        activeUsageJob.referenceFiles.Count,
                        lastReferenceCacheHits,
                        lastReferenceCacheMisses),
                    EditorStyles.wordWrappedMiniLabel);
            }
            if (!string.IsNullOrEmpty(lastBackupFolder))
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Last Backup", lastBackupFolder, EditorStyles.wordWrappedMiniLabel);
                if (GUILayout.Button("Reveal", GUILayout.Width(70f)))
                    EditorUtility.RevealInFinder(lastBackupFolder);
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
        }

        private void DrawGroups()
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("Duplicate Groups: " + groups.Count, EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll);

            foreach (var group in groups)
            {
                if (!PassesFilter(group))
                    continue;

                DrawGroup(group);
            }

            EditorGUILayout.EndScrollView();
        }

        private void DrawGroup(DuplicateGroup group)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            group.foldout = EditorGUILayout.Foldout(group.foldout,
                string.Format("{0} duplicates | {1} | {2}", group.assets.Count, FormatBytes(group.sizeBytes), group.extension), true);
            GUILayout.FlexibleSpace();
            EditorGUILayout.LabelField("Hash: " + group.shortHash, EditorStyles.miniLabel, GUILayout.Width(150f));
            EditorGUILayout.EndHorizontal();

            if (!group.foldout)
            {
                EditorGUILayout.EndVertical();
                return;
            }

            DrawMasterPicker(group);
            DrawAssetRows(group);
            DrawGroupActions(group);

            EditorGUILayout.EndVertical();
        }

        private void DrawMasterPicker(DuplicateGroup group)
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Master Asset", GUILayout.Width(90f));

            var currentMaster = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(group.masterPath);
            var picked = EditorGUILayout.ObjectField(currentMaster, typeof(UnityEngine.Object), false);
            if (picked != currentMaster && picked != null)
            {
                string pickedPath = AssetDatabase.GetAssetPath(picked);
                if (group.assets.Any(a => a.path == pickedPath))
                {
                    group.masterPath = pickedPath;
                    lastStatus = "Master selected: " + pickedPath;
                }
                else
                {
                    EditorUtility.DisplayDialog("Invalid master", "Master asset phải nằm trong duplicate group hiện tại.", "OK");
                }
            }

            if (GUILayout.Button("Pick Most Used", GUILayout.Width(120f)))
            {
                group.PickMostUsedAsMaster();
                lastStatus = "Master selected: " + group.masterPath;
            }
            EditorGUILayout.EndHorizontal();
        }

        private void DrawAssetRows(DuplicateGroup group)
        {
            foreach (var asset in group.assets)
            {
                bool isMaster = asset.path == group.masterPath;
                EditorGUILayout.BeginVertical(isMaster ? EditorStyles.helpBox : GUIStyle.none);
                EditorGUILayout.BeginHorizontal();

                GUILayout.Space(12f);
                EditorGUILayout.LabelField(isMaster ? "MASTER" : "DUP", GUILayout.Width(55f));

                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(asset.path);
                EditorGUILayout.ObjectField(obj, typeof(UnityEngine.Object), false, GUILayout.Width(220f));

                EditorGUILayout.LabelField("Refs: " + asset.usedBy.Count, GUILayout.Width(70f));
                EditorGUILayout.LabelField(asset.hasSubAssets ? ("SubAssets: " + asset.subAssetCount) : "", GUILayout.Width(110f));
                EditorGUILayout.LabelField(asset.path, EditorStyles.miniLabel);

                if (GUILayout.Button("Ping", GUILayout.Width(45f)))
                    PingAsset(asset.path);

                using (new EditorGUI.DisabledScope(isMaster))
                {
                    if (GUILayout.Button("Set Master", GUILayout.Width(85f)))
                        group.masterPath = asset.path;
                }

                EditorGUILayout.EndHorizontal();

                if (showUsedByFiles && asset.usedBy.Count > 0)
                {
                    EditorGUI.indentLevel++;
                    foreach (string usedBy in asset.usedBy.Take(50))
                        EditorGUILayout.LabelField("↳ " + usedBy, EditorStyles.miniLabel);

                    if (asset.usedBy.Count > 50)
                        EditorGUILayout.LabelField("... " + (asset.usedBy.Count - 50) + " more", EditorStyles.miniLabel);
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }
        }

        private void DrawGroupActions(DuplicateGroup group)
        {
            EditorGUILayout.Space(4f);
            using (new EditorGUI.DisabledScope(isAnalyzingUsage))
            {
                EditorGUILayout.BeginHorizontal();

                if (GUILayout.Button("Remap This Group To Master", GUILayout.Height(24f)))
                    RemapGroupToMaster(group, deleteAfterRemap: false);

                if (GUILayout.Button("Remap + Delete Duplicates", GUILayout.Height(24f)))
                    RemapGroupToMaster(group, deleteAfterRemap: true);

                allowDeleteStillReferenced = EditorGUILayout.ToggleLeft("Allow delete even if still referenced", allowDeleteStillReferenced, GUILayout.Width(235f));

                if (GUILayout.Button("Delete Non-Master Assets", GUILayout.Height(24f), GUILayout.Width(180f)))
                    DeleteNonMasterAssets(group);

                EditorGUILayout.EndHorizontal();
            }

            if (group.assets.Any(a => a.path != group.masterPath && a.hasSubAssets))
            {
                EditorGUILayout.HelpBox(
                    "Group này có asset chứa sub-assets. Tool sẽ cố remap local fileID theo Type + Name. " +
                    "Sau khi remap nên kiểm tra lại prefab/scene dùng Sprite/FBX/Animation sub-asset.",
                    MessageType.Warning);
            }
        }

        private void ScanDuplicates()
        {
            groups.Clear();
            lastStatus = "Collecting asset candidates...";
            lastBackupFolder = null;

            try
            {
                lastHashCacheHits = 0;
                lastHashCacheMisses = 0;
                lastHashCandidateFiles = 0;
                lastHashSkippedBySizeOrScope = 0;
                lastReferenceCacheHits = 0;
                lastReferenceCacheMisses = 0;
                if (useCache)
                    EnsureCacheLoaded();

                bool compareAgainstAllAssets = compareSelectedFolderAgainstAllAssets && !string.Equals(NormalizeProjectPath(scanFolderPath), DefaultScanFolder, StringComparison.OrdinalIgnoreCase);
                string[] rootFolders = compareAgainstAllAssets ? new[] { DefaultScanFolder } : new[] { scanFolderPath };
                string[] guids = AssetDatabase.FindAssets(string.Empty, rootFolders);
                long maxBytes = maxFileSizeMb <= 0 ? long.MaxValue : maxFileSizeMb * 1024L * 1024L;

                // Optimization #1: exact duplicate files must have the same file size.
                // So we first bucket by size and only compute SHA-256 for size buckets that can actually contain duplicates.
                // This avoids hashing thousands of unique-size files and also avoids LoadAllAssetsAtPath until a real duplicate group exists.
                var candidatesBySize = new Dictionary<long, List<ScanCandidate>>();
                int consideredAssetFiles = 0;
                int skippedByFilter = 0;
                int skippedByMaxSize = 0;

                for (int i = 0; i < guids.Length; i++)
                {
                    if (i % 100 == 0)
                    {
                        bool canceled = EditorUtility.DisplayCancelableProgressBar(
                            "Collecting asset sizes",
                            string.Format("{0}/{1} assets | Filtered: {2}", i, guids.Length, skippedByFilter),
                            guids.Length == 0 ? 1f : (float)i / guids.Length);

                        if (canceled)
                        {
                            lastStatus = string.Format("Scan canceled while collecting assets at {0}/{1}.", i, guids.Length);
                            return;
                        }
                    }

                    string guid = guids[i];
                    string path = AssetDatabase.GUIDToAssetPath(guid);
                    if (string.IsNullOrEmpty(path) || Directory.Exists(path))
                    {
                        skippedByFilter++;
                        continue;
                    }

                    if (!ShouldScanAsset(path))
                    {
                        skippedByFilter++;
                        continue;
                    }

                    string absolutePath = ProjectPathToAbsolutePath(path);
                    if (!File.Exists(absolutePath))
                    {
                        skippedByFilter++;
                        continue;
                    }

                    var info = new FileInfo(absolutePath);
                    if (info.Length > maxBytes)
                    {
                        skippedByMaxSize++;
                        continue;
                    }

                    consideredAssetFiles++;

                    List<ScanCandidate> sizeList;
                    if (!candidatesBySize.TryGetValue(info.Length, out sizeList))
                    {
                        sizeList = new List<ScanCandidate>();
                        candidatesBySize.Add(info.Length, sizeList);
                    }

                    sizeList.Add(new ScanCandidate
                    {
                        path = path,
                        guid = guid,
                        absolutePath = absolutePath,
                        fileInfo = info
                    });
                }

                var candidateSizeBuckets = new List<List<ScanCandidate>>();
                foreach (var pair in candidatesBySize)
                {
                    List<ScanCandidate> sizeList = pair.Value;
                    if (sizeList == null || sizeList.Count <= 1)
                    {
                        lastHashSkippedBySizeOrScope += sizeList == null ? 0 : sizeList.Count;
                        continue;
                    }

                    if (compareAgainstAllAssets && !sizeList.Any(c => IsPathUnderFolder(c.path, scanFolderPath)))
                    {
                        lastHashSkippedBySizeOrScope += sizeList.Count;
                        continue;
                    }

                    candidateSizeBuckets.Add(sizeList);
                    lastHashCandidateFiles += sizeList.Count;
                }

                var buckets = new Dictionary<string, DuplicateGroup>();
                int hashedIndex = 0;
                int totalHashFiles = lastHashCandidateFiles;

                foreach (var sizeList in candidateSizeBuckets)
                {
                    for (int j = 0; j < sizeList.Count; j++)
                    {
                        ScanCandidate candidate = sizeList[j];

                        if (hashedIndex % 20 == 0)
                        {
                            bool canceled = EditorUtility.DisplayCancelableProgressBar(
                                "Hashing candidate duplicate assets",
                                string.Format("{0}/{1} files | Cache hit/miss: {2}/{3} | Skipped by size/scope: {4}",
                                    hashedIndex,
                                    totalHashFiles,
                                    lastHashCacheHits,
                                    lastHashCacheMisses,
                                    lastHashSkippedBySizeOrScope),
                                totalHashFiles == 0 ? 1f : (float)hashedIndex / totalHashFiles);

                            if (canceled)
                            {
                                lastStatus = string.Format("Scan canceled while hashing at {0}/{1}. Hash cache hit/miss: {2}/{3}.",
                                    hashedIndex, totalHashFiles, lastHashCacheHits, lastHashCacheMisses);
                                return;
                            }
                        }

                        bool cacheHit;
                        string hash = GetSha256Cached(candidate.absolutePath, candidate.path, candidate.guid, candidate.fileInfo, out cacheHit);
                        if (cacheHit)
                            lastHashCacheHits++;
                        else
                            lastHashCacheMisses++;

                        string key = candidate.fileInfo.Length + "|" + hash;

                        DuplicateGroup group;
                        if (!buckets.TryGetValue(key, out group))
                        {
                            group = new DuplicateGroup
                            {
                                hash = hash,
                                shortHash = hash.Substring(0, Math.Min(12, hash.Length)),
                                sizeBytes = candidate.fileInfo.Length,
                                extension = Path.GetExtension(candidate.path).ToLowerInvariant()
                            };
                            buckets.Add(key, group);
                        }

                        group.assets.Add(new DuplicateAssetInfo
                        {
                            path = candidate.path,
                            guid = candidate.guid,
                            sizeBytes = candidate.fileInfo.Length
                        });

                        hashedIndex++;
                    }
                }

                foreach (var group in buckets.Values)
                {
                    if (group.assets.Count <= 1)
                        continue;

                    if (compareAgainstAllAssets && !group.assets.Any(a => IsPathUnderFolder(a.path, scanFolderPath)))
                        continue;

                    group.assets.Sort((a, b) => string.Compare(a.path, b.path, StringComparison.OrdinalIgnoreCase));

                    // Optimization #2: loading all sub-assets is relatively expensive.
                    // Only do it for assets that survived the exact duplicate hash stage.
                    foreach (var asset in group.assets)
                        PopulateSubAssetInfo(asset);

                    group.masterPath = PickInitialMasterPath(group, compareAgainstAllAssets);
                    group.foldout = true;
                    groups.Add(group);
                }

                groups.Sort((a, b) => b.assets.Count.CompareTo(a.assets.Count));

                if (useCache)
                    SaveCacheIfDirty();

                lastStatus = string.Format(
                    "Scan complete. Found {0} duplicate groups. Assets considered: {1}. Hashed: {2}. Skipped by size/scope: {3}. Filtered: {4}. Max-size skipped: {5}. Hash cache hit/miss: {6}/{7}.{8}",
                    groups.Count,
                    consideredAssetFiles,
                    totalHashFiles,
                    lastHashSkippedBySizeOrScope,
                    skippedByFilter,
                    skippedByMaxSize,
                    lastHashCacheHits,
                    lastHashCacheMisses,
                    compareAgainstAllAssets ? " Compared selected folder against all Assets." : string.Empty);

                if (analyzeUsageAfterScan && groups.Count > 0)
                    StartUsageAnalysis(groups, true);
            }
            catch (Exception ex)
            {
                lastStatus = "Scan failed: " + ex.Message;
                Debug.LogException(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private void StartUsageAnalysis(List<DuplicateGroup> targetGroups, bool autoPickMaster)
        {
            if (targetGroups == null || targetGroups.Count == 0)
            {
                lastStatus = "No duplicate groups to analyze.";
                return;
            }

            if (isAnalyzingUsage)
            {
                lastStatus = "Usage analysis is already running. Press Cancel Analyze first if you want to stop it.";
                return;
            }

            try
            {
                lastReferenceCacheHits = 0;
                lastReferenceCacheMisses = 0;
                if (useCache)
                    EnsureCacheLoaded();

                var usageBuffer = new Dictionary<DuplicateAssetInfo, HashSet<string>>();
                var guidToAssets = new Dictionary<string, List<DuplicateAssetInfo>>(StringComparer.OrdinalIgnoreCase);
                foreach (var group in targetGroups)
                {
                    foreach (var asset in group.assets)
                    {
                        if (!usageBuffer.ContainsKey(asset))
                            usageBuffer.Add(asset, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

                        List<DuplicateAssetInfo> list;
                        if (!guidToAssets.TryGetValue(asset.guid, out list))
                        {
                            list = new List<DuplicateAssetInfo>();
                            guidToAssets.Add(asset.guid, list);
                        }
                        list.Add(asset);
                    }
                }

                activeUsageJob = new UsageAnalysisJob
                {
                    targetGroups = targetGroups.ToList(),
                    autoPickMaster = autoPickMaster,
                    referenceFiles = EnumerateReferenceFiles().ToList(),
                    usageBuffer = usageBuffer,
                    guidToAssets = guidToAssets,
                    index = 0,
                    cancelRequested = false
                };

                isAnalyzingUsage = true;
                cancelAnalyzeRequested = false;
                EditorApplication.update -= TickUsageAnalysis;
                EditorApplication.update += TickUsageAnalysis;
                lastStatus = string.Format("Analyzing usage... 0/{0} files. Press Cancel Analyze to stop safely.", activeUsageJob.referenceFiles.Count);
                Repaint();
            }
            catch (Exception ex)
            {
                isAnalyzingUsage = false;
                activeUsageJob = null;
                cancelAnalyzeRequested = false;
                EditorApplication.update -= TickUsageAnalysis;
                EditorUtility.ClearProgressBar();
                lastStatus = "Usage analysis failed to start: " + ex.Message;
                Debug.LogException(ex);
                Repaint();
            }
        }

        private void TickUsageAnalysis()
        {
            if (activeUsageJob == null)
            {
                FinishUsageAnalysis(false, "Usage analysis stopped because job state was missing.");
                return;
            }

            try
            {
                var job = activeUsageJob;
                int total = job.referenceFiles == null ? 0 : job.referenceFiles.Count;
                float progress = total == 0 ? 1f : (float)job.index / total;

                bool canceledByProgressBar = EditorUtility.DisplayCancelableProgressBar(
                    "Analyzing references",
                    string.Format("{0}/{1} files | Cache hit/miss: {2}/{3}", job.index, total, lastReferenceCacheHits, lastReferenceCacheMisses),
                    progress);

                if (cancelAnalyzeRequested || job.cancelRequested || canceledByProgressBar)
                {
                    FinishUsageAnalysis(false, string.Format("Usage analysis canceled at {0}/{1} files. Previous usage results were kept.", job.index, total));
                    return;
                }

                double startTime = EditorApplication.timeSinceStartup;
                int processedThisTick = 0;
                const int maxFilesPerTick = 60;
                const double maxSecondsPerTick = 0.025;

                while (job.index < total)
                {
                    ProcessUsageReferenceFile(job, job.referenceFiles[job.index]);
                    job.index++;
                    processedThisTick++;

                    if (cancelAnalyzeRequested || job.cancelRequested)
                    {
                        FinishUsageAnalysis(false, string.Format("Usage analysis canceled at {0}/{1} files. Previous usage results were kept.", job.index, total));
                        return;
                    }

                    if (processedThisTick >= maxFilesPerTick)
                        break;
                    if (EditorApplication.timeSinceStartup - startTime >= maxSecondsPerTick)
                        break;
                }

                if (job.index >= total)
                {
                    FinishUsageAnalysis(true, null);
                    return;
                }

                lastStatus = string.Format("Analyzing usage... {0}/{1} files. Reference cache hit/miss: {2}/{3}.",
                    job.index, total, lastReferenceCacheHits, lastReferenceCacheMisses);
                Repaint();
            }
            catch (Exception ex)
            {
                lastStatus = "Usage analysis failed: " + ex.Message;
                Debug.LogException(ex);
                FinishUsageAnalysis(false, lastStatus);
            }
        }

        private void ProcessUsageReferenceFile(UsageAnalysisJob job, string absoluteFile)
        {
            string projectPath;
            string metaGuid;
            bool cacheHit;
            List<string> foundGuids = GetReferencedGuidsCached(absoluteFile, out projectPath, out metaGuid, out cacheHit);
            if (cacheHit)
                lastReferenceCacheHits++;
            else
                lastReferenceCacheMisses++;

            if (string.IsNullOrEmpty(projectPath) || foundGuids == null || foundGuids.Count == 0)
                return;

            foreach (string guid in foundGuids)
            {
                List<DuplicateAssetInfo> assets;
                if (!job.guidToAssets.TryGetValue(guid, out assets))
                    continue;

                foreach (var asset in assets)
                {
                    if (IsSelfReference(asset, projectPath, metaGuid))
                        continue;

                    HashSet<string> usedBy;
                    if (job.usageBuffer.TryGetValue(asset, out usedBy))
                        usedBy.Add(projectPath);
                }
            }
        }

        private void FinishUsageAnalysis(bool completed, string cancelOrErrorStatus)
        {
            EditorApplication.update -= TickUsageAnalysis;
            EditorUtility.ClearProgressBar();

            var job = activeUsageJob;
            activeUsageJob = null;
            isAnalyzingUsage = false;
            cancelAnalyzeRequested = false;

            if (completed && job != null)
            {
                foreach (var pair in job.usageBuffer)
                {
                    pair.Key.usedBy.Clear();
                    pair.Key.usedBy.AddRange(pair.Value.OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
                }

                if (job.autoPickMaster)
                {
                    foreach (var group in job.targetGroups)
                        group.PickMostUsedAsMaster();
                }

                if (useCache)
                    SaveCacheIfDirty();

                lastStatus = string.Format("Usage analysis complete. Reference cache hit/miss: {0}/{1}.", lastReferenceCacheHits, lastReferenceCacheMisses);
            }
            else
            {
                if (useCache)
                    SaveCacheIfDirty();
                lastStatus = string.IsNullOrEmpty(cancelOrErrorStatus) ? "Usage analysis canceled. Previous usage results were kept." : cancelOrErrorStatus;
            }

            Repaint();
        }

        private bool RefreshUsageForGroups(List<DuplicateGroup> targetGroups, bool autoPickMaster)
        {
            isAnalyzingUsage = true;
            cancelAnalyzeRequested = false;
            Repaint();

            try
            {
                lastReferenceCacheHits = 0;
                lastReferenceCacheMisses = 0;
                if (useCache)
                    EnsureCacheLoaded();

                var usageBuffer = new Dictionary<DuplicateAssetInfo, HashSet<string>>();
                var guidToAssets = new Dictionary<string, List<DuplicateAssetInfo>>(StringComparer.OrdinalIgnoreCase);
                foreach (var group in targetGroups)
                {
                    foreach (var asset in group.assets)
                    {
                        if (!usageBuffer.ContainsKey(asset))
                            usageBuffer.Add(asset, new HashSet<string>(StringComparer.OrdinalIgnoreCase));

                        List<DuplicateAssetInfo> list;
                        if (!guidToAssets.TryGetValue(asset.guid, out list))
                        {
                            list = new List<DuplicateAssetInfo>();
                            guidToAssets.Add(asset.guid, list);
                        }
                        list.Add(asset);
                    }
                }

                var referenceFiles = EnumerateReferenceFiles().ToList();
                for (int i = 0; i < referenceFiles.Count; i++)
                {
                    string absoluteFile = referenceFiles[i];
                    float progress = referenceFiles.Count == 0 ? 1f : (float)i / referenceFiles.Count;

                    if (i % 25 == 0)
                    {
                        bool canceledByProgressBar = EditorUtility.DisplayCancelableProgressBar(
                            "Analyzing references",
                            string.Format("{0}/{1} files | Cache hit/miss: {2}/{3}", i + 1, referenceFiles.Count, lastReferenceCacheHits, lastReferenceCacheMisses),
                            progress);

                        if (cancelAnalyzeRequested || canceledByProgressBar)
                        {
                            lastStatus = string.Format("Usage analysis canceled at {0}/{1} files. Previous usage results were kept.", i, referenceFiles.Count);
                            return false;
                        }
                    }

                    string projectPath;
                    string metaGuid;
                    bool cacheHit;
                    List<string> foundGuids = GetReferencedGuidsCached(absoluteFile, out projectPath, out metaGuid, out cacheHit);
                    if (cacheHit)
                        lastReferenceCacheHits++;
                    else
                        lastReferenceCacheMisses++;

                    if (string.IsNullOrEmpty(projectPath) || foundGuids == null || foundGuids.Count == 0)
                        continue;

                    foreach (string guid in foundGuids)
                    {
                        List<DuplicateAssetInfo> assets;
                        if (!guidToAssets.TryGetValue(guid, out assets))
                            continue;

                        foreach (var asset in assets)
                        {
                            if (IsSelfReference(asset, projectPath, metaGuid))
                                continue;

                            HashSet<string> usedBy;
                            if (usageBuffer.TryGetValue(asset, out usedBy))
                                usedBy.Add(projectPath);
                        }
                    }
                }

                foreach (var pair in usageBuffer)
                {
                    pair.Key.usedBy.Clear();
                    pair.Key.usedBy.AddRange(pair.Value.OrderBy(path => path, StringComparer.OrdinalIgnoreCase));
                }

                if (autoPickMaster)
                {
                    foreach (var group in targetGroups)
                        group.PickMostUsedAsMaster();
                }

                if (useCache)
                    SaveCacheIfDirty();

                lastStatus = string.Format("Usage analysis complete. Reference cache hit/miss: {0}/{1}.", lastReferenceCacheHits, lastReferenceCacheMisses);
                return true;
            }
            catch (Exception ex)
            {
                lastStatus = "Usage analysis failed: " + ex.Message;
                Debug.LogException(ex);
                return false;
            }
            finally
            {
                isAnalyzingUsage = false;
                cancelAnalyzeRequested = false;
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private void RemapGroupToMaster(DuplicateGroup group, bool deleteAfterRemap)
        {
            if (string.IsNullOrEmpty(group.masterPath))
            {
                EditorUtility.DisplayDialog("Missing master", "Chưa chọn master asset.", "OK");
                return;
            }

            string masterGuid = AssetDatabase.AssetPathToGUID(group.masterPath);
            if (string.IsNullOrEmpty(masterGuid))
            {
                EditorUtility.DisplayDialog("Invalid master", "Master asset không có GUID hợp lệ.", "OK");
                return;
            }

            var duplicates = group.assets.Where(a => a.path != group.masterPath).ToList();
            if (duplicates.Count == 0)
            {
                EditorUtility.DisplayDialog("No duplicate", "Group này không có duplicate non-master.", "OK");
                return;
            }

            string message = string.Format(
                "Remap {0} duplicate asset(s) về master:\n\n{1}\n\nTool sẽ sửa GUID reference trong các file text-serialized. Tiếp tục?",
                duplicates.Count,
                group.masterPath);

            if (!EditorUtility.DisplayDialog("Confirm remap", message, "Remap", "Cancel"))
                return;

            var remapPairs = new List<RemapPair>();
            foreach (var duplicate in duplicates)
            {
                remapPairs.Add(new RemapPair
                {
                    fromPath = duplicate.path,
                    fromGuid = duplicate.guid,
                    toPath = group.masterPath,
                    toGuid = masterGuid,
                    hasSubAssets = duplicate.hasSubAssets,
                    knownUsedBy = duplicate.usedBy.ToList()
                });
            }

            try
            {
                var result = RemapReferences(remapPairs, createBackups);
                lastBackupFolder = result.backupFolder;

                if (result.canceled)
                {
                    lastStatus = string.Format("Remap canceled. Modified {0} file(s) before cancel. Delete skipped for safety.", result.modifiedFiles);
                    AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
                    return;
                }

                ApplyRemapUsageResult(group, result);

                lastStatus = string.Format(
                    "Remap complete. Scanned {0}/{1} candidate file(s). Modified {2}. Local fileID warnings: {3}. Mode: {4}.",
                    result.scannedFiles,
                    result.candidateFiles,
                    result.modifiedFiles,
                    result.localFileIdWarnings,
                    result.usedOptimizedTargets ? "fast known refs" : "full reference scan");

                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

                bool usageRefreshed = true;
                if (refreshUsageAfterRemap)
                    usageRefreshed = RefreshUsageForGroups(new List<DuplicateGroup> { group }, false);

                if (!usageRefreshed)
                {
                    if (deleteAfterRemap)
                        lastStatus = "Remap complete, but full usage refresh was canceled. Delete skipped for safety.";
                    return;
                }

                if (deleteAfterRemap)
                    DeleteNonMasterAssets(group, skipUsageRefresh: !refreshUsageAfterRemap);
            }
            catch (Exception ex)
            {
                lastStatus = "Remap failed: " + ex.Message;
                Debug.LogException(ex);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                Repaint();
            }
        }

        private void DeleteNonMasterAssets(DuplicateGroup group, bool skipUsageRefresh = false)
        {
            if (!skipUsageRefresh && !RefreshUsageForGroups(new List<DuplicateGroup> { group }, false))
            {
                lastStatus = "Delete skipped because usage analysis was canceled.";
                return;
            }

            var deleteList = group.assets.Where(a => a.path != group.masterPath).ToList();
            var stillReferenced = deleteList.Where(a => a.usedBy.Count > 0).ToList();

            if (stillReferenced.Count > 0 && !allowDeleteStillReferenced)
            {
                string details = string.Join("\n", stillReferenced.Take(10).Select(a => a.path + " refs=" + a.usedBy.Count).ToArray());
                EditorUtility.DisplayDialog(
                    "Blocked delete",
                    "Một số duplicate vẫn còn reference nên chưa xóa để tránh mất link. Hãy remap lại hoặc bật 'Allow delete even if still referenced'.\n\n" + details,
                    "OK");
                return;
            }

            string confirm = string.Format(
                "Delete {0} non-master duplicate asset(s)?\n\nMaster giữ lại:\n{1}\n\n{2}",
                deleteList.Count,
                group.masterPath,
                moveToTrash ? "Assets sẽ được move vào OS Trash nếu Unity hỗ trợ." : "Assets sẽ bị xóa bằng AssetDatabase.DeleteAsset.");

            if (!EditorUtility.DisplayDialog("Confirm delete", confirm, "Delete", "Cancel"))
                return;

            int deleted = 0;
            var failed = new List<string>();

            AssetDatabase.StartAssetEditing();
            try
            {
                foreach (var asset in deleteList)
                {
                    bool ok = false;
                    if (moveToTrash)
                        ok = AssetDatabase.MoveAssetToTrash(asset.path);
                    else
                        ok = AssetDatabase.DeleteAsset(asset.path);

                    if (ok)
                    {
                        deleted++;
                        InvalidateHashCache(asset.path);
                        InvalidateReferenceCache(asset.path);
                        InvalidateReferenceCache(asset.path + ".meta");
                    }
                    else
                    {
                        failed.Add(asset.path);
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }

            groups.RemoveAll(g => g.assets.All(a => !File.Exists(ProjectPathToAbsolutePath(a.path))));
            group.assets.RemoveAll(a => a.path != group.masterPath && !File.Exists(ProjectPathToAbsolutePath(a.path)));

            if (useCache)
                SaveCacheIfDirty();

            lastStatus = string.Format("Delete complete. Deleted {0}/{1}. Failed: {2}", deleted, deleteList.Count, failed.Count);
            if (failed.Count > 0)
                Debug.LogWarning("DuplicateAssetResolver failed to delete:\n" + string.Join("\n", failed.ToArray()));
        }

        private RemapResult RemapReferences(List<RemapPair> remapPairs, bool backup)
        {
            if (useCache)
                EnsureCacheLoaded();

            string projectRoot = GetProjectRoot();
            string backupFolder = null;
            if (backup)
            {
                backupFolder = Path.Combine(projectRoot, BackupRootFolderName, DateTime.Now.ToString("yyyyMMdd_HHmmss"));
                Directory.CreateDirectory(backupFolder);
            }

            var targetFiles = GetRemapTargetFiles(remapPairs);
            bool usedOptimizedTargets = optimizedRemapUsingKnownReferences && targetFiles.usedOptimizedTargets;
            var files = targetFiles.files;

            int modifiedCount = 0;
            int warnings = 0;
            int scannedFiles = 0;
            bool canceled = false;
            var modifiedProjectPaths = new List<string>();
            var manifest = new StringBuilder();
            manifest.AppendLine("Duplicate Asset Resolver Remap Manifest");
            manifest.AppendLine("Time: " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            manifest.AppendLine("Mode: " + (usedOptimizedTargets ? "Fast known referenced files" : "Full reference file scan"));
            manifest.AppendLine("Candidate files: " + files.Count);
            manifest.AppendLine();

            for (int i = 0; i < files.Count; i++)
            {
                string file = files[i];
                if (i % 15 == 0)
                {
                    canceled = EditorUtility.DisplayCancelableProgressBar(
                        "Remapping references",
                        string.Format("{0}/{1} files | modified {2} | mode: {3}", i, files.Count, modifiedCount, usedOptimizedTargets ? "fast" : "full"),
                        files.Count == 0 ? 1f : (float)i / files.Count);
                    if (canceled)
                        break;
                }

                string projectPath = AbsolutePathToProjectPath(file);
                if (string.IsNullOrEmpty(projectPath))
                    continue;

                if (ShouldSkipRemapFile(remapPairs, projectPath))
                    continue;

                scannedFiles++;

                string original = TryReadText(file);
                if (string.IsNullOrEmpty(original))
                    continue;

                string updated = original;
                bool touched = false;

                foreach (var pair in remapPairs)
                {
                    if (IsOwnMetaFile(pair.fromPath, projectPath))
                        continue;

                    if (updated.IndexOf(pair.fromGuid, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    if (pair.localFileIdMap == null)
                        pair.localFileIdMap = BuildLocalFileIdMap(pair.fromPath, pair.toPath);

                    int localWarnings;
                    updated = ReplaceGuidReference(updated, pair, out localWarnings);
                    warnings += localWarnings;
                    touched = true;
                }

                if (!touched || updated == original)
                    continue;

                if (backup && !string.IsNullOrEmpty(backupFolder))
                    BackupFile(file, backupFolder, projectRoot);

                File.WriteAllText(file, updated, new UTF8Encoding(false));
                InvalidateReferenceCache(projectPath);
                modifiedCount++;
                modifiedProjectPaths.Add(projectPath);
                manifest.AppendLine(projectPath);
            }

            if (backup && !string.IsNullOrEmpty(backupFolder))
                File.WriteAllText(Path.Combine(backupFolder, "manifest.txt"), manifest.ToString(), new UTF8Encoding(false));

            if (useCache)
                SaveCacheIfDirty();

            return new RemapResult
            {
                modifiedFiles = modifiedCount,
                localFileIdWarnings = warnings,
                backupFolder = backupFolder,
                scannedFiles = scannedFiles,
                candidateFiles = files.Count,
                usedOptimizedTargets = usedOptimizedTargets,
                canceled = canceled,
                modifiedProjectPaths = modifiedProjectPaths
            };
        }

        private RemapTargetFileSet GetRemapTargetFiles(List<RemapPair> remapPairs)
        {
            var optimizedProjectPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var fromGuids = new HashSet<string>(remapPairs.Select(p => p.fromGuid), StringComparer.OrdinalIgnoreCase);

            if (optimizedRemapUsingKnownReferences)
            {
                foreach (var pair in remapPairs)
                {
                    if (pair.knownUsedBy == null)
                        continue;

                    foreach (string usedBy in pair.knownUsedBy)
                    {
                        if (string.IsNullOrEmpty(usedBy))
                            continue;

                        if (IsOwnMetaFile(pair.fromPath, usedBy))
                            continue;

                        optimizedProjectPaths.Add(NormalizeProjectPath(usedBy));
                    }
                }

                if (useCache)
                {
                    EnsureCacheLoaded();
                    foreach (var entry in referenceCacheByPath.Values)
                    {
                        if (entry == null || entry.guids == null || entry.guids.Count == 0)
                            continue;

                        bool containsTargetGuid = false;
                        foreach (string guid in entry.guids)
                        {
                            if (fromGuids.Contains(guid))
                            {
                                containsTargetGuid = true;
                                break;
                            }
                        }

                        if (!containsTargetGuid)
                            continue;

                        if (!IsReferenceCacheEntryStillValid(entry))
                            continue;

                        bool isOwnMeta = false;
                        foreach (var pair in remapPairs)
                        {
                            if (IsOwnMetaFile(pair.fromPath, entry.path))
                            {
                                isOwnMeta = true;
                                break;
                            }
                        }

                        if (!isOwnMeta)
                            optimizedProjectPaths.Add(NormalizeProjectPath(entry.path));
                    }
                }
            }

            if (optimizedRemapUsingKnownReferences && optimizedProjectPaths.Count > 0)
            {
                return new RemapTargetFileSet
                {
                    usedOptimizedTargets = true,
                    files = optimizedProjectPaths
                        .Select(ProjectPathToAbsolutePath)
                        .Where(File.Exists)
                        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                        .ToList()
                };
            }

            return new RemapTargetFileSet
            {
                usedOptimizedTargets = false,
                files = EnumerateReferenceFiles().ToList()
            };
        }

        private static bool ShouldSkipRemapFile(List<RemapPair> remapPairs, string projectPath)
        {
            foreach (var pair in remapPairs)
            {
                if (IsOwnMetaFile(pair.fromPath, projectPath))
                    continue;

                return false;
            }

            return true;
        }

        private bool IsReferenceCacheEntryStillValid(ReferenceCacheEntry entry)
        {
            if (entry == null || string.IsNullOrEmpty(entry.path))
                return false;

            string absolutePath = ProjectPathToAbsolutePath(entry.path);
            if (!File.Exists(absolutePath))
                return false;

            var info = new FileInfo(absolutePath);
            return info.Length == entry.sizeBytes && info.LastWriteTimeUtc.Ticks == entry.writeTicksUtc;
        }

        private void ApplyRemapUsageResult(DuplicateGroup group, RemapResult result)
        {
            if (group == null || result == null || result.modifiedProjectPaths == null || result.modifiedProjectPaths.Count == 0)
                return;

            var modifiedSet = new HashSet<string>(result.modifiedProjectPaths.Select(NormalizeProjectPath), StringComparer.OrdinalIgnoreCase);
            DuplicateAssetInfo master = group.assets.FirstOrDefault(a => a.path == group.masterPath);
            if (master == null)
                return;

            foreach (var asset in group.assets)
            {
                if (asset == master)
                    continue;

                asset.usedBy.RemoveAll(path => modifiedSet.Contains(NormalizeProjectPath(path)));
            }

            foreach (string path in modifiedSet)
            {
                if (!master.usedBy.Any(existing => string.Equals(NormalizeProjectPath(existing), path, StringComparison.OrdinalIgnoreCase)))
                    master.usedBy.Add(path);
            }

            master.usedBy.Sort(StringComparer.OrdinalIgnoreCase);
        }

        private static string ReplaceGuidReference(string content, RemapPair pair, out int localFileIdWarnings)
        {
            int warningCount = 0;

            string pattern = @"(fileID:\s*)(-?\d+)(\s*,\s*guid:\s*)" + Regex.Escape(pair.fromGuid) + @"(\s*,\s*type:\s*\d+)";
            var regex = new Regex(pattern, RegexOptions.IgnoreCase);

            string updated = regex.Replace(content, match =>
            {
                long sourceFileId;
                if (!long.TryParse(match.Groups[2].Value, out sourceFileId))
                {
                    warningCount++;
                    return match.Value.Replace(pair.fromGuid, pair.toGuid);
                }

                long targetFileId;
                if (!pair.localFileIdMap.TryGetValue(sourceFileId, out targetFileId))
                {
                    targetFileId = sourceFileId;
                    warningCount++;
                }

                return match.Groups[1].Value + targetFileId + match.Groups[3].Value + pair.toGuid + match.Groups[4].Value;
            });

            localFileIdWarnings = warningCount;

            // Fallback pass: catch "guid: X" references the strict fileID-paired pattern above
            // missed (differently-ordered or malformed YAML reference blocks). Scoped to the
            // "guid:" key specifically, not a bare word-boundary match on the GUID string -
            // a blind global replace would also rewrite unrelated occurrences of the same 32-hex
            // string (comments, hash fields, sourceAssetIdentifier blocks, etc.) that are not
            // actually references to this asset.
            updated = Regex.Replace(
                updated,
                @"(guid:\s*)" + Regex.Escape(pair.fromGuid) + @"\b",
                "${1}" + pair.toGuid,
                RegexOptions.IgnoreCase);

            return updated;
        }

        private static Dictionary<long, long> BuildLocalFileIdMap(string fromPath, string toPath)
        {
            var result = new Dictionary<long, long>();
            var fromObjects = AssetDatabase.LoadAllAssetsAtPath(fromPath).Where(o => o != null).ToArray();
            var toObjects = AssetDatabase.LoadAllAssetsAtPath(toPath).Where(o => o != null).ToArray();

            var toByKey = new Dictionary<string, List<ObjectIdInfo>>();
            foreach (var obj in toObjects)
            {
                string guid;
                long id;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out guid, out id))
                    continue;

                string key = MakeObjectKey(obj);
                List<ObjectIdInfo> list;
                if (!toByKey.TryGetValue(key, out list))
                {
                    list = new List<ObjectIdInfo>();
                    toByKey.Add(key, list);
                }
                list.Add(new ObjectIdInfo { localId = id, obj = obj });
            }

            foreach (var obj in fromObjects)
            {
                string guid;
                long fromId;
                if (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(obj, out guid, out fromId))
                    continue;

                string key = MakeObjectKey(obj);
                List<ObjectIdInfo> candidates;
                if (toByKey.TryGetValue(key, out candidates) && candidates.Count == 1)
                {
                    result[fromId] = candidates[0].localId;
                }
                else
                {
                    // For many Unity main asset types, the local fileID is stable across duplicate files.
                    result[fromId] = fromId;
                }
            }

            return result;
        }

        private static string MakeObjectKey(UnityEngine.Object obj)
        {
            string typeName = obj.GetType().FullName ?? obj.GetType().Name;
            return typeName + "|" + obj.name;
        }

        private bool ShouldScanAsset(string path)
        {
            string extension = Path.GetExtension(path).ToLowerInvariant();
            if (string.IsNullOrEmpty(extension))
                return false;

            if (path.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
                return false;

            if (!includeScenes && extension == ".unity")
                return false;

            if (!includeScripts && IsScriptOrAssembly(extension))
                return false;

            if (path.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))
                return false;

            if (path.StartsWith("Assets/Editor/DuplicateAssetResolver/", StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        private IEnumerable<string> EnumerateReferenceFiles()
        {
            string projectRoot = GetProjectRoot();
            var roots = new List<string> { Path.Combine(projectRoot, "Assets") };

            if (includeProjectSettings)
                roots.Add(Path.Combine(projectRoot, "ProjectSettings"));

            if (includePackages)
                roots.Add(Path.Combine(projectRoot, "Packages"));

            foreach (string root in roots)
            {
                if (!Directory.Exists(root))
                    continue;

                foreach (string file in Directory.GetFiles(root, "*", SearchOption.AllDirectories))
                {
                    if (!IsReferenceTextFile(file))
                        continue;

                    string normalized = file.Replace('\\', '/');
                    if (normalized.IndexOf("/" + BackupRootFolderName + "/", StringComparison.OrdinalIgnoreCase) >= 0)
                        continue;

                    yield return file;
                }
            }
        }

        private bool IsReferenceTextFile(string absolutePath)
        {
            string ext = Path.GetExtension(absolutePath).ToLowerInvariant();

            if (ext == ".meta")
                return includeMetaFilesInUsage;

            switch (ext)
            {
                case ".prefab":
                case ".unity":
                case ".asset":
                case ".mat":
                case ".anim":
                case ".controller":
                case ".overridecontroller":
                case ".playable":
                case ".mask":
                case ".preset":
                case ".spriteatlas":
                case ".inputactions":
                case ".shadergraph":
                case ".vfx":
                case ".uxml":
                case ".uss":
                case ".terrainlayer":
                case ".rendertexture":
                case ".guiskin":
                case ".fontsettings":
                case ".lighting":
                case ".flare":
                case ".physicmaterial":
                case ".physicsmaterial2d":
                case ".asmdef":
                case ".asmref":
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsScriptOrAssembly(string extension)
        {
            return extension == ".cs" || extension == ".asmdef" || extension == ".asmref" || extension == ".dll";
        }

        private static string ResolveFolderPath(DefaultAsset folderAsset, string fallback)
        {
            if (folderAsset == null)
                return fallback;

            string path = AssetDatabase.GetAssetPath(folderAsset);
            if (string.IsNullOrEmpty(path) || !Directory.Exists(ProjectPathToAbsolutePath(path)))
                return fallback;

            return path;
        }

        private static void PopulateSubAssetInfo(DuplicateAssetInfo info)
        {
            var all = AssetDatabase.LoadAllAssetsAtPath(info.path);
            int count = 0;
            foreach (var obj in all)
            {
                if (obj == null)
                    continue;
                count++;
            }

            info.subAssetCount = Mathf.Max(0, count - 1);
            info.hasSubAssets = info.subAssetCount > 0;
        }

        private bool PassesFilter(DuplicateGroup group)
        {
            if (string.IsNullOrEmpty(searchText))
                return true;

            string lowered = searchText.ToLowerInvariant();
            if (group.hash.ToLowerInvariant().Contains(lowered) || group.extension.Contains(lowered))
                return true;

            return group.assets.Any(a => a.path.ToLowerInvariant().Contains(lowered));
        }

        private static bool IsPathUnderFolder(string assetPath, string folderPath)
        {
            string normalizedAssetPath = NormalizeProjectPath(assetPath);
            string normalizedFolderPath = NormalizeProjectPath(folderPath);

            if (string.IsNullOrEmpty(normalizedFolderPath) || string.Equals(normalizedFolderPath, DefaultScanFolder, StringComparison.OrdinalIgnoreCase))
                return normalizedAssetPath.StartsWith(DefaultScanFolder + "/", StringComparison.OrdinalIgnoreCase);

            if (string.Equals(normalizedAssetPath, normalizedFolderPath, StringComparison.OrdinalIgnoreCase))
                return true;

            return normalizedAssetPath.StartsWith(normalizedFolderPath.TrimEnd('/') + "/", StringComparison.OrdinalIgnoreCase);
        }

        private string PickInitialMasterPath(DuplicateGroup group, bool preferSelectedFolder)
        {
            if (group == null || group.assets == null || group.assets.Count == 0)
                return null;

            if (preferSelectedFolder)
            {
                var inSelectedFolder = group.assets.FirstOrDefault(a => IsPathUnderFolder(a.path, scanFolderPath));
                if (inSelectedFolder != null)
                    return inSelectedFolder.path;
            }

            return group.assets[0].path;
        }

        private static bool IsSelfReference(DuplicateAssetInfo asset, string projectPath, string metaGuid)
        {
            string normalizedProjectPath = NormalizeProjectPath(projectPath);
            string normalizedAssetPath = NormalizeProjectPath(asset.path);

            // The asset file itself is not an external user of itself.
            if (string.Equals(normalizedProjectPath, normalizedAssetPath, StringComparison.OrdinalIgnoreCase))
                return true;

            // The asset .meta file contains its own GUID by design, so it must not count as a real usage.
            if (string.Equals(normalizedProjectPath, normalizedAssetPath + ".meta", StringComparison.OrdinalIgnoreCase))
                return true;

            // Extra-safe path-independent guard: if the scanned .meta belongs to this GUID, ignore it even if
            // Unity/path normalization is different on Windows, moved assets, package imports, etc.
            if (normalizedProjectPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.IsNullOrEmpty(metaGuid) && string.Equals(metaGuid, asset.guid, StringComparison.OrdinalIgnoreCase))
                    return true;
            }

            return false;
        }

        private static bool IsOwnMetaFile(string assetPath, string projectPath)
        {
            string normalizedAssetPath = NormalizeProjectPath(assetPath);
            string normalizedProjectPath = NormalizeProjectPath(projectPath);

            // During remap, never rewrite the duplicate asset's own .meta file.
            // That file stores the asset GUID itself; replacing it would corrupt the asset identity.
            return string.Equals(normalizedProjectPath, normalizedAssetPath + ".meta", StringComparison.OrdinalIgnoreCase);
        }

        private static string ExtractGuidFromMeta(string fileContent)
        {
            if (string.IsNullOrEmpty(fileContent))
                return null;

            Match match = Regex.Match(fileContent, @"(?m)^guid:\s*([a-fA-F0-9]{32})\s*$");
            return match.Success ? match.Groups[1].Value.ToLowerInvariant() : null;
        }

        private static string NormalizeProjectPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            return path.Replace('\\', '/').Trim();
        }

        private void RebuildCacheAndScan()
        {
            ClearCacheInternal(deleteFile: true);
            lastStatus = "Cache cleared. Rebuilding by scanning project...";
            ScanDuplicates();
        }

        private void ClearCache()
        {
            if (!EditorUtility.DisplayDialog("Clear Duplicate Resolver cache", "Xóa cache hash/reference hiện tại? Lần scan sau sẽ đọc lại toàn bộ asset/reference file.", "Clear", "Cancel"))
                return;

            ClearCacheInternal(deleteFile: true);
            lastStatus = "Cache cleared.";
            Repaint();
        }

        private void ClearCacheInternal(bool deleteFile)
        {
            cache = CreateEmptyCache();
            cacheLoaded = true;
            cacheDirty = false;
            hashCacheByPath = new Dictionary<string, HashCacheEntry>(StringComparer.OrdinalIgnoreCase);
            referenceCacheByPath = new Dictionary<string, ReferenceCacheEntry>(StringComparer.OrdinalIgnoreCase);
            lastHashCacheHits = 0;
            lastHashCacheMisses = 0;
            lastHashCandidateFiles = 0;
            lastHashSkippedBySizeOrScope = 0;
            lastReferenceCacheHits = 0;
            lastReferenceCacheMisses = 0;

            if (deleteFile)
            {
                string path = GetCacheFilePath();
                if (File.Exists(path))
                    File.Delete(path);
            }
        }

        private void EnsureCacheLoaded()
        {
            if (cacheLoaded && cache != null && hashCacheByPath != null && referenceCacheByPath != null)
                return;

            cache = null;
            string cachePath = GetCacheFilePath();
            if (File.Exists(cachePath))
            {
                try
                {
                    string json = File.ReadAllText(cachePath);
                    cache = JsonUtility.FromJson<ResolverCache>(json);
                }
                catch (Exception ex)
                {
                    Debug.LogWarning("DuplicateAssetResolver cache load failed. Rebuilding cache. " + ex.Message);
                    cache = null;
                }
            }

            if (cache == null || cache.version != CacheVersion)
                cache = CreateEmptyCache();

            if (cache.hashEntries == null)
                cache.hashEntries = new List<HashCacheEntry>();
            if (cache.referenceEntries == null)
                cache.referenceEntries = new List<ReferenceCacheEntry>();

            hashCacheByPath = new Dictionary<string, HashCacheEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in cache.hashEntries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.path))
                    continue;
                hashCacheByPath[NormalizeProjectPath(entry.path)] = entry;
            }

            referenceCacheByPath = new Dictionary<string, ReferenceCacheEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in cache.referenceEntries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.path))
                    continue;
                if (entry.guids == null)
                    entry.guids = new List<string>();
                referenceCacheByPath[NormalizeProjectPath(entry.path)] = entry;
            }

            cacheLoaded = true;
            cacheDirty = false;
        }

        private ResolverCache CreateEmptyCache()
        {
            return new ResolverCache
            {
                version = CacheVersion,
                createdUtc = DateTime.UtcNow.ToString("o"),
                updatedUtc = DateTime.UtcNow.ToString("o"),
                hashEntries = new List<HashCacheEntry>(),
                referenceEntries = new List<ReferenceCacheEntry>()
            };
        }

        private void SaveCacheIfDirty()
        {
            if (!useCache || !cacheLoaded || cache == null || !cacheDirty)
                return;

            try
            {
                cache.version = CacheVersion;
                cache.updatedUtc = DateTime.UtcNow.ToString("o");
                cache.hashEntries = hashCacheByPath == null ? new List<HashCacheEntry>() : hashCacheByPath.Values.OrderBy(e => e.path).ToList();
                cache.referenceEntries = referenceCacheByPath == null ? new List<ReferenceCacheEntry>() : referenceCacheByPath.Values.OrderBy(e => e.path).ToList();

                string cachePath = GetCacheFilePath();
                string directory = Path.GetDirectoryName(cachePath);
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                string json = JsonUtility.ToJson(cache, true);
                File.WriteAllText(cachePath, json, new UTF8Encoding(false));
                cacheDirty = false;
            }
            catch (Exception ex)
            {
                Debug.LogWarning("DuplicateAssetResolver cache save failed: " + ex.Message);
            }
        }

        private string GetSha256Cached(string absolutePath, string projectPath, string guid, FileInfo info, out bool cacheHit)
        {
            cacheHit = false;

            if (!useCache)
                return ComputeSha256(absolutePath);

            EnsureCacheLoaded();

            string normalizedPath = NormalizeProjectPath(projectPath);
            HashCacheEntry entry;
            if (hashCacheByPath.TryGetValue(normalizedPath, out entry) &&
                string.Equals(entry.guid, guid, StringComparison.OrdinalIgnoreCase) &&
                entry.sizeBytes == info.Length &&
                entry.writeTicksUtc == info.LastWriteTimeUtc.Ticks &&
                !string.IsNullOrEmpty(entry.sha256))
            {
                cacheHit = true;
                return entry.sha256;
            }

            string hash = ComputeSha256(absolutePath);
            entry = new HashCacheEntry
            {
                path = normalizedPath,
                guid = guid,
                sizeBytes = info.Length,
                writeTicksUtc = info.LastWriteTimeUtc.Ticks,
                sha256 = hash
            };
            hashCacheByPath[normalizedPath] = entry;
            cacheDirty = true;
            return hash;
        }

        private List<string> GetReferencedGuidsCached(string absoluteFile, out string projectPath, out string metaGuid, out bool cacheHit)
        {
            cacheHit = false;
            metaGuid = null;
            projectPath = AbsolutePathToProjectPath(absoluteFile);
            if (string.IsNullOrEmpty(projectPath))
                return new List<string>();

            var info = new FileInfo(absoluteFile);
            string normalizedPath = NormalizeProjectPath(projectPath);

            if (useCache)
            {
                EnsureCacheLoaded();
                ReferenceCacheEntry entry;
                if (referenceCacheByPath.TryGetValue(normalizedPath, out entry) &&
                    entry.sizeBytes == info.Length &&
                    entry.writeTicksUtc == info.LastWriteTimeUtc.Ticks &&
                    entry.guids != null)
                {
                    cacheHit = true;
                    metaGuid = entry.metaGuid;
                    return entry.guids;
                }
            }

            string content = TryReadText(absoluteFile);
            if (string.IsNullOrEmpty(content))
                return new List<string>();

            var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Only treat YAML/object-reference GUIDs as actual references.
            // This avoids false positives from Sprite importer fields such as spriteID/internal IDs
            // and other 32-hex strings inside .meta or serialized files.
            MatchCollection matches = GuidReferenceRegex.Matches(content);
            foreach (Match match in matches)
                found.Add(match.Groups[1].Value.ToLowerInvariant());

            metaGuid = normalizedPath.EndsWith(".meta", StringComparison.OrdinalIgnoreCase) ? ExtractGuidFromMeta(content) : null;
            var guids = found.OrderBy(g => g, StringComparer.OrdinalIgnoreCase).ToList();

            if (useCache)
            {
                referenceCacheByPath[normalizedPath] = new ReferenceCacheEntry
                {
                    path = normalizedPath,
                    sizeBytes = info.Length,
                    writeTicksUtc = info.LastWriteTimeUtc.Ticks,
                    metaGuid = metaGuid,
                    guids = guids
                };
                cacheDirty = true;
            }

            return guids;
        }

        private void InvalidateHashCache(string projectPath)
        {
            if (!useCache)
                return;

            EnsureCacheLoaded();
            string normalizedPath = NormalizeProjectPath(projectPath);
            if (hashCacheByPath.Remove(normalizedPath))
                cacheDirty = true;
        }

        private void InvalidateReferenceCache(string projectPath)
        {
            if (!useCache)
                return;

            EnsureCacheLoaded();
            string normalizedPath = NormalizeProjectPath(projectPath);
            if (referenceCacheByPath.Remove(normalizedPath))
                cacheDirty = true;
        }

        private string GetCacheSummary()
        {
            if (!useCache)
                return "Cache disabled.";

            EnsureCacheLoaded();
            int hashCount = hashCacheByPath == null ? 0 : hashCacheByPath.Count;
            int refCount = referenceCacheByPath == null ? 0 : referenceCacheByPath.Count;
            return string.Format("Cache: {0} hash entries, {1} reference entries | Last hit/miss: hash {2}/{3}, refs {4}/{5} | Last hashed/skipped: {6}/{7}",
                hashCount,
                refCount,
                lastHashCacheHits,
                lastHashCacheMisses,
                lastReferenceCacheHits,
                lastReferenceCacheMisses,
                lastHashCandidateFiles,
                lastHashSkippedBySizeOrScope);
        }

        private static string GetCacheFilePath()
        {
            return Path.Combine(GetProjectRoot(), CacheFileRelativePath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string ComputeSha256(string absolutePath)
        {
            using (var sha = SHA256.Create())
            using (var stream = File.OpenRead(absolutePath))
            {
                byte[] hash = sha.ComputeHash(stream);
                var sb = new StringBuilder(hash.Length * 2);
                foreach (byte b in hash)
                    sb.Append(b.ToString("x2"));
                return sb.ToString();
            }
        }

        private static string TryReadText(string absolutePath)
        {
            try
            {
                return File.ReadAllText(absolutePath);
            }
            catch
            {
                return null;
            }
        }

        private static void BackupFile(string absoluteFile, string backupFolder, string projectRoot)
        {
            string relative = absoluteFile.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string target = Path.Combine(backupFolder, relative);
            string directory = Path.GetDirectoryName(target);
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            File.Copy(absoluteFile, target, true);
        }

        private static string ProjectPathToAbsolutePath(string projectPath)
        {
            string projectRoot = GetProjectRoot();
            return Path.Combine(projectRoot, projectPath.Replace('/', Path.DirectorySeparatorChar));
        }

        private static string AbsolutePathToProjectPath(string absolutePath)
        {
            string projectRoot = GetProjectRoot().Replace('\\', '/').TrimEnd('/');
            string normalized = absolutePath.Replace('\\', '/');
            if (!normalized.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
                return null;

            return normalized.Substring(projectRoot.Length).TrimStart('/');
        }

        private static string GetProjectRoot()
        {
            return Directory.GetParent(Application.dataPath).FullName;
        }

        private static string FormatBytes(long bytes)
        {
            if (bytes < 1024L)
                return bytes + " B";
            if (bytes < 1024L * 1024L)
                return (bytes / 1024f).ToString("0.##") + " KB";
            if (bytes < 1024L * 1024L * 1024L)
                return (bytes / (1024f * 1024f)).ToString("0.##") + " MB";
            return (bytes / (1024f * 1024f * 1024f)).ToString("0.##") + " GB";
        }

        private static void PingAsset(string path)
        {
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
            if (obj == null)
                return;

            EditorGUIUtility.PingObject(obj);
            Selection.activeObject = obj;
        }

        private sealed class ScanCandidate
        {
            public string path;
            public string guid;
            public string absolutePath;
            public FileInfo fileInfo;
        }

        private sealed class UsageAnalysisJob
        {
            public List<DuplicateGroup> targetGroups;
            public bool autoPickMaster;
            public List<string> referenceFiles;
            public int index;
            public bool cancelRequested;
            public Dictionary<DuplicateAssetInfo, HashSet<string>> usageBuffer;
            public Dictionary<string, List<DuplicateAssetInfo>> guidToAssets;
        }

        [Serializable]
        private sealed class ResolverCache
        {
            public int version;
            public string createdUtc;
            public string updatedUtc;
            public List<HashCacheEntry> hashEntries = new List<HashCacheEntry>();
            public List<ReferenceCacheEntry> referenceEntries = new List<ReferenceCacheEntry>();
        }

        [Serializable]
        private sealed class HashCacheEntry
        {
            public string path;
            public string guid;
            public long sizeBytes;
            public long writeTicksUtc;
            public string sha256;
        }

        [Serializable]
        private sealed class ReferenceCacheEntry
        {
            public string path;
            public long sizeBytes;
            public long writeTicksUtc;
            public string metaGuid;
            public List<string> guids = new List<string>();
        }

        [Serializable]
        private sealed class DuplicateAssetInfo
        {
            public string path;
            public string guid;
            public long sizeBytes;
            public bool hasSubAssets;
            public int subAssetCount;
            public readonly List<string> usedBy = new List<string>();
        }

        [Serializable]
        private sealed class DuplicateGroup
        {
            public string hash;
            public string shortHash;
            public string extension;
            public long sizeBytes;
            public string masterPath;
            public bool foldout;
            public readonly List<DuplicateAssetInfo> assets = new List<DuplicateAssetInfo>();

            public void PickMostUsedAsMaster()
            {
                if (assets.Count == 0)
                    return;

                DuplicateAssetInfo best = assets
                    .OrderByDescending(a => a.usedBy.Count)
                    .ThenBy(a => a.path.Length)
                    .ThenBy(a => a.path, StringComparer.OrdinalIgnoreCase)
                    .First();

                masterPath = best.path;
            }
        }

        private sealed class RemapPair
        {
            public string fromPath;
            public string fromGuid;
            public string toPath;
            public string toGuid;
            public bool hasSubAssets;
            public List<string> knownUsedBy;
            public Dictionary<long, long> localFileIdMap;
        }

        private sealed class RemapTargetFileSet
        {
            public bool usedOptimizedTargets;
            public List<string> files = new List<string>();
        }

        private sealed class ObjectIdInfo
        {
            public long localId;
            public UnityEngine.Object obj;
        }

        private sealed class RemapResult
        {
            public int modifiedFiles;
            public int localFileIdWarnings;
            public string backupFolder;
            public int scannedFiles;
            public int candidateFiles;
            public bool usedOptimizedTargets;
            public bool canceled;
            public List<string> modifiedProjectPaths = new List<string>();
        }
    }
}
#endif
