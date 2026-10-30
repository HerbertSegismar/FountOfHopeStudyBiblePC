using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Threading;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Controls
{
    public partial class ChapterView : UserControl
    {
        public ScrollViewer Scroller => VerseScroller;
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
        private readonly Dictionary<int, int> _crossRefCounts = [];
        private readonly List<SelectedWord> _selectedWords = [];
        private readonly Dictionary<int, List<SelectedWord>> _groupedHighlights = [];
        private Color _markerColor;

        private Run? _dragStartRun;
        private Run? _dragEndRun;
        private bool _isWordDragActive;
        private readonly Dictionary<Run, Brush?> _previewOriginals = [];

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

                case nameof(AppViewModel.WordMarkerColor):
                    _markerColor = _viewModel.WordMarkerColor;
                    break;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadCrossRefCounts();
            LoadWordHighlights();

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
                    HighlightTargetVerse();
                    _onInitialScrollComplete();
                }), System.Windows.Threading.DispatcherPriority.Loaded);
            }
            else
            {
                _onInitialScrollComplete();
            }
        }

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
                    _groupedHighlights[w.VerseNumber] = list = [];
                list.Add(w);
            }
        }

        private void RenderAll()
        {
            _processor.ClearCache();

            ClearPreview();
            _dragStartRun = null;
            _dragEndRun = null;
            _isWordDragActive = false;

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

        private TextBlock BuildSubheadingBlock(Subheading sub)
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

        private Border BuildVerseBlock(Verse verse)
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

            // The line height the surrounding body uses. Note markers and the
            // asterisk are constrained to this so they don't inflate the row.
            var baseLineHeight = _viewModel.FontSize * 1.333;

            var tb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = _viewModel.FontSize,
                LineHeight = baseLineHeight,
                FontFamily = FontsHelper.GetFontFamily(_viewModel.SelectedFontFamily),
                Tag = verse,
                Cursor = Cursors.Hand,
            };

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

            if (processed.Body.Count == 0 && !string.IsNullOrWhiteSpace(verse.Text))
            {
                var stripped = System.Text.RegularExpressions.Regex
                    .Replace(verse.Text, "<[^>]+>", "").Trim();
                if (!string.IsNullOrWhiteSpace(stripped))
                {
                    App.Log($"BuildVerseBlock FALLBACK: v={verse.VerseNumber} " +
                            $"using stripped text <{stripped}>");
                    tb.Inlines.Add(new Run(stripped)
                    {
                        Foreground = new SolidColorBrush(themeColors.TextColor),
                        FontSize = _viewModel.FontSize,
                    });
                }
            }

            if (isBookmarked) tb.Inlines.Add(new Run(" 🔖"));
            if (hasNote) tb.Inlines.Add(new Run(" 📝"));

            if (_viewModel.IsStudyMode && refCount > 0)
            {
                tb.Inlines.Add(new Run(" "));

                var refLink = new Hyperlink(new Run($"[{refCount}]"))
                {
                    Foreground = new SolidColorBrush(theme),
                    FontWeight = FontWeights.Bold,
                    TextDecorations = [],
                    Cursor = Cursors.Hand,
                    ToolTip = refCount == 1
                        ? "View cross-reference"
                        : $"View {refCount} cross-references",
                };

                var capturedBook = _passage.BookNumber;
                var capturedChapter = _passage.Chapter;
                var capturedVerse = verse.VerseNumber;
                var capturedPrimary = _isPrimary;
                var capturedCallback = _onCrossRefClick;

                refLink.Click += (s, e) =>
                {
                    e.Handled = true;

                    if (capturedCallback == null)
                    {
                        return;
                    }

                    capturedCallback(capturedBook, capturedChapter, capturedVerse, capturedPrimary);
                };
                refLink.Inlines.Add(new Run("\u200B")
                {
                    Foreground = Brushes.Transparent,
                    FontSize = _viewModel.FontSize,
                });

                tb.Inlines.Add(refLink);
            }

            if (_viewModel.IsStudyMode)
            {
                tb.Inlines.Add(new Run(" "));

                var star = new TextBlock
                {
                    Text = "*",
                    FontSize = _viewModel.FontSize * 1.5,
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(theme),
                    Cursor = Cursors.Hand,
                    ToolTip = "Open verse commentary",
                    RenderTransform = new TranslateTransform(
                        0, Math.Round(_viewModel.FontSize * 0.4)),
                    Padding = new Thickness(2, 0, 2, 0),
                    // Constrain the line box to the surrounding body's line
                    // height so the oversized glyph doesn't inflate the row.
                    // ClipToBounds is false by default, so the glyph still
                    // paints at its natural visual size, overflowing the slot.
                    LineStackingStrategy = LineStackingStrategy.BlockLineHeight,
                    LineHeight = baseLineHeight,
                    Background = Brushes.Transparent,
                };

                var cBook = _passage.BookNumber;
                var cChap = _passage.Chapter;
                var cVerse = verse.VerseNumber;
                var cCallback = _onVerseCommentaryClick;

                star.MouseLeftButtonUp += (s, e) =>
                {
                    e.Handled = true;
                    cCallback?.Invoke(cBook, cChap, cVerse);
                };

                tb.Inlines.Add(new InlineUIContainer(star)
                {
                    BaselineAlignment = BaselineAlignment.Baseline,
                });
            }

            _groupedHighlights.TryGetValue(verse.VerseNumber, out var highlights);
            ApplyWordHighlights(tb, highlights ?? []);

            tb.MouseLeftButtonUp += VerseTextBlock_MouseLeftButtonUp;
            tb.MouseRightButtonUp += (_, e) =>
            {
                e.Handled = true;
                var v = verse;
                var p = _passage;
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    _onVerseLongPress?.Invoke(v, p);
                }), DispatcherPriority.Background);
            };

            return new Border
            {
                Padding = new Thickness(4),
                CornerRadius = new CornerRadius(4),
                Background = bg,
                Child = tb,
            };
        }

        private static void ApplyWordHighlights(TextBlock tb, List<SelectedWord> highlights)
        {
            var inlines = tb.Inlines.ToList();
            int offset = 0;
            bool skippedVerseNumber = false;

            foreach (var inline in inlines)
            {
                if (inline is not Run run) continue;
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

        private void VerseTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBlock tb) return;
            var verse = tb.Tag as Verse;
            if (verse == null) return;

            var pt = e.GetPosition(tb);
            var pos = tb.GetPositionFromPoint(pt, snapToText: true);
            if (pos == null) return;

            var run = pos.Parent as Run;

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
            _highlightedVerse = null;

            if (run.Parent is not TextBlock tb) return;

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

            var overlapping = _selectedWords
                .Where(w => w.VerseNumber == verse.VerseNumber
                            && w.Start < end
                            && w.End > start)
                .ToList();

            if (overlapping.Count > 0)
            {
                foreach (var entry in overlapping)
                {
                    _selectedWords.Remove(entry);
                    _databaseHelper?.RemoveWordHighlight(fullVerse, entry.Start, entry.End);
                }
            }
            else
            {
                var sw = new SelectedWord(verse.VerseNumber, start, end, _markerColor);
                _selectedWords.Add(sw);
                if (_databaseHelper != null)
                {
                    var argb = (_markerColor.A << 24) | (_markerColor.R << 16) |
                               (_markerColor.G << 8) | _markerColor.B;
                    _databaseHelper.AddWordHighlight(fullVerse, start, end, argb);
                }
            }

            _groupedHighlights.Clear();
            foreach (var w in _selectedWords)
            {
                if (!_groupedHighlights.TryGetValue(w.VerseNumber, out var list))
                    _groupedHighlights[w.VerseNumber] = list = [];
                list.Add(w);
            }

            _onWordHighlightAction?.Invoke();
            RenderAll();
        }

        public bool BeginWordDrag(Point positionInChapterView)
        {
            var run = FindWordRunAt(positionInChapterView);
            if (run == null) return false;

            _dragStartRun = run;
            _dragEndRun = run;
            _isWordDragActive = true;
            return true;
        }

        public void UpdateWordDrag(Point positionInChapterView)
        {
            if (!_isWordDragActive) return;

            var run = FindWordRunAt(positionInChapterView);
            if (run != null) _dragEndRun = run;

            UpdatePreview();
        }

        public void CommitWordDrag()
        {
            if (!_isWordDragActive || _dragStartRun == null || _dragEndRun == null)
            {
                CancelWordDrag();
                return;
            }

            var runs = EnumerateAllWordRuns().ToList();
            int a = runs.IndexOf(_dragStartRun);
            int b = runs.IndexOf(_dragEndRun);

            if (a < 0 || b < 0 || a == b)
            {
                CancelWordDrag();
                return;
            }

            int lo = Math.Min(a, b);
            int hi = Math.Max(a, b);

            _highlightedVerse = null;

            bool anyAdded = false;

            for (int i = lo; i <= hi; i++)
            {
                var span = GetWordSpan(runs[i]);
                if (span == null) continue;
                var (verse, _, start, end) = span.Value;
                if (end <= start) continue;

                bool already = _selectedWords.Any(w =>
                    w.VerseNumber == verse.VerseNumber
                    && w.Start < end
                    && w.End > start);
                if (already) continue;

                var fullVerse = verse with
                {
                    BookName = _passage.BookName,
                    Chapter = _passage.Chapter,
                };

                _selectedWords.Add(new SelectedWord(verse.VerseNumber, start, end, _markerColor));
                anyAdded = true;

                if (_databaseHelper != null)
                {
                    var argb = (_markerColor.A << 24) | (_markerColor.R << 16) |
                               (_markerColor.G << 8) | _markerColor.B;
                    _databaseHelper.AddWordHighlight(fullVerse, start, end, argb);
                }
            }

            _dragStartRun = null;
            _dragEndRun = null;
            _isWordDragActive = false;
            ClearPreview();

            if (!anyAdded) return;

            _groupedHighlights.Clear();
            foreach (var w in _selectedWords)
            {
                if (!_groupedHighlights.TryGetValue(w.VerseNumber, out var list))
                    _groupedHighlights[w.VerseNumber] = list = [];
                list.Add(w);
            }

            _onWordHighlightAction?.Invoke();
            RenderAll();
        }

        public void CancelWordDrag()
        {
            _dragStartRun = null;
            _dragEndRun = null;
            _isWordDragActive = false;
            ClearPreview();
        }

        private void UpdatePreview()
        {
            if (_dragStartRun == null || _dragEndRun == null) return;

            var runs = EnumerateAllWordRuns().ToList();
            int a = runs.IndexOf(_dragStartRun);
            int b = runs.IndexOf(_dragEndRun);
            if (a < 0 || b < 0) return;

            int lo = Math.Min(a, b);
            int hi = Math.Max(a, b);

            var inRange = new HashSet<Run>();
            for (int i = lo; i <= hi; i++) inRange.Add(runs[i]);

            foreach (var r in _previewOriginals.Keys.ToList())
            {
                if (!inRange.Contains(r))
                {
                    r.Background = _previewOriginals[r];
                    _previewOriginals.Remove(r);
                }
            }

            var brush = new SolidColorBrush(_markerColor) { Opacity = 0.4 };
            foreach (var r in inRange)
            {
                if (!_previewOriginals.ContainsKey(r))
                {
                    _previewOriginals[r] = r.Background;
                    r.Background = brush;
                }
            }
        }

        private void ClearPreview()
        {
            foreach (var kvp in _previewOriginals)
                kvp.Key.Background = kvp.Value;
            _previewOriginals.Clear();
        }

        private Run? FindWordRunAt(Point positionInChapterView)
        {
            if (InputHitTest(positionInChapterView) is not DependencyObject hit) return null;

            TextBlock? tb = null;
            var current = hit;
            while (current != null)
            {
                if (current is TextBlock t && t.Tag is Verse) { tb = t; break; }
                current = GetNodeParent(current);
            }
            if (tb == null) return null;

            var local = this.TranslatePoint(positionInChapterView, tb);
            var pos = tb.GetPositionFromPoint(local, snapToText: true);
            if (pos == null) return null;

            var run = pos.Parent as Run ?? FindRunAtPosition(tb, pos);
            if (run == null) return null;

            return run.Tag is VerseAnnotation { Tag: "word" } ? run : null;
        }

        private static DependencyObject? GetNodeParent(DependencyObject node)
        {
            if (node is Visual || node is Visual3D)
                return VisualTreeHelper.GetParent(node);

            if (node is FrameworkContentElement fce)
                return fce.Parent;

            return null;
        }
        private IEnumerable<Run> EnumerateAllWordRuns()
        {
            for (int i = 0; i < _content.Count; i++)
            {
                if (_content[i] is not VerseContent.VerseVal) continue;

                if (VerseItems.ItemContainerGenerator
                    .ContainerFromIndex(i) is not FrameworkElement container) continue;

                TextBlock? tb = container as TextBlock
                    ?? (container as Border)?.Child as TextBlock;
                if (tb == null) continue;

                foreach (var inline in tb.Inlines)
                    if (inline is Run r && r.Tag is VerseAnnotation { Tag: "word" })
                        yield return r;
            }
        }

        private static (Verse verse, TextBlock tb, int start, int end)? GetWordSpan(Run run)
        {
            if (run.Tag is not VerseAnnotation { Tag: "word" }) return null;
            if (run.Parent is not TextBlock tb) return null;
            if (tb.Tag is not Verse verse) return null;

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

            if (!found) return null;

            int len = run.Text?.Length ?? 0;
            if (len <= 0) return null;
            return (verse, tb, start, start + len);
        }

        private int FindVerseIndex(int verseNumber)
        {
            for (int i = 0; i < _content.Count; i++)
                if (_content[i] is VerseContent.VerseVal v
                    && v.Verse.VerseNumber == verseNumber)
                    return i;
            return -1;
        }

        public (int verseIndex, double offsetFromTop)? GetTopVerseInfo()
        {
            var sv = VerseScroller;
            if (sv == null) return null;

            double offset = sv.VerticalOffset;
            double accumulated = 0;

            for (int i = 0; i < VerseItems.Items.Count; i++)
            {
                if (VerseItems.ItemContainerGenerator
                    .ContainerFromIndex(i) is not FrameworkElement container) return null;
                double h = container.ActualHeight;
                if (h <= 0) return null;

                if (accumulated + h > offset)
                    return (i, offset - accumulated);

                accumulated += h;
            }
            return null;
        }

        public void ScrollToVerseOffset(int verseIndex, double offsetFromTop)
        {
            var sv = VerseScroller;
            if (sv == null) return;

            double accumulated = 0;
            for (int i = 0; i < verseIndex && i < VerseItems.Items.Count; i++)
            {
                if (VerseItems.ItemContainerGenerator
                    .ContainerFromIndex(i) is FrameworkElement container) accumulated += container.ActualHeight;
            }

            sv.ScrollToVerticalOffset(accumulated + offsetFromTop);
        }

        private void HighlightTargetVerse()
        {
            var timer = new DispatcherTimer
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