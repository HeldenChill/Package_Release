using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Gameplay.Character.BrainSystem
{
    // Auto-discovers every IBrainGraphNode implementation in the loaded assemblies and maps it by
    // Kind. Adding a node = drop a new file implementing IBrainGraphNode + add its enum value. No
    // switch to touch. Built lazily once on first Tick.
    public static class BrainGraphNodeRegistry
    {
        private static Dictionary<BrainGraphNodeKind, IBrainGraphNode> handlers;

        public static BrainGraphStatus Tick(BrainGraphNode node, BrainGraphExecutionContext context)
        {
            if (handlers == null) Build();
            if (handlers.TryGetValue(node.kind, out IBrainGraphNode handler))
                return handler.Tick(node, context);

            context.SetMessage($"No handler for node kind {node.kind}.");
            return BrainGraphStatus.Success;
        }

        private static void Build()
        {
            handlers = new Dictionary<BrainGraphNodeKind, IBrainGraphNode>();
            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => a.GetName().Name.StartsWith("Hung.Character"))
                .SelectMany(SafeGetTypes)
                .Where(t => typeof(IBrainGraphNode).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface);

            foreach (Type t in types)
            {
                var handler = (IBrainGraphNode)Activator.CreateInstance(t);
                if (handlers.ContainsKey(handler.Kind))
                {
                    Debug.LogError($"BrainGraph: duplicate handler for kind {handler.Kind} ({t.Name}).");
                    continue;
                }
                handlers[handler.Kind] = handler;
            }
        }

        private static IEnumerable<Type> SafeGetTypes(Assembly a)
        {
            try { return a.GetTypes(); }
            catch (ReflectionTypeLoadException e) { return e.Types.Where(t => t != null); }
        }
    }
}
