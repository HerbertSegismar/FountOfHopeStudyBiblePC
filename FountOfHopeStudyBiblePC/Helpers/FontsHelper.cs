// Helpers/FontsHelper.cs
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class FontsHelper
    {
        /// <summary>
        /// All bundled font keys, matching the Kotlin `availableFontFamilies` list.
        /// The UI displays these with the first letter capitalized.
        /// </summary>
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

        /// <summary>
        /// Maps each Kotlin key → (TTF file name, internal font family name).
        /// The internal family name must match the font's metadata — that's
        /// what WPF uses after the `#` in the pack URI.
        /// </summary>
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

        /// <summary>
        /// Cache — FontFamily objects are expensive to construct and are
        /// already immutable once created, so caching is safe.
        /// </summary>
        private static readonly Dictionary<string, FontFamily> _cache =
            new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Returns a WPF FontFamily for the given key.
        /// Falls back to the system font if the key is unknown or the font
        /// fails to load (missing TTF, wrong family name, etc.).
        /// </summary>
        public static FontFamily GetFontFamily(string key)
        {
            if (string.IsNullOrEmpty(key) || key.Equals("system", StringComparison.OrdinalIgnoreCase))
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
                // Pack URI points at the embedded resource folder; `#FamilyName`
                // must be the font's internal family name, not the file name.
                var uri = new Uri("pack://application:,,,/Assets/fonts/");
                var family = new FontFamily(uri, $"./#{info.Family}");

                // Verify the family actually has at least one typeface.
                // If not, WPF will silently fall back — we do it explicitly
                // so we can log and cache the fallback correctly.
                bool hasTypeface;
                try
                {
                    hasTypeface = family.GetTypefaces()?.GetEnumerator().MoveNext() == true;
                }
                catch
                {
                    hasTypeface = false;
                }

                if (!hasTypeface)
                {
                    App.Log($"FontsHelper: '{key}' → '{info.Family}' not found in pack, " +
                            $"falling back to Segoe UI (check Assets/fonts/{info.File})");
                    _cache[key] = SystemFontFamily;
                    return SystemFontFamily;
                }

                App.Log($"FontsHelper: '{key}' → loaded '{info.Family}'");
                _cache[key] = family;
                return family;
            }
            catch (Exception ex)
            {
                App.Log($"FontsHelper: '{key}' → EXCEPTION {ex.Message}, falling back to Segoe UI");
                _cache[key] = SystemFontFamily;
                return SystemFontFamily;
            }
        }

        /// <summary>Human-readable display name for the font family pill.</summary>
        public static string GetDisplayName(string key)
        {
            if (string.IsNullOrEmpty(key)) return "System";
            return char.ToUpperInvariant(key[0]) + key.Substring(1);
        }
    }
}