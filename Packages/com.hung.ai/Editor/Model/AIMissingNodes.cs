using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
namespace Hung.AI.Editor
{
    public sealed class MissingNodeInfo
    {
        public long ReferenceId;
        public string ClassName;
        public string NodeId;
        public Vector2 Position;
    }

    /// <summary>Nodes whose class was deleted or renamed without [MovedFrom]. Unity keeps their data but loads them as null.</summary>
    public static class AIMissingNodes
    {
        static readonly Regex IdPattern = new Regex(@"(?m)^\s*id:\s*([0-9A-Za-z_\-]+)\s*$");
        static readonly Regex PositionPattern = new Regex(@"editorPosition:\s*\{\s*x:\s*([-+0-9.eE]+)\s*,\s*y:\s*([-+0-9.eE]+)\s*\}");

        public static List<MissingNodeInfo> Read(AIGraphAsset asset)
        {
            var list = new List<MissingNodeInfo>();
            if (asset == null || !SerializationUtility.HasManagedReferencesWithMissingTypes(asset)) return list;
            foreach (ManagedReferenceMissingType missing in SerializationUtility.GetManagedReferencesWithMissingTypes(asset))
            {
                string className = string.IsNullOrEmpty(missing.namespaceName) ? missing.className : missing.namespaceName + "." + missing.className;
                list.Add(Parse(missing.referenceId, className, missing.serializedData));
            }
            return list;
        }

        /// <summary>Reads the node id and editor position out of Unity's serialized text. Falls back to "missing-&lt;ref&gt;" at the origin.</summary>
        public static MissingNodeInfo Parse(long referenceId, string className, string serializedData)
        {
            var info = new MissingNodeInfo { ReferenceId = referenceId, ClassName = className, NodeId = "missing-" + referenceId };
            if (string.IsNullOrEmpty(serializedData)) return info;
            Match id = IdPattern.Match(serializedData);
            if (id.Success) info.NodeId = id.Groups[1].Value;
            Match position = PositionPattern.Match(serializedData);
            if (position.Success
                && float.TryParse(position.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float x)
                && float.TryParse(position.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float y))
                info.Position = new Vector2(x, y);
            return info;
        }

        /// <summary>Deletes every missing node and its kept data. Undoable.</summary>
        public static void RemoveAll(AIGraphAsset asset)
        {
            if (asset == null) return;
            Undo.RecordObject(asset, "Remove Missing AI Nodes");
            asset.RemoveMissingNodes();
            SerializationUtility.ClearAllManagedReferencesWithMissingTypes(asset);
            EditorUtility.SetDirty(asset);
        }
    }
}
