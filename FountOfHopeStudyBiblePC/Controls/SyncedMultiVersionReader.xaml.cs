// Controls/SyncedMultiVersionReader.xaml.cs
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Controls
{
    public partial class SyncedMultiVersionReader : UserControl
    {
        // ── Dependencies ────────────────────────────────────────────
        private readonly PassageSelection _primaryCurrent;
        private readonly int? _targetVerse;
        private readonly DatabaseHelper? _databaseHelper;
        private readonly DatabaseHelper? _secondaryDatabaseHelper;
        private readonly DatabaseHelper? _subheadingsDbHelper;
        private readonly Dictionary<(int book, int chapter), List<VerseContent>> _primaryLoadedVerses;
        private readonly Dictionary<(int book, int chapter), List<VerseContent>> _secondaryLoadedVerses;
        private readonly AppViewModel _viewModel;
        private readonly Action<PassageSelection> _onPassageChange;
        private readonly Action _onScheduleFade;
        private readonly Action<string> _onPrimaryWordPress;
        private readonly Action<string> _onSecondaryWordPress;
        private readonly Action<string, int, bool> _onStrongsPress;
        private readonly Action<string, int, int, int, bool> _onTagPress;
        private readonly Action<Verse, PassageSelection, bool> _onVerseLongPress;
        private readonly Action<int, int, int, bool> _onCrossRefClick;
        private readonly DatabaseHelper? _crossRefHelper;
        private readonly int _refreshKey;
        private readonly Action<int, int, int> _onVerseCommentaryClick;
        private readonly Action _onWordHighlightAction;

        // ── Live state ──────────────────────────────────────────────
        private ChapterView? _primaryView;
        private ChapterView? _secondaryView;
        private bool _isSyncing;

        // ═══════════════════════════════════════════════════════════
        //  Constructor
        // ═══════════════════════════════════════════════════════════
        public SyncedMultiVersionReader(
            PassageSelection primaryCurrent,
            int? targetVerse,
            DatabaseHelper? databaseHelper,
            DatabaseHelper? secondaryDatabaseHelper,
            DatabaseHelper? subheadingsDbHelper,
            Dictionary<(int book, int chapter), List<VerseContent>> primaryLoadedVerses,
            Dictionary<(int book, int chapter), List<VerseContent>> secondaryLoadedVerses,
            AppViewModel viewModel,
            Action<PassageSelection> onPassageChange,
            Action onScheduleFade,
            Action<string> onPrimaryWordPress,
            Action<string> onSecondaryWordPress,
            Action<string, int, bool> onStrongsPress,
            Action<string, int, int, int, bool> onTagPress,
            Action<Verse, PassageSelection, bool> onVerseLongPress,
            Action<int, int, int, bool> onCrossRefClick,
            DatabaseHelper? crossRefHelper,
            int refreshKey,
            Action<int, int, int> onVerseCommentaryClick,
            Action onWordHighlightAction)
        {
            InitializeComponent();

            _primaryCurrent = primaryCurrent;
            _targetVerse = targetVerse;
            _databaseHelper = databaseHelper;
            _secondaryDatabaseHelper = secondaryDatabaseHelper;
            _subheadingsDbHelper = subheadingsDbHelper;
            _primaryLoadedVerses = primaryLoadedVerses;
            _secondaryLoadedVerses = secondaryLoadedVerses;
            _viewModel = viewModel;
            _onPassageChange = onPassageChange;
            _onScheduleFade = onScheduleFade;
            _onPrimaryWordPress = onPrimaryWordPress;
            _onSecondaryWordPress = onSecondaryWordPress;
            _onStrongsPress = onStrongsPress;
            _onTagPress = onTagPress;
            _onVerseLongPress = onVerseLongPress;
            _onCrossRefClick = onCrossRefClick;
            _crossRefHelper = crossRefHelper;
            _refreshKey = refreshKey;
            _onVerseCommentaryClick = onVerseCommentaryClick;
            _onWordHighlightAction = onWordHighlightAction;

            PreloadBoth(_primaryCurrent);
            BuildLayout();

            // Rebuild when multi-layout toggles (horizontal <-> vertical)
            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppViewModel.MultiViewLayout))
                    Dispatcher.BeginInvoke(new Action(BuildLayout),
                        DispatcherPriority.Background);
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  Preloading
        // ═══════════════════════════════════════════════════════════
        private void PreloadBoth(PassageSelection passage)
        {
            PreloadChapterForSide(passage, _primaryLoadedVerses, _databaseHelper);
            PreloadChapterForSide(passage, _secondaryLoadedVerses, _secondaryDatabaseHelper);
        }

        private void PreloadChapterForSide(
            PassageSelection passage,
            Dictionary<(int book, int chapter), List<VerseContent>> cache,
            DatabaseHelper? db)
        {
            var key = (passage.BookNumber, passage.Chapter);
            if (cache.ContainsKey(key)) return;

            var content = new List<VerseContent>();
            if (db != null)
            {
                var verses = db.GetVerses(passage.BookNumber, passage.Chapter)
                             ?? new List<Verse>();
                var subheadings = _subheadingsDbHelper?.GetSubheadings(
                                      passage.BookNumber, passage.Chapter)
                                  ?? new List<Subheading>();

                var subByVerse = new Dictionary<int, List<Subheading>>();
                foreach (var s in subheadings)
                {
                    if (!subByVerse.TryGetValue(s.Verse, out var list))
                        subByVerse[s.Verse] = list = new List<Subheading>();
                    list.Add(s);
                }

                foreach (var v in verses)
                {
                    if (subByVerse.TryGetValue(v.VerseNumber, out var subs))
                        foreach (var s in subs)
                            content.Add(new VerseContent.SubheadingVal(s));

                    content.Add(new VerseContent.VerseVal(v with
                    {
                        BookName = passage.BookName,
                        Chapter = passage.Chapter,
                    }));
                }
            }
            cache[key] = content;
        }

        // ═══════════════════════════════════════════════════════════
        //  Layout
        // ═══════════════════════════════════════════════════════════
        private void BuildLayout()
        {
            // Detach old handlers before replacing
            if (_primaryView?.Scroller != null)
                _primaryView.Scroller.ScrollChanged -= OnPrimaryScroll;
            if (_secondaryView?.Scroller != null)
                _secondaryView.Scroller.ScrollChanged -= OnSecondaryScroll;

            RootGrid.Children.Clear();
            RootGrid.ColumnDefinitions.Clear();
            RootGrid.RowDefinitions.Clear();

            var key = (_primaryCurrent.BookNumber, _primaryCurrent.Chapter);
            _primaryLoadedVerses.TryGetValue(key, out var primaryContent);
            _secondaryLoadedVerses.TryGetValue(key, out var secondaryContent);

            primaryContent ??= new List<VerseContent>();
            secondaryContent ??= new List<VerseContent>();

            var dividerBrush = new SolidColorBrush(
                Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));

            // ── Build the two ChapterViews ──────────────────────────
            _primaryView = new ChapterView(
                passage: _primaryCurrent,
                content: primaryContent,
                viewModel: _viewModel,
                isCurrentPage: true,
                targetVerse: _targetVerse,
                versionAbbr: _viewModel.CurrentVersionAbbr,
                isPrimary: true,
                databaseHelper: _databaseHelper,
                crossRefHelper: _crossRefHelper,
                onVerseLongPress: (v, p) => _onVerseLongPress(v, p, true),
                onCrossRefClick: _onCrossRefClick,
                onVerseCommentaryClick: _onVerseCommentaryClick,
                onWordPress: _onPrimaryWordPress,
                onStrongsPress: _onStrongsPress,
                onTagPress: _onTagPress,
                onWordHighlightAction: _onWordHighlightAction,
                onInitialScrollComplete: () => { },
                onThemeToggle: () => _viewModel.DarkTheme = !_viewModel.DarkTheme,
                onColorLensClick: () => _viewModel.ShowColorThemeDialog = true,
                onScreenChange: _ => { },
                scrollSyncEnabled: _viewModel.ScrollSync,
                markerColor: _viewModel.WordMarkerColor);

            _secondaryView = new ChapterView(
                passage: _primaryCurrent,
                content: secondaryContent,
                viewModel: _viewModel,
                isCurrentPage: true,
                targetVerse: _targetVerse,
                versionAbbr: _viewModel.SecondaryVersionAbbr,
                isPrimary: false,
                databaseHelper: _secondaryDatabaseHelper,
                crossRefHelper: _crossRefHelper,
                onVerseLongPress: (v, p) => _onVerseLongPress(v, p, false),
                onCrossRefClick: _onCrossRefClick,
                onVerseCommentaryClick: _onVerseCommentaryClick,
                onWordPress: _onSecondaryWordPress,
                onStrongsPress: _onStrongsPress,
                onTagPress: _onTagPress,
                onWordHighlightAction: _onWordHighlightAction,
                onInitialScrollComplete: () => { },
                onThemeToggle: () => _viewModel.DarkTheme = !_viewModel.DarkTheme,
                onColorLensClick: () => _viewModel.ShowColorThemeDialog = true,
                onScreenChange: _ => { },
                scrollSyncEnabled: _viewModel.ScrollSync,
                markerColor: _viewModel.WordMarkerColor);

            // ── Arrange per layout ──────────────────────────────────
            if (_viewModel.MultiViewLayout == "horizontal")
            {
                RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
                RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1) });
                RootGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                Grid.SetColumn(_primaryView, 0);
                Grid.SetColumn(_secondaryView, 2);

                var divider = new Border { Background = dividerBrush };
                Grid.SetColumn(divider, 1);

                RootGrid.Children.Add(_primaryView);
                RootGrid.Children.Add(divider);
                RootGrid.Children.Add(_secondaryView);
            }
            else // vertical
            {
                RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1) });
                RootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

                Grid.SetRow(_primaryView, 0);
                Grid.SetRow(_secondaryView, 2);

                var divider = new Border { Background = dividerBrush };
                Grid.SetRow(divider, 1);

                RootGrid.Children.Add(_primaryView);
                RootGrid.Children.Add(divider);
                RootGrid.Children.Add(_secondaryView);
            }

            WireScrollSync();
        }

        // ═══════════════════════════════════════════════════════════
        //  Scroll sync
        // ═══════════════════════════════════════════════════════════
        private void WireScrollSync()
        {
            if (_primaryView?.Scroller != null)
                _primaryView.Scroller.ScrollChanged += OnPrimaryScroll;
            if (_secondaryView?.Scroller != null)
                _secondaryView.Scroller.ScrollChanged += OnSecondaryScroll;
        }

        private void OnPrimaryScroll(object sender, ScrollChangedEventArgs e)
        {
            if (_isSyncing) return;
            if (!_viewModel.ScrollSync) return;
            if (_primaryView?.Scroller == null || _secondaryView?.Scroller == null) return;
            if (e.VerticalChange == 0 && e.ExtentHeightChange == 0) return;

            _isSyncing = true;
            try
            {
                SyncScroll(_primaryView, _secondaryView);
            }
            finally
            {
                Dispatcher.BeginInvoke(new Action(() => _isSyncing = false),
                    DispatcherPriority.Background);
            }
        }

        private void OnSecondaryScroll(object sender, ScrollChangedEventArgs e)
        {
            if (_isSyncing) return;
            if (!_viewModel.ScrollSync) return;
            if (_primaryView?.Scroller == null || _secondaryView?.Scroller == null) return;
            if (e.VerticalChange == 0 && e.ExtentHeightChange == 0) return;

            _isSyncing = true;
            try
            {
                SyncScroll(_secondaryView, _primaryView);
            }
            finally
            {
                Dispatcher.BeginInvoke(new Action(() => _isSyncing = false),
                    DispatcherPriority.Background);
            }
        }

        /// <summary>
        /// Maps the top-of-viewport position from one ChapterView onto the
        /// other by aligning on verse index. Handles differing verse heights
        /// (e.g. KJV vs ESV) better than a raw percentage-of-extent sync.
        /// </summary>
        private static void SyncScroll(ChapterView? from, ChapterView? to)
        {
            if (from == null || to == null) return;

            var info = from.GetTopVerseInfo();
            if (info == null) return;

            to.ScrollToVerseOffset(info.Value.verseIndex, info.Value.offsetFromTop);
        }
    }
}