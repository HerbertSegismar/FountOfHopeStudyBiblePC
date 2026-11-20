using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class ThemeManager
    {
        private static Color _primaryColor = Color.FromRgb(0x67, 0x50, 0xA4);
        private static Color _secondaryColor = Color.FromRgb(0x62, 0x50, 0x71);
        private static bool _darkTheme;
        private static bool _isCustomColor;

        public static event Action? Changed;

        public static Color PrimaryColor
        {
            get => _primaryColor;
            set
            {
                if (_primaryColor == value) return;
                _primaryColor = value;
                Changed?.Invoke();
            }
        }

        public static Color SecondaryColor
        {
            get => _secondaryColor;
            set
            {
                if (_secondaryColor == value) return;
                _secondaryColor = value;
                Changed?.Invoke();
            }
        }

        public static bool DarkTheme
        {
            get => _darkTheme;
            set
            {
                if (_darkTheme == value) return;
                _darkTheme = value;
                Changed?.Invoke();
            }
        }

        public static bool IsCustomColor
        {
            get => _isCustomColor;
            set
            {
                if (_isCustomColor == value) return;
                _isCustomColor = value;
                Changed?.Invoke();
            }
        }

        public static void Apply(Color primary, Color secondary, bool darkTheme, bool isCustomColor)
        {
            var changed = _primaryColor != primary
                       || _secondaryColor != secondary
                       || _darkTheme != darkTheme
                       || _isCustomColor != isCustomColor;

            _primaryColor = primary;
            _secondaryColor = secondary;
            _darkTheme = darkTheme;
            _isCustomColor = isCustomColor;

            if (changed) Changed?.Invoke();
        }
    }
}