namespace Hung.Base
{
    /// <summary>Live product-owned mute preferences; no aggregate or write policy.</summary>
    public interface IAudioSettings
    {
        bool IsBgmMuted { get; }
        bool IsSfxMuted { get; }
    }
}
