// MainWindow.xaml.cs
using FountOfHopeStudyBiblePC.Controls;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;
using FountOfHopeStudyBiblePC.Views;
using FountOfHopeStudyBiblePC.Views.Modals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC
{
    public partial class MainWindow : Window
    {
        private readonly AppViewModel _viewModel;
        private readonly PreferencesService _prefs = new();
        private DatabaseHelper _databaseHelper;

        // ── ADDED: app-wide fullscreen chrome manager ────────────────
        private readonly FullScreenManager _fullScreenManager;

        private Uri? _pendingTemplateUri;
        private Uri? _animatorTemplateUri;

        public MainWindow()
        {
            App.Log("MainWindow ctor - enter");
            InitializeComponent();
            App.Log("MainWindow ctor - after InitializeComponent");

            // ═══ Install theme resources BEFORE any view is created ═══
            ThemeResources.Install();

            Loaded += (_, _) => App.Log("MainWindow.Loaded - event fired");

            _viewModel = new AppViewModel();
            ViewModels.AppViewModelAccessor.Current = _viewModel;
            App.Log("MainWindow ctor - after AppViewModel");

            // ── ADDED: create the fullscreen manager before any
            //    PropertyChanged handler is wired, so it's always live.
            _fullScreenManager = new FullScreenManager(this);

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
                        RefreshPrimaryDatabase();
                        break;

                    case nameof(AppViewModel.SecondaryDbName):
                        App.Log($"MainWindow: SecondaryDbName changed → " +
                                $"{_viewModel.SecondaryDbName} (delegated to ReaderScreen)");
                        break;

                    case nameof(AppViewModel.CurrentVersionAbbr):
                    case nameof(AppViewModel.SecondaryVersionAbbr):
                        UpdateAppBar();
                        break;

                    case nameof(AppViewModel.DarkTheme):
                    case nameof(AppViewModel.SelectedColor):
                    case nameof(AppViewModel.IsCustomColor):
                        ApplyTheme();
                        break;

                    // ── ADDED: app-wide fullscreen — the only place
                    //    the window chrome is ever touched. Any screen
                    //    that toggles the flag gets fullscreen, and the
                    //    state survives navigation between screens.
                    case nameof(AppViewModel.IsReaderFullScreen):
                        _fullScreenManager.Apply(_viewModel.IsReaderFullScreen);
                        break;

                    case nameof(AppViewModel.ShowNavigationModal):
                        if (_viewModel.ShowNavigationModal)
                        {
                            _viewModel.ShowNavigationModal = false;
                            Dispatcher.BeginInvoke(new Action(OpenNavigationModal),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;

                    case nameof(AppViewModel.ShowPrimaryVersionDropdown):
                        if (_viewModel.ShowPrimaryVersionDropdown)
                        {
                            _viewModel.ShowPrimaryVersionDropdown = false;
                            Dispatcher.BeginInvoke(new Action(OpenPrimaryVersionModal),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;

                    case nameof(AppViewModel.ShowSecondaryVersionDropdown):
                        if (_viewModel.ShowSecondaryVersionDropdown)
                        {
                            _viewModel.ShowSecondaryVersionDropdown = false;
                            Dispatcher.BeginInvoke(new Action(OpenSecondaryVersionModal),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;

                    // ═══════════════════════════════════════════════════
                    //  Color picker triggers
                    // ═══════════════════════════════════════════════════
                    case nameof(AppViewModel.ShowColorThemeDialog):
                        if (_viewModel.ShowColorThemeDialog)
                        {
                            _viewModel.ShowColorThemeDialog = false;
                            Dispatcher.BeginInvoke(new Action(OpenColorThemeDialog),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;

                    case nameof(AppViewModel.ShowColorWheelDialog):
                        if (_viewModel.ShowColorWheelDialog)
                        {
                            _viewModel.ShowColorWheelDialog = false;
                            var initial = _viewModel.SelectedColor ?? DefaultPrimaryColor;
                            Dispatcher.BeginInvoke(new Action(() =>
                                OpenColorWheel(initial, c =>
                                {
                                    _viewModel.SelectedColor = c;
                                    _viewModel.IsCustomColor = true;
                                    _viewModel.CustomColor = c;
                                }, title: "Custom Theme Color")),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;

                    case nameof(AppViewModel.ShowWordMarkerColorWheelDialog):
                        if (_viewModel.ShowWordMarkerColorWheelDialog)
                        {
                            _viewModel.ShowWordMarkerColorWheelDialog = false;
                            var initial = _viewModel.WordMarkerColor;
                            Dispatcher.BeginInvoke(new Action(() =>
                                OpenColorWheel(initial, c => _viewModel.WordMarkerColor = c,
                                    title: "Word Marker Color")),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;

                    case nameof(AppViewModel.ShowVerseMarkerColorWheelDialog):
                        if (_viewModel.ShowVerseMarkerColorWheelDialog)
                        {
                            _viewModel.ShowVerseMarkerColorWheelDialog = false;
                            var initial = _viewModel.VerseMarkerColor;
                            Dispatcher.BeginInvoke(new Action(() =>
                                OpenColorWheel(initial, c => _viewModel.VerseMarkerColor = c,
                                    title: "Verse Marker Color")),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;

                    case nameof(AppViewModel.ShowJesusWordsColorWheelDialog):
                        if (_viewModel.ShowJesusWordsColorWheelDialog)
                        {
                            _viewModel.ShowJesusWordsColorWheelDialog = false;
                            var initial = _viewModel.WordsOfJesus;
                            Dispatcher.BeginInvoke(new Action(() =>
                                OpenColorWheel(initial, c => _viewModel.WordsOfJesus = c,
                                    title: "Jesus' Words Color")),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;

                    case nameof(AppViewModel.ShowLightReaderFontColorWheelDialog):
                        if (_viewModel.ShowLightReaderFontColorWheelDialog)
                        {
                            _viewModel.ShowLightReaderFontColorWheelDialog = false;
                            var initial = _viewModel.LightThemeReaderFontColor;
                            Dispatcher.BeginInvoke(new Action(() =>
                                OpenColorWheel(initial, c => _viewModel.LightThemeReaderFontColor = c,
                                    title: "Light Reader Font Color")),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;

                    case nameof(AppViewModel.ShowDarkReaderFontColorWheelDialog):
                        if (_viewModel.ShowDarkReaderFontColorWheelDialog)
                        {
                            _viewModel.ShowDarkReaderFontColorWheelDialog = false;
                            var initial = _viewModel.DarkThemeReaderFontColor;
                            Dispatcher.BeginInvoke(new Action(() =>
                                OpenColorWheel(initial, c => _viewModel.DarkThemeReaderFontColor = c,
                                    title: "Dark Reader Font Color")),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;

                    case nameof(AppViewModel.ShowLightOverlayColorWheel):
                        if (_viewModel.ShowLightOverlayColorWheel)
                        {
                            _viewModel.ShowLightOverlayColorWheel = false;
                            var initial = _viewModel.LightOverlayColor;
                            Dispatcher.BeginInvoke(new Action(() =>
                                OpenColorWheel(initial, c => _viewModel.LightOverlayColor = c,
                                    title: "Light Overlay Color")),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;

                    case nameof(AppViewModel.ShowDarkOverlayColorWheel):
                        if (_viewModel.ShowDarkOverlayColorWheel)
                        {
                            _viewModel.ShowDarkOverlayColorWheel = false;
                            var initial = _viewModel.DarkOverlayColor;
                            Dispatcher.BeginInvoke(new Action(() =>
                                OpenColorWheel(initial, c => _viewModel.DarkOverlayColor = c,
                                    title: "Dark Overlay Color")),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;

                    case nameof(AppViewModel.ShowReaderOverlayColorWheel):
                        if (_viewModel.ShowReaderOverlayColorWheel)
                        {
                            _viewModel.ShowReaderOverlayColorWheel = false;
                            var isDark = _viewModel.DarkTheme;
                            var initial = isDark ? _viewModel.DarkOverlayColor : _viewModel.LightOverlayColor;
                            Dispatcher.BeginInvoke(new Action(() =>
                                OpenColorWheel(initial, c =>
                                {
                                    if (_viewModel.DarkTheme) _viewModel.DarkOverlayColor = c;
                                    else _viewModel.LightOverlayColor = c;
                                }, title: "Reader Overlay Color")),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;

                    case nameof(AppViewModel.ShowHeaderButtonsColorWheel):
                        if (_viewModel.ShowHeaderButtonsColorWheel)
                        {
                            _viewModel.ShowHeaderButtonsColorWheel = false;
                            var initial = _viewModel.HeaderButtonsColor;
                            Dispatcher.BeginInvoke(new Action(() =>
                                OpenColorWheel(initial, c => _viewModel.HeaderButtonsColor = c,
                                    title: "Header Buttons Color")),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;

                    case nameof(AppViewModel.ShowHighlightColorEditor):
                        if (_viewModel.ShowHighlightColorEditor)
                        {
                            _viewModel.ShowHighlightColorEditor = false;
                            var idx = _viewModel.EditingHighlightColorIndex;
                            var initial = (idx >= 0 && idx < _viewModel.PredefinedHighlightColors.Count)
                                          ? _viewModel.PredefinedHighlightColors[idx]
                                          : Colors.White;
                            Dispatcher.BeginInvoke(new Action(() =>
                                OpenColorWheel(initial, c =>
                                {
                                    _viewModel.UpdateHighlightColor(idx, c);
                                    _viewModel.EditingHighlightColorIndex = -1;
                                }, title: "Highlight Color")),
                                System.Windows.Threading.DispatcherPriority.Background);
                        }
                        break;
                }
            };
            App.Log("MainWindow ctor - after PropertyChanged hookup");

            // ── ADDED: global keyboard shortcuts (F11 toggles fullscreen
            //    from any screen; Esc exits fullscreen). Window-level
            //    PreviewKeyDown fires before any child control sees the
            //    key, so it works regardless of which screen is active.
            //    Modals that use ShowDialog disable the owner window, so
            //    they naturally take priority.
            PreviewKeyDown += OnMainWindowPreviewKeyDown;

            NavigateToCurrentScreen();
            App.Log("MainWindow ctor - after NavigateToCurrentScreen");

            HandleIntent(_pendingTemplateUri);
            App.Log("MainWindow ctor - COMPLETE");
        }

        // ── ADDED: global keyboard shortcuts ─────────────────────────
        private void OnMainWindowPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.F11)
            {
                // Toggle from any screen — MainWindow's property handler
                // applies the chrome change immediately.
                _viewModel.IsReaderFullScreen = !_viewModel.IsReaderFullScreen;
                e.Handled = true;
            }
            else if (e.Key == Key.Escape && _viewModel.IsReaderFullScreen)
            {
                _viewModel.IsReaderFullScreen = false;
                e.Handled = true;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Database swap
        // ═══════════════════════════════════════════════════════════════
        private void RefreshPrimaryDatabase()
        {
            var name = _viewModel.CurrentDbName;
            App.Log($"MainWindow: RefreshPrimaryDatabase → {name}");

            try
            {
                _databaseHelper?.Dispose();
                _databaseHelper = new DatabaseHelper(name);

                if (_databaseHelper.IsEmpty())
                    App.Log($"!!! RefreshPrimaryDatabase: '{name}' failed to open " +
                            $"(missing file? check Assets\\databases\\{name})");

                var onReader = _viewModel.NavigationStack.Count > 0
                               && _viewModel.NavigationStack[^1] is Screen.Reader;
                if (onReader)
                {
                    App.Log("MainWindow: RefreshPrimaryDatabase — recreating ReaderScreen");
                    NavigateToCurrentScreen();
                }
            }
            catch (Exception ex)
            {
                App.Log($"!!! RefreshPrimaryDatabase FAILED: {ex}");
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  App bar
        // ═══════════════════════════════════════════════════════════════
        private void UpdateAppBar()
        {
            if (_viewModel.NavigationStack.Count == 0)
            {
                AppBarHost.Content = null;
                return;
            }

            var current = _viewModel.NavigationStack[^1];
            var hasBack = _viewModel.NavigationStack.Count > 1;

            if (current is Screen.Reader r)
            {
                var appBar = new ReaderAppBar(_viewModel);
                var passage = r.Passage ?? _viewModel.PrimaryPassage;
                appBar.UpdatePassage(passage, _viewModel.CurrentVersionAbbr);
                appBar.SetBackVisible(hasBack);

                appBar.BibleIconClicked += () => _viewModel.ShowNavigationModal = true;
                appBar.ThemeToggled += () => _viewModel.DarkTheme = !_viewModel.DarkTheme;
                appBar.ColorLensClicked += () => _viewModel.ShowColorThemeDialog = true;
                appBar.BackRequested += () => { _viewModel.GoBack(); NavigateToCurrentScreen(); };
                appBar.ScreenRequested += screen => { _viewModel.NavigateTo(screen); NavigateToCurrentScreen(); };

                AppBarHost.Content = appBar;
            }
            else
            {
                var appBar = new HomeAppBar(_viewModel, current);
                appBar.SetBackVisible(hasBack);

                appBar.BibleIconClicked += () => _viewModel.ShowNavigationModal = true;
                appBar.ThemeToggled += () => _viewModel.DarkTheme = !_viewModel.DarkTheme;
                appBar.ColorLensClicked += () => _viewModel.ShowColorThemeDialog = true;
                appBar.BackRequested += () => { _viewModel.GoBack(); NavigateToCurrentScreen(); };
                appBar.ScreenRequested += screen => { _viewModel.NavigateTo(screen); NavigateToCurrentScreen(); };

                AppBarHost.Content = appBar;
            }
        }

        private void OpenNavigationModal()
        {
            try
            {

                var modal = new NavigationModal(
                    viewModel: _viewModel,
                    databaseHelper: _databaseHelper,
                    onPassageSelected: passage =>
                    {
                        _viewModel.PrimaryPassage = passage;
                        if (_viewModel.ScrollSync) _viewModel.SecondaryPassage = passage;
                        _viewModel.NavigateTo(new Screen.Reader(passage));
                        NavigateToCurrentScreen();
                    },
                    onDismiss: () => App.Log("MainWindow: NavigationModal dismissed"),
                    initialBookNumber: _viewModel.PrimaryPassage.BookNumber,
                    initialChapter: _viewModel.PrimaryPassage.Chapter,
                    initialVerse: _viewModel.PrimaryPassage.Verse)
                {
                    Owner = this,
                };

                modal.Show();
            }
            catch (Exception ex)
            {
                App.Log("!!! OpenNavigationModal FAILED: " + ex);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Version pickers
        // ═══════════════════════════════════════════════════════════════
        private void OpenPrimaryVersionModal()
        {
            App.Log("MainWindow: OpenPrimaryVersionModal — enter");
            try
            {
                var modal = new VersionSelectionModal(
                    viewModel: _viewModel,
                    currentVersionKey: _viewModel.CurrentDbName,
                    isSecondary: false,
                    onVersionSelected: file =>
                    {
                        App.Log($"MainWindow: primary version picked: {file}");

                        _viewModel.CurrentVersionAbbr =
                            BibleVersionUtils.VersionMap.GetValueOrDefault(file, "Bible");
                        _viewModel.CurrentDbName = file;
                    },
                    onDismiss: () => App.Log("MainWindow: VersionModal dismissed"))
                {
                    Owner = this,
                };

                modal.Show();
            }
            catch (Exception ex)
            {
                App.Log("!!! OpenPrimaryVersionModal FAILED: " + ex);
            }
        }

        private void OpenSecondaryVersionModal()
        {
            App.Log("MainWindow: OpenSecondaryVersionModal — enter");
            try
            {
                var modal = new VersionSelectionModal(
                    viewModel: _viewModel,
                    currentVersionKey: _viewModel.SecondaryDbName,
                    isSecondary: true,
                    onVersionSelected: file =>
                    {
                        App.Log($"MainWindow: secondary version picked: {file}");

                        _viewModel.SecondaryVersionAbbr =
                            BibleVersionUtils.VersionMap.GetValueOrDefault(file, "Bible");
                        _viewModel.SecondaryDbName = file;
                    },
                    onDismiss: () => App.Log("MainWindow: SecondaryVersionModal dismissed"))
                {
                    Owner = this,
                };

                modal.Show();
                App.Log("MainWindow: SecondaryVersionModal closed");
            }
            catch (Exception ex)
            {
                App.Log("!!! OpenSecondaryVersionModal FAILED: " + ex);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Color pickers
        // ═══════════════════════════════════════════════════════════════
        private void OpenColorWheel(Color initial, Action<Color> onSelected, string title = "Color Picker")
        {
            try
            {
                var dialog = new Views.Modals.ColorWheelDialog(
                    initialColor: initial,
                    onColorSelected: c =>
                    {
                        onSelected(c);
                    },
                    onDismiss: () => App.Log("MainWindow: ColorWheel dismissed"))
                {
                    Owner = this,
                };
                dialog.Show();
            }
            catch (Exception ex)
            {
                App.Log("!!! OpenColorWheel FAILED: " + ex);
            }
        }

        private void OpenColorThemeDialog()
        {
            try
            {
                var dialog = new Views.Modals.ColorThemeDialog(
                    currentColor: _viewModel.SelectedColor ?? DefaultPrimaryColor,
                    onColorSelected: c =>
                    {
                        App.Log($"MainWindow: theme preset selected = {ColorHelpers.ToHexString(c)}");
                        _viewModel.SelectedColor = c;
                        _viewModel.IsCustomColor = false;
                        _viewModel.CustomColor = null;
                    },
                    onCustomColorClick: () =>
                    {
                        Dispatcher.BeginInvoke(new Action(() =>
                        {
                            OpenColorWheel(
                                _viewModel.SelectedColor ?? DefaultPrimaryColor,
                                c =>
                                {
                                    _viewModel.SelectedColor = c;
                                    _viewModel.IsCustomColor = true;
                                    _viewModel.CustomColor = c;
                                },
                                title: "Custom Theme Color");
                        }), System.Windows.Threading.DispatcherPriority.Background);
                    },
                    onDismiss: () => App.Log("MainWindow: ColorTheme dismissed"))
                {
                    Owner = this,
                };
                dialog.Show();
            }
            catch (Exception ex)
            {
                App.Log("!!! OpenColorThemeDialog FAILED: " + ex);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Window lifecycle
        // ═══════════════════════════════════════════════════════════════
        protected override void OnSourceInitialized(EventArgs e)
        {
            App.Log("MainWindow.OnSourceInitialized");
            base.OnSourceInitialized(e);

            // ── ADDED: apply any persisted fullscreen state before
            //    the window is fully shown, so a user who left the
            //    app in fullscreen gets it back on relaunch with no
            //    flash of the windowed chrome.
            if (_viewModel.IsReaderFullScreen)
                _fullScreenManager.Apply(true);
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
        }

        // ── Intent / deep-link handling ──────────────────────────────
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

            _animatorTemplateUri = _pendingTemplateUri;
            _pendingTemplateUri = null;
            NavigateToCurrentScreen();
        }

        // ── Preference loading ───────────────────────────────────────
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

            // ── ADDED: restore last session's fullscreen state ──
            _viewModel.IsReaderFullScreen = _prefs.GetBool(PrefKeys.IsReaderFullScreen, false);

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

            var disabled = _prefs.GetString(PrefKeys.DisabledVersions, "");
            _viewModel.DisabledVersions = string.IsNullOrWhiteSpace(disabled)
                ? new HashSet<string>()
                : new HashSet<string>(disabled.Split(',', StringSplitOptions.RemoveEmptyEntries));
        }

        // ── Reactive preference saving ───────────────────────────────
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

                    // ── ADDED: persist fullscreen so it survives restart ──
                    case nameof(AppViewModel.IsReaderFullScreen):
                        _prefs.Set(PrefKeys.IsReaderFullScreen, _viewModel.IsReaderFullScreen);
                        break;
                }
            };

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

        // ── Theme wiring ─────────────────────────────────────────────
        private static readonly Color DefaultPrimaryColor = Color.FromRgb(0x67, 0x50, 0xA4);

        private void HookViewModelEvents()
        {
            ApplyTheme();
        }

        /// <summary>
        /// Pushes the current view-model theme into <see cref="ThemeManager"/>,
        /// then asks <see cref="ThemeResources"/> to refresh <c>App.Resources</c>.
        /// Every <c>{DynamicResource …}</c> in the app repaints; every view that
        /// subscribes to <c>ThemeManager.Changed</c> (HomeScreen, FountRain, …)
        /// gets its own callback via the earlier subscription.
        /// </summary>
        private void ApplyTheme()
        {
            var primary = _viewModel.SelectedColor ?? DefaultPrimaryColor;
            App.Log($"ApplyTheme: dark={_viewModel.DarkTheme}, primary={primary}");

            ThemeManager.Apply(primary, _viewModel.DarkTheme, _viewModel.IsCustomColor);

            // Belt-and-braces: if the values happened to match ThemeManager's
            // existing ones, no Changed event fires — so push the brushes
            // directly once. Also covers the very first call from
            // HookViewModelEvents() before any UI has subscribed.
            ThemeResources.Refresh();
        }

        // ── Navigation ───────────────────────────────────────────────
        private void NavigateToCurrentScreen()
        {
            try
            {
                UpdateAppBar();

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
                                _viewModel.CurrentVersionAbbr =
                                    BibleVersionUtils.VersionMap.GetValueOrDefault(newKey, "Bible");
                                _viewModel.CurrentDbName = newKey;
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
            catch (Exception ex)
            {
                App.Log($"!!! NavigateToCurrentScreen FAILED: {ex}");

                RootContent.Content = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x10, 0x14, 0x18)),
                    Padding = new Thickness(24),
                    Child = new TextBlock
                    {
                        Text = $"Screen failed to load:\n\n{ex.GetType().Name}\n\n{ex.Message}",
                        Foreground = new SolidColorBrush(Color.FromRgb(0xFF, 0x7F, 0x7F)),
                        TextWrapping = TextWrapping.Wrap,
                        FontSize = 14,
                    },
                };
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

        // ── Navigation callbacks ─────────────────────────────────────
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

        // ── Image picker ─────────────────────────────────────────────
        public void PickCustomBackground()
        {
            var dlg = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Images|*.jpg;*.jpeg;*.png;*.bmp;*.gif",
                Title = "Select a background image"
            };
            if (dlg.ShowDialog() != true) return;

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