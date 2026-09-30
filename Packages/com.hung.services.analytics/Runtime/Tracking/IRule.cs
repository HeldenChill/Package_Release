namespace Hung.Analytics.Tracking
{
    /// <summary>One tracking rule: reacts to facts, updates state through operators, emits events.</summary>
    public interface IRule
    {
        /// <summary>Declares every event this rule may emit, via <see cref="EventEmitter.Declare"/>.</summary>
        void Declare(EventEmitter emit, TrackingSettings settings);
        /// <summary>Handles one fact. Context is already updated for this fact; see <see cref="TrackingContext"/>.</summary>
        void OnFact(in Fact fact, TrackingContext ctx, EventEmitter emit);
    }
}
