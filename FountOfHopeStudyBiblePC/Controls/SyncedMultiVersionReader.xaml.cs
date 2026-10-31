using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;
using FountOfHopeStudyBiblePC.Views;

namespace FountOfHopeStudyBiblePC.Controls
{
    public partial class SyncedMultiVersionReader : UserControl, IWordHighlightHost
    {
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

        private ChapterView? _primaryView;
        private ChapterView? _secondaryView;
        private bool _isSyncing;

        private ChapterView? _activePane;

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

            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppViewModel.MultiViewLayout))
                    Dispatcher.BeginInvoke(new Action(BuildLayout),
                        DispatcherPriority.Background);
            };
        }

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
                             ?? [];
                var subheadings = _subheadingsDbHelper?.GetSubheadings(
                                      passage.BookNumber, passage.Chapter)
                                  ?? [];

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

        private void BuildLayout()
        {
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

            primaryContent ??= [];
            secondaryContent ??= [];

            var dividerBrush = new SolidColorBrush(
                Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF));

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
            else
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

        private static void SyncScroll(ChapterView? from, ChapterView? to)
        {
            if (from == null || to == null) return;

            var info = from.GetTopVerseInfo();
            if (info == null) return;

            to.ScrollToVerseOffset(info.Value.verseIndex, info.Value.offsetFromTop);
        }

        public bool BeginWordDrag(Point positionInHost)
        {
            var pane = FindPaneAt(positionInHost);
            if (pane == null) return false;

            var local = TranslatePoint(positionInHost, pane);
            if (!pane.BeginWordDrag(local)) return false;

            _activePane = pane;
            return true;
        }

        public void UpdateWordDrag(Point positionInHost)
        {
            if (_activePane == null) return;
            var local = TranslatePoint(positionInHost, _activePane);
            _activePane.UpdateWordDrag(local);
        }

        public void CommitWordDrag()
        {
            _activePane?.CommitWordDrag();
            _activePane = null;
        }

        public void CancelWordDrag()
        {
            _activePane?.CancelWordDrag();
            _activePane = null;
        }

        private ChapterView? FindPaneAt(Point positionInHost)
        {
            var hit = InputHitTest(positionInHost) as DependencyObject;
            if (hit == null) return null;

            var current = hit;
            while (current != null)
            {
                if (ReferenceEquals(current, _primaryView)) return _primaryView;
                if (ReferenceEquals(current, _secondaryView)) return _secondaryView;
                current = GetSafeParent(current);
            }
            return null;
        }

        /// <summary>
        /// Walks one level up the WPF element tree, correctly handling
        /// ContentElement / FrameworkContentElement (e.g. <see cref="System.Windows.Documents.Run"/>)
        /// which are not Visuals and would otherwise throw
        /// from <see cref="VisualTreeHelper.GetParent"/>.
        /// </summary>
        private static DependencyObject? GetSafeParent(DependencyObject current)
        {
            // Real Visual / Visual3D → use the visual tree.
            if (current is Visual || current is System.Windows.Media.Media3D.Visual3D)
                return VisualTreeHelper.GetParent(current);

            // Content tree (Run, Paragraph, etc.) → use logical parent.
            // e.g. Run → TextBlock.
            if (current is FrameworkContentElement fce)
                return fce.Parent;

            if (current is ContentElement ce)
                return ContentOperations.GetParent(ce);

            return null;
        }
    }
}