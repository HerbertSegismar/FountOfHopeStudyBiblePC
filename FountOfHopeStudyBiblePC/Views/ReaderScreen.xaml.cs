// Views/ReaderScreen.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;
using FountOfHopeStudyBiblePC.Controls;
using FountOfHopeStudyBiblePC.Views.Modals;

namespace FountOfHopeStudyBiblePC.Views
{
    public partial class ReaderScreen : UserControl
    {
        // ── Dependencies ─────────────────────────────────────────────────
        private readonly PassageSelection _passage;
        private readonly DatabaseHelper? _databaseHelper;
        private readonly Action<PassageSelection> _onPassageChange;
        private readonly AppViewModel _viewModel;
        private bool _hasAppliedInitialScroll;

        // ── Passage state ───────────────────────────────────────────────
        private PassageSelection _primaryCurrent;
        private PassageSelection _secondaryCurrent;
        private int? _targetVerse;
        private int? _secondaryTargetVerse;

        // ── Loaded verse caches (book/chapter → content) ────────────────
        private readonly Dictionary<(int book, int chapter), List<VerseContent>> _primaryLoadedVerses = new();
        private readonly Dictionary<(int book, int chapter), List<VerseContent>> _secondaryLoadedVerses = new();

        // ── Secondary DB (multi-version) ────────────────────────────────
        private DatabaseHelper? _secondaryDatabaseHelper;

        // ── Dictionary / commentary DBs ─────────────────────────────────
        private DatabaseHelper? _crossRefHelper;
        private DatabaseHelper? _primaryDictionaryDbHelper;
        private DatabaseHelper? _secondaryDictionaryDbHelper;
        private DatabaseHelper? _strongDbHelper;
        private DatabaseHelper? _commentaryDbHelper;            // footnotes for primary version
        private DatabaseHelper? _secondaryCommentaryDbHelper;   // footnotes for secondary version
        private readonly DatabaseHelper? _subheadingsDbHelper;

        // ── FAB / highlight state ───────────────────────────────────────
        private bool _isButtonVisible = true;
        private bool _isWordHighlightMode;
        private System.Windows.Threading.DispatcherTimer? _fadeTimer;
        private System.Windows.Threading.DispatcherTimer? _highlightTimer;

        // ── Modal state ─────────────────────────────────────────────────
        private ReaderModalState _modalState = new ReaderModalState.None();
        private List<Verse> _selectedVersesForNote = new();   // dead — kept for parity

        // ── Verse options state ─────────────────────────────────────────
        private Verse? _selectedVerse;
        private PassageSelection? _selectedPassage;
        private bool _selectedIsPrimary;

        // ── Cross-ref state (dead — kept for parity with Kotlin) ────────
        private int _crossRefBook;
        private int _crossRefChapter;
        private int _crossRefVerse;

        // ── Commentary context (dead — kept for parity with Kotlin) ─────
        private int _verseCommentaryBook;
        private int _verseCommentaryChapter;
        private int _verseCommentaryVerse;

        // ═══════════════════════════════════════════════════════════════
        //  Constructors
        // ═══════════════════════════════════════════════════════════════
        public ReaderScreen(
            PassageSelection passage,
            DatabaseHelper? databaseHelper,
            Action<PassageSelection> onPassageChange,
            AppViewModel viewModel)
        {
            InitializeComponent();

            _passage = passage;
            _databaseHelper = databaseHelper;
            _onPassageChange = onPassageChange;
            _viewModel = viewModel;

            _primaryCurrent = passage.With(verse: 1);
            _secondaryCurrent = _viewModel.SecondaryPassage.With(verse: 1);
            _targetVerse = passage.Verse;
            _secondaryTargetVerse = _viewModel.SecondaryPassage.Verse;

            _subheadingsDbHelper = new DatabaseHelper("kjvsubheadings.sqlite3");

            ReloadCrossRefHelper();
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            ReloadDictionaryHelpers();
            ReloadSecondaryDatabase();

            _fadeTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _fadeTimer.Tick += (_, _) =>
            {
                _isButtonVisible = false;
                UpdateFabVisibility();
                _fadeTimer!.Stop();
            };

            _highlightTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(5)
            };
            _highlightTimer.Tick += (_, _) =>
            {
                _isWordHighlightMode = false;
                UpdateHighlightColorBar();
                _highlightTimer!.Stop();
            };

            Loaded += ReaderScreen_Loaded;
            Unloaded += ReaderScreen_Unloaded;

            BuildHighlightColorBar();
            RenderCurrentMode();
            _hasAppliedInitialScroll = true;
        }

        public ReaderScreen(
            PassageSelection passage,
            DatabaseHelper? databaseHelper,
            Action<PassageSelection> onPassageChange)
            : this(passage, databaseHelper, onPassageChange,
                   ViewModels.AppViewModelAccessor.Current
                   ?? throw new InvalidOperationException(
                       "ReaderScreen requires a live AppViewModel. " +
                       "Set AppViewModelAccessor.Current in MainWindow."))
        {
        }

        // ═══════════════════════════════════════════════════════════════
        //  Lifecycle
        // ═══════════════════════════════════════════════════════════════
        private void ReaderScreen_Loaded(object sender, RoutedEventArgs e)
        {
            _isButtonVisible = true;
            UpdateFabVisibility();
            ScheduleFade();
        }

        private void ReaderScreen_Unloaded(object sender, RoutedEventArgs e)
        {
            _fadeTimer?.Stop();
            _highlightTimer?.Stop();

            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            _crossRefHelper?.Dispose();
            _primaryDictionaryDbHelper?.Dispose();
            _secondaryDictionaryDbHelper?.Dispose();
            _strongDbHelper?.Dispose();
            _commentaryDbHelper?.Dispose();
            _secondaryCommentaryDbHelper?.Dispose();
            _subheadingsDbHelper?.Dispose();
            _secondaryDatabaseHelper?.Dispose();
        }

        // ═══════════════════════════════════════════════════════════════
        //  ViewModel subscription
        // ═══════════════════════════════════════════════════════════════
        private void OnViewModelPropertyChanged(object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppViewModel.SelectedCrossReferenceDatabase):
                    ReloadCrossRefHelper();
                    break;

                case nameof(AppViewModel.SelectedPrimaryDictionary):
                case nameof(AppViewModel.SelectedSecondaryDictionary):
                    ReloadDictionaryHelpers();
                    break;

                case nameof(AppViewModel.MultiVersion):
                    _secondaryLoadedVerses.Clear();
                    ReloadSecondaryDatabase();
                    RenderCurrentMode();
                    break;

                case nameof(AppViewModel.SecondaryDbName):
                    _secondaryLoadedVerses.Clear();
                    ReloadSecondaryDatabase();
                    RenderCurrentMode();
                    break;

                case nameof(AppViewModel.CurrentDbName):
                    _primaryLoadedVerses.Clear();
                    RenderCurrentMode();
                    break;

                case nameof(AppViewModel.DarkTheme):
                case nameof(AppViewModel.WordsOfJesus):
                case nameof(AppViewModel.FontSize):
                case nameof(AppViewModel.LightThemeReaderFontColor):
                case nameof(AppViewModel.DarkThemeReaderFontColor):
                case nameof(AppViewModel.WordMarkerColor):
                case nameof(AppViewModel.VerseMarkerColor):
                case nameof(AppViewModel.IsDictionaryMode):
                case nameof(AppViewModel.BgImageIndex):
                case nameof(AppViewModel.OverlayOpacity):
                case nameof(AppViewModel.LightOverlayColor):
                case nameof(AppViewModel.DarkOverlayColor):
                case nameof(AppViewModel.ShowStrongs):
                case nameof(AppViewModel.IsStudyMode):
                case nameof(AppViewModel.PredefinedHighlightColors):
                    RenderCurrentMode();
                    break;

                case nameof(AppViewModel.ScrollSync):
                    if (_viewModel.MultiVersion && _viewModel.ScrollSync)
                    {
                        _viewModel.SecondaryPassage = _viewModel.PrimaryPassage;
                        _secondaryCurrent = _primaryCurrent;
                        RenderCurrentMode();
                    }
                    break;
            }
        }

        private void ReloadCrossRefHelper()
        {
            _crossRefHelper?.Dispose();
            var name = _viewModel.SelectedCrossReferenceDatabase;
            if (string.IsNullOrWhiteSpace(name)) { _crossRefHelper = null; return; }
            _crossRefHelper = new DatabaseHelper($"{name}.crossreferences.sqlite3");
        }

        private void ReloadDictionaryHelpers()
        {
            _primaryDictionaryDbHelper?.Dispose();
            _primaryDictionaryDbHelper =
                !string.IsNullOrWhiteSpace(_viewModel.SelectedPrimaryDictionary)
                    ? new DatabaseHelper($"{_viewModel.SelectedPrimaryDictionary}.dictionary.sqlite3")
                    : null;

            _secondaryDictionaryDbHelper?.Dispose();
            _secondaryDictionaryDbHelper =
                !string.IsNullOrWhiteSpace(_viewModel.SelectedSecondaryDictionary)
                    ? new DatabaseHelper($"{_viewModel.SelectedSecondaryDictionary}.dictionary.sqlite3")
                    : null;

            _strongDbHelper?.Dispose();
            _strongDbHelper = new DatabaseHelper("secedictionary.sqlite3");

            _commentaryDbHelper?.Dispose();
            _commentaryDbHelper = CreateCommentaryHelperIfExists(_databaseHelper?.DatabaseName);

            _secondaryCommentaryDbHelper?.Dispose();
            _secondaryCommentaryDbHelper = CreateCommentaryHelperIfExists(
                _secondaryDatabaseHelper?.DatabaseName);
        }

        // ── FIXED: footnote DB is "<version>com.sqlite3", NOT ".commentaries.sqlite3" ──
        private static DatabaseHelper? CreateCommentaryHelperIfExists(string? bibleDbName)
        {
            if (string.IsNullOrWhiteSpace(bibleDbName)) return null;

            var comDbName = bibleDbName.Replace(".sqlite3", "com.sqlite3");

            // Only open if the file can actually be found — otherwise GetCommentary
            // would silently return null because _connection is null.
            var localPath = DatabaseHelper.GetDatabaseFilePath(comDbName);
            if (System.IO.File.Exists(localPath))
                return new DatabaseHelper(comDbName);

            var assetCandidates = new[]
            {
                System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "databases", comDbName),
                System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", comDbName),
            };
            foreach (var asset in assetCandidates)
                if (System.IO.File.Exists(asset))
                    return new DatabaseHelper(comDbName);

            System.Diagnostics.Debug.WriteLine(
                $"[Reader.CreateCommentaryHelper] '{comDbName}' not found. Footnotes disabled.");
            return null;
        }

        private void ReloadSecondaryDatabase()
        {
            _secondaryDatabaseHelper?.Dispose();
            _secondaryDatabaseHelper =
                _viewModel.MultiVersion && !string.IsNullOrWhiteSpace(_viewModel.SecondaryDbName)
                    ? new DatabaseHelper(_viewModel.SecondaryDbName)
                    : null;

            // Secondary footnote DB must be reloaded whenever the secondary version changes.
            _secondaryCommentaryDbHelper?.Dispose();
            _secondaryCommentaryDbHelper = CreateCommentaryHelperIfExists(
                _secondaryDatabaseHelper?.DatabaseName);
        }

        // ═══════════════════════════════════════════════════════════════
        //  FAB / fade
        // ═══════════════════════════════════════════════════════════════
        private void ScheduleFade()
        {
            _fadeTimer?.Stop();
            _fadeTimer?.Start();
        }

        private void UpdateFabVisibility()
        {
            var target = _isButtonVisible ? 1.0 : 0.2;
            var anim = new DoubleAnimation(target, TimeSpan.FromMilliseconds(200));
            FullscreenFab.BeginAnimation(UIElement.OpacityProperty, anim);
        }

        private void FullscreenFab_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.IsReaderFullScreen = !_viewModel.IsReaderFullScreen;

            _isButtonVisible = true;
            UpdateFabVisibility();
            ScheduleFade();
        }

        // ═══════════════════════════════════════════════════════════════
        //  Word-highlight color bar
        // ═══════════════════════════════════════════════════════════════
        private void BuildHighlightColorBar()
        {
            HighlightColorList.Children.Clear();
            foreach (var color in _viewModel.PredefinedHighlightColors)
            {
                var swatch = new Border
                {
                    Width = 30,
                    Height = 30,
                    CornerRadius = new CornerRadius(15),
                    Margin = new Thickness(3, 0, 3, 0),
                    Background = new SolidColorBrush(color),
                    Cursor = Cursors.Hand,
                    Tag = color,
                };
                if (color == _viewModel.WordMarkerColor)
                {
                    swatch.BorderBrush = new SolidColorBrush(Colors.MediumPurple);
                    swatch.BorderThickness = new Thickness(3);
                }
                swatch.MouseLeftButtonUp += (_, _) =>
                {
                    if (swatch.Tag is Color c)
                    {
                        _viewModel.WordMarkerColor = c;
                        ActivateWordHighlightMode();
                        BuildHighlightColorBar();
                    }
                };
                HighlightColorList.Children.Add(swatch);
            }
        }

        private void ActivateWordHighlightMode()
        {
            _isWordHighlightMode = true;
            UpdateHighlightColorBar();
            _highlightTimer?.Stop();
            _highlightTimer?.Start();
        }

        private void UpdateHighlightColorBar()
        {
            HighlightColorBar.Visibility =
                _isWordHighlightMode ? Visibility.Visible : Visibility.Collapsed;
            FullscreenFab.Visibility =
                _isWordHighlightMode ? Visibility.Collapsed : Visibility.Visible;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Menu (top-end popup)
        // ═══════════════════════════════════════════════════════════════
        public void OpenOptionsMenu()
        {
            OptionsMenuBorder.Background =
                new SolidColorBrush(_viewModel.DarkTheme
                    ? _viewModel.DarkModalBackgroundColor
                    : _viewModel.LightModalBackgroundColor);
            OverlayOpacitySlider.Value = _viewModel.OverlayOpacity;
            OptionsMenuPopup.IsOpen = true;
        }

        private void BackgroundTextureMenu_Click(object sender, RoutedEventArgs e)
        {
            OptionsMenuPopup.IsOpen = false;
            _viewModel.ShowBgModal = true;
        }

        private void OverlayOpacitySlider_ValueChanged(object sender,
            RoutedPropertyChangedEventArgs<double> e)
        {
            _viewModel.OverlayOpacity = (float)e.NewValue;
        }

        private void RenderCurrentMode()
        {
            var multi = _viewModel.MultiVersion;
            var synced = _viewModel.ScrollSync && multi && _secondaryDatabaseHelper != null;

            int? primaryTarget = _hasAppliedInitialScroll ? (int?)null : _targetVerse;
            int? secondaryTarget = _hasAppliedInitialScroll ? (int?)null : _secondaryTargetVerse;

            if (!multi)
            {
                ReaderHost.Content = new SingleVersionReader(
                    primaryCurrent: _primaryCurrent,
                    targetVerse: primaryTarget,
                    databaseHelper: _databaseHelper,
                    subheadingsDbHelper: _subheadingsDbHelper,
                    primaryLoadedVerses: _primaryLoadedVerses,
                    viewModel: _viewModel,
                    onPassageChange: OnPrimaryPassageChanged,
                    onScheduleFade: ScheduleFade,
                    onWordPress: OnPrimaryWordPress,
                    onStrongsPress: OnStrongsPress,
                    onTagPress: OnTagPress,
                    onVerseLongPress: OnVerseLongPress,
                    onCrossRefClick: OnCrossRefClick,
                    crossRefHelper: _crossRefHelper,
                    refreshKey: 0,
                    onVerseCommentaryClick: OnVerseCommentaryClick,
                    onWordHighlightAction: ActivateWordHighlightMode);
            }
            else if (synced)
            {
                ReaderHost.Content = new SyncedMultiVersionReader(
                    primaryCurrent: _primaryCurrent,
                    targetVerse: primaryTarget,
                    databaseHelper: _databaseHelper,
                    secondaryDatabaseHelper: _secondaryDatabaseHelper,
                    subheadingsDbHelper: _subheadingsDbHelper,
                    primaryLoadedVerses: _primaryLoadedVerses,
                    secondaryLoadedVerses: _secondaryLoadedVerses,
                    viewModel: _viewModel,
                    onPassageChange: OnSyncedPassageChanged,
                    onScheduleFade: ScheduleFade,
                    onPrimaryWordPress: OnPrimaryWordPress,
                    onSecondaryWordPress: OnSecondaryWordPress,
                    onStrongsPress: OnStrongsPress,
                    onTagPress: OnTagPress,
                    onVerseLongPress: OnVerseLongPress,
                    onCrossRefClick: OnCrossRefClick,
                    crossRefHelper: _crossRefHelper,
                    refreshKey: 0,
                    onVerseCommentaryClick: OnVerseCommentaryClick,
                    onWordHighlightAction: ActivateWordHighlightMode);
            }
            else
            {
                ReaderHost.Content = new IndependentMultiVersionReader(
                    primaryCurrent: _primaryCurrent,
                    secondaryCurrent: _secondaryCurrent,
                    targetVerse: primaryTarget,
                    secondaryTargetVerse: secondaryTarget,
                    databaseHelper: _databaseHelper,
                    secondaryDatabaseHelper: _secondaryDatabaseHelper,
                    subheadingsDbHelper: _subheadingsDbHelper,
                    primaryLoadedVerses: _primaryLoadedVerses,
                    secondaryLoadedVerses: _secondaryLoadedVerses,
                    viewModel: _viewModel,
                    onPrimaryPassageChange: OnPrimaryPassageChanged,
                    onSecondaryPassageChange: OnSecondaryPassageChanged,
                    onScheduleFade: ScheduleFade,
                    onPrimaryWordPress: OnPrimaryWordPress,
                    onSecondaryWordPress: OnSecondaryWordPress,
                    onStrongsPress: OnStrongsPress,
                    onTagPress: OnTagPress,
                    onVerseLongPress: OnVerseLongPress,
                    onCrossRefClick: OnCrossRefClick,
                    crossRefHelper: _crossRefHelper,
                    refreshKey: 0,
                    onVerseCommentaryClick: OnVerseCommentaryClick,
                    onWordHighlightAction: ActivateWordHighlightMode);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Passage change handlers
        // ═══════════════════════════════════════════════════════════════
        private void OnPrimaryPassageChanged(PassageSelection newPassage)
        {
            _primaryCurrent = newPassage;
            _targetVerse = newPassage.Verse;
            _onPassageChange(newPassage);
        }

        private void OnSyncedPassageChanged(PassageSelection newPassage)
        {
            _primaryCurrent = newPassage;
            _secondaryCurrent = newPassage;
            _targetVerse = newPassage.Verse;
            _onPassageChange(newPassage);
            _viewModel.SecondaryPassage = newPassage;
        }

        private void OnSecondaryPassageChanged(PassageSelection newPassage)
        {
            _secondaryCurrent = newPassage;
            _secondaryTargetVerse = newPassage.Verse;
            _viewModel.SecondaryPassage = newPassage;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Word / Strong / Tag / Commentary handlers
        // ═══════════════════════════════════════════════════════════════
        private void OnPrimaryWordPress(string word)
        {
            var trimmed = word.Trim();
            var definition = _primaryDictionaryDbHelper?.GetWordDefinition(trimmed)
                             ?? "Definition not found.";
            _modalState = new ReaderModalState.Word(
                trimmed, definition, _databaseHelper,
                _viewModel.IsOldTestament, UseSecondaryDictionary: false);
            ShowActiveModal();
        }

        private void OnSecondaryWordPress(string word)
        {
            var trimmed = word.Trim();
            var definition = _secondaryDictionaryDbHelper?.GetWordDefinition(trimmed)
                             ?? _primaryDictionaryDbHelper?.GetWordDefinition(trimmed)
                             ?? "Definition not found.";
            _modalState = new ReaderModalState.Word(
                trimmed, definition, _secondaryDatabaseHelper ?? _databaseHelper,
                _viewModel.IsSecondaryOldTestament, UseSecondaryDictionary: true);
            ShowActiveModal();
        }

        private void OnStrongsPress(string strongNumber, int _, bool isPrimary)
        {
            var isOt = isPrimary ? _viewModel.IsOldTestament : _viewModel.IsSecondaryOldTestament;
            var trimmed = strongNumber.Trim();
            var prefixed = (trimmed.Length > 0 && char.IsLetter(trimmed[0]))
                ? trimmed
                : (isOt ? "H" : "G") + trimmed;
            var definition = _strongDbHelper?.GetStrongDefinition(prefixed)
                             ?? "Strong's definition not found.";
            _modalState = new ReaderModalState.Strong(
                prefixed, definition,
                isPrimary ? _databaseHelper : _secondaryDatabaseHelper,
                isOt);
            ShowActiveModal();
        }

        private void OnTagPress(string marker, int bookNumber, int chapter, int verse, bool isPrimary)
        {
            var isOt = isPrimary ? _viewModel.IsOldTestament : _viewModel.IsSecondaryOldTestament;
            var db = isPrimary ? _commentaryDbHelper : _secondaryCommentaryDbHelper;
            var text = db?.GetCommentary(bookNumber, chapter, verse, marker)
                       ?? "No commentary found.";
            var bookName = BibleData.GetBookByCustomNumber(bookNumber)?.Name ?? "";
            var title = $"Notes on {bookName} {chapter}:{verse}{marker}";
            _modalState = new ReaderModalState.Commentary(
                title, text,
                isPrimary ? _databaseHelper : _secondaryDatabaseHelper,
                isOt);
            ShowActiveModal();
        }

        private void OnVerseCommentaryClick(int book, int chapter, int verse)
        {
            _verseCommentaryBook = book;
            _verseCommentaryChapter = chapter;
            _verseCommentaryVerse = verse;
            _modalState = new ReaderModalState.VerseCommentary(
                book, chapter, verse, _databaseHelper, _viewModel.IsOldTestament);
            ShowActiveModal();
        }

        private void OnCrossRefClick(int book, int chapter, int verse, bool isPrimary)
        {
            var refs = _crossRefHelper?.GetCrossReferences(book, chapter, verse)
                       ?? new List<CrossReference>();
            var bookName = BibleData.GetBookByCustomNumber(book)?.Name ?? book.ToString();
            var source = $"References for {bookName} {chapter}:{verse}";

            var items = refs.Select(r =>
            {
                var toBook = BibleData.GetBookByCustomNumber(r.BookTo)?.Name ?? r.BookTo.ToString();
                var range = r.VerseToStart == r.VerseToEnd
                    ? r.VerseToStart.ToString()
                    : $"{r.VerseToStart}-{r.VerseToEnd}";
                return $"<a href=\"B:{r.BookTo} {r.ChapterTo}:{range}\">{toBook} {r.ChapterTo}:{range}</a>";
            });
            var content = string.Join("<br>", items);

            _crossRefBook = book;
            _crossRefChapter = chapter;
            _crossRefVerse = verse;

            _modalState = new ReaderModalState.CrossRef(
                source, content,
                isPrimary ? _databaseHelper : _secondaryDatabaseHelper,
                book, chapter, verse,
                isPrimary ? _viewModel.IsOldTestament : _viewModel.IsSecondaryOldTestament);
            ShowActiveModal();
        }

        private void OnVerseLongPress(Verse verse, PassageSelection passage, bool isPrimary)
        {
            _selectedVerse = verse;
            _selectedPassage = passage;
            _selectedIsPrimary = isPrimary;
            ShowVerseOptionsModal();
        }

        private void ShowActiveModal()
        {
            if (_modalState is ReaderModalState.None) return;

            var modal = new Views.Modals.InteractiveModal(
                viewModel: _viewModel,
                bibleDb: _databaseHelper,
                onNavigateToReader: (p) =>
                {
                    _viewModel.PrimaryPassage = p;
                    _viewModel.NavigateTo(new Screen.Reader(p));
                },
                initialType: GetModalType(_modalState),
                isOldTestament: GetModalIsOldTestament(_modalState),
                useSecondaryDictionary: GetModalUseSecondaryDict(_modalState),
                initialTitle: GetModalTitle(_modalState),
                initialContent: GetModalContent(_modalState),
                word: GetModalWord(_modalState),
                definition: GetModalDefinition(_modalState),
                strongNumber: GetModalStrongNumber(_modalState),
                strongDefinition: GetModalStrongDefinition(_modalState),
                bookNumber: GetModalBook(_modalState),
                chapter: GetModalChapter(_modalState),
                verse: GetModalVerse(_modalState))
            {
                Owner = Window.GetWindow(this),
            };

            modal.ShowDialog();
            _modalState = new ReaderModalState.None();
        }

        private static string GetModalType(ReaderModalState s) => s switch
        {
            ReaderModalState.Word => "definition",
            ReaderModalState.Strong => "strong",
            ReaderModalState.Commentary => "commentary",
            ReaderModalState.CrossRef => "crossreference",
            ReaderModalState.VerseCommentary => "versecommentary",
            _ => ""
        };

        private static bool GetModalIsOldTestament(ReaderModalState s) => s switch
        {
            ReaderModalState.Word w => w.IsOldTestament,
            ReaderModalState.Strong st => st.IsOldTestament,
            ReaderModalState.Commentary c => c.IsOldTestament,
            ReaderModalState.CrossRef cr => cr.IsOldTestament,
            ReaderModalState.VerseCommentary v => v.IsOldTestament,
            _ => false
        };

        private static bool GetModalUseSecondaryDict(ReaderModalState s) =>
            s is ReaderModalState.Word w && w.UseSecondaryDictionary;

        private static string GetModalTitle(ReaderModalState s) =>
            s is ReaderModalState.Commentary c ? c.Title : "";

        private static string GetModalContent(ReaderModalState s) =>
            s is ReaderModalState.Commentary c ? c.Content
            : s is ReaderModalState.CrossRef cr ? cr.Content
            : "";

        private static string GetModalWord(ReaderModalState s) =>
            s is ReaderModalState.Word w ? w.Term : "";

        private static string GetModalDefinition(ReaderModalState s) =>
            s is ReaderModalState.Word w ? w.Definition : "";

        private static string GetModalStrongNumber(ReaderModalState s) =>
            s is ReaderModalState.Strong st ? st.StrongNumber : "";

        private static string GetModalStrongDefinition(ReaderModalState s) =>
            s is ReaderModalState.Strong st ? st.Definition : "";

        private static int? GetModalBook(ReaderModalState s) => s switch
        {
            ReaderModalState.CrossRef cr => cr.Book,
            ReaderModalState.VerseCommentary v => v.Book,
            _ => null
        };

        private static int? GetModalChapter(ReaderModalState s) => s switch
        {
            ReaderModalState.CrossRef cr => cr.Chapter,
            ReaderModalState.VerseCommentary v => v.Chapter,
            _ => null
        };

        private static int? GetModalVerse(ReaderModalState s) => s switch
        {
            ReaderModalState.CrossRef cr => cr.Verse,
            ReaderModalState.VerseCommentary v => v.Verse,
            _ => null
        };

        private async void ShowVerseOptionsModal()
        {
            if (_selectedVerse == null || _selectedPassage == null)
            {
                App.Log("ShowVerseOptionsModal: skipped — selectedVerse or selectedPassage is null");
                return;
            }

            App.Log($"ShowVerseOptionsModal: opening for verse {_selectedVerse.VerseNumber}");

            var cache = _selectedIsPrimary ? _primaryLoadedVerses : _secondaryLoadedVerses;
            var db = _selectedIsPrimary ? _databaseHelper : _secondaryDatabaseHelper;
            var chapterVerses = GetChapterVerses(cache, _selectedPassage);

            await System.Threading.Tasks.Task.Delay(120);

            var modal = new Views.Modals.VerseOptionsModal(
                viewModel: _viewModel,
                passage: _selectedPassage,
                verse: _selectedVerse,
                chapterVerses: chapterVerses,
                databaseHelper: db,
                onAddBookmark: () => App.Log("VerseOptions: bookmark toggled"),
                onAddHighlight: () => App.Log("VerseOptions: highlight toggled"),
                onShare: ShareVersesFromModal,
                onAddNote: OpenNotesModal);

            var owner = Window.GetWindow(this);
            if (owner != null) modal.Owner = owner;

            App.Log("ShowVerseOptionsModal: calling ShowDialog");
            modal.ShowDialog();
            App.Log("ShowVerseOptionsModal: ShowDialog returned — refreshing chapter");

            RenderCurrentMode();
        }

        private static List<Verse> GetChapterVerses(
            Dictionary<(int book, int chapter), List<VerseContent>> cache,
            PassageSelection passage)
        {
            var key = (passage.BookNumber, passage.Chapter);
            if (!cache.TryGetValue(key, out var content)) return new List<Verse>();

            return content
                .OfType<VerseContent.VerseVal>()
                .Select(v => v.Verse with
                {
                    BookName = passage.BookName,
                    Chapter = passage.Chapter,
                })
                .ToList();
        }

        private void ShareVersesFromModal(List<Verse> verses)
        {
            if (verses.Count == 0) return;

            var minV = verses.Min(v => v.VerseNumber);
            var maxV = verses.Max(v => v.VerseNumber);
            var rangeStr = minV == maxV ? $"{minV}" : $"{minV}-{maxV}";

            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{_selectedPassage?.BookName} {_selectedPassage?.Chapter}:{rangeStr}");
            foreach (var v in verses)
                sb.AppendLine($"{v.VerseNumber} {SimpleVerseProcessor.StripXmlTags(v.Text)}");

            try
            {
                Clipboard.SetText(sb.ToString().TrimEnd());
                MessageBox.Show("Verses copied to clipboard.", "Share",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { /* clipboard may fail if no access */ }
        }

        private async void OpenNotesModal(List<Verse> verses)
        {
            if (verses == null || verses.Count == 0) return;

            App.Log($"Reader: OpenNotesModal with {verses.Count} verse(s)");

            await System.Threading.Tasks.Task.Delay(120);

            var modal = new Views.Modals.NotesModal(
                viewModel: _viewModel,
                verses: verses,
                passage: _selectedPassage ?? _primaryCurrent,
                databaseHelper: _databaseHelper,
                onSave: () => App.Log("Notes: save callback fired"));

            var owner = Window.GetWindow(this);
            if (owner != null) modal.Owner = owner;

            modal.ShowDialog();
            App.Log("Reader: NotesModal closed — refreshing chapter");

            RenderCurrentMode();
        }
    }
}