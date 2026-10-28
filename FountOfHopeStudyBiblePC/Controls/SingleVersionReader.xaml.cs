using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;
using FountOfHopeStudyBiblePC.Views;

namespace FountOfHopeStudyBiblePC.Controls
{
    public partial class SingleVersionReader : UserControl, IWordHighlightHost
    {
        private readonly PassageSelection _primaryCurrent;
        private readonly int? _targetVerse;
        private readonly DatabaseHelper? _databaseHelper;
        private readonly DatabaseHelper? _subheadingsDbHelper;
        private readonly Dictionary<(int book, int chapter), List<VerseContent>> _primaryLoadedVerses;
        private readonly AppViewModel _viewModel;
        private readonly Action<PassageSelection> _onPassageChange;
        private readonly Action _onScheduleFade;
        private readonly Action<string> _onWordPress;
        private readonly Action<string, int, bool> _onStrongsPress;
        private readonly Action<string, int, int, int, bool> _onTagPress;
        private readonly Action<Verse, PassageSelection, bool> _onVerseLongPress;
        private readonly Action<int, int, int, bool> _onCrossRefClick;
        private readonly DatabaseHelper? _crossRefHelper;
        private readonly int _refreshKey;
        private readonly Action<int, int, int> _onVerseCommentaryClick;
        private readonly Action _onWordHighlightAction;

        public SingleVersionReader(
            PassageSelection primaryCurrent,
            int? targetVerse,
            DatabaseHelper? databaseHelper,
            DatabaseHelper? subheadingsDbHelper,
            Dictionary<(int book, int chapter), List<VerseContent>> primaryLoadedVerses,
            AppViewModel viewModel,
            Action<PassageSelection> onPassageChange,
            Action onScheduleFade,
            Action<string> onWordPress,
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
            _subheadingsDbHelper = subheadingsDbHelper;
            _primaryLoadedVerses = primaryLoadedVerses;
            _viewModel = viewModel;
            _onPassageChange = onPassageChange;
            _onScheduleFade = onScheduleFade;
            _onWordPress = onWordPress;
            _onStrongsPress = onStrongsPress;
            _onTagPress = onTagPress;
            _onVerseLongPress = onVerseLongPress;
            _onCrossRefClick = onCrossRefClick;
            _crossRefHelper = crossRefHelper;
            _refreshKey = refreshKey;
            _onVerseCommentaryClick = onVerseCommentaryClick;
            _onWordHighlightAction = onWordHighlightAction;

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            PreloadChapter(_primaryCurrent);
            RenderChapter();
        }

        private void PreloadChapter(PassageSelection passage)
        {
            var key = (passage.BookNumber, passage.Chapter);
            if (_primaryLoadedVerses.ContainsKey(key)) return;

            var content = new List<VerseContent>();

            if (_databaseHelper == null)
            {
                _primaryLoadedVerses[key] = content;
                return;
            }

            var verses = _databaseHelper.GetVerses(passage.BookNumber, passage.Chapter)
                         ?? [];

            var subheadings = _subheadingsDbHelper?.GetSubheadings(
                                  passage.BookNumber, passage.Chapter)
                              ?? [];

            var subByVerse = subheadings
                .GroupBy(s => s.Verse)
                .ToDictionary(g => g.Key, g => g.ToList());

            foreach (var v in verses.OrderBy(v => v.VerseNumber))
            {
                if (subByVerse.TryGetValue(v.VerseNumber, out var subs))
                {
                    foreach (var s in subs)
                        content.Add(new VerseContent.SubheadingVal(s));
                }

                var enriched = v with
                {
                    BookName = passage.BookName,
                    Chapter = passage.Chapter,
                };
                content.Add(new VerseContent.VerseVal(enriched));
            }

            _primaryLoadedVerses[key] = content;
        }

        private void RenderChapter()
        {
            var key = (_primaryCurrent.BookNumber, _primaryCurrent.Chapter);

            if (!_primaryLoadedVerses.TryGetValue(key, out var content) || content.Count == 0)
            {
                ChapterHost.Content = BuildEmptyMessage();
                return;
            }

            var chapterView = new ChapterView(
                passage: _primaryCurrent,
                content: content,
                viewModel: _viewModel,
                isCurrentPage: true,
                targetVerse: _targetVerse,
                versionAbbr: _viewModel.CurrentVersionAbbr,
                isPrimary: true,
                databaseHelper: _databaseHelper,
                crossRefHelper: _crossRefHelper,
                onVerseLongPress: (verse, p) => _onVerseLongPress(verse, p, true),
                onCrossRefClick: _onCrossRefClick,
                onVerseCommentaryClick: _onVerseCommentaryClick,
                onWordPress: _onWordPress,
                onStrongsPress: _onStrongsPress,
                onTagPress: _onTagPress,
                onWordHighlightAction: _onWordHighlightAction,
                onInitialScrollComplete: () => { },
                onThemeToggle: () => _viewModel.DarkTheme = !_viewModel.DarkTheme,
                onColorLensClick: () => _viewModel.ShowColorThemeDialog = true,
                onScreenChange: _ => { },
                scrollSyncEnabled: _viewModel.ScrollSync,
                markerColor: _viewModel.WordMarkerColor);

            ChapterHost.Content = chapterView;
        }

        private TextBlock BuildEmptyMessage()
        {
            var dbPath = _databaseHelper == null
                ? "(no database helper)"
                : DatabaseHelper.GetDatabaseFilePath(_databaseHelper.DatabaseName);

            return new TextBlock
            {
                Text = $"No verses found for {_primaryCurrent.BookName} " +
                       $"{_primaryCurrent.Chapter}.\n\n" +
                       $"Database file: {dbPath}\n" +
                       $"Book number: {_primaryCurrent.BookNumber}",
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(24),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Colors.Gray),
            };
        }

        public bool BeginWordDrag(Point positionInHost)
        {
            if (ChapterHost.Content is not ChapterView cv) return false;
            var local = TranslatePoint(positionInHost, cv);
            return cv.BeginWordDrag(local);
        }

        public void UpdateWordDrag(Point positionInHost)
        {
            if (ChapterHost.Content is not ChapterView cv) return;
            var local = TranslatePoint(positionInHost, cv);
            cv.UpdateWordDrag(local);
        }

        public void CommitWordDrag()
        {
            if (ChapterHost.Content is ChapterView cv) cv.CommitWordDrag();
        }

        public void CancelWordDrag()
        {
            if (ChapterHost.Content is ChapterView cv) cv.CancelWordDrag();
        }
    }
}