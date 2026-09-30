using System.Collections.Generic;

namespace Hung.Analytics.Tracking
{
    /// <summary>What happened. Facts are the only input to the tracking pipeline.</summary>
    public enum FactKind { ColdStart, Tick, Blur, Focus, Monetize, StageStart, WaveReached, StageEnd, Progress, Gacha, Feature, Tutorial }

    /// <summary>How a stage attempt ended. Abandon means the player quit to Home mid-stage (D3).</summary>
    public enum StageResult { Win, Fail, Abandon }

    /// <summary>A monetization action; it resolves a pending gacha feeling as positive.</summary>
    public enum MonetizeKind { RewardedAd, Iap }

    /// <summary>Tutorial flow lifecycle point.</summary>
    public enum TutorialPhase { Start, End, Abort }

    /// <summary>
    /// One immutable fact. Build it with the static factories. Read it with the accessors that match its
    /// <see cref="Kind"/>; accessors of other kinds return defaults.
    /// </summary>
    public readonly struct Fact
    {
        /// <summary>What kind of fact this is.</summary>
        public readonly FactKind Kind;
        readonly int _int;
        readonly int _code;
        readonly bool _flag;
        readonly double _amount;
        readonly string _id;
        readonly string _text;
        readonly IReadOnlyDictionary<string, object> _args;

        Fact(FactKind kind, int i = 0, int code = 0, bool flag = false, double amount = 0,
            string id = null, string text = null, IReadOnlyDictionary<string, object> args = null)
        {
            Kind = kind;
            _int = i;
            _code = code;
            _flag = flag;
            _amount = amount;
            _id = id;
            _text = text;
            _args = args;
        }

        /// <summary>The app process started.</summary>
        public static Fact ColdStart() => new Fact(FactKind.ColdStart);
        /// <summary>Foreground time passed, in minutes.</summary>
        public static Fact Tick(double minutes) => new Fact(FactKind.Tick, amount: minutes);
        /// <summary>The app went to the background.</summary>
        public static Fact Blur() => new Fact(FactKind.Blur);
        /// <summary>The app came back to the foreground.</summary>
        public static Fact Focus() => new Fact(FactKind.Focus);
        /// <summary>A rewarded ad completed, or an IAP completed.</summary>
        public static Fact Monetize(MonetizeKind kind) => new Fact(FactKind.Monetize, code: (int)kind);
        /// <summary>A stage attempt started.</summary>
        public static Fact StageStart(int stage, bool replay) => new Fact(FactKind.StageStart, i: stage, flag: replay);
        /// <summary>A wave started in the current stage.</summary>
        public static Fact WaveReached(int wave) => new Fact(FactKind.WaveReached, i: wave);
        /// <summary>The current stage attempt ended.</summary>
        public static Fact StageEnd(StageResult result) => new Fact(FactKind.StageEnd, code: (int)result);
        /// <summary>The player's next uncleared stage changed.</summary>
        public static Fact Progress(int currentStage) => new Fact(FactKind.Progress, i: currentStage);
        /// <summary>A gacha pull.</summary>
        public static Fact Gacha(string pool, int count, string costType, long costAmount) =>
            new Fact(FactKind.Gacha, i: count, amount: costAmount, id: pool, text: costType);
        /// <summary>The player used feature <paramref name="id"/>.</summary>
        public static Fact Feature(string id, IReadOnlyDictionary<string, object> args) =>
            new Fact(FactKind.Feature, id: id, args: args);
        /// <summary>A tutorial flow reached <paramref name="phase"/>.</summary>
        public static Fact Tutorial(string flowId, TutorialPhase phase, bool skipped) =>
            new Fact(FactKind.Tutorial, code: (int)phase, flag: skipped, id: flowId);

        /// <summary>StageStart: the stage index.</summary>
        public int Stage => _int;
        /// <summary>StageStart: whether the stage was cleared before.</summary>
        public bool Replay => _flag;
        /// <summary>WaveReached: the wave index.</summary>
        public int Wave => _int;
        /// <summary>StageEnd: how the attempt ended.</summary>
        public StageResult Result => (StageResult)_code;
        /// <summary>Progress: the next uncleared stage.</summary>
        public int CurrentStage => _int;
        /// <summary>Tick: foreground minutes.</summary>
        public double Minutes => _amount;
        /// <summary>Monetize: which action.</summary>
        public MonetizeKind Monetization => (MonetizeKind)_code;
        /// <summary>Gacha: the pool id.</summary>
        public string Pool => _id;
        /// <summary>Gacha: the pull count.</summary>
        public int Count => _int;
        /// <summary>Gacha: the currency kind (gem, ad, free, ticket).</summary>
        public string CostType => _text;
        /// <summary>Gacha: the cost amount.</summary>
        public long CostAmount => (long)_amount;
        /// <summary>Feature: the feature id. Tutorial: the flow id.</summary>
        public string Id => _id;
        /// <summary>Feature: extra params; may be null.</summary>
        public IReadOnlyDictionary<string, object> Args => _args;
        /// <summary>Tutorial: the lifecycle point.</summary>
        public TutorialPhase Phase => (TutorialPhase)_code;
        /// <summary>Tutorial: completed by skipping.</summary>
        public bool Skipped => _flag;
    }
}
