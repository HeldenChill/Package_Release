namespace Hung.Base
{
    /// <summary>Standard base package audio cue identifiers used by base UI and features.</summary>
    public static class BaseAudioIds
    {
        /// <summary>Standard UI click sound cue (<c>base.ui.click</c>).</summary>
        public static readonly AudioId UiClick = new("base.ui.click");

        /// <summary>Standard UI popup show sound cue (<c>base.ui.popup_show</c>).</summary>
        public static readonly AudioId UiPopupShow = new("base.ui.popup_show");

        /// <summary>Standard UI popup hide sound cue (<c>base.ui.popup_hide</c>).</summary>
        public static readonly AudioId UiPopupHide = new("base.ui.popup_hide");

        /// <summary>Standard item claim sound cue (<c>base.item.claim</c>).</summary>
        public static readonly AudioId ItemClaim = new("base.item.claim");

        /// <summary>Standard spin end sound cue (<c>base.spin.end</c>).</summary>
        public static readonly AudioId SpinEnd = new("base.spin.end");

        /// <summary>Standard spin wheel sound cue (<c>base.spin.wheel</c>).</summary>
        public static readonly AudioId SpinWheel = new("base.spin.wheel");
    }
}
