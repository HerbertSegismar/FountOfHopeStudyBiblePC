// MainWindow.xaml.cs
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;
using FountOfHopeStudyBiblePC.Views;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC
{
    public partial class MainWindow : Window
    {
        private readonly AppViewModel _viewModel;
        private readonly PreferencesService _prefs = new();
        private DatabaseHelper _databaseHelper;
        private DatabaseHelper? _secondaryDbHelper;

        // Animator template flow (equivalent of pendingTemplateUri / animatorTemplateUri)
        private Uri? _pendingTemplateUri;
        private Uri? _animatorTemplateUri;

        public MainWindow()
        {
            App.Log("MainWindow ctor - enter");
            InitializeComponent();
            App.Log("MainWindow ctor - after InitializeComponent");

            // Loaded is a routed event, not an overridable method
            Loaded += (_, _) => App.Log("MainWindow.Loaded - event fired");

            _viewModel = new AppViewModel();
            ViewModels.AppViewModelAccessor.Current = _viewModel;
            App.Log("MainWindow ctor - after AppViewModel");

            LoadPreferences();
            App.Log("MainWindow ctor - after LoadPreferences");

            HookPreferenceSaving();
            App.Log("MainWindow ctor - after HookPreferenceSaving");

            HookViewModelEvents();
            App.Log("MainWindow ctor - after HookViewModelEvents");

            _databaseHelper = new DatabaseHelper(_viewModel.CurrentDbName);
            App.Log("MainWindow ctor - after DatabaseHelper");

            _viewModel.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(AppViewModel.CurrentDbName):
                        _databaseHelper?.Dispose();
                        _databaseHelper = new DatabaseHelper(_viewModel.CurrentDbName);
                        break;
                    case nameof(AppViewModel.SecondaryDbName):
                        _secondaryDbHelper?.Dispose();
                        _secondaryDbHelper = new DatabaseHelper(_viewModel.SecondaryDbName);
                        break;
                    case nameof(AppViewModel.DarkTheme):
                    case nameof(AppViewModel.SelectedColor):
                    case nameof(AppViewModel.IsCustomColor):
                        ApplyTheme();
                        break;
                }
            };
            App.Log("MainWindow ctor - after PropertyChanged hookup");

            NavigateToCurrentScreen();
            App.Log("MainWindow ctor - after NavigateToCurrentScreen");

            HandleIntent(_pendingTemplateUri);
            App.Log("MainWindow ctor - COMPLETE");
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            App.Log("MainWindow.OnSourceInitialized");
            base.OnSourceInitialized(e);
        }

        protected override void OnContentRendered(EventArgs e)
        {
            App.Log("MainWindow.OnContentRendered - window is on screen");
            base.OnContentRendered(e);
        }

        protected override void OnActivated(EventArgs e)
        {
            App.Log("MainWindow.OnActivated");
            base.OnActivated(e);
        }

        protected override void OnDeactivated(EventArgs e)
        {
            App.Log("MainWindow.OnDeactivated");
            base.OnDeactivated(e);
        }

        protected override void OnStateChanged(EventArgs e)
        {
            App.Log("MainWindow.OnStateChanged - WindowState=" + WindowState);
            base.OnStateChanged(e);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            App.Log("!!! MainWindow.OnClosing - Cancel=" + e.Cancel);
            base.OnClosing(e);
        }

        protected override void OnClosed(EventArgs e)
        {
            App.Log("MainWindow.OnClosed");
            base.OnClosed(e);
            _databaseHelper?.Dispose();
            _secondaryDbHelper?.Dispose();
        }

        // ── Intent / deep-link handling (equivalent of handleIntent) ────
        public void HandleIncomingUri(Uri uri) => HandleIntent(uri);

        private void HandleIntent(Uri? uri)
        {
            if (uri == null) return;
            _pendingTemplateUri = uri;

            if (_viewModel.NavigationStack.Count == 0 ||
                _viewModel.NavigationStack[^1] is not Screen.Animator)
            {
                _viewModel.NavigateTo(new Screen.Animator());
            }

            // Consume: mark as ready for the Animator screen
            _animatorTemplateUri = _pendingTemplateUri;
            _pendingTemplateUri = null;
            NavigateToCurrentScreen();
        }

        // ── Preference loading (equivalent of LaunchedEffect(Unit) read) ─
        private void LoadPreferences()
        {
            _viewModel.CustomWidth = _prefs.GetInt(PrefKeys.CustomWidth, 1920);
            _viewModel.CustomHeight = _prefs.GetInt(PrefKeys.CustomHeight, 1080);
            _viewModel.FontSize = _prefs.GetInt(PrefKeys.FontSize, 18);

            _viewModel.LightThemeReaderFontColor = _prefs.GetColor(PrefKeys.LightThemeReaderFontColor, Color.FromRgb(0x10, 0x10, 0x15));
            _viewModel.DarkThemeReaderFontColor = _prefs.GetColor(PrefKeys.DarkThemeReaderFontColor, Colors.White);

            _viewModel.DarkTheme = _prefs.GetBool(PrefKeys.DarkTheme, true);
            _viewModel.SelectedColor = _prefs.GetColor(PrefKeys.SelectedColor, DefaultPrimaryColor);
            _viewModel.IsCustomColor = _prefs.GetBool(PrefKeys.IsCustomColor, false);
            _viewModel.CustomColor = _prefs.GetColor(PrefKeys.CustomColor, DefaultPrimaryColor);

            _viewModel.SelectedFontFamily = _prefs.GetString(PrefKeys.FontFamily, "system");

            _viewModel.CurrentDbName = _prefs.GetString(PrefKeys.PrimaryDb, "kj2.sqlite3");
            _viewModel.CurrentVersionAbbr = _prefs.GetString(PrefKeys.PrimaryAbbr,
                BibleVersionUtils.VersionMap.GetValueOrDefault("kj2.sqlite3", "Bible"));

            _viewModel.MultiVersion = _prefs.GetBool(PrefKeys.MultiVersion, false);
            _viewModel.SecondaryDbName = _prefs.GetString(PrefKeys.SecondaryDb, "kjv+.sqlite3");
            _viewModel.SecondaryVersionAbbr = _prefs.GetString(PrefKeys.SecondaryAbbr,
                BibleVersionUtils.VersionMap.GetValueOrDefault("kjv+.sqlite3", "Bible"));
            _viewModel.MultiViewLayout = _prefs.GetString(PrefKeys.MultiLayout, "horizontal");
            _viewModel.ScrollSync = _prefs.GetBool(PrefKeys.ScrollSync, true);

            _viewModel.BgImageIndex = _prefs.GetInt(PrefKeys.BgIndex, 0);
            _viewModel.CustomTextureUri = _prefs.GetString(PrefKeys.CustomTexture, "") is { Length: > 0 } s ? s : null;
            _viewModel.OverlayOpacity = _prefs.GetFloat(PrefKeys.OverlayOpacity, 0.8f);
            _viewModel.LightOverlayColor = _prefs.GetColor(PrefKeys.LightOverlayColor, Colors.White);
            _viewModel.DarkOverlayColor = _prefs.GetColor(PrefKeys.DarkOverlayColor, Color.FromRgb(0x10, 0x0F, 0x21));

            _viewModel.WordMarkerColor = _prefs.GetColor(PrefKeys.MarkerColor, Color.FromArgb(0xDD, 0xAC, 0x95, 0xE1));

            _viewModel.IsStudyMode = _prefs.GetBool(PrefKeys.IsStudyMode, false);
            _viewModel.ShowStrongs = _prefs.GetBool(PrefKeys.ShowStrongs, false);
            _viewModel.IsDictionaryMode = _prefs.GetBool(PrefKeys.IsDictionaryMode, true);

            _viewModel.VerseMarkerColor = _prefs.GetColor(PrefKeys.VerseMarkerColor, Color.FromRgb(0x95, 0xF1, 0x98));
            _viewModel.LightModalBackgroundColor = _prefs.GetColor(PrefKeys.LightModalBgColor, Color.FromRgb(0xEA, 0xE7, 0xE3));
            _viewModel.DarkModalBackgroundColor = _prefs.GetColor(PrefKeys.DarkModalBgColor, Color.FromRgb(0x12, 0x15, 0x23));
            _viewModel.WordsOfJesus = _prefs.GetColor(PrefKeys.WordsOfJesus, Color.FromRgb(0xDA, 0x42, 0x27));
            _viewModel.HeaderButtonsColor = _prefs.GetColor(PrefKeys.HeaderButtonsColor, Colors.White);

            _viewModel.SelectedPrimaryDictionary = _prefs.GetString(PrefKeys.SelectedDictionary, "atsbd");
            _viewModel.SelectedPrimaryDictLanguage = _prefs.GetString(PrefKeys.PrimaryDictLanguage, "English");
            _viewModel.SelectedSecondaryDictLanguage = _prefs.GetString(PrefKeys.SecondaryDictLanguage, "English");
            _viewModel.SelectedSecondaryDictionary = _prefs.GetString(PrefKeys.SecondaryDictionary, "cbtel");
            _viewModel.SelectedVerseCommentary = _prefs.GetString(PrefKeys.SelectedVerseCommentary, "cbsc");
            _viewModel.SelectedCrossReferenceDatabase = _prefs.GetString(PrefKeys.SelectedCrossRefDb, "obx");

            _viewModel.RenderOrbs = _prefs.GetBool(PrefKeys.RenderOrbs, false);
            _viewModel.OrbsCount = _prefs.GetInt(PrefKeys.OrbsCount, 3);

            // Passages
            _viewModel.PrimaryPassage = new PassageSelection(
                _prefs.GetInt(PrefKeys.PrimaryBookNumber, 10),
                _prefs.GetString(PrefKeys.PrimaryBookName, "Genesis"),
                _prefs.GetInt(PrefKeys.PrimaryChapter, 1),
                _prefs.GetInt(PrefKeys.PrimaryVerse, 1));

            _viewModel.SecondaryPassage = new PassageSelection(
                _prefs.GetInt(PrefKeys.SecondaryBookNumber, 500),
                _prefs.GetString(PrefKeys.SecondaryBookName, "John"),
                _prefs.GetInt(PrefKeys.SecondaryChapter, 1),
                _prefs.GetInt(PrefKeys.SecondaryVerse, 1));

            // Predefined highlight colors
            _viewModel.PredefinedHighlightColors.Clear();
            var saved = _prefs.GetString(PrefKeys.PredefinedHighlightColors, "");
            if (!string.IsNullOrWhiteSpace(saved))
            {
                foreach (var argbStr in saved.Split(',', StringSplitOptions.RemoveEmptyEntries))
                {
                    if (int.TryParse(argbStr, out var argb))
                        _viewModel.PredefinedHighlightColors.Add(PreferencesService.ArgbToColor(argb));
                }
            }
            if (_viewModel.PredefinedHighlightColors.Count == 0)
                _viewModel.ResetHighlightColorsToDefault();

            // Disabled versions
            var disabled = _prefs.GetString(PrefKeys.DisabledVersions, "");
            _viewModel.DisabledVersions = string.IsNullOrWhiteSpace(disabled)
                ? new HashSet<string>()
                : new HashSet<string>(disabled.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }

        // ── Reactive preference saving (equivalent of SavePreference calls) ─
        private void HookPreferenceSaving()
        {
            _viewModel.PropertyChanged += (_, e) =>
            {
                switch (e.PropertyName)
                {
                    case nameof(AppViewModel.DisabledVersions):
                        _prefs.Set(PrefKeys.DisabledVersions, string.Join(",", _viewModel.DisabledVersions)); break;
                    case nameof(AppViewModel.RenderOrbs): _prefs.Set(PrefKeys.RenderOrbs, _viewModel.RenderOrbs); break;
                    case nameof(AppViewModel.OrbsCount): _prefs.Set(PrefKeys.OrbsCount, _viewModel.OrbsCount); break;
                    case nameof(AppViewModel.LightThemeReaderFontColor):
                        _prefs.SetColor(PrefKeys.LightThemeReaderFontColor, _viewModel.LightThemeReaderFontColor); break;
                    case nameof(AppViewModel.DarkThemeReaderFontColor):
                        _prefs.SetColor(PrefKeys.DarkThemeReaderFontColor, _viewModel.DarkThemeReaderFontColor); break;
                    case nameof(AppViewModel.FontSize): _prefs.Set(PrefKeys.FontSize, _viewModel.FontSize); break;
                    case nameof(AppViewModel.CustomWidth): _prefs.Set(PrefKeys.CustomWidth, _viewModel.CustomWidth); break;
                    case nameof(AppViewModel.CustomHeight): _prefs.Set(PrefKeys.CustomHeight, _viewModel.CustomHeight); break;
                    case nameof(AppViewModel.DarkTheme): _prefs.Set(PrefKeys.DarkTheme, _viewModel.DarkTheme); break;
                    case nameof(AppViewModel.SelectedColor):
                        _prefs.SetColor(PrefKeys.SelectedColor, _viewModel.SelectedColor ?? DefaultPrimaryColor); break;
                    case nameof(AppViewModel.IsCustomColor): _prefs.Set(PrefKeys.IsCustomColor, _viewModel.IsCustomColor); break;
                    case nameof(AppViewModel.CustomColor):
                        _prefs.SetColor(PrefKeys.CustomColor, _viewModel.CustomColor ?? DefaultPrimaryColor); break;
                    case nameof(AppViewModel.SelectedFontFamily): _prefs.Set(PrefKeys.FontFamily, _viewModel.SelectedFontFamily); break;
                    case nameof(AppViewModel.CurrentDbName): _prefs.Set(PrefKeys.PrimaryDb, _viewModel.CurrentDbName); break;
                    case nameof(AppViewModel.CurrentVersionAbbr): _prefs.Set(PrefKeys.PrimaryAbbr, _viewModel.CurrentVersionAbbr); break;
                    case nameof(AppViewModel.MultiVersion): _prefs.Set(PrefKeys.MultiVersion, _viewModel.MultiVersion); break;
                    case nameof(AppViewModel.SecondaryDbName): _prefs.Set(PrefKeys.SecondaryDb, _viewModel.SecondaryDbName); break;
                    case nameof(AppViewModel.SecondaryVersionAbbr): _prefs.Set(PrefKeys.SecondaryAbbr, _viewModel.SecondaryVersionAbbr); break;
                    case nameof(AppViewModel.MultiViewLayout): _prefs.Set(PrefKeys.MultiLayout, _viewModel.MultiViewLayout); break;
                    case nameof(AppViewModel.ScrollSync): _prefs.Set(PrefKeys.ScrollSync, _viewModel.ScrollSync); break;
                    case nameof(AppViewModel.BgImageIndex): _prefs.Set(PrefKeys.BgIndex, _viewModel.BgImageIndex); break;
                    case nameof(AppViewModel.OverlayOpacity): _prefs.Set(PrefKeys.OverlayOpacity, _viewModel.OverlayOpacity); break;
                    case nameof(AppViewModel.LightOverlayColor): _prefs.SetColor(PrefKeys.LightOverlayColor, _viewModel.LightOverlayColor); break;
                    case nameof(AppViewModel.DarkOverlayColor): _prefs.SetColor(PrefKeys.DarkOverlayColor, _viewModel.DarkOverlayColor); break;
                    case nameof(AppViewModel.WordMarkerColor): _prefs.SetColor(PrefKeys.MarkerColor, _viewModel.WordMarkerColor); break;
                    case nameof(AppViewModel.IsStudyMode): _prefs.Set(PrefKeys.IsStudyMode, _viewModel.IsStudyMode); break;
                    case nameof(AppViewModel.ShowStrongs): _prefs.Set(PrefKeys.ShowStrongs, _viewModel.ShowStrongs); break;
                    case nameof(AppViewModel.IsDictionaryMode): _prefs.Set(PrefKeys.IsDictionaryMode, _viewModel.IsDictionaryMode); break;
                    case nameof(AppViewModel.VerseMarkerColor): _prefs.SetColor(PrefKeys.VerseMarkerColor, _viewModel.VerseMarkerColor); break;
                    case nameof(AppViewModel.LightModalBackgroundColor): _prefs.SetColor(PrefKeys.LightModalBgColor, _viewModel.LightModalBackgroundColor); break;
                    case nameof(AppViewModel.DarkModalBackgroundColor): _prefs.SetColor(PrefKeys.DarkModalBgColor, _viewModel.DarkModalBackgroundColor); break;
                    case nameof(AppViewModel.WordsOfJesus): _prefs.SetColor(PrefKeys.WordsOfJesus, _viewModel.WordsOfJesus); break;
                    case nameof(AppViewModel.HeaderButtonsColor): _prefs.SetColor(PrefKeys.HeaderButtonsColor, _viewModel.HeaderButtonsColor); break;
                    case nameof(AppViewModel.SelectedPrimaryDictionary): _prefs.Set(PrefKeys.SelectedDictionary, _viewModel.SelectedPrimaryDictionary); break;
                    case nameof(AppViewModel.SelectedPrimaryDictLanguage): _prefs.Set(PrefKeys.PrimaryDictLanguage, _viewModel.SelectedPrimaryDictLanguage); break;
                    case nameof(AppViewModel.SelectedSecondaryDictLanguage): _prefs.Set(PrefKeys.SecondaryDictLanguage, _viewModel.SelectedSecondaryDictLanguage); break;
                    case nameof(AppViewModel.SelectedSecondaryDictionary): _prefs.Set(PrefKeys.SecondaryDictionary, _viewModel.SelectedSecondaryDictionary); break;
                    case nameof(AppViewModel.SelectedVerseCommentary): _prefs.Set(PrefKeys.SelectedVerseCommentary, _viewModel.SelectedVerseCommentary); break;
                    case nameof(AppViewModel.SelectedCrossReferenceDatabase): _prefs.Set(PrefKeys.SelectedCrossRefDb, _viewModel.SelectedCrossReferenceDatabase); break;
                    case nameof(AppViewModel.CustomTextureUri): _prefs.Set(PrefKeys.CustomTexture, _viewModel.CustomTextureUri); break;
                }
            };

            // Passage changes (equivalent of snapshotFlow collectLatest)
            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppViewModel.PrimaryPassage) && _viewModel.PrimaryPassage is { } p)
                {
                    _prefs.Set(PrefKeys.PrimaryBookNumber, p.BookNumber);
                    _prefs.Set(PrefKeys.PrimaryBookName, p.BookName);
                    _prefs.Set(PrefKeys.PrimaryChapter, p.Chapter);
                    _prefs.Set(PrefKeys.PrimaryVerse, p.Verse ?? 1);
                }
                else if (e.PropertyName == nameof(AppViewModel.SecondaryPassage) && _viewModel.SecondaryPassage is { } s)
                {
                    _prefs.Set(PrefKeys.SecondaryBookNumber, s.BookNumber);
                    _prefs.Set(PrefKeys.SecondaryBookName, s.BookName);
                    _prefs.Set(PrefKeys.SecondaryChapter, s.Chapter);
                    _prefs.Set(PrefKeys.SecondaryVerse, s.Verse ?? 1);
                }
                else if (e.PropertyName == nameof(AppViewModel.PredefinedHighlightColors))
                {
                    var joined = string.Join(",",
                        System.Linq.Enumerable.Select(_viewModel.PredefinedHighlightColors,
                            c => PreferencesService.ColorToArgb(c).ToString()));
                    _prefs.Set(PrefKeys.PredefinedHighlightColors, joined);
                }
            };
        }

        // ── Theme wiring (equivalent of ThemeManager updates) ───────────
        private static readonly Color DefaultPrimaryColor = Color.FromRgb(0x67, 0x50, 0xA4); // placeholder

        private void HookViewModelEvents()
        {
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            ThemeManager.PrimaryColor = _viewModel.SelectedColor ?? DefaultPrimaryColor;
            ThemeManager.DarkTheme = _viewModel.DarkTheme;
            ThemeManager.IsCustomColor = _viewModel.IsCustomColor;
        }

        // ── Navigation ─────────────────────────────────────────────────
        private void NavigateToCurrentScreen()
        {
            if (_viewModel.NavigationStack.Count == 0)
            {
                ShowHome();
                return;
            }

            var current = _viewModel.NavigationStack[^1];
            switch (current)
            {
                case Screen.Home:
                    ShowHome();
                    break;

                case Screen.Reader r:
                    var passage = r.Passage ?? _viewModel.PrimaryPassage;
                    RootContent.Content = new ReaderScreen(
                        passage: passage,
                        databaseHelper: _databaseHelper,
                        onPassageChange: newPassage =>
                        {
                            _viewModel.NavigationStack[^1] = new Screen.Reader(newPassage);
                            _viewModel.PrimaryPassage = newPassage;
                            if (_viewModel.ScrollSync) _viewModel.SecondaryPassage = newPassage;
                            NavigateToCurrentScreen();
                        });
                    break;

                case Screen.Bookmarks:
                    RootContent.Content = new BookmarksScreen(onNavigateToReader: NavigateToReader);
                    break;

                case Screen.Notes:
                    RootContent.Content = new NotesScreen(
                        onNavigateToReader: NavigateToReader,
                        viewModel: _viewModel,
                        initialDbName: _viewModel.CurrentDbName);
                    break;

                case Screen.Settings:
                    RootContent.Content = new SettingsScreen();
                    break;

                case Screen.Search:
                    RootContent.Content = new SearchScreen(
                        databaseHelper: _databaseHelper,
                        onPassageSelected: passage =>
                        {
                            _viewModel.PrimaryPassage = passage;
                            if (_viewModel.ScrollSync) _viewModel.SecondaryPassage = passage;

                            var stack = _viewModel.NavigationStack;
                            if (stack.Count > 1 && stack[^2] is Screen.Reader)
                            {
                                stack[^2] = new Screen.Reader(passage);
                                _viewModel.GoBack();
                            }
                            else
                            {
                                _viewModel.GoBack();
                                _viewModel.NavigateTo(new Screen.Reader(passage));
                            }
                            NavigateToCurrentScreen();
                        },
                        currentVersionKey: _viewModel.CurrentDbName,
                        onVersionChange: newKey =>
                        {
                            _viewModel.CurrentDbName = newKey;
                            _viewModel.CurrentVersionAbbr =
                                BibleVersionUtils.VersionMap.GetValueOrDefault(newKey, "Bible");
                        });
                    break;

                case Screen.Quiz:
                    RootContent.Content = new BibleQuizScreen();
                    break;

                case Screen.LearnHebrew:
                    RootContent.Content = new LearnHebrewScreen();
                    break;

                case Screen.LearnGreek:
                    RootContent.Content = new LearnGreekScreen();
                    break;

                case Screen.Dictionary:
                    RootContent.Content = new DictionaryScreen(onNavigateToReader: NavigateToReader);
                    break;

                case Screen.Animator:
                    RootContent.Content = new AnimatorScreen(
                        templateUriToLoad: _animatorTemplateUri,
                        onTemplateConsumed: () =>
                        {
                            _animatorTemplateUri = null;
                            _pendingTemplateUri = null;
                        });
                    break;

                case Screen.Generations:
                    RootContent.Content = new GenerationsScreen();
                    break;
            }
        }

        private void ShowHome()
        {
            var home = new HomeScreen(
                viewModel: _viewModel,
                onNavigateToReader: NavigateToReader,
                onNavigateToScreen: NavigateToScreen,
                databaseHelper: _databaseHelper,
                onTakeBibleQuizClick: () => NavigateToScreen(new Screen.Quiz()),
                onLearnHebrewClick: () => NavigateToScreen(new Screen.LearnHebrew()),
                onLearnGreekClick: () => NavigateToScreen(new Screen.LearnGreek()),
                onOpenDictionaryClick: () => NavigateToScreen(new Screen.Dictionary()),
                onOpenAnimatorClick: () => NavigateToScreen(new Screen.Animator()),
                onOpenGenerationsClick: () => NavigateToScreen(new Screen.Generations()));

            RootContent.Content = home;
        }

        // ── Navigation callbacks ───────────────────────────────────────
        private void NavigateToReader(PassageSelection selection)
        {
            _viewModel.PrimaryPassage = selection;
            if (_viewModel.ScrollSync) _viewModel.SecondaryPassage = selection;
            _viewModel.NavigateTo(new Screen.Reader(selection));
            NavigateToCurrentScreen();
        }

        private void NavigateToScreen(Screen screen)
        {
            var target = screen is Screen.Reader
                ? new Screen.Reader(_viewModel.PrimaryPassage)
                : screen;

            _viewModel.NavigateTo(target);
            NavigateToCurrentScreen();
        }

        // ── Image picker (equivalent of imagePickerLauncher) ───────────
        public void PickCustomBackground()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = "Select a background image"
            };
            if (dlg.ShowDialog() != true) return;

            // Delete previous custom texture
            if (!string.IsNullOrEmpty(_viewModel.CustomTextureUri))
            {
                try { File.Delete(_viewModel.CustomTextureUri); } catch { }
            }

            var persistentPath = CopyUriToInternalStorage(dlg.FileName);
            if (persistentPath != null)
            {
                _viewModel.CustomTextureUri = persistentPath;
                _viewModel.BgImageIndex = 36;
                _prefs.Set(PrefKeys.CustomTexture, persistentPath);
            }
        }

        private static string? CopyUriToInternalStorage(string sourcePath)
        {
            try
            {
                var dir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "FountOfHopeStudyBiblePC", "textures");
                Directory.CreateDirectory(dir);

                var fileName = $"custom_bg_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.jpg";
                var destFile = Path.Combine(dir, fileName);

                File.Copy(sourcePath, destFile, overwrite: true);
                return destFile;
            }
            catch { return null; }
        }
    }
}