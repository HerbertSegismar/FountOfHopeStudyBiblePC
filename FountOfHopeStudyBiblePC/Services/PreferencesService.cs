using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace FountOfHopeStudyBiblePC.Services
{
    public class PreferencesService
    {
        private static readonly TimeSpan SaveDebounce = TimeSpan.FromMilliseconds(500);

        private readonly Dictionary<string, object?> _data = [];
        private readonly Lock _dataLock = new();
        private readonly string _filePath;
        private readonly Dispatcher? _dispatcher;
        private readonly DispatcherTimer? _saveTimer;
        private bool _dirty;

        public PreferencesService()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "FountOfHopeStudyBiblePC");
            Directory.CreateDirectory(dir);
            _filePath = Path.Combine(dir, "app_preferences.json");
            Load();

            _dispatcher = Application.Current?.Dispatcher;
            if (_dispatcher is not null && _dispatcher.CheckAccess())
            {
                _saveTimer = new DispatcherTimer(
                    SaveDebounce, DispatcherPriority.Background, OnSaveTimerTick, _dispatcher);
                Application.Current!.Exit += (_, _) => Flush();
            }
        }

        private void OnSaveTimerTick(object? sender, EventArgs e)
        {
            _saveTimer?.Stop();
            Flush();
        }

        public int GetInt(string key, int def = 0)
        {
            lock (_dataLock)
            {
                if (!_data.TryGetValue(key, out var v) || v is null) return def;
                return v is int i ? i : Convert.ToInt32(v);
            }
        }

        public float GetFloat(string key, float def = 0f)
        {
            lock (_dataLock)
            {
                if (!_data.TryGetValue(key, out var v) || v is null) return def;
                return v is float f ? f : Convert.ToSingle(v);
            }
        }

        public bool GetBool(string key, bool def = false)
        {
            lock (_dataLock)
            {
                if (!_data.TryGetValue(key, out var v) || v is null) return def;
                return v is bool b ? b : Convert.ToBoolean(v);
            }
        }

        public string GetString(string key, string def = "")
        {
            lock (_dataLock)
            {
                if (!_data.TryGetValue(key, out var v) || v is null) return def;
                return v as string ?? v.ToString() ?? def;
            }
        }

        public void Set(string key, object? value)
        {
            lock (_dataLock)
            {
                _data[key] = value;
                _dirty = true;
            }
            ScheduleSave();
        }

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

        public void Flush()
        {
            Dictionary<string, object?>? snapshot = null;
            lock (_dataLock)
            {
                if (_dirty)
                {
                    _dirty = false;
                    snapshot = new Dictionary<string, object?>(_data);
                }
            }
            if (snapshot is null) return;

            try
            {
                var merged = MergeWithExisting(snapshot);
                File.WriteAllText(_filePath, JsonSerializer.Serialize(merged));
            }
            catch
            {
                lock (_dataLock) { _dirty = true; }
            }
        }

        private void ScheduleSave()
        {
            if (_saveTimer is null || _dispatcher is null || !_dispatcher.CheckAccess())
            {
                Flush();
                return;
            }
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        private Dictionary<string, object?> MergeWithExisting(Dictionary<string, object?> inMemory)
        {
            var merged = new Dictionary<string, object?>(inMemory);
            if (!File.Exists(_filePath)) return merged;

            try
            {
                var json = File.ReadAllText(_filePath);
                var existing = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (existing is null) return merged;

                foreach (var kv in existing)
                {
                    if (!merged.ContainsKey(kv.Key))
                        merged[kv.Key] = DecodeJsonElement(kv.Value);
                }
            }
            catch { }

            return merged;
        }

        private void Load()
        {
            try
            {
                if (!File.Exists(_filePath)) return;
                var json = File.ReadAllText(_filePath);
                var dict = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(json);
                if (dict is null) return;

                lock (_dataLock)
                {
                    foreach (var kv in dict)
                        _data[kv.Key] = DecodeJsonElement(kv.Value);
                }
            }
            catch { }
        }

        private static object? DecodeJsonElement(JsonElement element) => element.ValueKind switch
        {
            JsonValueKind.Number => element.TryGetInt32(out var i) ? i : element.GetDouble(),
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            JsonValueKind.String => element.GetString(),
            _ => null
        };
    }

    public static class PrefKeys
    {
        public const string LightThemeReaderFontColor = "light_theme_reader_font_color";
        public const string DarkThemeReaderFontColor = "dark_theme_reader_font_color";
        public const string MarkerColor = "marker_color";
        public const string FontSize = "font_size";
        public const string FontFamily = "font_family";

        public const string PrimaryBookNumber = "primary_book_number";
        public const string PrimaryBookName = "primary_book_name";
        public const string PrimaryChapter = "primary_chapter";
        public const string PrimaryVerse = "primary_verse";

        public const string SecondaryBookNumber = "secondary_book_number";
        public const string SecondaryBookName = "secondary_book_name";
        public const string SecondaryChapter = "secondary_chapter";
        public const string SecondaryVerse = "secondary_verse";

        public const string DarkTheme = "dark_theme";
        public const string SelectedColor = "selected_color";
        public const string IsCustomColor = "is_custom_color";
        public const string CustomColor = "custom_color";

        public const string PrimaryDb = "primary_db";
        public const string PrimaryAbbr = "primary_abbr";

        public const string MultiVersion = "multi_version";
        public const string SecondaryDb = "secondary_db";
        public const string SecondaryAbbr = "secondary_abbr";
        public const string MultiLayout = "multi_layout";
        public const string ScrollSync = "scroll_sync";

        public const string BgIndex = "bg_index";
        public const string CustomTexture = "custom_texture";
        public const string OverlayOpacity = "overlay_opacity";
        public const string LightOverlayColor = "light_overlay_color";
        public const string DarkOverlayColor = "dark_overlay_color";

        public const string IsStudyMode = "is_study_mode";
        public const string ShowStrongs = "show_strongs";
        public const string IsDictionaryMode = "is_dictionary_mode";

        public const string VerseMarkerColor = "verse_marker_color";
        public const string WordsOfJesus = "words_of_jesus";
        public const string HeaderButtonsColor = "header_buttons_color";

        public const string LightModalBgColor = "light_modal_bg_color";
        public const string DarkModalBgColor = "dark_modal_bg_color";

        public const string SelectedDictionary = "selected_dictionary";
        public const string PrimaryDictLanguage = "primary_dict_language";
        public const string SecondaryDictLanguage = "secondary_dict_language";
        public const string SecondaryDictionary = "secondary_dictionary";
        public const string SelectedVerseCommentary = "selected_verse_commentary";
        public const string SelectedCrossRefDb = "selected_cross_ref_db";

        public const string PredefinedHighlightColors = "predefined_highlight_colors";
        public const string RenderOrbs = "show_orbs";
        public const string OrbsCount = "orbs_count";
        public const string DisabledVersions = "disabled_versions";

        public const string CustomWidth = "custom_width";
        public const string CustomHeight = "custom_height";

        public const string IsReaderFullScreen = "is_reader_full_screen";
    }
}