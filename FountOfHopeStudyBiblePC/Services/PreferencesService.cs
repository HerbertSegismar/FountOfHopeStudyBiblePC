// Services/PreferencesService.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Services
{
    public class PreferencesService
    {
        private readonly Dictionary<string, object?> _data = new();
        private readonly string _filePath;

        public PreferencesService()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FountOfHopeStudyBiblePC");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "app_preferences.json");
            Load();
        }

        // ── Typed getters ───────────────────────────────────────────────
        public int GetInt(string key, int def = 0)
            => _data.TryGetValue(key, out var v) && v != null ? Convert.ToInt32(v) : def;

        public float GetFloat(string key, float def = 0f)
            => _data.TryGetValue(key, out var v) && v != null ? Convert.ToSingle(v) : def;

        public bool GetBool(string key, bool def = false)
            => _data.TryGetValue(key, out var v) && v != null ? Convert.ToBoolean(v) : def;

        public string GetString(string key, string def = "")
            => _data.TryGetValue(key, out var v) && v != null ? v.ToString() ?? def : def;

        // ── Typed setters ───────────────────────────────────────────────
        public void Set(string key, object? value) { _data[key] = value; Save(); }

        // ── Helpers for ARGB <-> Color ──────────────────────────────────
        public static int ColorToArgb(Color c) => (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B;

        public static Color ArgbToColor(int argb) => Color.FromArgb(
            (byte)((argb >> 24) & 0xFF),
            (byte)((argb >> 16) & 0xFF),
            (byte)((argb >> 8) & 0xFF),
            (byte)(argb & 0xFF));

        public Color GetColor(string key, Color def)
            => ArgbToColor(GetInt(key, ColorToArgb(def)));

        public void SetColor(string key, Color value)
            => Set(key, ColorToArgb(value));

        // ── Persistence ─────────────────────────────────────────────────
        private void Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return;
                var json = File.ReadAllText(_filePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (dict == null) return;
                foreach (var kv in dict)
                {
                    _data[kv.Key] = kv.Value.ValueKind switch
                    {
                        JsonValueKind.Number => kv.Value.TryGetInt32(out var i) ? i : kv.Value.GetDouble(),
                        JsonValueKind.True => true,
                        JsonValueKind.False => false,
                        JsonValueKind.String => kv.Value.GetString(),
                        _ => null
                    };
                }
            }
            catch { /* ignore corrupt prefs */ }
        }

        private void Save()
        {
            try { File.WriteAllText(_filePath, JsonSerializer.Serialize(_data)); }
            catch { /* ignore */ }
        }
    }

    // Preference key constants (matches the Kotlin `*_KEY` values)
    public static class PrefKeys
    {
        // ── Reader typography / theme ───────────────────────────────
        public const string LightThemeReaderFontColor = "light_theme_reader_font_color";
        public const string DarkThemeReaderFontColor = "dark_theme_reader_font_color";
        public const string MarkerColor = "marker_color";
        public const string FontSize = "font_size";
        public const string FontFamily = "font_family";

        // ── Passage position (primary) ──────────────────────────────
        public const string PrimaryBookNumber = "primary_book_number";
        public const string PrimaryBookName = "primary_book_name";
        public const string PrimaryChapter = "primary_chapter";
        public const string PrimaryVerse = "primary_verse";

        // ── Passage position (secondary) ────────────────────────────
        public const string SecondaryBookNumber = "secondary_book_number";
        public const string SecondaryBookName = "secondary_book_name";
        public const string SecondaryChapter = "secondary_chapter";
        public const string SecondaryVerse = "secondary_verse";

        // ── Theme ───────────────────────────────────────────────────
        public const string DarkTheme = "dark_theme";
        public const string SelectedColor = "selected_color";
        public const string IsCustomColor = "is_custom_color";
        public const string CustomColor = "custom_color";

        // ── Primary Bible version ───────────────────────────────────
        public const string PrimaryDb = "primary_db";
        public const string PrimaryAbbr = "primary_abbr";

        // ── Secondary Bible version (multi-version reader) ──────────
        public const string MultiVersion = "multi_version";
        public const string SecondaryDb = "secondary_db";
        public const string SecondaryAbbr = "secondary_abbr";
        public const string MultiLayout = "multi_layout";
        public const string ScrollSync = "scroll_sync";

        // ── Reader background texture / overlay ─────────────────────
        public const string BgIndex = "bg_index";
        public const string CustomTexture = "custom_texture";
        public const string OverlayOpacity = "overlay_opacity";
        public const string LightOverlayColor = "light_overlay_color";
        public const string DarkOverlayColor = "dark_overlay_color";

        // ── Study mode / strongs / dictionary mode ──────────────────
        public const string IsStudyMode = "is_study_mode";
        public const string ShowStrongs = "show_strongs";
        public const string IsDictionaryMode = "is_dictionary_mode";

        // ── Reader colours ──────────────────────────────────────────
        public const string VerseMarkerColor = "verse_marker_color";
        public const string WordsOfJesus = "words_of_jesus";
        public const string HeaderButtonsColor = "header_buttons_color";

        // ── Modal palette ───────────────────────────────────────────
        public const string LightModalBgColor = "light_modal_bg_color";
        public const string DarkModalBgColor = "dark_modal_bg_color";

        // ── Dictionaries / commentary / cross-refs ──────────────────
        public const string SelectedDictionary = "selected_dictionary";
        public const string PrimaryDictLanguage = "primary_dict_language";
        public const string SecondaryDictLanguage = "secondary_dict_language";
        public const string SecondaryDictionary = "secondary_dictionary";
        public const string SelectedVerseCommentary = "selected_verse_commentary";
        public const string SelectedCrossRefDb = "selected_cross_ref_db";

        // ── Highlight palette / list state ──────────────────────────
        public const string PredefinedHighlightColors = "predefined_highlight_colors";
        public const string RenderOrbs = "show_orbs";
        public const string OrbsCount = "orbs_count";
        public const string DisabledVersions = "disabled_versions";

        // ── Window sizing ───────────────────────────────────────────
        public const string CustomWidth = "custom_width";
        public const string CustomHeight = "custom_height";

        // ── App-wide fullscreen (persists across restarts) ──────────
        public const string IsReaderFullScreen = "is_reader_full_screen";
    }
}