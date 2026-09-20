using System.Collections.Generic;

namespace Gameplay.Character.BrainSystem
{
    public static class BrainGraphRuntimeRegistry
    {
        private static readonly List<BrainGraphModule> Modules = new();
        public static IReadOnlyList<BrainGraphModule> ActiveModules => Modules;

        public static void Register(BrainGraphModule module)
        {
            if (module == null) return;
            if (!Modules.Contains(module)) Modules.Add(module);
        }

        public static void Unregister(BrainGraphModule module)
        {
            if (module == null) return;
            Modules.Remove(module);
        }

        public static BrainGraphModule FindFirstByGraph(BrainGraphAsset graph)
        {
            if (graph == null) return null;
            for (int i = Modules.Count - 1; i >= 0; i--)
            {
                BrainGraphModule module = Modules[i];
                if (module == null)
                {
                    Modules.RemoveAt(i);
                    continue;
                }
                if (module.Graph == graph) return module;
            }
            return null;
        }
    }
}
