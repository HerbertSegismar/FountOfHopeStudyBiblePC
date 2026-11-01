using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;
using MahApps.Metro.IconPacks;

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

        private const double InlineIconButtonSize = 35;

        private Popup? _inlineLayoutPopup;
        private Popup? _inlineMenuPopup;
        private RotateTransform? _inlineLayoutRotation;
        private RotateTransform? _inlineMenuRotation;
        private TextBox? _fontSizeBox;
        private Window? _hookedWindow;

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

            // ── Ported from ReaderAppBar: build the two inline popups in code ──
            BuildInlinePopups();

            _viewModel.PropertyChanged += (_, e) => OnViewModelPropertyChanged(e);
            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        // ── Orientation (mirrors Kotlin's LocalConfiguration.orientation) ──
        private bool IsLandscape
        {
            get
            {
                var w = Window.GetWindow(this);
                if (w is null || w.ActualWidth <= 0 || w.ActualHeight <= 0) return false;
                return w.ActualWidth > w.ActualHeight;
            }
        }

        private bool IsLandscapeVerticalMulti
            => IsLandscape && _viewModel.MultiVersion
                && _viewModel.MultiViewLayout == "vertical";

        private void OnViewModelPropertyChanged(System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppViewModel.BgImageIndex):
                case nameof(AppViewModel.OverlayOpacity):
                case nameof(AppViewModel.LightOverlayColor):
                case nameof(AppViewModel.DarkOverlayColor):
                    ApplyBackground();
                    RenderAll();
                    break;

                case nameof(AppViewModel.DarkTheme):
                    ApplyBackground();
                    RenderAll();
                    UpdateInlineHeader();
                    if (_inlineMenuPopup is { IsOpen: true })
                        _inlineMenuPopup.Child = BuildReaderMenuContent;
                    break;

                case nameof(AppViewModel.MultiVersion):
                case nameof(AppViewModel.MultiViewLayout):
                    UpdateInlineHeader();
                    if (_inlineLayoutPopup is { IsOpen: true })
                        _inlineLayoutPopup.Child = BuildLayoutMenuContent();
                    break;

                case nameof(AppViewModel.ScrollSync):
                case nameof(AppViewModel.HeaderButtonsColor):
                    UpdateInlineHeader();
                    break;

                case nameof(AppViewModel.FontSize):
                case nameof(AppViewModel.WordsOfJesus):
                case nameof(AppViewModel.SelectedColor):
                case nameof(AppViewModel.SelectedFontFamily):
                case nameof(AppViewModel.DarkThemeReaderFontColor):
                case nameof(AppViewModel.LightThemeReaderFontColor):
                case nameof(AppViewModel.IsStudyMode):
                case nameof(AppViewModel.ShowStrongs):
                case nameof(AppViewModel.IsDictionaryMode):
                    if (!_isWordDragActive) RenderAll();
                    if (e.PropertyName == nameof(AppViewModel.IsDictionaryMode)
                        && _inlineMenuPopup is { IsOpen: true })
                        _inlineMenuPopup.Child = BuildReaderMenuContent;
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

            HookWindowSizeChanged();

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
                }), DispatcherPriority.Loaded);
            }
            else
            {
                _onInitialScrollComplete();
            }
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (_inlineLayoutPopup is { IsOpen: true }) _inlineLayoutPopup.IsOpen = false;
            if (_inlineMenuPopup is { IsOpen: true }) _inlineMenuPopup.IsOpen = false;
            UnhookWindowSizeChanged();
        }

        private void HookWindowSizeChanged()
        {
            var window = Window.GetWindow(this);
            if (window is null || ReferenceEquals(window, _hookedWindow)) return;
            UnhookWindowSizeChanged();
            _hookedWindow = window;
            _hookedWindow.SizeChanged += OnWindowSizeChanged;
        }

        private void UnhookWindowSizeChanged()
        {
            if (_hookedWindow is null) return;
            _hookedWindow.SizeChanged -= OnWindowSizeChanged;
            _hookedWindow = null;
        }

        private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
            => UpdateInlineHeader();

        // ═══════════════════════════════════════════════════════════════════
        //  Background / header / verse rendering — unchanged from before
        // ═══════════════════════════════════════════════════════════════════

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
            var fg = new SolidColorBrush(_viewModel.HeaderButtonsColor);

            InlineBookText.Text = _passage.BookName;
            InlineBookText.Foreground = fg;

            InlineChapterText.Text = _passage.Chapter.ToString();
            InlineChapterText.Foreground = fg;

            InlineVersionText.Text = _versionAbbr;
            InlineVersionText.Foreground = fg;

            InlineHeader.Visibility = _viewModel.MultiVersion
                ? Visibility.Visible
                : Visibility.Collapsed;

            InlineNavPanel.Visibility = IsLandscapeVerticalMulti
                ? Visibility.Visible
                : Visibility.Collapsed;

            InlineHomeIcon.Foreground = fg;
            InlineBookmarksIcon.Foreground = fg;
            InlineNotesIcon.Foreground = fg;
            InlineSearchIcon.Foreground = fg;
            InlineSettingsIcon.Foreground = fg;

            InlineThemeIcon.Kind = _viewModel.DarkTheme
                ? PackIconMaterialKind.Brightness6
                : PackIconMaterialKind.Brightness2;
            InlineThemeIcon.Foreground = fg;

            InlineColorIcon.Foreground = fg;

            InlineScrollSyncBtn.Visibility = _viewModel.MultiVersion
                ? Visibility.Visible
                : Visibility.Collapsed;
            InlineScrollSyncIcon.Kind = _viewModel.ScrollSync
                ? PackIconMaterialKind.LinkVariant
                : PackIconMaterialKind.LinkVariantOff;
            InlineScrollSyncIcon.Foreground = fg;

            // Kind on these two is owned by the popup lifecycle — only
            // repaint the foreground here so an open popup keeps its Close icon.
            InlineLayoutIcon.Foreground = fg;
            InlineMenuIcon.Foreground = fg;
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
            var sv = VerseScroller;
            var savedVOffset = sv?.VerticalOffset ?? 0;
            var savedHOffset = sv?.HorizontalOffset ?? 0;

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
                        VerseItems.Items.Add(BuildSubheadingBlock(sub.Subheading)); break;
                    case VerseContent.VerseVal v:
                        VerseItems.Items.Add(BuildVerseBlock(v.Verse)); break;
                }
            }

            if (savedVOffset > 0.5)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    if (VerseScroller != null)
                    {
                        VerseScroller.ScrollToVerticalOffset(savedVOffset);
                        VerseScroller.ScrollToHorizontalOffset(savedHOffset);
                    }
                }), DispatcherPriority.Loaded);
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

            int highlightedCount = 0;
            int totalCount = 0;
            bool? firstWordHighlighted = null;

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

                if (firstWordHighlighted == null)
                    firstWordHighlighted = already;

                totalCount++;
                if (already) highlightedCount++;
            }

            if (totalCount == 0)
            {
                CancelWordDrag();
                return;
            }

            bool clearBlock;
            if (highlightedCount * 2 > totalCount)
                clearBlock = true;
            else if (highlightedCount * 2 < totalCount)
                clearBlock = false;
            else
                clearBlock = firstWordHighlighted == true;

            bool anyChanged = false;

            for (int i = lo; i <= hi; i++)
            {
                var span = GetWordSpan(runs[i]);
                if (span == null) continue;
                var (verse, _, start, end) = span.Value;
                if (end <= start) continue;

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

                if (clearBlock)
                {
                    if (overlapping.Count == 0) continue;
                    foreach (var entry in overlapping)
                    {
                        _selectedWords.Remove(entry);
                        _databaseHelper?.RemoveWordHighlight(fullVerse, entry.Start, entry.End);
                    }
                    anyChanged = true;
                }
                else
                {
                    if (overlapping.Count > 0) continue;
                    _selectedWords.Add(new SelectedWord(verse.VerseNumber, start, end, _markerColor));
                    anyChanged = true;

                    if (_databaseHelper != null)
                    {
                        var argb = (_markerColor.A << 24) | (_markerColor.R << 16) |
                                   (_markerColor.G << 8) | _markerColor.B;
                        _databaseHelper.AddWordHighlight(fullVerse, start, end, argb);
                    }
                }
            }

            _dragStartRun = null;
            _dragEndRun = null;
            _isWordDragActive = false;
            ClearPreview();

            if (!anyChanged) return;

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

            int highlightedCount = 0;
            int totalCount = 0;
            bool? firstWordHighlighted = null;

            for (int i = lo; i <= hi; i++)
            {
                var r = runs[i];
                if (!inRange.Contains(r)) continue;

                var span = GetWordSpan(r);
                if (span == null) continue;
                var (verse, _, start, end) = span.Value;
                if (end <= start) continue;

                bool already = _selectedWords.Any(w =>
                    w.VerseNumber == verse.VerseNumber
                    && w.Start < end
                    && w.End > start);

                if (firstWordHighlighted == null)
                    firstWordHighlighted = already;

                totalCount++;
                if (already) highlightedCount++;
            }

            bool clearBlock;
            if (totalCount == 0)
                clearBlock = false;
            else if (highlightedCount * 2 > totalCount)
                clearBlock = true;
            else if (highlightedCount * 2 < totalCount)
                clearBlock = false;
            else
                clearBlock = firstWordHighlighted == true;

            foreach (var r in _previewOriginals.Keys.ToList())
            {
                if (!inRange.Contains(r))
                {
                    r.Background = _previewOriginals[r];
                    _previewOriginals.Remove(r);
                }
            }

            var addBrush = new SolidColorBrush(_markerColor) { Opacity = 0.4 };
            foreach (var r in inRange)
            {
                if (!_previewOriginals.ContainsKey(r))
                    _previewOriginals[r] = r.Background;

                r.Background = clearBlock ? null : addBrush;
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
                current = VisualTreeHelpers.GetParent(current);
            }
            if (tb == null) return null;

            var local = this.TranslatePoint(positionInChapterView, tb);
            var pos = tb.GetPositionFromPoint(local, snapToText: true);
            if (pos == null) return null;

            var run = pos.Parent as Run ?? FindRunAtPosition(tb, pos);
            if (run == null) return null;

            return run.Tag is VerseAnnotation { Tag: "word" } ? run : null;
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
                    .ContainerFromIndex(i) is FrameworkElement container)
                    accumulated += container.ActualHeight;
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

        // ═══════════════════════════════════════════════════════════════════
        //  Inline header button handlers — same wiring as ReaderAppBar
        // ═══════════════════════════════════════════════════════════════════

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
    => _onThemeToggle();

        private void InlineColorBtn_Click(object sender, RoutedEventArgs e)
            => _onColorLensClick();

        // ── Nav row — matches ReaderAppBar's ScreenRequested?.Invoke(new Screen.X()) ──
        private void InlineHomeBtn_Click(object sender, RoutedEventArgs e)
            => _onScreenChange?.Invoke(new Screen.Home());

        private void InlineBookmarksBtn_Click(object sender, RoutedEventArgs e)
            => _onScreenChange?.Invoke(new Screen.Bookmarks());

        private void InlineNotesBtn_Click(object sender, RoutedEventArgs e)
            => _onScreenChange?.Invoke(new Screen.Notes());

        private void InlineSearchBtn_Click(object sender, RoutedEventArgs e)
            => _onScreenChange?.Invoke(new Screen.Search());

        private void InlineSettingsBtn_Click(object sender, RoutedEventArgs e)
            => _onScreenChange?.Invoke(new Screen.Settings());

        private void InlineScrollSyncBtn_Click(object sender, RoutedEventArgs e)
            => _viewModel.ScrollSync = !_viewModel.ScrollSync;

        // ═══════════════════════════════════════════════════════════════════
        //  Inline Layout / Menu — full ReaderAppBar popup port
        // ═══════════════════════════════════════════════════════════════════

        private void BuildInlinePopups()
        {
            // Rotation origins so the Open/Close animation pivots correctly.
            _inlineLayoutRotation = new RotateTransform(0);
            InlineLayoutIcon.RenderTransformOrigin = new Point(0.5, 0.5);
            InlineLayoutIcon.RenderTransform = _inlineLayoutRotation;

            _inlineMenuRotation = new RotateTransform(0);
            InlineMenuIcon.RenderTransformOrigin = new Point(0.5, 0.5);
            InlineMenuIcon.RenderTransform = _inlineMenuRotation;

            _inlineLayoutPopup = new Popup
            {
                StaysOpen = true,
                AllowsTransparency = true,
                Placement = PlacementMode.Bottom,
            };
            _inlineLayoutPopup.Opened += (_, _) =>
            {
                InlineLayoutIcon.Kind = PackIconMaterialKind.Close;
                AnimateRotation(_inlineLayoutRotation, 180);

                var window = Window.GetWindow(this);
                if (window is not null)
                {
                    window.PreviewMouseLeftButtonDown += OnInlineLayoutMouseDown;
                    window.Deactivated += OnInlineLayoutDeactivated;
                }
            };
            _inlineLayoutPopup.Closed += (_, _) =>
            {
                InlineLayoutIcon.Kind = PackIconMaterialKind.ViewGrid;
                AnimateRotation(_inlineLayoutRotation, 0);

                var window = Window.GetWindow(this);
                if (window is not null)
                {
                    window.PreviewMouseLeftButtonDown -= OnInlineLayoutMouseDown;
                    window.Deactivated -= OnInlineLayoutDeactivated;
                }
            };

            _inlineMenuPopup = new Popup
            {
                StaysOpen = true,
                AllowsTransparency = true,
                Placement = PlacementMode.Bottom,
            };
            _inlineMenuPopup.Opened += (_, _) =>
            {
                InlineMenuIcon.Kind = PackIconMaterialKind.Close;
                AnimateRotation(_inlineMenuRotation, 180);

                var window = Window.GetWindow(this);
                if (window is not null)
                {
                    window.PreviewMouseLeftButtonDown += OnInlineMenuMouseDown;
                    window.Deactivated += OnInlineMenuDeactivated;
                }
            };
            _inlineMenuPopup.Closed += (_, _) =>
            {
                CommitPendingFontSize();
                _fontSizeBox = null;

                InlineMenuIcon.Kind = PackIconMaterialKind.Menu;
                AnimateRotation(_inlineMenuRotation, 0);

                var window = Window.GetWindow(this);
                if (window is not null)
                {
                    window.PreviewMouseLeftButtonDown -= OnInlineMenuMouseDown;
                    window.Deactivated -= OnInlineMenuDeactivated;
                }
            };
        }

        private void InlineLayoutBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_inlineLayoutPopup is null || sender is not UIElement btn) return;

            if (_inlineLayoutPopup.IsOpen)
            {
                _inlineLayoutPopup.IsOpen = false;
                return;
            }

            _inlineLayoutPopup.Child = BuildLayoutMenuContent();
            _inlineLayoutPopup.PlacementTarget = btn;
            _inlineLayoutPopup.Placement = PlacementMode.Bottom;
            _inlineLayoutPopup.IsOpen = true;
        }

        private void InlineMenuBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_inlineMenuPopup is null || sender is not UIElement btn) return;

            if (_inlineMenuPopup.IsOpen)
            {
                _inlineMenuPopup.IsOpen = false;
                return;
            }

            _inlineMenuPopup.Child = BuildReaderMenuContent;
            _inlineMenuPopup.PlacementTarget = btn;
            _inlineMenuPopup.Placement = PlacementMode.Bottom;
            _inlineMenuPopup.IsOpen = true;
        }

        private void OnInlineLayoutMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_inlineLayoutPopup is null || !_inlineLayoutPopup.IsOpen) return;

            var source = e.OriginalSource as DependencyObject;
            if (_inlineLayoutPopup.PlacementTarget is UIElement pt &&
                (ReferenceEquals(source, pt) || VisualTreeHelpers.IsDescendantOf(source, pt)))
                return;

            _inlineLayoutPopup.IsOpen = false;
        }

        private void OnInlineLayoutDeactivated(object? sender, EventArgs e)
        {
            if (_inlineLayoutPopup?.IsOpen == true) _inlineLayoutPopup.IsOpen = false;
        }

        private void OnInlineMenuMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_inlineMenuPopup is null || !_inlineMenuPopup.IsOpen) return;

            var source = e.OriginalSource as DependencyObject;
            if (_inlineMenuPopup.PlacementTarget is UIElement pt &&
                (ReferenceEquals(source, pt) || VisualTreeHelpers.IsDescendantOf(source, pt)))
                return;

            _inlineMenuPopup.IsOpen = false;
        }

        private void OnInlineMenuDeactivated(object? sender, EventArgs e)
        {
            if (_inlineMenuPopup?.IsOpen == true) _inlineMenuPopup.IsOpen = false;
        }

        private static void AnimateRotation(RotateTransform? rotation, double angle)
        {
            if (rotation is null) return;
            var anim = new DoubleAnimation
            {
                To = angle,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            };
            rotation.BeginAnimation(RotateTransform.AngleProperty, anim);
        }

        private void CommitPendingFontSize()
        {
            if (_fontSizeBox is null) return;
            if (int.TryParse(_fontSizeBox.Text, out var value))
                _viewModel.FontSize = Math.Clamp(value, 1, 100);
            _fontSizeBox.Text = _viewModel.FontSize.ToString();
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Layout popup content — verbatim port of ReaderAppBar
        // ═══════════════════════════════════════════════════════════════════

        private Border BuildLayoutMenuContent()
        {
            var bg = _viewModel.DarkTheme
                ? _viewModel.DarkModalBackgroundColor
                : _viewModel.LightModalBackgroundColor;
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);
            var primary = ThemeManager.PrimaryColor;

            var container = new Border
            {
                Background = new SolidColorBrush(bg),
                BorderBrush = AppBarShared.MenuBorderBrush(_viewModel.DarkTheme, 0x33),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(4),
                MinWidth = 220,
                MaxWidth = 340,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 14,
                    ShadowDepth = 3,
                    Direction = 270,
                    Color = Colors.Black,
                    Opacity = 0.4,
                },
            };

            var stack = new StackPanel { Margin = new Thickness(4) };

            stack.Children.Add(new Border
            {
                Height = 25,
                Child = new TextBlock
                {
                    Text = _viewModel.MultiVersion ? "Parallel View" : "Single View",
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(primary),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                },
            });
            stack.Children.Add(AppBarShared.BuildSeparator());

            var layouts = new (string Title, PackIconMaterialKind Icon, double Rotate)[]
            {
                ("Single",     PackIconMaterialKind.Numeric1,   0),
                ("Horizontal", PackIconMaterialKind.ViewStream, 90),
                ("Vertical",   PackIconMaterialKind.ViewStream, 0),
            };

            foreach (var (title, iconKind, rotate) in layouts)
            {
                var isActive = title switch
                {
                    "Single" => !_viewModel.MultiVersion,
                    "Horizontal" => _viewModel.MultiVersion && _viewModel.MultiViewLayout == "horizontal",
                    "Vertical" => _viewModel.MultiVersion && _viewModel.MultiViewLayout == "vertical",
                    _ => false,
                };

                var icon = new PackIconMaterial
                {
                    Kind = iconKind,
                    Width = 16,
                    Height = 16,
                    Foreground = fg,
                };
                if (rotate != 0)
                {
                    icon.RenderTransformOrigin = new Point(0.5, 0.5);
                    icon.RenderTransform = new RotateTransform(rotate);
                }

                var capturedTitle = title;
                stack.Children.Add(AppBarShared.BuildMenuRow(
                    label: title,
                    icon: icon,
                    onClick: () =>
                    {
                        switch (capturedTitle)
                        {
                            case "Single":
                                _viewModel.MultiVersion = false;
                                break;
                            case "Horizontal":
                                _viewModel.SquareAspectViews = false;
                                _viewModel.MultiVersion = true;
                                _viewModel.MultiViewLayout = "horizontal";
                                break;
                            case "Vertical":
                                _viewModel.SquareAspectViews = false;
                                _viewModel.MultiVersion = true;
                                _viewModel.MultiViewLayout = "vertical";
                                break;
                        }
                        if (_inlineLayoutPopup is not null) _inlineLayoutPopup.IsOpen = false;
                    },
                    foreground: fg,
                    isActive: isActive,
                    isDarkTheme: _viewModel.DarkTheme));
            }

            if (_viewModel.MultiVersion)
            {
                stack.Children.Add(AppBarShared.BuildSeparator());

                var rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

                var label = new TextBlock
                {
                    Text = "Square Aspect Ratio",
                    Foreground = fg,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 6, 8, 6),
                };
                Grid.SetColumn(label, 0);
                rowGrid.Children.Add(label);

                var check = new CheckBox
                {
                    IsChecked = _viewModel.SquareAspectViews,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0),
                    IsHitTestVisible = false,
                };
                Grid.SetColumn(check, 1);
                rowGrid.Children.Add(check);

                var row = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Cursor = Cursors.Hand,
                    Child = rowGrid,
                };

                var hoverBg = new SolidColorBrush(_viewModel.DarkTheme
                    ? Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)
                    : Color.FromArgb(0x22, 0x00, 0x00, 0x00));
                row.MouseEnter += (_, _) => row.Background = hoverBg;
                row.MouseLeave += (_, _) => row.Background = Brushes.Transparent;
                row.MouseLeftButtonUp += (_, _) =>
                {
                    _viewModel.SquareAspectViews = !_viewModel.SquareAspectViews;
                    check.IsChecked = _viewModel.SquareAspectViews;
                };

                stack.Children.Add(row);
            }

            container.Child = stack;
            return container;
        }

        // ═══════════════════════════════════════════════════════════════════
        //  Reader menu content — verbatim port of ReaderAppBar
        // ═══════════════════════════════════════════════════════════════════

        private FrameworkElement BuildReaderMenuContent
        {
            get
            {
                var isLandscape = IsLandscape;
                var bg = _viewModel.DarkTheme
                    ? _viewModel.DarkModalBackgroundColor
                    : _viewModel.LightModalBackgroundColor;

                var container = new Border
                {
                    Background = new SolidColorBrush(bg),
                    BorderBrush = AppBarShared.MenuBorderBrush(_viewModel.DarkTheme, 0x33),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(6),
                    Padding = new Thickness(4),
                    MinWidth = 220,
                    MaxWidth = isLandscape ? 900 : 340,
                    Effect = new DropShadowEffect
                    {
                        BlurRadius = 14,
                        ShadowDepth = 3,
                        Direction = 270,
                        Color = Colors.Black,
                        Opacity = 0.4,
                    },
                };

                if (isLandscape)
                {
                    var grid = new Grid();
                    grid.ColumnDefinitions.Add(new ColumnDefinition
                    { Width = new GridLength(1, GridUnitType.Star) });
                    grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                    grid.ColumnDefinitions.Add(new ColumnDefinition
                    { Width = new GridLength(1, GridUnitType.Star) });

                    var leftColumn = new StackPanel { Margin = new Thickness(4) };
                    BuildCommonScreenItems(leftColumn);
                    if (_viewModel.BgImageIndex != 0)
                        leftColumn.Children.Add(BuildBackgroundTextureItem());
                    Grid.SetColumn(leftColumn, 0);
                    grid.Children.Add(leftColumn);

                    var divider = new Border
                    {
                        Width = 1,
                        Background = new SolidColorBrush(Color.FromArgb(0x33, 0x80, 0x80, 0x80)),
                        Margin = new Thickness(4, 6, 4, 6),
                    };
                    Grid.SetColumn(divider, 1);
                    grid.Children.Add(divider);

                    var rightColumn = new StackPanel { Margin = new Thickness(4) };
                    BuildExtraReaderControls(rightColumn);
                    Grid.SetColumn(rightColumn, 2);
                    grid.Children.Add(rightColumn);

                    container.Child = grid;
                }
                else
                {
                    var stack = new StackPanel { Margin = new Thickness(4) };
                    BuildCommonItems(stack);
                    container.Child = stack;
                }

                return container;
            }
        }

        private void BuildCommonScreenItems(Panel host)
        {
            var readerScreen = new Screen.Reader(_viewModel.PrimaryPassage);
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);

            foreach (var (title, screen, iconKind) in
                     AppBarShared.GetNavigationItems(_viewModel.PrimaryPassage))
            {
                var icon = new PackIconMaterial
                {
                    Kind = iconKind,
                    Width = 16,
                    Height = 16,
                    Foreground = fg,
                };
                var isActive = AppBarShared.ScreensMatch(readerScreen, screen);
                var target = screen;

                host.Children.Add(AppBarShared.BuildMenuRow(
                    label: title,
                    icon: icon,
                    onClick: () =>
                    {
                        if (_inlineMenuPopup is not null) _inlineMenuPopup.IsOpen = false;
                        _onScreenChange?.Invoke(target);
                    },
                    foreground: fg,
                    isActive: isActive,
                    isDarkTheme: _viewModel.DarkTheme));
            }
        }

        private void BuildCommonItems(Panel host)
        {
            BuildCommonScreenItems(host);

            host.Children.Add(AppBarShared.BuildSeparator());
            host.Children.Add(BuildBackgroundTextureItem());
            host.Children.Add(AppBarShared.BuildSeparator());

            if (_viewModel.BgImageIndex != 0)
            {
                host.Children.Add(BuildOverlayOpacityItem());
                host.Children.Add(AppBarShared.BuildSeparator());
            }

            host.Children.Add(BuildDictionaryModeItem());
            host.Children.Add(AppBarShared.BuildSeparator());

            host.Children.Add(_viewModel.IsDictionaryMode
                ? BuildColorPickerItem("Verse Marker Color",
                    _viewModel.VerseMarkerColor,
                    () => _viewModel.ShowVerseMarkerColorWheelDialog = true)
                : BuildColorPickerItem("Word Marker Color",
                    _viewModel.WordMarkerColor,
                    () => _viewModel.ShowWordMarkerColorWheelDialog = true));

            host.Children.Add(AppBarShared.BuildSeparator());
            host.Children.Add(BuildColorPickerItem("Jesus' Words Color",
                _viewModel.WordsOfJesus,
                () => _viewModel.ShowJesusWordsColorWheelDialog = true));

            host.Children.Add(AppBarShared.BuildSeparator());

            var fontColor = _viewModel.DarkTheme
                ? _viewModel.DarkThemeReaderFontColor
                : _viewModel.LightThemeReaderFontColor;
            host.Children.Add(BuildColorPickerItem("Font Color", fontColor, () =>
            {
                if (_viewModel.DarkTheme) _viewModel.ShowDarkReaderFontColorWheelDialog = true;
                else _viewModel.ShowLightReaderFontColorWheelDialog = true;
            }));

            host.Children.Add(AppBarShared.BuildSeparator());
            host.Children.Add(BuildFontSizeItem());
        }

        private void BuildExtraReaderControls(Panel host)
        {
            if (_viewModel.BgImageIndex != 0)
            {
                host.Children.Add(BuildOverlayOpacityItem());
                host.Children.Add(AppBarShared.BuildSeparator());
            }

            host.Children.Add(BuildDictionaryModeItem());
            host.Children.Add(AppBarShared.BuildSeparator());

            host.Children.Add(_viewModel.IsDictionaryMode
                ? BuildColorPickerItem("Verse Marker Color",
                    _viewModel.VerseMarkerColor,
                    () => _viewModel.ShowVerseMarkerColorWheelDialog = true)
                : BuildColorPickerItem("Word Marker Color",
                    _viewModel.WordMarkerColor,
                    () => _viewModel.ShowWordMarkerColorWheelDialog = true));

            host.Children.Add(AppBarShared.BuildSeparator());
            host.Children.Add(BuildColorPickerItem("Jesus' Words Color",
                _viewModel.WordsOfJesus,
                () => _viewModel.ShowJesusWordsColorWheelDialog = true));

            host.Children.Add(AppBarShared.BuildSeparator());

            var fontColor = _viewModel.DarkTheme
                ? _viewModel.DarkThemeReaderFontColor
                : _viewModel.LightThemeReaderFontColor;
            host.Children.Add(BuildColorPickerItem("Font Color", fontColor, () =>
            {
                if (_viewModel.DarkTheme) _viewModel.ShowDarkReaderFontColorWheelDialog = true;
                else _viewModel.ShowLightReaderFontColorWheelDialog = true;
            }));

            host.Children.Add(AppBarShared.BuildSeparator());
            host.Children.Add(BuildFontSizeItem());
        }

        private Border BuildBackgroundTextureItem()
        {
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);
            var icon = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.TextureBox,
                Width = 16,
                Height = 16,
                Foreground = fg,
            };
            return AppBarShared.BuildMenuRow("Background Texture", icon, () =>
            {
                if (_inlineMenuPopup is not null) _inlineMenuPopup.IsOpen = false;
                _viewModel.ShowBgModal = true;
            }, fg, isDarkTheme: _viewModel.DarkTheme);
        }

        private Border BuildDictionaryModeItem()
        {
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);
            var icon = new PackIconMaterial
            {
                Kind = _viewModel.IsDictionaryMode
                    ? PackIconMaterialKind.LabelVariant
                    : PackIconMaterialKind.Fire,
                Width = 16,
                Height = 16,
                Foreground = new SolidColorBrush(ThemeManager.PrimaryColor),
            };
            var label = _viewModel.IsDictionaryMode
                ? "Dictionary Mode On"
                : "Word Marker On";

            return AppBarShared.BuildMenuRow(label, icon, () =>
            {
                if (_viewModel.IsDictionaryMode)
                    _viewModel.IsHighlightMode = true;
                else
                    _viewModel.IsDictionaryMode = true;

                if (_inlineMenuPopup is not null)
                    _inlineMenuPopup.Child = BuildReaderMenuContent;
            }, fg, isDarkTheme: _viewModel.DarkTheme);
        }

        private Border BuildColorPickerItem(string label, Color color, Action onClick)
        {
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);
            var swatch = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(color),
                BorderBrush = AppBarShared.MenuBorderBrush(_viewModel.DarkTheme, 0x66),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
            };
            return AppBarShared.BuildMenuRow(label, swatch, () =>
            {
                if (_inlineMenuPopup is not null) _inlineMenuPopup.IsOpen = false;
                onClick();
            }, fg, isDarkTheme: _viewModel.DarkTheme);
        }

        private Border BuildOverlayOpacityItem()
        {
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);

            var slider = new Slider
            {
                Width = 140,
                Minimum = 0,
                Maximum = 1,
                Value = _viewModel.OverlayOpacity,
                VerticalAlignment = VerticalAlignment.Center,
            };
            slider.ValueChanged += (_, e) => _viewModel.OverlayOpacity = (float)e.NewValue;

            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(24, 4, 8, 4),
            };
            stack.Children.Add(new TextBlock
            {
                Text = "Overlay: ",
                Foreground = fg,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
            });
            stack.Children.Add(slider);

            return new Border
            {
                CornerRadius = new CornerRadius(4),
                Child = stack,
            };
        }

        private Border BuildFontSizeItem()
        {
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);
            var borderBrush = AppBarShared.MenuBorderBrush(_viewModel.DarkTheme, 0x33);

            var sizeBox = new TextBox
            {
                Text = _viewModel.FontSize.ToString(),
                Foreground = fg,
                CaretBrush = fg,
                Background = Brushes.Transparent,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Width = 44,
                Height = 26,
                Padding = new Thickness(0),
                Margin = new Thickness(4, 0, 4, 0),
            };
            _fontSizeBox = sizeBox;

            sizeBox.PreviewTextInput += (_, e) =>
            {
                e.Handled = e.Text.Any(c => !char.IsDigit(c));
            };

            DataObject.AddPastingHandler(sizeBox, (_, e) =>
            {
                if (e.DataObject.GetData(DataFormats.Text) is string pasted
                    && pasted.Any(c => !char.IsDigit(c)))
                {
                    e.CancelCommand();
                }
            });

            void CommitFontSize()
            {
                if (int.TryParse(sizeBox.Text, out var value))
                    _viewModel.FontSize = Math.Clamp(value, 1, 100);

                sizeBox.Text = _viewModel.FontSize.ToString();
                sizeBox.CaretIndex = sizeBox.Text.Length;
            }

            sizeBox.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    CommitFontSize();
                    e.Handled = true;
                }
            };
            sizeBox.LostFocus += (_, _) => CommitFontSize();

            var minusBtn = MakeSmallButton("A-", () =>
            {
                CommitFontSize();
                _viewModel.FontSize = Math.Max(1, _viewModel.FontSize - 1);
                sizeBox.Text = _viewModel.FontSize.ToString();
            });
            var plusBtn = MakeSmallButton("A+", () =>
            {
                CommitFontSize();
                _viewModel.FontSize = Math.Min(100, _viewModel.FontSize + 1);
                sizeBox.Text = _viewModel.FontSize.ToString();
            });

            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(24, 4, 8, 4),
            };
            stack.Children.Add(new TextBlock
            {
                Text = "Font Size: ",
                Foreground = fg,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
            });
            stack.Children.Add(minusBtn);
            stack.Children.Add(sizeBox);
            stack.Children.Add(plusBtn);

            return new Border
            {
                CornerRadius = new CornerRadius(4),
                Child = stack,
            };
        }

        private Button MakeSmallButton(string text, Action onClick)
        {
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);

            var btn = new Button
            {
                Content = text,
                Width = 32,
                Height = 26,
                Margin = new Thickness(4, 0, 4, 0),
                Background = Brushes.Transparent,
                BorderBrush = AppBarShared.MenuBorderBrush(_viewModel.DarkTheme, 0x33),
                BorderThickness = new Thickness(1),
                Foreground = fg,
                Cursor = Cursors.Hand,
            };
            btn.Click += (_, _) => onClick();
            return btn;
        }
    }
}