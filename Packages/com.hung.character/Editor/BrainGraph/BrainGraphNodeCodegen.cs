using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Gameplay.Character.BrainSystem;

namespace Gameplay.Character.EditorTools
{
    // Behavior-Graph-style "new custom node type" codegen. Given a name + category it:
    //   1) inserts a new value into the BrainGraphNodeKind enum (next free int in the category band)
    //   2) writes a <Name>Node.cs stub implementing IBrainGraphNode
    //   3) refreshes AssetDatabase (Unity recompiles) and opens the stub in the IDE.
    // After recompile the kind appears in the grouped create menu and the registry auto-discovers it.
    public static class BrainGraphNodeCodegen
    {
        public static readonly string[] Categories = { "Flow", "Perception", "Action", "Logic", "Debug" };

        // Band base per category (matches BrainGraphEditorWindow.CategoryOf and the enum layout).
        private static int BandBase(string category)
        {
            switch (category)
            {
                case "Flow": return 0;
                case "Perception": return 100;
                case "Action": return 200;
                case "Logic": return 300;
                default: return 900; // Debug
            }
        }

        private static int BandMax(string category) => BandBase(category) + 99;

        public static bool Create(string rawName, string category, out string error)
        {
            error = null;
            string name = SanitizeName(rawName);
            if (string.IsNullOrEmpty(name)) { error = "Invalid node name."; return false; }
            if (Enum.GetNames(typeof(BrainGraphNodeKind)).Contains(name)) { error = $"Node kind '{name}' already exists."; return false; }

            string enumPath = FindScriptPath("BrainGraphTypes");
            if (enumPath == null) { error = "Could not locate BrainGraphTypes.cs."; return false; }

            int value = NextFreeValue(category);
            if (value < 0) { error = $"No free enum value left in the {category} band."; return false; }

            if (!InsertEnumValue(enumPath, name, value, out error)) return false;

            string nodesDir = Path.Combine(Path.GetDirectoryName(enumPath), "Nodes");
            string scriptPath = Path.Combine(nodesDir, name + "Node.cs");
            if (File.Exists(scriptPath)) { error = $"{name}Node.cs already exists."; return false; }
            File.WriteAllText(scriptPath, BuildStub(name));

            AssetDatabase.Refresh();
            AssetDatabase.ImportAsset(scriptPath.Replace(Application.dataPath, "Assets"));
            EditorApplication.delayCall += () =>
            {
                var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(ToProjectRelative(scriptPath));
                if (ms != null) AssetDatabase.OpenAsset(ms);
            };
            return true;
        }

        private static int NextFreeValue(string category)
        {
            int baseVal = BandBase(category);
            int max = BandMax(category);
            int highest = baseVal;
            foreach (BrainGraphNodeKind k in Enum.GetValues(typeof(BrainGraphNodeKind)))
            {
                int v = (int)k;
                if (v >= baseVal && v <= max && v > highest) highest = v;
            }
            int next = highest + 10;
            // Round odd existing values (e.g. Debug 900/999) up to a clean +10 slot.
            return next <= max ? next : -1;
        }

        // Insert "        <Name> = <value>,\n" before the enum's closing brace.
        private static bool InsertEnumValue(string path, string name, int value, out string error)
        {
            error = null;
            string text = File.ReadAllText(path);
            Match m = Regex.Match(text, @"enum\s+BrainGraphNodeKind\s*\{");
            if (!m.Success) { error = "Could not find BrainGraphNodeKind enum body."; return false; }

            int braceStart = text.IndexOf('{', m.Index);
            int braceEnd = FindMatchingBrace(text, braceStart);
            if (braceEnd < 0) { error = "Malformed enum (no closing brace)."; return false; }

            // Ensure the last existing member has a trailing comma so our insert is valid.
            string body = text.Substring(braceStart + 1, braceEnd - braceStart - 1);
            string trimmed = body.TrimEnd();
            if (!trimmed.EndsWith(",")) trimmed += ",";
            string insertion = trimmed + $"\n        {name} = {value}\n    ";
            string result = text.Substring(0, braceStart + 1) + insertion + text.Substring(braceEnd);
            File.WriteAllText(path, result);
            return true;
        }

        private static int FindMatchingBrace(string text, int openIndex)
        {
            int depth = 0;
            for (int i = openIndex; i < text.Length; i++)
            {
                if (text[i] == '{') depth++;
                else if (text[i] == '}') { depth--; if (depth == 0) return i; }
            }
            return -1;
        }

        private static string BuildStub(string name)
        {
            return
$@"namespace Gameplay.Character.BrainSystem
{{
    // Custom BrainGraph node. Fill in Tick with the behavior; it is auto-discovered by
    // BrainGraphNodeRegistry via the Kind below — no registration needed.
    public sealed class {name}Node : IBrainGraphNode
    {{
        public BrainGraphNodeKind Kind => BrainGraphNodeKind.{name};

        public BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {{
            context.SetMessage(""{name} (stub)."");
            return BrainGraphStatus.Success;
        }}
    }}
}}
";
        }

        private static string SanitizeName(string raw)
        {
            if (string.IsNullOrWhiteSpace(raw)) return null;
            string cleaned = Regex.Replace(raw.Trim(), @"[^A-Za-z0-9_]", "");
            if (cleaned.Length == 0) return null;
            if (char.IsDigit(cleaned[0])) cleaned = "_" + cleaned;
            return char.ToUpper(cleaned[0]) + cleaned.Substring(1);
        }

        private static string FindScriptPath(string scriptName)
        {
            string guid = AssetDatabase.FindAssets($"{scriptName} t:MonoScript").FirstOrDefault();
            if (guid == null) return null;
            return Path.GetFullPath(AssetDatabase.GUIDToAssetPath(guid));
        }

        private static string ToProjectRelative(string fullPath)
        {
            string assets = Path.GetFullPath(Application.dataPath);
            return "Assets" + fullPath.Substring(assets.Length).Replace('\\', '/');
        }
    }
}
