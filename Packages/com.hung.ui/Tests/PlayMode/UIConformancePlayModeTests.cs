using System;
using System.Collections;
using System.Reflection;
using System.Threading.Tasks;
using Hung.Base;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Hung.UI.PlayModeTests
{
    public sealed class C0PlayCanvas : UICanvas { }

    public sealed class UIConformancePlayModeTests
    {
        private sealed class ResourceObservation
        {
            public UICanvas Canvas;
            public int Callbacks;
            public bool Cached;
        }

        private static ResourceObservation AcquireResource<T>(UIManager manager) where T : UICanvas
        {
            var result = new ResourceObservation();
            manager.GetUIAsync<T>(canvas => { result.Callbacks++; result.Canvas = canvas; });
            result.Cached = ReferenceEquals(result.Canvas, manager.GetUI<T>());
            return result;
        }

        private sealed class Deferred : IUIAsyncPrefabProvider
        {
            public readonly TaskCompletionSource<UIPrefabLease> Completion = new TaskCompletionSource<UIPrefabLease>();
            public int Releases;
            public Task<UIPrefabLease> LoadAsync(string address) => Completion.Task;
        }

        [UnityTest]
        public IEnumerator Duplicate_owner_is_disabled_and_its_destroy_preserves_first_owner()
        {
            IUIService previous = ServiceSlots.Get<IUIService>();
            Assert.IsNull(previous, "Run only in the existing isolated proof host.");
            GameObject firstRoot = null, secondRoot = null;
            try
            {
                firstRoot = new GameObject("UI-C0 accepted owner", typeof(UIManager));
                var first = firstRoot.GetComponent<UIManager>();
                Assert.AreSame(first, Locator.UI);
                LogAssert.Expect(LogType.Error, "[ServiceSlots] Active scene owner already exists for IUIService.");
                secondRoot = new GameObject("UI-C0 rejected owner", typeof(UIManager));
                Assert.IsFalse(secondRoot.GetComponent<UIManager>().enabled);
                Assert.AreSame(first, Locator.UI);
                UnityEngine.Object.Destroy(secondRoot);
                yield return null;
                Assert.AreSame(first, Locator.UI);
                UnityEngine.Object.Destroy(firstRoot);
                yield return null;
                Assert.IsNull(Locator.UI);
            }
            finally
            {
                if (secondRoot != null) UnityEngine.Object.Destroy(secondRoot);
                if (firstRoot != null) UnityEngine.Object.Destroy(firstRoot);
            }
        }

        [UnityTest]
        public IEnumerator Destroy_during_load_returns_failure_and_releases_late_prefab_once()
        {
            Assert.IsNull(ServiceSlots.Get<IUIService>(), "Run only in the isolated proof host.");
            GameObject root = new GameObject("UI-C0 pending owner", typeof(UIManager));
            GameObject prefab = new GameObject("UI-C0 late prefab", typeof(RectTransform), typeof(C0PlayCanvas));
            prefab.SetActive(false);
            var provider = new Deferred();
            try
            {
                var manager = root.GetComponent<UIManager>();
                manager.SetAsyncProvider(provider);
                var task = manager.AcquireAsync<C0PlayCanvas>("ui/c0-late");
                UnityEngine.Object.Destroy(root);
                yield return null;
                provider.Completion.SetResult(new UIPrefabLease(prefab, () => provider.Releases++));
                float deadline = Time.realtimeSinceStartup + 5f;
                while (!task.IsCompleted && Time.realtimeSinceStartup < deadline) yield return null;
                Assert.IsTrue(task.IsCompleted, "Timed out awaiting late provider completion.");
                Assert.AreEqual(UIAcquireFailure.ManagerDestroyed, task.Result.Failure);
                Assert.IsNull(task.Result.Canvas);
                Assert.AreEqual(1, provider.Releases);
                Assert.IsNull(Locator.UI);
            }
            finally
            {
                if (root != null) UnityEngine.Object.Destroy(root);
                UnityEngine.Object.Destroy(prefab);
            }
        }

        [UnityTest]
        public IEnumerator Existing_resource_popup_uses_manager_callback_and_cache_without_resolver()
        {
            Assert.IsNull(ServiceSlots.Get<IUIService>(), "Run only in the isolated proof host.");
            GameObject prefab = Resources.Load<GameObject>("UI/NoWifiPopup");
            Assert.IsNotNull(prefab, "Existing Resources witness must be present; do not invent a replacement asset.");
            UICanvas authored = prefab.GetComponent<UICanvas>();
            Assert.IsNotNull(authored);
            GameObject root = new GameObject("UI-C0 Resources owner", typeof(UIManager));
            ResourceObservation observed = null;
            try
            {
                var method = typeof(UIConformancePlayModeTests).GetMethod("AcquireResource", BindingFlags.Static | BindingFlags.NonPublic);
                observed = (ResourceObservation)method.MakeGenericMethod(authored.GetType()).Invoke(null,
                    new object[] { root.GetComponent<UIManager>() });
                Assert.AreEqual(1, observed.Callbacks, "Unconfigured Resources callback is synchronous.");
                Assert.IsNotNull(observed.Canvas);
                Assert.IsFalse(observed.Canvas.gameObject.activeSelf);
                Assert.IsTrue(observed.Cached);
                yield return null;
            }
            finally
            {
                if (observed != null && observed.Canvas != null) UnityEngine.Object.Destroy(observed.Canvas.gameObject);
                if (root != null) UnityEngine.Object.Destroy(root);
            }
        }
    }

    // Run alone. Deliberately seeds UI globals after asserting entry reset, for the next real play.
    public sealed class UIRepeatPlayWitnessTests
    {
        private static int entries;
        private static UIBackPushPolicy entryPolicy;
        private static bool entryClick;
        private static int entryStaleObservers;
        private static bool entryReloadEnabled;
        private static int entryReloadFlags;
        private static void StaleObserver(UISCanvas canvas) { }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void CaptureEntry()
        {
            entries++;
            entryPolicy = UICanvas.BackPushPolicy;
            entryClick = UIButton.DefaultClickSfx;
#if UNITY_EDITOR
            entryReloadEnabled = UnityEditor.EditorSettings.enterPlayModeOptionsEnabled;
            entryReloadFlags = (int)UnityEditor.EditorSettings.enterPlayModeOptions;
#endif
            var field = typeof(UISCanvas).GetField("AnyOpened", BindingFlags.Static | BindingFlags.NonPublic);
            var handlers = field.GetValue(null) as Delegate;
            entryStaleObservers = 0;
            if (handlers != null)
                foreach (Delegate handler in handlers.GetInvocationList())
                    if (handler.Method == ((Action<UISCanvas>)StaleObserver).Method) entryStaleObservers++;
        }

        [UnityTest]
        public IEnumerator Fresh_entry_resets_UI_globals_before_product_composition()
        {
#if UNITY_EDITOR
            if (!UnityEditor.SessionState.GetBool("Horizon1.UiC0.RepeatArmed", false))
                Assert.Ignore("Standalone UI repeat-play proof is not armed; ordinary runs must not seed shared state.");
#else
            Assert.Ignore("UI repeat-play proof requires an explicitly armed Editor protocol.");
#endif
            Debug.Log("UI_C0_ENTRY=" + entries + ";policy=" + entryPolicy + ";click=" + entryClick + ";stale=" + entryStaleObservers);
            Assert.Greater(entries, 0, "RuntimeInitialize witness did not run on a real play entry.");
            Assert.AreEqual(UIBackPushPolicy.OnOpen, entryPolicy);
            Assert.IsTrue(entryClick);
            Assert.AreEqual(0, entryStaleObservers);
#if UNITY_EDITOR
            string evidencePath = UnityEditor.SessionState.GetString("Horizon1.UiC0.EntryEvidence", "");
            string root = System.IO.Directory.GetParent(Application.dataPath).FullName;
            string boundary = System.IO.Path.GetFullPath(System.IO.Path.Combine(root,
                "Docs/reports/horizon1-boundaries/service-architecture/ui-p3/review/fixes")) + System.IO.Path.DirectorySeparatorChar;
            string output = System.IO.Path.GetFullPath(System.IO.Path.Combine(root, evidencePath));
            Assert.IsTrue(output.StartsWith(boundary, StringComparison.OrdinalIgnoreCase), "Proof needs an owned evidence output path.");
            Assert.IsFalse(System.IO.File.Exists(output), "Never overwrite proof evidence.");
            var evidence = new EntryEvidence { entry = entries, policy = entryPolicy.ToString(), click = entryClick,
                stale = entryStaleObservers, reloadEnabled = entryReloadEnabled, reloadFlags = entryReloadFlags,
                dataPath = Application.dataPath, utc = DateTime.UtcNow.ToString("O") };
            System.IO.File.WriteAllText(output, JsonUtility.ToJson(evidence, true));
#endif
            UICanvas.BackPushPolicy = UIBackPushPolicy.ExplicitSetup;
            UIButton.DefaultClickSfx = false;
            UISCanvas.AnyOpened += StaleObserver;
            yield return null;
        }

        [Serializable]
        private sealed class EntryEvidence
        {
            public int entry;
            public string policy;
            public bool click;
            public int stale;
            public bool reloadEnabled;
            public int reloadFlags;
            public string dataPath;
            public string utc;
        }
    }
}
