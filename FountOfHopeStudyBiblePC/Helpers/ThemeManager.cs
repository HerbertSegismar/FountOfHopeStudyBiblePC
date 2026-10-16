using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class ThemeManager
    {
        public static Color PrimaryColor { get; set; } = Color.FromRgb(0x67, 0x50, 0xA4);
        public static bool DarkTheme { get; set; }
        public static bool IsCustomColor { get; set; }
    }
}