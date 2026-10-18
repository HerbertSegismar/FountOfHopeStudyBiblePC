using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Helpers
{
    /// <summary>
    /// Installs / refreshes the theme brushes in
    /// <see cref="Application.Current"/>.<c>Resources</c>.
    ///
    /// XAML uses these via <c>{DynamicResource X}</c>. When a brush instance
    /// is replaced, every DynamicResource reference in the visual tree
    /// repaints — no per-element code-behind required.
    ///
    /// The constant string values are deliberately the same keys
    /// <c>MainWindow.ApplyTheme</c> used to write inline (AppBackground,
    /// CardBackground, OnSurface, …) so existing XAML keeps working.
    /// </summary>
    public static class ThemeResources
    {
        // Brush keys (referenced from C# through these constants).
        public const string Primary = "AppPrimaryBrush";      // primary accent
        public const string PrimaryColor = "AppPrimaryColor";      // legacy: raw Color
        public const string PrimaryTint = "AppPrimaryTintBrush";  // primary @ 20%
        public const string Text = "OnSurface";            // legacy
        public const string SecondaryText = "OnSurfaceVariant";     // legacy
        public const string Icon = "AppIconBrush";
        public const string Card = "CardBackground";       // legacy
        public const string CardHover = "CardBackgroundAlt";    // legacy
        public const string Border = "DividerBrush";         // legacy
        public const string Hover = "HoverOverlayBrush";    // legacy
        public const string PageBackground = "AppBackground";        // legacy

        /// <summary>Call once at startup, before any view is created.</summary>
        public static void Install() => Refresh();

        /// <summary>
        /// Replaces every theme resource with a fresh instance derived from
        /// <see cref="ThemeManager"/>. Subscribing this to
        /// <c>ThemeManager.Changed</c> makes the whole app reactive.
        /// </summary>
        public static void Refresh()
        {
            var app = Application.Current;
            if (app == null) return;

            var primary = ThemeManager.PrimaryColor;
            var isDark = ThemeManager.DarkTheme;
            var res = app.Resources;

            // ── Primary (theme accent) ────────────────────────────────
            res[PrimaryColor] = primary;                                            // Color
            res[Primary] = Freeze(new SolidColorBrush(primary));               // Brush
            res[PrimaryTint] = Freeze(new SolidColorBrush(
                                    Color.FromArgb(0x33, primary.R, primary.G, primary.B)));

            // ── Foreground / surface palette ──────────────────────────
            if (isDark)
            {
                res[PageBackground] = Freeze(new SolidColorBrush(Color.FromRgb(0x10, 0x14, 0x18)));
                res[Card] = Freeze(new SolidColorBrush(Color.FromRgb(0x1C, 0x21, 0x28)));
                res[CardHover] = Freeze(new SolidColorBrush(Color.FromRgb(0x23, 0x2A, 0x33)));
                res[Text] = Freeze(new SolidColorBrush(Color.FromRgb(0xE6, 0xEA, 0xF0)));
                res[SecondaryText] = Freeze(new SolidColorBrush(Color.FromRgb(0xAA, 0xB4, 0xC0)));
                res[Border] = Freeze(new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)));
                res[Hover] = Freeze(new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)));
                res[Icon] = Freeze(new SolidColorBrush(Color.FromRgb(0xE6, 0xEA, 0xF0)));
            }
            else
            {
                res[PageBackground] = Freeze(new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)));
                res[Card] = Freeze(new SolidColorBrush(Colors.White));
                res[CardHover] = Freeze(new SolidColorBrush(Color.FromRgb(0xF0, 0xF0, 0xF0)));
                res[Text] = Freeze(new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)));
                res[SecondaryText] = Freeze(new SolidColorBrush(Color.FromRgb(0x6B, 0x72, 0x80)));
                res[Border] = Freeze(new SolidColorBrush(Color.FromArgb(0x22, 0x00, 0x00, 0x00)));
                res[Hover] = Freeze(new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x00, 0x00)));
                res[Icon] = Freeze(new SolidColorBrush(Color.FromRgb(0x1A, 0x1A, 0x1A)));
            }
        }

        private static Brush Freeze(Brush b) { b.Freeze(); return b; }
    }
}