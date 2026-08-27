using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Base
{
#if UNITY_EDITOR

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using UnityEditor;
    using UnityEngine;

    public class MetaGuidUnifierWindow : EditorWindow
    {
        private const string WindowTitle = "Meta GUID Unifier";
        private const string ExportHeader =
            "# Unity Meta GUID Map v1\n" +
            "# Format: AssetPath<TAB>GUID\n" +
            "# Example: Assets/MyFolder/MyAsset.asset\t0123456789abcdef0123456789abcdef\n";

        private static readonly Regex GuidRegex =
            new Regex("^[a-fA-F0-9]{32}$", RegexOptions.Compiled);

        private readonly List<UnityEngine.Object> _assets = new List<UnityEngine.Object>();

        private Vector2 _scroll;
        private bool _backupBeforeUnify = true;
        private bool _revealExportFile = true;
        private bool _allowReleaseOutsideGuidOwners = false;

        [MenuItem("Tools/Universal/Maintenance/Danger/Meta GUID Unifier")]
        private static void Open()
        {
            GetWindow<MetaGuidUnifierWindow>(WindowTitle);
        }

        private void OnGUI()
        {
            EditorGUILayout.Space(6);

            EditorGUILayout.HelpBox(
                "Kéo asset/file/folder trong Assets vào đây. Nếu kéo folder, tool sẽ tự thêm toàn bộ asset con bên trong. " +
                "Export sẽ tạo file text chứa AssetPath và GUID. Unify sẽ đọc file text rồi ghi đè GUID hiện tại trong .meta theo GUID đã export. " +
                "Tool cho phép swap GUID nếu toàn bộ asset liên quan cùng nằm trong file map. " +
                "Nếu asset cũ đã bị xóa/đổi tên nhưng vẫn đang giữ GUID cần dùng, bật tùy chọn release outside owner bên dưới.",
                MessageType.Info
            );

            DrawDropArea();

            EditorGUILayout.Space(8);

            DrawToolbar();

            EditorGUILayout.Space(8);

            _backupBeforeUnify = EditorGUILayout.ToggleLeft("Backup .meta before Unify", _backupBeforeUnify);
            _revealExportFile = EditorGUILayout.ToggleLeft("Reveal exported file after export", _revealExportFile);
            _allowReleaseOutsideGuidOwners = EditorGUILayout.ToggleLeft(
                "Allow release/steal GUID from assets outside map (for deleted/renamed stale assets)",
                _allowReleaseOutsideGuidOwners
            );

            if (_allowReleaseOutsideGuidOwners)
            {
                EditorGUILayout.HelpBox(
                    "Danger mode: if a target GUID is currently owned by an asset outside the map, " +
                    "the tool will assign a temporary new GUID to that outside owner first, then apply the target GUID to the mapped asset. " +
                    "Use this only when the outside owner is obsolete, deleted, or renamed on the correct source project.",
                    MessageType.Warning
                );
            }

            EditorGUILayout.Space(8);

            DrawAssetList();
        }

        private void DrawToolbar()
        {
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Add Selected Assets", GUILayout.Height(28)))
            {
                AddObjects(Selection.objects);
            }

            if (GUILayout.Button("Clear List", GUILayout.Height(28)))
            {
                _assets.Clear();
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(4);

            EditorGUILayout.BeginHorizontal();

            GUI.enabled = GetValidSelectedEntries().Count > 0;

            if (GUILayout.Button("Export GUID Map", GUILayout.Height(32)))
            {
                ExportGuidMap();
            }

            GUI.enabled = true;

            if (GUILayout.Button("Unify From Text", GUILayout.Height(32)))
            {
                UnifyFromTextFile();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawDropArea()
        {
            Rect dropArea = GUILayoutUtility.GetRect(0, 72, GUILayout.ExpandWidth(true));
            GUI.Box(dropArea, "Drag & Drop Assets / Folders Here");

            Event evt = Event.current;

            if (!dropArea.Contains(evt.mousePosition))
                return;

            switch (evt.type)
            {
                case EventType.DragUpdated:
                    DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                    evt.Use();
                    break;

                case EventType.DragPerform:
                    DragAndDrop.AcceptDrag();

                    if (DragAndDrop.objectReferences != null && DragAndDrop.objectReferences.Length > 0)
                    {
                        AddObjects(DragAndDrop.objectReferences);
                    }

                    if (DragAndDrop.paths != null && DragAndDrop.paths.Length > 0)
                    {
                        foreach (string path in DragAndDrop.paths)
                        {
                            AddAssetPath(path);
                        }
                    }

                    evt.Use();
                    break;
            }
        }

        private void DrawAssetList()
        {
            List<GuidEntry> entries = GetValidSelectedEntries();

            EditorGUILayout.LabelField($"Valid Assets: {entries.Count}", EditorStyles.boldLabel);

            _scroll = EditorGUILayout.BeginScrollView(_scroll);

            for (int i = 0; i < _assets.Count; i++)
            {
                UnityEngine.Object obj = _assets[i];

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();

                UnityEngine.Object newObj = EditorGUILayout.ObjectField(obj, typeof(UnityEngine.Object), false);

                if (newObj != obj)
                {
                    _assets[i] = newObj;
                }

                if (GUILayout.Button("X", GUILayout.Width(28)))
                {
                    _assets.RemoveAt(i);
                    i--;
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }

                EditorGUILayout.EndHorizontal();

                if (obj != null)
                {
                    string path = AssetDatabase.GetAssetPath(obj);
                    string normalizedPath = NormalizeAssetPath(path);
                    string guid = AssetDatabase.AssetPathToGUID(normalizedPath);

                    if (IsAllowedAssetPath(normalizedPath) && IsValidGuid(guid))
                    {
                        EditorGUILayout.SelectableLabel(normalizedPath, GUILayout.Height(18));
                        EditorGUILayout.SelectableLabel(guid, GUILayout.Height(18));
                    }
                    else
                    {
                        EditorGUILayout.HelpBox(
                            "Asset này không hợp lệ hoặc không nằm trong thư mục Assets/. Tool không sửa Packages/ hoặc asset ngoài project.",
                            MessageType.Warning
                        );
                    }
                }

                EditorGUILayout.EndVertical();
            }

            EditorGUILayout.EndScrollView();
        }

        private void AddObjects(UnityEngine.Object[] objects)
        {
            if (objects == null)
                return;

            foreach (UnityEngine.Object obj in objects)
            {
                AddObject(obj);
            }
        }

        private void AddObject(UnityEngine.Object obj)
        {
            if (obj == null)
                return;

            string path = NormalizeAssetPath(AssetDatabase.GetAssetPath(obj));

            if (!IsAllowedAssetPath(path))
            {
                Debug.LogWarning($"[Meta GUID Unifier] Skipped invalid path: {path}");
                return;
            }

            if (AssetDatabase.IsValidFolder(path))
            {
                AddAllAssetsInFolder(path);
                return;
            }

            if (ContainsAssetPath(path))
                return;

            _assets.Add(obj);
        }

        private void AddAllAssetsInFolder(string folderPath)
        {
            folderPath = NormalizeAssetPath(folderPath);

            if (!AssetDatabase.IsValidFolder(folderPath))
                return;

            string[] guids = AssetDatabase.FindAssets(string.Empty, new[] { folderPath });
            int added = 0;

            foreach (string guid in guids)
            {
                string assetPath = NormalizeAssetPath(AssetDatabase.GUIDToAssetPath(guid));

                if (!IsAllowedAssetPath(assetPath))
                    continue;

                if (AssetDatabase.IsValidFolder(assetPath))
                    continue;

                UnityEngine.Object asset = AssetDatabase.LoadMainAssetAtPath(assetPath);

                if (asset == null)
                    continue;

                if (ContainsAssetPath(assetPath))
                    continue;

                _assets.Add(asset);
                added++;
            }

            Debug.Log($"[Meta GUID Unifier] Added {added} assets from folder: {folderPath}");
        }

        private void AddAssetPath(string rawPath)
        {
            string assetPath = NormalizeAssetPath(rawPath);

            if (!IsAllowedAssetPath(assetPath))
                return;

            UnityEngine.Object obj = AssetDatabase.LoadMainAssetAtPath(assetPath);

            if (obj == null)
            {
                Debug.LogWarning($"[Meta GUID Unifier] Cannot load asset at path: {assetPath}");
                return;
            }

            AddObject(obj);
        }

        private bool ContainsAssetPath(string path)
        {
            path = NormalizeAssetPath(path);

            foreach (UnityEngine.Object obj in _assets)
            {
                if (obj == null)
                    continue;

                string existingPath = NormalizeAssetPath(AssetDatabase.GetAssetPath(obj));

                if (existingPath == path)
                    return true;
            }

            return false;
        }

        private void ExportGuidMap()
        {
            List<GuidEntry> entries = GetValidSelectedEntries();

            if (entries.Count == 0)
            {
                EditorUtility.DisplayDialog("Export Failed", "Không có asset hợp lệ để export.", "OK");
                return;
            }

            string savePath = EditorUtility.SaveFilePanel(
                "Export Unity GUID Map",
                Application.dataPath,
                "unity_guid_map.txt",
                "txt"
            );

            if (string.IsNullOrEmpty(savePath))
                return;

            StringBuilder sb = new StringBuilder();
            sb.Append(ExportHeader);

            foreach (GuidEntry entry in entries.OrderBy(e => e.assetPath))
            {
                sb.Append(entry.assetPath);
                sb.Append('\t');
                sb.Append(entry.guid);
                sb.AppendLine();
            }

            File.WriteAllText(savePath, sb.ToString(), Encoding.UTF8);

            AssetDatabase.Refresh();

            if (_revealExportFile)
            {
                EditorUtility.RevealInFinder(savePath);
            }

            EditorUtility.DisplayDialog(
                "Export Complete",
                $"Exported {entries.Count} entries:\n{savePath}",
                "OK"
            );
        }

        private void UnifyFromTextFile()
        {
            string filePath = EditorUtility.OpenFilePanel(
                "Select Unity GUID Map Text File",
                Application.dataPath,
                "txt"
            );

            if (string.IsNullOrEmpty(filePath))
                return;

            List<string> parseErrors;
            List<GuidEntry> entries = ParseGuidMapFile(filePath, out parseErrors);

            if (parseErrors.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Parse Failed",
                    "Format Error:\n\n" + JoinPreview(parseErrors, 12),
                    "OK"
                );
                return;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.ReleaseCachedFileHandles();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);

            List<string> errors;
            List<string> warnings;
            List<UnifyPlanItem> plan = BuildUnifyPlan(entries, out errors, out warnings);

            if (errors.Count > 0)
            {
                EditorUtility.DisplayDialog(
                    "Unify Blocked",
                    "The tool has blocked the operation due to a critical error:\n\n" + JoinPreview(errors, 16),
                    "OK"
                );
                return;
            }

            if (plan.Count == 0)
            {
                EditorUtility.DisplayDialog(
                    "Nothing To Unify",
                    "No valid files require a GUID change.\n\n" + JoinPreview(warnings, 12),
                    "OK"
                );
                return;
            }

            int willChange = plan.Count(x => x.currentGuid != x.targetGuid);
            int alreadySame = plan.Count(x => x.currentGuid == x.targetGuid);
            int willReleaseOutsideOwners = plan.Count(x => x.releaseOutsideOwnerBeforeApply);

            string message =
                $"File map: {filePath}\n\n" +
                $"Total valid entries: {plan.Count}\n" +
                $"Will change: {willChange}\n" +
                $"Already same: {alreadySame}\n" +
                $"Will release outside GUID owners: {willReleaseOutsideOwners}\n";

            if (warnings.Count > 0)
            {
                message += "\nWarnings:\n" + JoinPreview(warnings, 8);
            }

            message += "\n\nBạn nên commit/stash project trước khi chạy. Tiếp tục ghi đè GUID trong .meta?";

            bool confirm = EditorUtility.DisplayDialog(
                "Confirm Unify",
                message,
                "Unify Now",
                "Cancel"
            );

            if (!confirm)
                return;

            ExecuteUnify(plan);
        }

        private void ExecuteUnify(List<UnifyPlanItem> plan)
        {
            string backupRoot = null;

            if (_backupBeforeUnify)
            {
                backupRoot = Path.Combine(
                    "Library",
                    "MetaGuidUnifierBackups",
                    DateTime.Now.ToString("yyyyMMdd_HHmmss")
                );
            }

            int changed = 0;
            int unchanged = 0;
            int releasedOutsideOwners = 0;
            int failed = 0;

            AssetDatabase.SaveAssets();
            AssetDatabase.ReleaseCachedFileHandles();

            AssetDatabase.StartAssetEditing();

            try
            {
                foreach (UnifyPlanItem item in plan)
                {
                    try
                    {
                        if (item.currentGuid == item.targetGuid)
                        {
                            unchanged++;
                            continue;
                        }

                        if (item.releaseOutsideOwnerBeforeApply)
                        {
                            if (string.IsNullOrEmpty(item.outsideOwnerMetaPath) || !File.Exists(item.outsideOwnerMetaPath))
                            {
                                throw new InvalidOperationException(
                                    $"Outside owner meta file is missing: {item.outsideOwnerMetaPath}"
                                );
                            }

                            if (_backupBeforeUnify)
                            {
                                BackupMetaFile(item.outsideOwnerMetaPath, backupRoot);
                            }

                            string releaseError;

                            if (!TryReplaceGuidInMeta(item.outsideOwnerMetaPath, item.outsideOwnerTemporaryGuid, out releaseError))
                            {
                                throw new InvalidOperationException(releaseError);
                            }

                            releasedOutsideOwners++;

                            Debug.Log(
                                $"[Meta GUID Unifier] Released outside GUID owner:\n" +
                                $"{item.outsideOwnerPath}\n" +
                                $"{item.targetGuid} -> {item.outsideOwnerTemporaryGuid}\n" +
                                $"Reason: target GUID is required by {item.assetPath}"
                            );
                        }

                        if (_backupBeforeUnify)
                        {
                            BackupMetaFile(item.metaPath, backupRoot);
                        }

                        string replaceError;

                        if (!TryReplaceGuidInMeta(item.metaPath, item.targetGuid, out replaceError))
                        {
                            throw new InvalidOperationException(replaceError);
                        }

                        changed++;

                        Debug.Log(
                            $"[Meta GUID Unifier] Changed GUID:\n" +
                            $"{item.assetPath}\n" +
                            $"{item.currentGuid} -> {item.targetGuid}"
                        );
                    }
                    catch (Exception ex)
                    {
                        failed++;
                        Debug.LogError($"[Meta GUID Unifier] Failed: {item.assetPath}\n{ex}");
                    }
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                AssetDatabase.ReleaseCachedFileHandles();
                AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            }

            string result =
                $"Unify complete.\n\n" +
                $"Changed: {changed}\n" +
                $"Unchanged: {unchanged}\n" +
                $"Released outside GUID owners: {releasedOutsideOwners}\n" +
                $"Failed: {failed}";

            if (_backupBeforeUnify)
            {
                result += $"\n\nBackup folder:\n{backupRoot}";
            }

            EditorUtility.DisplayDialog("Unify Complete", result, "OK");
        }

        private List<GuidEntry> GetValidSelectedEntries()
        {
            Dictionary<string, GuidEntry> dict = new Dictionary<string, GuidEntry>();

            foreach (UnityEngine.Object obj in _assets)
            {
                if (obj == null)
                    continue;

                string path = NormalizeAssetPath(AssetDatabase.GetAssetPath(obj));

                if (!IsAllowedAssetPath(path))
                    continue;

                string guid = AssetDatabase.AssetPathToGUID(path);

                if (!IsValidGuid(guid))
                    continue;

                if (!dict.ContainsKey(path))
                {
                    dict.Add(path, new GuidEntry(path, guid.ToLowerInvariant()));
                }
            }

            return dict.Values.ToList();
        }

        private List<GuidEntry> ParseGuidMapFile(string filePath, out List<string> errors)
        {
            errors = new List<string>();
            List<GuidEntry> entries = new List<GuidEntry>();

            string[] lines = File.ReadAllLines(filePath, Encoding.UTF8);

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];

                if (string.IsNullOrWhiteSpace(line))
                    continue;

                line = line.Trim();

                if (line.StartsWith("#"))
                    continue;

                string[] parts;

                if (line.Contains("\t"))
                {
                    parts = line.Split('\t');
                }
                else if (line.Contains("|"))
                {
                    parts = line.Split('|');
                }
                else
                {
                    errors.Add($"Line {i + 1}: Missing separator TAB or |");
                    continue;
                }

                if (parts.Length < 2)
                {
                    errors.Add($"Line {i + 1}: Invalid format");
                    continue;
                }

                string assetPath = NormalizeAssetPath(parts[0].Trim());
                string guid = parts[1].Trim().ToLowerInvariant();

                if (!IsAllowedAssetPath(assetPath))
                {
                    errors.Add($"Line {i + 1}: Invalid asset path: {assetPath}");
                    continue;
                }

                if (!IsValidGuid(guid))
                {
                    errors.Add($"Line {i + 1}: Invalid GUID: {guid}");
                    continue;
                }

                entries.Add(new GuidEntry(assetPath, guid));
            }

            return entries;
        }

        private List<UnifyPlanItem> BuildUnifyPlan(
            List<GuidEntry> entries,
            out List<string> errors,
            out List<string> warnings)
        {
            errors = new List<string>();
            warnings = new List<string>();

            List<UnifyPlanItem> plan = new List<UnifyPlanItem>();

            Dictionary<string, GuidEntry> uniqueByPath = new Dictionary<string, GuidEntry>();
            Dictionary<string, string> targetGuidToPath = new Dictionary<string, string>();
            HashSet<string> reservedGuids = new HashSet<string>();

            foreach (GuidEntry entry in entries)
            {
                string normalizedPath = NormalizeAssetPath(entry.assetPath);
                string normalizedGuid = entry.guid.ToLowerInvariant();

                if (uniqueByPath.ContainsKey(normalizedPath))
                {
                    warnings.Add($"Duplicate path ignored: {normalizedPath}");
                    continue;
                }

                GuidEntry normalizedEntry = new GuidEntry(normalizedPath, normalizedGuid);
                uniqueByPath.Add(normalizedPath, normalizedEntry);
                reservedGuids.Add(normalizedGuid);

                string existingPathForTargetGuid;

                if (targetGuidToPath.TryGetValue(normalizedGuid, out existingPathForTargetGuid))
                {
                    if (existingPathForTargetGuid != normalizedPath)
                    {
                        errors.Add(
                            $"Duplicate target GUID in map:\n" +
                            $"{normalizedGuid}\n" +
                            $"{existingPathForTargetGuid}\n" +
                            $"{normalizedPath}"
                        );
                    }
                }
                else
                {
                    targetGuidToPath.Add(normalizedGuid, normalizedPath);
                }
            }

            if (errors.Count > 0)
                return plan;

            HashSet<string> mappedAssetPaths = new HashSet<string>(uniqueByPath.Keys);
            HashSet<string> plannedReleasedOwnerPaths = new HashSet<string>();

            foreach (GuidEntry entry in uniqueByPath.Values)
            {
                if (!File.Exists(entry.assetPath) && !Directory.Exists(entry.assetPath))
                {
                    warnings.Add($"Missing asset skipped: {entry.assetPath}");
                    continue;
                }

                string metaPath = GetMetaPathForAssetPath(entry.assetPath);

                if (!File.Exists(metaPath))
                {
                    errors.Add($"Missing .meta file: {metaPath}");
                    continue;
                }

                string readError;
                string currentGuid = ReadGuidFromMeta(metaPath, out readError);

                if (readError != null)
                {
                    errors.Add(readError);
                    continue;
                }

                if (!IsValidGuid(currentGuid))
                {
                    errors.Add($"Cannot read current GUID from meta: {metaPath}");
                    continue;
                }

                currentGuid = currentGuid.ToLowerInvariant();

                UnifyPlanItem item = new UnifyPlanItem
                {
                    assetPath = entry.assetPath,
                    metaPath = metaPath,
                    currentGuid = currentGuid,
                    targetGuid = entry.guid
                };

                string pathCurrentlyUsingTargetGuid = NormalizeAssetPath(AssetDatabase.GUIDToAssetPath(entry.guid));

                if (!string.IsNullOrEmpty(pathCurrentlyUsingTargetGuid) &&
                    pathCurrentlyUsingTargetGuid != entry.assetPath)
                {
                    bool currentOwnerIsAlsoInMap = mappedAssetPaths.Contains(pathCurrentlyUsingTargetGuid);

                    if (currentOwnerIsAlsoInMap)
                    {
                        warnings.Add(
                            $"GUID swap detected and allowed:\n" +
                            $"Target GUID: {entry.guid}\n" +
                            $"Map wants: {entry.assetPath}\n" +
                            $"Currently used by: {pathCurrentlyUsingTargetGuid}\n" +
                            $"Reason: current owner is also included in this map."
                        );
                    }
                    else if (_allowReleaseOutsideGuidOwners)
                    {
                        string outsideOwnerMetaPath = GetMetaPathForAssetPath(pathCurrentlyUsingTargetGuid);

                        if (!File.Exists(outsideOwnerMetaPath))
                        {
                            errors.Add(
                                $"Target GUID is reported by AssetDatabase as owned by another asset, but its .meta file cannot be found:\n" +
                                $"Target GUID: {entry.guid}\n" +
                                $"Map wants: {entry.assetPath}\n" +
                                $"Reported owner: {pathCurrentlyUsingTargetGuid}\n" +
                                $"Expected meta: {outsideOwnerMetaPath}\n\n" +
                                $"Try right-click Project window > Reimport, or restart Unity, then run Unify again."
                            );

                            continue;
                        }

                        string outsideOwnerReadError;
                        string outsideOwnerCurrentGuid = ReadGuidFromMeta(outsideOwnerMetaPath, out outsideOwnerReadError);

                        if (outsideOwnerReadError != null)
                        {
                            errors.Add(outsideOwnerReadError);
                            continue;
                        }

                        if (!IsValidGuid(outsideOwnerCurrentGuid))
                        {
                            errors.Add($"Cannot read current GUID from outside owner meta: {outsideOwnerMetaPath}");
                            continue;
                        }

                        outsideOwnerCurrentGuid = outsideOwnerCurrentGuid.ToLowerInvariant();

                        if (outsideOwnerCurrentGuid != entry.guid)
                        {
                            errors.Add(
                                $"AssetDatabase and .meta disagree for outside GUID owner. Operation blocked:\n" +
                                $"Target GUID: {entry.guid}\n" +
                                $"Reported owner: {pathCurrentlyUsingTargetGuid}\n" +
                                $"Owner meta GUID: {outsideOwnerCurrentGuid}\n\n" +
                                $"Try AssetDatabase.Refresh/Reimport or restart Unity before running again."
                            );

                            continue;
                        }

                        if (plannedReleasedOwnerPaths.Contains(pathCurrentlyUsingTargetGuid))
                        {
                            errors.Add(
                                $"Outside owner is scheduled to be released more than once. Operation blocked:\n" +
                                $"Owner: {pathCurrentlyUsingTargetGuid}"
                            );

                            continue;
                        }

                        item.releaseOutsideOwnerBeforeApply = true;
                        item.outsideOwnerPath = pathCurrentlyUsingTargetGuid;
                        item.outsideOwnerMetaPath = outsideOwnerMetaPath;
                        item.outsideOwnerCurrentGuid = outsideOwnerCurrentGuid;
                        item.outsideOwnerTemporaryGuid = GenerateTemporaryGuid(reservedGuids);

                        reservedGuids.Add(item.outsideOwnerTemporaryGuid);
                        plannedReleasedOwnerPaths.Add(pathCurrentlyUsingTargetGuid);

                        warnings.Add(
                            $"Outside GUID owner will be released before applying target GUID:\n" +
                            $"Target GUID: {entry.guid}\n" +
                            $"Map wants: {entry.assetPath}\n" +
                            $"Currently used by obsolete/renamed asset: {pathCurrentlyUsingTargetGuid}\n" +
                            $"Temporary GUID for outside owner: {item.outsideOwnerTemporaryGuid}"
                        );
                    }
                    else
                    {
                        errors.Add(
                            $"Target GUID already belongs to another asset outside this map. Operation blocked:\n" +
                            $"Target GUID: {entry.guid}\n" +
                            $"Map wants: {entry.assetPath}\n" +
                            $"Currently used by: {pathCurrentlyUsingTargetGuid}\n\n" +
                            $"If this owner was deleted or renamed on the correct source project, enable: \"Allow release/steal GUID from assets outside map\"."
                        );

                        continue;
                    }
                }

                plan.Add(item);
            }

            return plan;
        }

        private string GetMetaPathForAssetPath(string assetPath)
        {
            string metaPath = AssetDatabase.GetTextMetaFilePathFromAssetPath(assetPath);

            if (string.IsNullOrEmpty(metaPath))
            {
                metaPath = assetPath + ".meta";
            }

            return NormalizeAssetPath(metaPath);
        }

        private string GenerateTemporaryGuid(HashSet<string> reservedGuids)
        {
            for (int i = 0; i < 64; i++)
            {
                string guid = Guid.NewGuid().ToString("N").ToLowerInvariant();

                if (reservedGuids != null && reservedGuids.Contains(guid))
                    continue;

                string existingPath = NormalizeAssetPath(AssetDatabase.GUIDToAssetPath(guid));

                if (string.IsNullOrEmpty(existingPath))
                    return guid;
            }

            throw new InvalidOperationException("Could not generate a unique temporary GUID.");
        }

        private string ReadGuidFromMeta(string metaPath)
        {
            return ReadGuidFromMeta(metaPath, out _);
        }

        // A meta file with 2+ "guid:" lines is corrupt (e.g. an unresolved cross-branch GUID
        // collision merged as literal duplicate lines instead of a real conflict). Returning the
        // first match silently would let Unify "succeed" while leaving the stray duplicate line
        // in place. Surface it as an explicit error instead so BuildUnifyPlan blocks the file.
        private string ReadGuidFromMeta(string metaPath, out string error)
        {
            error = null;
            string[] lines = File.ReadAllLines(metaPath, Encoding.UTF8);

            string foundGuid = null;
            int guidLineCount = 0;

            foreach (string rawLine in lines)
            {
                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                string line = rawLine.Trim();

                if (!line.StartsWith("guid:", StringComparison.Ordinal))
                    continue;

                guidLineCount++;

                if (guidLineCount > 1)
                    continue;

                string guid = line.Substring("guid:".Length).Trim();

                // Phòng trường hợp sau GUID có thêm ký tự thừa.
                char[] separators = { ' ', '\t', '\r', '\n', ',', '#' };
                guid = guid.Split(separators, StringSplitOptions.RemoveEmptyEntries)[0];

                foundGuid = IsValidGuid(guid) ? guid.ToLowerInvariant() : null;
            }

            if (guidLineCount > 1)
            {
                error = $"Meta file has {guidLineCount} 'guid:' lines (corrupt, likely an unresolved GUID collision merge): {metaPath}";
                return null;
            }

            return foundGuid;
        }
        private bool TryReplaceGuidInMeta(string metaPath, string targetGuid, out string error)
        {
            error = null;

            if (!IsValidGuid(targetGuid))
            {
                error = $"Invalid target GUID: {targetGuid}";
                return false;
            }

            string text = File.ReadAllText(metaPath, Encoding.UTF8);

            string newline = text.Contains("\r\n") ? "\r\n" : "\n";

            string[] lines = text.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);

            int guidLineCount = 0;
            for (int i = 0; i < lines.Length; i++)
            {
                if (lines[i].TrimStart().StartsWith("guid:", StringComparison.Ordinal))
                    guidLineCount++;
            }

            // A corrupt meta (e.g. an unresolved cross-branch GUID collision merged as two literal
            // "guid:" lines) must never be written to — replacing only the first match would leave
            // the stray duplicate behind and the file would still be invalid YAML afterward.
            if (guidLineCount > 1)
            {
                error = $"Meta file has {guidLineCount} 'guid:' lines (corrupt, likely an unresolved GUID collision merge) - refusing to write: {metaPath}";
                return false;
            }

            for (int i = 0; i < lines.Length; i++)
            {
                string rawLine = lines[i];

                if (string.IsNullOrWhiteSpace(rawLine))
                    continue;

                string trimmedStart = rawLine.TrimStart();

                if (!trimmedStart.StartsWith("guid:", StringComparison.Ordinal))
                    continue;

                int leadingSpaceCount = rawLine.Length - trimmedStart.Length;
                string leadingSpaces = rawLine.Substring(0, leadingSpaceCount);

                string currentGuid = trimmedStart.Substring("guid:".Length).Trim();

                char[] separators = { ' ', '\t', '\r', '\n', ',', '#' };
                currentGuid = currentGuid.Split(separators, StringSplitOptions.RemoveEmptyEntries)[0];

                if (!IsValidGuid(currentGuid))
                {
                    error = $"Found guid line but current GUID is invalid in meta: {metaPath}";
                    return false;
                }

                lines[i] = $"{leadingSpaces}guid: {targetGuid.ToLowerInvariant()}";

                File.WriteAllText(metaPath, string.Join(newline, lines), Encoding.UTF8);
                return true;
            }

            error = $"Cannot find guid line in meta: {metaPath}";
            return false;
        }
        private void BackupMetaFile(string metaPath, string backupRoot)
        {
            if (string.IsNullOrEmpty(backupRoot))
                return;

            string backupPath = Path.Combine(backupRoot, metaPath + ".bak");
            string backupDir = Path.GetDirectoryName(backupPath);

            if (!Directory.Exists(backupDir))
            {
                Directory.CreateDirectory(backupDir);
            }

            File.Copy(metaPath, backupPath, true);
        }

        private static string NormalizeAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return string.Empty;

            path = path.Replace("\\", "/");

            string projectRoot = Directory.GetCurrentDirectory().Replace("\\", "/");

            if (path.StartsWith(projectRoot + "/", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(projectRoot.Length + 1);
            }

            return path.Trim();
        }

        private static bool IsAllowedAssetPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;

            path = NormalizeAssetPath(path);

            if (path == "Assets")
                return false;

            return path.StartsWith("Assets/", StringComparison.Ordinal);
        }

        private static bool IsValidGuid(string guid)
        {
            return !string.IsNullOrEmpty(guid) && GuidRegex.IsMatch(guid);
        }

        private static string JoinPreview(List<string> lines, int max)
        {
            if (lines == null || lines.Count == 0)
                return string.Empty;

            StringBuilder sb = new StringBuilder();

            int count = Mathf.Min(lines.Count, max);

            for (int i = 0; i < count; i++)
            {
                sb.AppendLine(lines[i]);
            }

            if (lines.Count > max)
            {
                sb.AppendLine($"...and {lines.Count - max} more.");
            }

            return sb.ToString();
        }

        private struct GuidEntry
        {
            public string assetPath;
            public string guid;

            public GuidEntry(string assetPath, string guid)
            {
                this.assetPath = assetPath;
                this.guid = guid;
            }
        }

        private class UnifyPlanItem
        {
            public string assetPath;
            public string metaPath;
            public string currentGuid;
            public string targetGuid;

            public bool releaseOutsideOwnerBeforeApply;
            public string outsideOwnerPath;
            public string outsideOwnerMetaPath;
            public string outsideOwnerCurrentGuid;
            public string outsideOwnerTemporaryGuid;
        }
    }

#endif
}
