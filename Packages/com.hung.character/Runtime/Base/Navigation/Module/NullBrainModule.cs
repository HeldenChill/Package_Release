namespace Gameplay.Character.BrainSystem
{
    /// <summary>
    /// Null Object brain for non-moving negatives. Writes no intent; BrainData stays zeroed.
    /// </summary>
    public class NullBrainModule : BrainModule
    {
        public override void StartNavigation() { }
        public override void StopNavigation() { }
        public override void UpdateData() { }
    }
}
