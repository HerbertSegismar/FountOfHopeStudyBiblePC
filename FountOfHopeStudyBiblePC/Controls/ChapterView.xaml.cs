// Controls/ChapterView.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Controls
{
    public partial class ChapterView : UserControl
    {
        private readonly VerseTextProcessor _processor = new();

        private readonly PassageSelection _passage;
        private readonly List<VerseContent> _content;
        private readonly AppViewModel _viewModel;
        private readonly bool _isCurrentPage;
        private readonly int? _targetVerse;
        private readonly string _versionAbbr;
        private readonly bool _isPrimary;
        private readonly DatabaseHelper? _databaseHelper;
        private readonly DatabaseHelper? _crossRefHelper;
        private readonly Action<Verse, PassageSelection>? _onVerseLongPress;
        private readonly Action<int, int, int, bool>? _onCrossRefClick;
        private readonly Action<int, int, int>? _onVerseCommentaryClick;
        private readonly Action<string>? _onWordPress;
        private readonly Action<string, int, bool>? _onStrongsPress;
        private readonly Action<string, int, int, int, bool>? _onTagPress;
        private readonly Action? _onWordHighlightAction;
        private readonly Action _onInitialScrollComplete;
        private readonly Action _onThemeToggle;
        private readonly Action _onColorLensClick;
        private readonly Action<Screen> _onScreenChange;
        private readonly bool _scrollSyncEnabled;

        private int? _highlightedVerse;
        private readonly Dictionary<int, int> _crossRefCounts = new();
        private readonly List<SelectedWord> _selectedWords = new();
        private readonly Dictionary<int, List<SelectedWord>> _groupedHighlights = new();
        private Color _markerColor;

        // ═══════════════════════════════════════════════════════════════
        //  Constructor
        // ═══════════════════════════════════════════════════════════════
        public ChapterView(
            PassageSelection passage,
            List<VerseContent> content,
            AppViewModel viewModel,
            bool isCurrentPage,
            int? targetVerse,
            string versionAbbr,
            bool isPrimary,
            DatabaseHelper? databaseHelper,
            DatabaseHelper? crossRefHelper,
            Action<Verse, PassageSelection>? onVerseLongPress,
            Action<int, int, int, bool>? onCrossRefClick,
            Action<int, int, int>? onVerseCommentaryClick,
            Action<string>? onWordPress,
            Action<string, int, bool>? onStrongsPress,
            Action<string, int, int, int, bool>? onTagPress,
            Action? onWordHighlightAction,
            Action onInitialScrollComplete,
            Action onThemeToggle,
            Action onColorLensClick,
            Action<Screen> onScreenChange,
            bool scrollSyncEnabled,
            Color markerColor)
        {
            InitializeComponent();

            _passage = passage;
            _content = content;
            _viewModel = viewModel;
            _isCurrentPage = isCurrentPage;
            _targetVerse = targetVerse;
            _versionAbbr = versionAbbr;
            _isPrimary = isPrimary;
            _databaseHelper = databaseHelper;
            _crossRefHelper = crossRefHelper;
            _onVerseLongPress = onVerseLongPress;
            _onCrossRefClick = onCrossRefClick;
            _onVerseCommentaryClick = onVerseCommentaryClick;
            _onWordPress = onWordPress;
            _onStrongsPress = onStrongsPress;
            _onTagPress = onTagPress;
            _onWordHighlightAction = onWordHighlightAction;
            _onInitialScrollComplete = onInitialScrollComplete;
            _onThemeToggle = onThemeToggle;
            _onColorLensClick = onColorLensClick;
            _onScreenChange = onScreenChange;
            _scrollSyncEnabled = scrollSyncEnabled;
            _markerColor = markerColor;

            _viewModel.PropertyChanged += (_, e) => OnViewModelPropertyChanged(e);
            Loaded += OnLoaded;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Lifecycle
        // ═══════════════════════════════════════════════════════════════
        private void OnViewModelPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppViewModel.DarkTheme):
                case nameof(AppViewModel.FontSize):
                case nameof(AppViewModel.WordsOfJesus):
                case nameof(AppViewModel.BgImageIndex):
                case nameof(AppViewModel.OverlayOpacity):
                case nameof(AppViewModel.SelectedColor):
                case nameof(AppViewModel.SelectedFontFamily):
                case nameof(AppViewModel.DarkThemeReaderFontColor):
                case nameof(AppViewModel.LightThemeReaderFontColor):
                case nameof(AppViewModel.IsStudyMode):
                case nameof(AppViewModel.ShowStrongs):
                case nameof(AppViewModel.IsDictionaryMode):
                    RenderAll();
                    break;

                // ── Field only, NO re-render ────────────────────────────
                case nameof(AppViewModel.WordMarkerColor):
                    _markerColor = _viewModel.WordMarkerColor;
                    break;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadCrossRefCounts();
            LoadWordHighlights();

            // Determine target BEFORE the first render so the temp
            // highlight is included in the initial paint.
            bool hasTarget = _isCurrentPage && _targetVerse.HasValue && _targetVerse.Value > 0;
            if (hasTarget)
            {
                _highlightedVerse = _targetVerse!.Value;
            }

            RenderAll();
            ApplyBackground();
            UpdateInlineHeader();

            if (hasTarget)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var targetIdx = FindVerseIndex(_targetVerse!.Value);
                    if (targetIdx >= 0 && VerseItems.Items.Count > targetIdx)
                    {
                        var container = VerseItems.ItemContainerGenerator
                            .ContainerFromIndex(targetIdx) as FrameworkElement;
                        container?.BringIntoView();
                    }
                    HighlightTargetVerse();   // start the 2-second clear timer
                    _onInitialScrollComplete();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            else
            {
                _onInitialScrollComplete();
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Background rendering
        // ═══════════════════════════════════════════════════════════════
        private void ApplyBackground()
        {
            var bgIndex = _viewModel.BgImageIndex;
            if (bgIndex == 0)
            {
                BackgroundImage.Visibility = Visibility.Collapsed;
                OverlayBorder.Visibility = Visibility.Collapsed;
                return;
            }

            if (bgIndex == 36 && !string.IsNullOrEmpty(_viewModel.CustomTextureUri))
            {
                try
                {
                    var bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(_viewModel.CustomTextureUri, UriKind.Absolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.EndInit();
                    BackgroundImage.Source = bmp;
                    BackgroundImage.Visibility = Visibility.Visible;
                }
                catch { BackgroundImage.Visibility = Visibility.Collapsed; }
            }
            else
            {
                var path = System.IO.Path.Combine(
                    AppContext.BaseDirectory, "Assets", "textures", $"{bgIndex}.jpg");
                if (System.IO.File.Exists(path))
                {
                    try
                    {
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.UriSource = new Uri(path, UriKind.Absolute);
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.EndInit();
                        BackgroundImage.Source = bmp;
                        BackgroundImage.Visibility = Visibility.Visible;
                    }
                    catch { BackgroundImage.Visibility = Visibility.Collapsed; }
                }
                else BackgroundImage.Visibility = Visibility.Collapsed;
            }

            var overlayColor = _viewModel.DarkTheme
                ? _viewModel.DarkOverlayColor
                : _viewModel.LightOverlayColor;
            OverlayBorder.Background = new SolidColorBrush(overlayColor)
            {
                Opacity = _viewModel.OverlayOpacity
            };
            OverlayBorder.Visibility = Visibility.Visible;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Header
        // ═══════════════════════════════════════════════════════════════
        private void UpdateInlineHeader()
        {
            InlineBookText.Text = _passage.BookName;
            InlineChapterText.Text = _passage.Chapter.ToString();
            InlineVersionText.Text = _versionAbbr;
            InlineHeader.Visibility = _viewModel.MultiVersion
                ? Visibility.Visible
                : Visibility.Collapsed;
            InlineThemeIcon.Text = _viewModel.DarkTheme ? "☀" : "☾";
        }

        // ═══════════════════════════════════════════════════════════════
        //  Caches
        // ═══════════════════════════════════════════════════════════════
        private void LoadCrossRefCounts()
        {
            _crossRefCounts.Clear();
            var counts = _crossRefHelper?.GetCrossReferenceCountsForChapter(
                _passage.BookNumber, _passage.Chapter);
            if (counts != null)
                foreach (var kv in counts) _crossRefCounts[kv.Key] = kv.Value;
        }

        private void LoadWordHighlights()
        {
            _selectedWords.Clear();
            _groupedHighlights.Clear();
            if (_databaseHelper == null) return;

            foreach (var item in _content.OfType<VerseContent.VerseVal>())
            {
                var fullVerse = item.Verse with
                {
                    BookName = _passage.BookName,
                    Chapter = _passage.Chapter,
                };
                _selectedWords.AddRange(_databaseHelper.GetWordHighlightsForVerse(fullVerse));
            }

            foreach (var w in _selectedWords)
            {
                if (!_groupedHighlights.TryGetValue(w.VerseNumber, out var list))
                    _groupedHighlights[w.VerseNumber] = list = new List<SelectedWord>();
                list.Add(w);
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Render
        // ═══════════════════════════════════════════════════════════════
        private void RenderAll()
        {
            VerseItems.Items.Clear();

            foreach (var item in _content)
            {
                switch (item)
                {
                    case VerseContent.SubheadingVal sub:
                        VerseItems.Items.Add(BuildSubheadingBlock(sub.Subheading));
                        break;
                    case VerseContent.VerseVal v:
                        VerseItems.Items.Add(BuildVerseBlock(v.Verse));
                        break;
                }
            }
        }

        private Color GetThemeColor()
            => _viewModel.SelectedColor ?? (Color)ColorConverter.ConvertFromString("#FF7FB2FF");

        private FrameworkElement BuildSubheadingBlock(Subheading sub)
        {
            var theme = GetThemeColor();
            return new TextBlock
            {
                Text = sub.Text,
                FontWeight = FontWeights.Bold,
                FontSize = _viewModel.FontSize + 1,
                FontFamily = FontsHelper.GetFontFamily(_viewModel.SelectedFontFamily),
                Foreground = new SolidColorBrush(theme),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(4, 6, 4, 2),
            };
        }

        private FrameworkElement BuildVerseBlock(Verse verse)
        {
            var fullVerse = verse with
            {
                BookName = _passage.BookName,
                Chapter = _passage.Chapter,
            };

            var isPersistentHl = _databaseHelper?.IsHighlighted(fullVerse) ?? false;
            var persistColor = isPersistentHl
                ? _databaseHelper?.GetHighlightColor(fullVerse)
                : null;
            var isBookmarked = _databaseHelper?.IsBookmarked(fullVerse) ?? false;
            var hasNote = _databaseHelper?.HasElement(fullVerse) ?? false;
            var refCount = _crossRefCounts.TryGetValue(verse.VerseNumber, out var c) ? c : 0;

            var isTempHl = verse.VerseNumber == _highlightedVerse;

            var theme = GetThemeColor();

            var bg = persistColor != null
                ? new SolidColorBrush(persistColor.Value)
                : (isTempHl
                    ? new SolidColorBrush(Color.FromArgb(0x55, theme.R, theme.G, theme.B))
                    : Brushes.Transparent);

            var tb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = _viewModel.FontSize,
                LineHeight = _viewModel.FontSize * 1.333,
                FontFamily = FontsHelper.GetFontFamily(_viewModel.SelectedFontFamily),
                Tag = verse,
                Cursor = Cursors.Hand,
            };

            // Verse number (offset 0). Body runs start after this.
            tb.Inlines.Add(new Run($"{verse.VerseNumber} ")
            {
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(theme),
                FontSize = _viewModel.FontSize * 0.9,
            });

            var themeColors = new ThemeColors(
                TextColor: _viewModel.DarkTheme
                    ? _viewModel.DarkThemeReaderFontColor
                    : _viewModel.LightThemeReaderFontColor,
                VerseNumber: theme,
                Primary: theme,
                TagColor: theme,
                TagBg: Color.FromArgb(0x33, theme.R, theme.G, theme.B),
                WordsOfJesus: _viewModel.WordsOfJesus,
                SearchHighlightBg: Color.FromArgb(0x55, theme.R, theme.G, theme.B),
                HighlightIcon: theme,
                WordHighlightBg: _viewModel.WordMarkerColor);

            var verseNumber = verse.VerseNumber;

            var processed = _processor.ProcessVerse(
                verseText: verse.Text,
                baseFontSize: _viewModel.FontSize,
                themeColors: themeColors,
                textColor: themeColors.TextColor,
                isOldTestament: _isPrimary
                    ? _viewModel.IsOldTestament
                    : _viewModel.IsSecondaryOldTestament,
                options: new ProcessingOptions
                {
                    ShowStrongs = _viewModel.ShowStrongs,
                    // Word annotations are always created so clicks fire
                    // in both Dictionary and Highlight modes.
                    EnableWordClick = true,
                    EnableStrongsClick = _onStrongsPress != null,
                    EnableTagClick = _onTagPress != null,
                },
                onWordPress: _onWordPress,
                onStrongsPress: s =>
                    _onStrongsPress?.Invoke(s, _passage.BookNumber, _isPrimary),
                onTagPress: m =>
                    _onTagPress?.Invoke(m, _passage.BookNumber, _passage.Chapter,
                                        verseNumber, _isPrimary));

            foreach (var inline in processed.Body)
                tb.Inlines.Add(inline);

            if (isBookmarked) tb.Inlines.Add(new Run(" 🔖"));
            if (hasNote) tb.Inlines.Add(new Run(" 📝"));
            if (_viewModel.IsStudyMode && refCount > 0)
                tb.Inlines.Add(new Run($" [{refCount}]"));

            // Apply word highlights — ALWAYS called so stale backgrounds from
            // the VerseTextProcessor cache are cleared even when there are none.
            _groupedHighlights.TryGetValue(verse.VerseNumber, out var highlights);
            ApplyWordHighlights(tb, highlights ?? new List<SelectedWord>());

            tb.MouseLeftButtonUp += VerseTextBlock_MouseLeftButtonUp;
            tb.MouseRightButtonUp += (_, e) =>
            {
                e.Handled = true;
                var v = verse;
                var p = _passage;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _onVerseLongPress?.Invoke(v, p);
                }), System.Windows.Threading.DispatcherPriority.Background);
            };

            return new Border
            {
                Padding = new Thickness(4),
                CornerRadius = new CornerRadius(4),
                Background = bg,
                Child = tb,
            };
        }

        /// <summary>
        /// Applies persisted word highlights from the DB by matching character
        /// offsets against each body Run. Clears any stale Background first —
        /// important because VerseTextProcessor caches the Run objects across
        /// renders, so a previous highlight would otherwise stick around.
        /// </summary>
        private static void ApplyWordHighlights(TextBlock tb, List<SelectedWord> highlights)
        {
            var inlines = tb.Inlines.ToList();
            int offset = 0;
            bool skippedVerseNumber = false;

            foreach (var inline in inlines)
            {
                if (inline is not Run run) continue;

                // ── KEY FIX: clear stale background from the cached Run ──
                run.Background = null;

                if (!skippedVerseNumber)
                {
                    skippedVerseNumber = true;
                    continue;
                }

                int len = run.Text?.Length ?? 0;
                if (len == 0) continue;

                int runStart = offset;
                int runEnd = offset + len;

                foreach (var hl in highlights)
                {
                    if (hl.Start < runEnd && hl.End > runStart)
                    {
                        run.Background = new SolidColorBrush(hl.Color);
                        break;
                    }
                }

                offset = runEnd;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Click handling
        // ═══════════════════════════════════════════════════════════════
        private void VerseTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBlock tb) return;
            var verse = tb.Tag as Verse;
            if (verse == null) return;

            var pt = e.GetPosition(tb);
            var pos = tb.GetPositionFromPoint(pt, snapToText: true);
            if (pos == null) return;

            var run = pos.Parent as Run;

            // Fallback: if pos.Parent isn't a Run (click landed on the
            // background rectangle WPF renders behind a highlighted Run),
            // find the Run at that character offset.
            if (run == null)
            {
                run = FindRunAtPosition(tb, pos);
                if (run == null) return;
            }

            if (run.Tag is not VerseAnnotation ann) return;

            switch (ann.Tag)
            {
                case "word":
                    if (_viewModel.IsDictionaryMode)
                        _onWordPress?.Invoke(ann.Value);
                    else
                        ToggleWordHighlight(verse, run);
                    break;

                case "strong":
                    _onStrongsPress?.Invoke(ann.Value, _passage.BookNumber, _isPrimary);
                    break;

                case "tag":
                    _onTagPress?.Invoke(ann.Value, _passage.BookNumber,
                        _passage.Chapter, verse.VerseNumber, _isPrimary);
                    break;
            }
        }

        /// <summary>
        /// Finds the Run whose character span contains the TextPointer.
        /// Used as a fallback when pos.Parent is not directly a Run.
        /// </summary>
        private static Run? FindRunAtPosition(TextBlock tb, TextPointer pos)
        {
            var start = tb.ContentStart;
            var offset = start.GetOffsetToPosition(pos);
            if (offset < 0) return null;

            int cursor = 0;
            foreach (var inline in tb.Inlines)
            {
                if (inline is not Run r) continue;
                int len = r.Text?.Length ?? 0;
                if (offset >= cursor && offset < cursor + len) return r;
                cursor += len;
            }
            return null;
        }

        private void ToggleWordHighlight(Verse verse, Run run)
        {
            // Clear the "just navigated here" highlight so it doesn't
            // re-appear when we re-render below.
            _highlightedVerse = null;

            var tb = run.Parent as TextBlock;
            if (tb == null) return;

            // Compute body-relative offset by skipping the verse number Run.
            int start = 0;
            bool found = false;
            bool skippedVerseNumber = false;

            foreach (var inline in tb.Inlines)
            {
                if (ReferenceEquals(inline, run)) { found = true; break; }
                if (inline is not Run r) continue;
                if (!skippedVerseNumber) { skippedVerseNumber = true; continue; }
                start += r.Text?.Length ?? 0;
            }

            if (!found) return;

            int len = run.Text?.Length ?? 0;
            int end = start + len;
            if (end <= start) return;

            var fullVerse = verse with
            {
                BookName = _passage.BookName,
                Chapter = _passage.Chapter,
            };

            // Find ALL overlapping entries, not just exact matches.
            // Handles offset drift between renders and cleans up
            // duplicate entries from earlier buggy toggles.
            var overlapping = _selectedWords
                .Where(w => w.VerseNumber == verse.VerseNumber
                            && w.Start < end
                            && w.End > start)
                .ToList();

            App.Log($"ToggleWordHighlight: v={verse.VerseNumber}, " +
                    $"[{start},{end}], overlapping={overlapping.Count}, " +
                    $"runBg={(run.Background != null)}");

            if (overlapping.Count > 0)
            {
                // ── TOGGLE OFF: remove every overlapping highlight ──
                foreach (var entry in overlapping)
                {
                    _selectedWords.Remove(entry);
                    _databaseHelper?.RemoveWordHighlight(fullVerse, entry.Start, entry.End);
                }
            }
            else
            {
                // ── TOGGLE ON ──
                var sw = new SelectedWord(verse.VerseNumber, start, end, _markerColor);
                _selectedWords.Add(sw);
                if (_databaseHelper != null)
                {
                    var argb = (_markerColor.A << 24) | (_markerColor.R << 16) |
                               (_markerColor.G << 8) | _markerColor.B;
                    _databaseHelper.AddWordHighlight(fullVerse, start, end, argb);
                }
            }

            // Rebuild grouped highlights
            _groupedHighlights.Clear();
            foreach (var w in _selectedWords)
            {
                if (!_groupedHighlights.TryGetValue(w.VerseNumber, out var list))
                    _groupedHighlights[w.VerseNumber] = list = new List<SelectedWord>();
                list.Add(w);
            }

            _onWordHighlightAction?.Invoke();
            RenderAll();
        }

        // ═══════════════════════════════════════════════════════════════
        //  Scroll helpers
        // ═══════════════════════════════════════════════════════════════
        private int FindVerseIndex(int verseNumber)
        {
            for (int i = 0; i < _content.Count; i++)
                if (_content[i] is VerseContent.VerseVal v
                    && v.Verse.VerseNumber == verseNumber)
                    return i;
            return -1;
        }

        private void HighlightTargetVerse()
        {
            var timer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2)
            };
            timer.Tick += (_, _) =>
            {
                timer.Stop();
                _highlightedVerse = null;
                RenderAll();
            };
            timer.Start();
        }

        // ═══════════════════════════════════════════════════════════════
        //  Header button handlers
        // ═══════════════════════════════════════════════════════════════
        private void InlineBookChapterBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_scrollSyncEnabled || _isPrimary)
                _viewModel.ShowNavigationModal = true;
            else
                _viewModel.ShowSecondaryNavigationModal = true;
        }

        private void InlineVersionBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_isPrimary) _viewModel.ShowPrimaryVersionDropdown = true;
            else _viewModel.ShowSecondaryVersionDropdown = true;
        }

        private void InlineThemeBtn_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.DarkTheme = !_viewModel.DarkTheme;
            _onThemeToggle();
        }

        private void InlineColorBtn_Click(object sender, RoutedEventArgs e)
            => _onColorLensClick();
    }
}