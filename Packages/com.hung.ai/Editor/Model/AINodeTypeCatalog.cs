using System;
using System.Collections.Generic;
using UnityEditor;
namespace Hung.AI.Editor
{
    public enum AINodeCategory
    {
        Composite = 0,
        Decorator = 1,
        Condition = 2,
        Action = 3,
        Missing = 4,
        Perception = 5
    }

    public sealed class AINodeTypeInfo
    {
        public Type Type;
        public string Path;
        public string Name;
        public string Description;
        public AINodeCategory Category;
    }

    /// <summary>Every concrete node class in loaded assemblies, described by its [AINode] attribute.</summary>
    public static class AINodeTypeCatalog
    {
        static List<AINodeTypeInfo> all;

        /// <summary>Cached; rebuilt after every domain reload (script compile).</summary>
        public static IReadOnlyList<AINodeTypeInfo> All => all ??= Build(TypeCache.GetTypesDerivedFrom<AINode>());

        public static List<AINodeTypeInfo> Build(IEnumerable<Type> types)
        {
            var list = new List<AINodeTypeInfo>();
            foreach (Type type in types)
            {
                if (type == null || type.IsAbstract || type.IsGenericTypeDefinition || type.GetConstructor(Type.EmptyTypes) == null) continue;
                if (!typeof(AINode).IsAssignableFrom(type)) continue;
                if (type.Assembly.GetName().Name.EndsWith(".Tests", StringComparison.Ordinal)) continue;
                list.Add(Describe(type));
            }
            list.Sort((a, b) => string.CompareOrdinal(a.Path, b.Path));
            return list;
        }

        public static AINodeTypeInfo Describe(Type type)
        {
            AINodeAttribute attribute = AttributeOf(type);
            string path = PathOf(type);
            int slash = path.LastIndexOf('/');
            return new AINodeTypeInfo
            {
                Type = type,
                Path = path,
                Name = slash >= 0 ? path.Substring(slash + 1) : path,
                Description = attribute != null && attribute.Description != null ? attribute.Description : string.Empty,
                Category = CategoryOf(type)
            };
        }

        /// <summary>Composite and Decorator by base type; leaves under a "Perception" menu folder are Perception; else Condition or Action.</summary>
        public static AINodeCategory CategoryOf(Type type)
        {
            if (type == null) return AINodeCategory.Missing;
            if (typeof(CompositeNode).IsAssignableFrom(type)) return AINodeCategory.Composite;
            if (typeof(DecoratorNode).IsAssignableFrom(type)) return AINodeCategory.Decorator;
            if (IsPerceptionPath(PathOf(type))) return AINodeCategory.Perception;
            if (typeof(ConditionNode).IsAssignableFrom(type)) return AINodeCategory.Condition;
            return AINodeCategory.Action;
        }

        /// <summary>True for paths with a "Perception" folder, such as the bridge's "Character/Perception/Sense World".</summary>
        public static bool IsPerceptionPath(string path)
        {
            if (string.IsNullOrEmpty(path)) return false;
            return path.StartsWith("Perception/", StringComparison.Ordinal) || path.Contains("/Perception/");
        }

        /// <summary>The node's custom title, or its catalog name when the title is the class name.</summary>
        public static string DisplayName(AINode node)
        {
            if (node == null) return string.Empty;
            return node.Title != node.GetType().Name ? node.Title : Describe(node.GetType()).Name;
        }

        static AINodeAttribute AttributeOf(Type type) => (AINodeAttribute)Attribute.GetCustomAttribute(type, typeof(AINodeAttribute), false);

        static string PathOf(Type type)
        {
            AINodeAttribute attribute = AttributeOf(type);
            return attribute != null && !string.IsNullOrEmpty(attribute.Path) ? attribute.Path : "Other/" + ObjectNames.NicifyVariableName(type.Name);
        }
    }
}
