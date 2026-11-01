using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
namespace Hung.AI.Editor
{
    /// <summary>Per-node problems shown as badges in the graph and as warnings in the node inspector.</summary>
    public static class AIGraphValidator
    {
        public static Dictionary<string, List<string>> ValidateAll(AIGraphAsset asset)
        {
            var result = new Dictionary<string, List<string>>();
            if (asset == null) return result;
            HashSet<string> reachable = Reachable(asset);
            foreach (AINode node in asset.Nodes)
            {
                if (node == null) continue;
                var issues = new List<string>();
                ValidateNode(asset, node, issues);
                if (!reachable.Contains(node.Id)) issues.Add("Not connected to the root.");
                if (issues.Count > 0) result[node.Id] = issues;
            }
            return result;
        }

        public static void ValidateNode(AIGraphAsset asset, AINode node, List<string> issues)
        {
            IReadOnlyList<string> childIds = AIGraphAsset.ChildIdsOf(node);
            for (int i = 0; i < childIds.Count; i++)
                if (asset.GetNode(childIds[i]) == null) issues.Add($"Missing child '{childIds[i]}'.");
            if (node is DecoratorNode decorator && string.IsNullOrEmpty(decorator.ChildId)) issues.Add("Needs a child.");
            if (node is CompositeNode composite && composite.ChildIds.Count == 0) issues.Add("Has no children.");
            foreach (var (fieldType, keyRef) in KeyRefFieldsOf(node))
            {
                if (!keyRef.IsAssigned)
                {
                    issues.Add("Blackboard key not set.");
                    continue;
                }
                BlackboardKey key = asset.Blackboard.FindById(keyRef.KeyId);
                if (key == null) issues.Add($"Blackboard key '{keyRef.KeyId}' was deleted.");
                else if (!BlackboardTypeMap.Accepts(fieldType, key.Type)) issues.Add($"Blackboard key '{key.Name}' is {key.Type}; this field needs {BlackboardTypeMap.ValueTypeOf(fieldType).Name}.");
            }
        }

        /// <summary>Serialized BlackboardKeyRef fields of a node (public, or private with [SerializeField]), including base classes.</summary>
        public static IEnumerable<BlackboardKeyRef> KeyRefsOf(AINode node)
        {
            foreach (var (_, keyRef) in KeyRefFieldsOf(node)) yield return keyRef;
        }

        /// <summary>Like KeyRefsOf, with each field's declared type (BlackboardRef of T limits which keys fit).</summary>
        public static IEnumerable<(Type fieldType, BlackboardKeyRef keyRef)> KeyRefFieldsOf(AINode node)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
            for (Type type = node.GetType(); type != null && type != typeof(object); type = type.BaseType)
            {
                foreach (FieldInfo field in type.GetFields(flags))
                {
                    if (!typeof(BlackboardKeyRef).IsAssignableFrom(field.FieldType) || field.IsNotSerialized) continue;
                    if (!field.IsPublic && !field.IsDefined(typeof(SerializeField), false)) continue;
                    if (field.GetValue(node) is BlackboardKeyRef keyRef) yield return (field.FieldType, keyRef);
                }
            }
        }

        static HashSet<string> Reachable(AIGraphAsset asset)
        {
            var reachable = new HashSet<string>();
            if (string.IsNullOrEmpty(asset.RootNodeId)) return reachable;
            var stack = new Stack<string>();
            stack.Push(asset.RootNodeId);
            while (stack.Count > 0)
            {
                string id = stack.Pop();
                if (!reachable.Add(id)) continue;
                AINode node = asset.GetNode(id);
                if (node == null) continue;
                IReadOnlyList<string> childIds = AIGraphAsset.ChildIdsOf(node);
                for (int i = 0; i < childIds.Count; i++) stack.Push(childIds[i]);
            }
            return reachable;
        }
    }
}
