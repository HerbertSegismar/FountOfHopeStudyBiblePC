using System;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Helpers
{
    /// <summary>
    /// Single source of truth for the app palette. Raises <see cref="Changed"/>
    /// on any mutation so views/controls can repaint.
    /// </summary>
    public static class ThemeManager
    {
        private static Color _primaryColor = Color.FromRgb(0x67, 0x50, 0xA4);
        private static bool _darkTheme;
        private static bool _isCustomColor;

        /// <summary>
        /// Raised after any of <see cref="PrimaryColor"/>, <see cref="DarkTheme"/>,
        /// or <see cref="IsCustomColor"/> changes. May be raised on any thread;
        /// subscribers should marshal to their dispatcher.
        /// </summary>
        public static event Action? Changed;

        public static Color PrimaryColor
        {
            get => _primaryColor;
            set
            {
                if (_primaryColor == value) return;
                _primaryColor = value;
                OnChanged();
            }
        }

        public static bool DarkTheme
        {
            get => _darkTheme;
            set
            {
                if (_darkTheme == value) return;
                _darkTheme = value;
                OnChanged();
            }
        }

        public static bool IsCustomColor
        {
            get => _isCustomColor;
            set
            {
                if (_isCustomColor == value) return;
                _isCustomColor = value;
                OnChanged();
            }
        }

        /// <summary>Convenience: change all three at once with a single event.</summary>
        public static void Apply(Color primary, bool darkTheme, bool isCustomColor)
        {
            var changed = false;
            if (_primaryColor != primary) { _primaryColor = primary; changed = true; }
            if (_darkTheme != darkTheme) { _darkTheme = darkTheme; changed = true; }
            if (_isCustomColor != isCustomColor) { _isCustomColor = isCustomColor; changed = true; }
            if (changed) OnChanged();
        }

        private static void OnChanged() => Changed?.Invoke();
    }
}