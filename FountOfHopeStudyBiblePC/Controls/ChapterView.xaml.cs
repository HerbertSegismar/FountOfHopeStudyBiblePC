// Controls/ChapterView.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
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
        // ═══════════════════════════════════════════════════════════════
        //  Fields (all inside the class)
        // ═══════════════════════════════════════════════════════════════
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
                    RenderAll();
                    break;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            LoadCrossRefCounts();
            LoadWordHighlights();
            RenderAll();
            ApplyBackground();
            UpdateInlineHeader();

            if (_isCurrentPage && _targetVerse.HasValue && _targetVerse.Value > 0)
            {
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    var targetIdx = FindVerseIndex(_targetVerse.Value);
                    if (targetIdx >= 0 && VerseItems.Items.Count > targetIdx)
                    {
                        var container = VerseItems.ItemContainerGenerator
                            .ContainerFromIndex(targetIdx) as FrameworkElement;
                        container?.BringIntoView();
                        _highlightedVerse = _targetVerse.Value;
                        HighlightTargetVerse();
                    }
                    _onInitialScrollComplete();
                }), System.Windows.Threading.DispatcherPriority.ContextIdle);
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

        private FrameworkElement BuildSubheadingBlock(Subheading sub)
        {
            return new TextBlock
            {
                Text = sub.Text,
                FontWeight = FontWeights.Bold,
                FontSize = _viewModel.FontSize + 1,
                Foreground = new SolidColorBrush(_viewModel.DarkTheme
                    ? Colors.LightSteelBlue : Colors.SteelBlue),
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

            var bg = persistColor != null
                ? new SolidColorBrush(persistColor.Value)
                : (isTempHl
                    ? new SolidColorBrush(Color.FromArgb(0x55, 0x7F, 0xB2, 0xFF))
                    : Brushes.Transparent);

            var tb = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = _viewModel.FontSize,
                LineHeight = _viewModel.FontSize * 1.333,
                Tag = verse,
                Cursor = Cursors.Hand,
            };

            // Bold verse number
            tb.Inlines.Add(new Run($"{verse.VerseNumber} ")
            {
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(_viewModel.DarkTheme
                    ? Colors.LightSteelBlue : Colors.SteelBlue),
                FontSize = _viewModel.FontSize * 0.9,
            });

            // Theme palette
            var themeColors = new ThemeColors(
                TextColor: _viewModel.DarkTheme
                    ? _viewModel.DarkThemeReaderFontColor
                    : _viewModel.LightThemeReaderFontColor,
                VerseNumber: _viewModel.DarkTheme ? Colors.LightSteelBlue : Colors.SteelBlue,
                Primary: _viewModel.HeaderButtonsColor,
                TagColor: Colors.MediumPurple,
                TagBg: Color.FromArgb(0x33, 0x7F, 0xB2, 0xFF),
                WordsOfJesus: _viewModel.WordsOfJesus,
                SearchHighlightBg: Color.FromArgb(0x55, 0x7F, 0xB2, 0xFF),
                HighlightIcon: _viewModel.HeaderButtonsColor,
                WordHighlightBg: _viewModel.WordMarkerColor);

            var verseNumber = verse.VerseNumber;   // capture for closures below

            // ═══════════════════════════════════════════════════════════════
            //  THE FIX: pass the click callbacks to the processor
            // ═══════════════════════════════════════════════════════════════
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
                    EnableWordClick = _viewModel.IsDictionaryMode,
                    EnableStrongsClick = _onStrongsPress != null,
                    EnableTagClick = _onTagPress != null,
                },
                // ── The three callbacks ────────────────────────────────────
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

            tb.MouseLeftButtonUp += VerseTextBlock_MouseLeftButtonUp;
            tb.MouseRightButtonUp += (_, e) =>
            {
                e.Handled = true;                                 // stop the event bubbling
                var v = verse;                                    // capture
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

        private void VerseTextBlock_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is not TextBlock tb) return;
            var verse = tb.Tag as Verse;

            var pt = e.GetPosition(tb);
            var pos = tb.GetPositionFromPoint(pt, snapToText: true);
            if (pos == null) return;

            var run = pos.Parent as Run;
            if (run?.Tag is VerseAnnotation ann)
            {
                switch (ann.Tag)
                {
                    case "word":
                        _onWordPress?.Invoke(ann.Value);
                        break;
                    case "strong":
                        _onStrongsPress?.Invoke(ann.Value, _passage.BookNumber, _isPrimary);
                        break;
                    case "tag":
                        _onTagPress?.Invoke(ann.Value, _passage.BookNumber,
                            _passage.Chapter, verse?.VerseNumber ?? 0, _isPrimary);
                        break;
                }
            }
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