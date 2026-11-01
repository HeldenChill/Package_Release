namespace Hung.AI.Editor
{
    /// <summary>Symbols the editor draws with JetBrains Mono. If the font test fails for one, replace it with the ASCII fallback in its comment.</summary>
    public static class AIGlyphs
    {
        public const string Play = "▶"; // fallback ">"
        public const string Pause = "❚❚"; // fallback "||"
        public const string KeyRef = "◆"; // fallback "*"
        public const string Arrow = "→"; // fallback "->"
        public const string More = "⋯"; // fallback "..."
        public const string All = Play + Pause + KeyRef + Arrow + More;
    }
}
