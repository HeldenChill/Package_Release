using UnityEngine;

namespace Hung.Analytics.Tracking
{
    /// <summary>
    /// Feeds the pipeline from Unity. Start begins the pipeline on the first frame, after every BeforeSceneLoad bootstrap (persistence
    /// included) and after scene Awakes, so game rules registered in Awake see ColdStart. Update sends foreground minutes, and pause
    /// sends Blur/Focus.
    /// </summary>
    internal sealed class TrackingRunner : MonoBehaviour
    {
        TrackingPipeline _pipeline;
        bool _focused = true;

        internal static TrackingRunner Create(TrackingPipeline pipeline)
        {
            var go = new GameObject("[HungAnalyticsTracking]");
            DontDestroyOnLoad(go);
            var runner = go.AddComponent<TrackingRunner>();
            runner._pipeline = pipeline;
            return runner;
        }

        void Start() => _pipeline.Start();

        // ponytail: one Tick per frame through every rule; batch per second if profiling ever shows it.
        void Update()
        {
            if (_focused) _pipeline.Dispatch(Fact.Tick(Time.unscaledDeltaTime / 60.0));
        }

        void OnApplicationPause(bool paused)
        {
            if (paused != _focused) return;
            _focused = !paused;
            _pipeline.Dispatch(paused ? Fact.Blur() : Fact.Focus());
        }

        void OnApplicationQuit()
        {
            if (!_focused) return;
            _focused = false;
            _pipeline.Dispatch(Fact.Blur());
        }
    }
}
