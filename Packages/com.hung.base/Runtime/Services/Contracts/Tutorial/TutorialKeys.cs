namespace Hung.Base
{
    public static class TutorialKeys
    {
        public const string None = "";

        // Built-in actions (engine resolves via injected delegates / internal logic)
        public const string EnableInput = "tutorial.enable_input";
        public const string DisableInput = "tutorial.disable_input";
        public const string UpdateUI = "tutorial.update_ui";
        public const string GetHandInfo = "tutorial.get_hand_info";
        public const string GetOldHandUIPos = "tutorial.get_old_hand_ui_pos";
        public const string GetOldHandWorldPos = "tutorial.get_old_hand_world_pos";

        // Engine-internal conditions (ShowContentPopup wires these itself)
        public const string ConfirmButtonClick = "tutorial.confirm_button_click";
        public const string VideoPopupClose = "tutorial.video_popup_close";
    }
}
