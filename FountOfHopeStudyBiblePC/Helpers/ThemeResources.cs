using System.Windows;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class ThemeResources
    {
        public const string Primary = "AppPrimaryBrush";
        public const string PrimaryColor = "AppPrimaryColor";
        public const string PrimaryTint = "AppPrimaryTintBrush";
        public const string Text = "OnSurface";
        public const string SecondaryText = "OnSurfaceVariant";
        public const string Icon = "AppIconBrush";
        public const string Card = "CardBackground";
        public const string CardHover = "CardBackgroundAlt";
        public const string Border = "DividerBrush";
        public const string Hover = "HoverOverlayBrush";
        public const string PageBackground = "AppBackground";

        private sealed record Palette(
            Brush PageBackground,
            Brush Card,
            Brush CardHover,
            Brush Text,
            Brush SecondaryText,
            Brush Border,
            Brush Hover,
            Brush Icon);

        private static readonly Palette DarkPalette = new(
            PageBackground: Frozen(Color.FromRgb(0x10, 0x14, 0x18)),
            Card: Frozen(Color.FromRgb(0x1C, 0x21, 0x28)),
            CardHover: Frozen(Color.FromRgb(0x23, 0x2A, 0x33)),
            Text: Frozen(Color.FromRgb(0xE6, 0xEA, 0xF0)),
            SecondaryText: Frozen(Color.FromRgb(0xAA, 0xB4, 0xC0)),
            Border: Frozen(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)),
            Hover: Frozen(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
            Icon: Frozen(Color.FromRgb(0xE6, 0xEA, 0xF0)));

        private static readonly Palette LightPalette = new(
            PageBackground: Frozen(Color.FromRgb(0xF5, 0xF5, 0xF5)),
            Card: Frozen(Colors.White),
            CardHover: Frozen(Color.FromRgb(0xF0, 0xF0, 0xF0)),
            Text: Frozen(Color.FromRgb(0x1A, 0x1A, 0x1A)),
            SecondaryText: Frozen(Color.FromRgb(0x6B, 0x72, 0x80)),
            Border: Frozen(Color.FromArgb(0x22, 0x00, 0x00, 0x00)),
            Hover: Frozen(Color.FromArgb(0x33, 0x00, 0x00, 0x00)),
            Icon: Frozen(Color.FromRgb(0x1A, 0x1A, 0x1A)));

        private static Color _cachedPrimary;
        private static Brush? _cachedPrimaryBrush;
        private static Brush? _cachedPrimaryTint;

        public static void Install() => Refresh();

        public static void Refresh()
        {
            var app = Application.Current;
            if (app == null) return;

            var primary = ThemeManager.PrimaryColor;
            var isDark = ThemeManager.DarkTheme;
            var res = app.Resources;

            if (_cachedPrimaryBrush is null || primary != _cachedPrimary)
            {
                _cachedPrimary = primary;
                _cachedPrimaryBrush = Frozen(primary);
                _cachedPrimaryTint = Frozen(Color.FromArgb(0x33, primary.R, primary.G, primary.B));
            }

            res[PrimaryColor] = primary;
            res[Primary] = _cachedPrimaryBrush;
            res[PrimaryTint] = _cachedPrimaryTint;

            var p = isDark ? DarkPalette : LightPalette;
            res[PageBackground] = p.PageBackground;
            res[Card] = p.Card;
            res[CardHover] = p.CardHover;
            res[Text] = p.Text;
            res[SecondaryText] = p.SecondaryText;
            res[Border] = p.Border;
            res[Hover] = p.Hover;
            res[Icon] = p.Icon;
        }

        private static SolidColorBrush Frozen(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }
    }
}