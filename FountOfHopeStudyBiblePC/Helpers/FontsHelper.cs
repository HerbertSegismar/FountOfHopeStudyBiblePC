using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class FontsHelper
    {
        public static readonly string[] AvailableFontFamilies =
        {
            "system",
            "oswald",
            "rubikglitch",
            "rubiklines",
            "poppins",
            "cookie",
            "emilyscandy",
            "googlesanscode",
            "ptserif",
            "pirataone",
            "quintessential",
            "rougescript",
            "sairastencilone",
            "shadowsintolight",
            "smoochsans",
            "truculenta",
            "honk"
        };

        private static readonly Dictionary<string, (string File, string Family)> FontMap =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["oswald"] = ("Oswald.ttf", "Oswald"),
                ["rubikglitch"] = ("RubikGlitch.ttf", "Rubik Glitch"),
                ["rubiklines"] = ("RubikLines.ttf", "Rubik Lines"),
                ["poppins"] = ("Poppins.ttf", "Poppins"),
                ["cookie"] = ("Cookie.ttf", "Cookie"),
                ["emilyscandy"] = ("EmilysCandy.ttf", "Emilys Candy"),
                ["googlesanscode"] = ("GoogleSansCode.ttf", "Google Sans Code"),
                ["ptserif"] = ("PTSerif.ttf", "PT Serif"),
                ["pirataone"] = ("PirataOne.ttf", "Pirata One"),
                ["quintessential"] = ("Quintessential.ttf", "Quintessential"),
                ["rougescript"] = ("RougeScript.ttf", "Rouge Script"),
                ["sairastencilone"] = ("SairaStencilOne.ttf", "Saira Stencil One"),
                ["shadowsintolight"] = ("ShadowsIntoLight.ttf", "Shadows Into Light"),
                ["smoochsans"] = ("SmoochSans.ttf", "Smooch Sans"),
                ["truculenta"] = ("Truculenta.ttf", "Truculenta"),
                ["honk"] = ("HonkVariable.ttf", "Honk"),
            };

        private static readonly FontFamily SystemFontFamily = new("Segoe UI");
        private static readonly Uri FontsBaseUri = new("pack://application:,,,/Assets/fonts/");

        private static readonly Dictionary<string, FontFamily> _cache =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["system"] = SystemFontFamily,
            };

        public static FontFamily GetFontFamily(string key)
        {
            if (string.IsNullOrEmpty(key))
                return SystemFontFamily;

            if (_cache.TryGetValue(key, out var cached))
                return cached;

            if (!FontMap.TryGetValue(key, out var info))
            {
                _cache[key] = SystemFontFamily;
                return SystemFontFamily;
            }

            try
            {
                var family = new FontFamily(FontsBaseUri, $"./#{info.Family}");

                bool hasTypeface;
                try
                {
                    hasTypeface = family.GetTypefaces() is { Count: > 0 };
                }
                catch
                {
                    hasTypeface = false;
                }

                if (!hasTypeface)
                {
                    _cache[key] = SystemFontFamily;
                    return SystemFontFamily;
                }

                _cache[key] = family;
                return family;
            }
            catch
            {
                _cache[key] = SystemFontFamily;
                return SystemFontFamily;
            }
        }

        public static string GetDisplayName(string key)
        {
            if (string.IsNullOrEmpty(key)) return "System";
            return char.ToUpperInvariant(key[0]) + key[1..];
        }
    }
}