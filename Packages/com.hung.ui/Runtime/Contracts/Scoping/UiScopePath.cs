using System;

namespace Hung.UI.Scoping
{
    /// <summary>
    /// A node in the UI scope tree, written as slash-separated segments
    /// ("Gameplay", "Gameplay/TraderPhase", "Home/Shop").
    ///
    /// Every popup auto-close decision in the game reduces to <see cref="IsAtOrUnder"/>, so the
    /// comparison is segment-wise on purpose: a plain string prefix test would report
    /// "HomeExtra" as living inside "Home" and keep stale popups alive across a transition.
    ///
    /// Source design: PVM .cursor/plans/ui-scope-hierarchy-popup-lifecycle.md
    /// </summary>
    public readonly struct UiScopePath : IEquatable<UiScopePath>
    {
        const char Separator = '/';

        /// <summary>No scope. Under nothing, contains nothing — the value a Global canvas binds to.</summary>
        public static readonly UiScopePath None = default;

        readonly string _value;

        UiScopePath(string value) => _value = value;

        public string Value => _value ?? string.Empty;

        public bool HasValue => !string.IsNullOrEmpty(_value);

        /// <summary>Null, empty, or whitespace all collapse to <see cref="None"/>.</summary>
        public static UiScopePath Of(string value) =>
            string.IsNullOrWhiteSpace(value) ? None : new UiScopePath(value.Trim());

        /// <summary>
        /// True when this path IS <paramref name="ancestor"/> or lives beneath it.
        /// <see cref="None"/> is neither an ancestor nor a descendant of anything, including itself.
        /// </summary>
        public bool IsAtOrUnder(UiScopePath ancestor)
        {
            if (!HasValue || !ancestor.HasValue) return false;
            if (string.Equals(_value, ancestor._value, StringComparison.Ordinal)) return true;

            // The separator is what makes this segment-wise rather than a string prefix.
            return _value.Length > ancestor._value.Length
                   && _value[ancestor._value.Length] == Separator
                   && _value.StartsWith(ancestor._value, StringComparison.Ordinal);
        }

        public bool Equals(UiScopePath other) =>
            string.Equals(_value, other._value, StringComparison.Ordinal);

        public override bool Equals(object obj) => obj is UiScopePath other && Equals(other);

        public override int GetHashCode() =>
            _value == null ? 0 : StringComparer.Ordinal.GetHashCode(_value);

        public override string ToString() => HasValue ? _value : "<none>";

        public static bool operator ==(UiScopePath a, UiScopePath b) => a.Equals(b);
        public static bool operator !=(UiScopePath a, UiScopePath b) => !a.Equals(b);
    }
}
