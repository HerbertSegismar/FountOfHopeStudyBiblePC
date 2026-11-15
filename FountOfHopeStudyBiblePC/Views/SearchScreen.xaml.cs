using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;
using FountOfHopeStudyBiblePC.Views.Modals;

namespace FountOfHopeStudyBiblePC.Views
{
    public partial class SearchScreen : UserControl
    {
        private const int MaxRenderResults = 500;
        private const string QueryPrefKey = "search_query";

        private readonly Action<PassageSelection> _onPassageSelected;
        private readonly Action<string> _onVersionChange;
        private readonly AppViewModel _viewModel;
        private readonly PreferencesService _prefs = new();
        private readonly VerseTextProcessor _processor = new();
        private readonly DispatcherTimer _saveQueryTimer;

        private DatabaseHelper? _db;

        private string _query = "";
        private string _scope = SearchScopes.Whole;
        private bool _hasSearched;
        private bool _loading;
        private List<SearchVerse> _results = [];

        private int _renderedCount;
        private TextBlock? _limitNotice;

        private SearchColors _colors = null!;
        private SearchBrushes _brushes = null!;
        private ThemeColors _themeColors = null!;
        private readonly Dictionary<Color, SolidColorBrush> _bookColorCache = [];

        public SearchScreen(
            DatabaseHelper? databaseHelper,
            Action<PassageSelection> onPassageSelected,
            string currentVersionKey,
            Action<string> onVersionChange)
        {
            InitializeComponent();

            _viewModel = AppViewModelAccessor.Current ?? new AppViewModel();
            _onPassageSelected = onPassageSelected ?? (_ => { });
            _onVersionChange = onVersionChange ?? (_ => { });

            _query = _prefs.GetString(QueryPrefKey, "");

            _saveQueryTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(400) };
            _saveQueryTimer.Tick += (_, _) =>
            {
                _saveQueryTimer.Stop();
                _prefs.Set(QueryPrefKey, _query);
            };

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            RebuildDb();
            RebuildColors();
            RefreshScopeCard();
            RefreshVersionChip();
            SearchBox.Text = _query;
            ClearBtn.Visibility = _query.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateEmptyState();
            UpdateSearchButtonLabel();

            Dispatcher.BeginInvoke(new Action(UpdateSearchButtonLabel), DispatcherPriority.Loaded);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _saveQueryTimer.Stop();
            _prefs.Set(QueryPrefKey, _query);

            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _db?.Dispose();
        }

        private void OnViewModelPropertyChanged(object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppViewModel.CurrentDbName):
                case nameof(AppViewModel.CurrentVersionAbbr):
                    RebuildDb();
                    RefreshVersionChip();
                    _results.Clear();
                    _hasSearched = false;
                    RebuildResults();
                    UpdateEmptyState();
                    break;

                case nameof(AppViewModel.DarkTheme):
                    RebuildColors();
                    RebuildResults();
                    UpdateEmptyState();
                    break;

                case nameof(AppViewModel.WordsOfJesus):
                    _themeColors = BuildThemeColors();
                    RebuildResults();
                    break;
            }
        }

        private void RebuildDb()
        {
            _db?.Dispose();
            _db = new DatabaseHelper(_viewModel.CurrentDbName);
        }

        private void RebuildColors()
        {
            var isDark = _viewModel.DarkTheme;
            _colors = new SearchColors(
                Primary: (Color)ColorConverter.ConvertFromString("#FF7FB2FF"),
                Background: Colors.Transparent,
                Text: isDark ? Colors.White : Color.FromRgb(0x1A, 0x1A, 0x1A),
                Muted: isDark ? Color.FromRgb(0x9C, 0xA3, 0xAF) : Color.FromRgb(0x6B, 0x72, 0x80),
                Card: isDark ? Color.FromRgb(0x23, 0x2A, 0x33) : Color.FromRgb(0xEA, 0xE7, 0xE3),
                Border: isDark ? Color.FromRgb(0x37, 0x41, 0x51) : Color.FromRgb(0xE5, 0xE7, 0xEB));

            _brushes = new SearchBrushes(
                Text: Frozen(_colors.Text),
                Muted: Frozen(_colors.Muted),
                Card: Frozen(_colors.Card),
                Primary: Frozen(_colors.Primary),
                PrimaryBorderSoft: Frozen(Color.FromArgb(0x33, _colors.Primary.R, _colors.Primary.G, _colors.Primary.B)),
                PrimaryBgSoft: Frozen(Color.FromArgb(0x1A, _colors.Primary.R, _colors.Primary.G, _colors.Primary.B)));

            _themeColors = BuildThemeColors();
            _bookColorCache.Clear();

            ScopeCard.Background = _brushes.Primary;
        }

        private ThemeColors BuildThemeColors() => new(
            TextColor: _colors.Text,
            VerseNumber: _colors.Primary,
            Primary: _colors.Primary,
            TagColor: _colors.Muted,
            TagBg: _colors.Card,
            WordsOfJesus: _viewModel.WordsOfJesus,
            SearchHighlightBg: Color.FromArgb(0x55, _colors.Primary.R, _colors.Primary.G, _colors.Primary.B),
            HighlightIcon: _colors.Primary,
            WordHighlightBg: Color.FromArgb(0x33, _colors.Primary.R, _colors.Primary.G, _colors.Primary.B));

        private static SolidColorBrush Frozen(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        private SolidColorBrush GetBookBrush(Color c)
        {
            if (_bookColorCache.TryGetValue(c, out var b)) return b;
            b = Frozen(c);
            _bookColorCache[c] = b;
            return b;
        }

        private void RefreshScopeCard()
        {
            var config = SearchScopes.GetScopeConfig(_scope);
            ScopeLabel.Text = config.Label;
            ScopeDescription.Text = config.Description;
        }

        private void RefreshVersionChip()
        {
            VersionChipText.Text = _viewModel.CurrentVersionAbbr;
        }

        private string BuildTermDescription(string prefix)
        {
            var inverse = InverseCheck.IsChecked == true;
            var exact = ExactCheck.IsChecked == true;
            if (inverse && exact) return $"{prefix}without exact term \"{_query}\"";
            if (inverse) return $"{prefix}without \"{_query}\"";
            if (exact) return $"{prefix}with exact term \"{_query}\"";
            return $"{prefix}for \"{_query}\"";
        }

        private string BuildModeSuffix()
        {
            var inverse = InverseCheck.IsChecked == true;
            var exact = ExactCheck.IsChecked == true;
            if (exact && inverse) return " (exact phrase & opposite)";
            if (exact) return " (exact phrase)";
            if (inverse) return " (opposite)";
            return "";
        }

        private void UpdateSearchButtonLabel()
        {
            var scopeConfig = SearchScopes.GetScopeConfig(_scope);

            string label;
            if (!_hasSearched || _loading)
            {
                label = $"Search {scopeConfig.Label}{BuildModeSuffix()}";
            }
            else if (_results.Count == 0)
            {
                label = $"No results found {BuildTermDescription("")} in {scopeConfig.Label}";
            }
            else
            {
                var bookCount = _results.Select(v => v.BookNumber).Distinct().Count();
                var found = $"Found {_results.Count} result{(_results.Count == 1 ? "" : "s")}";
                var scopeStr = SearchScopes.IsBookScope(_scope)
                    ? $" in {scopeConfig.Label}"
                    : $" in {bookCount} book{(bookCount == 1 ? "" : "s")}";
                label = $"{found}{BuildTermDescription(" ")}{scopeStr}";
            }

            SetSearchButtonLabel(label);
            SearchBtn.IsEnabled = _query.Trim().Length > 0;
        }

        private void SetSearchButtonLabel(string text)
        {
            if (SearchBtn.Template == null) return;
            if (SearchBtn.Template.FindName("Label", SearchBtn) is TextBlock tb)
                tb.Text = text;
        }

        private async void RunSearch()
        {
            if (_loading) return;

            _hasSearched = true;
            _loading = true;
            ShowLoading($"Searching {SearchScopes.GetScopeConfig(_scope).Label}…");

            try
            {
                var options = new SearchOptions { Scope = _scope };
                var inverse = InverseCheck.IsChecked == true;
                var exact = ExactCheck.IsChecked == true;
                var query = _query;

                var raw = await System.Threading.Tasks.Task.Run(() =>
                    _db?.SearchVerses(query, options, inverse, exact) ?? new List<SearchVerse>());

                var results = raw
                    .Select(v => v with { BookColor = SearchHelpers.GetBookColor(v.BookName ?? "", v) })
                    .ToList();

                _results = results;
                _loading = false;

                HideOverlays();
                RebuildResults();
                UpdateEmptyState();
                UpdateSearchButtonLabel();
                ResultsScroller.ScrollToTop();
            }
            catch
            {
                _loading = false;
                _results.Clear();
                ShowError("Failed to search. Please try again.");
            }
        }

        private void ShowLoading(string message)
        {
            LoadingLabel.Text = message;
            LoadingState.Visibility = Visibility.Visible;
            EmptyState.Visibility = Visibility.Collapsed;
            ErrorState.Visibility = Visibility.Collapsed;
            ResultsScroller.Visibility = Visibility.Collapsed;
        }

        private void ShowError(string message)
        {
            ErrorLabel.Text = message;
            ErrorState.Visibility = Visibility.Visible;
            LoadingState.Visibility = Visibility.Collapsed;
            EmptyState.Visibility = Visibility.Collapsed;
            ResultsScroller.Visibility = Visibility.Collapsed;
        }

        private void HideOverlays()
        {
            LoadingState.Visibility = Visibility.Collapsed;
            ErrorState.Visibility = Visibility.Collapsed;
        }

        private void RebuildResults()
        {
            ResultsPanel.Children.Clear();
            _limitNotice = null;
            _renderedCount = 0;

            if (_results.Count == 0)
            {
                ResultsScroller.Visibility = Visibility.Collapsed;
                return;
            }

            ResultsScroller.Visibility = Visibility.Visible;
            RenderNextBatch();
        }

        private void RenderNextBatch()
        {
            var end = Math.Min(_renderedCount + MaxRenderResults, _results.Count);

            if (_limitNotice != null)
            {
                ResultsPanel.Children.Remove(_limitNotice);
                _limitNotice = null;
            }

            for (var i = _renderedCount; i < end; i++)
                ResultsPanel.Children.Add(BuildResultCard(_results[i]));

            _renderedCount = end;

            if (_renderedCount < _results.Count)
            {
                _limitNotice = new TextBlock
                {
                    Text = $"Showing {_renderedCount} of {_results.Count} results — scroll for more",
                    FontSize = 12,
                    Foreground = _brushes.Muted,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 12, 0, 0),
                };
                ResultsPanel.Children.Add(_limitNotice);
            }
        }

        private Border BuildResultCard(SearchVerse verse)
        {
            var bookName = verse.BookName ?? "Unknown";
            var bookColorStr = verse.BookColor ?? SearchHelpers.GetBookColor(bookName, verse);
            var bookBrush = GetBookBrush((Color)ColorConverter.ConvertFromString(bookColorStr));

            var processed = _processor.ProcessVerse(
                verseText: verse.Text ?? "",
                baseFontSize: 14,
                themeColors: _themeColors,
                textColor: _colors.Text,
                isOldTestament: BibleData.GetBookByCustomNumber(verse.BookNumber)?.Testament == Testament.Old,
                options: new ProcessingOptions
                {
                    ShowStrongs = false,
                    EnableWordClick = false,
                    EnableTagClick = false,
                },
                highlight: _query);

            var bodyText = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 14,
                LineHeight = 20,
                Margin = new Thickness(0, 4, 0, 0),
                Foreground = _brushes.Text,
            };
            foreach (var inline in processed.Body)
                bodyText.Inlines.Add(inline);

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = $"{bookName} {verse.Chapter}:{verse.VerseNumber}",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = bookBrush,
            });
            stack.Children.Add(bodyText);

            var card = new Border
            {
                Background = _brushes.Card,
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12),
                Margin = new Thickness(0, 0, 0, 6),
                Cursor = Cursors.Hand,
                Tag = verse,
                Child = stack,
            };

            card.MouseLeftButtonUp += (_, _) =>
            {
                if (card.Tag is SearchVerse sv)
                {
                    _onPassageSelected(new PassageSelection(
                        sv.BookNumber,
                        sv.BookName ?? "Unknown",
                        sv.Chapter,
                        sv.VerseNumber));
                }
            };

            return card;
        }

        private void UpdateEmptyState()
        {
            if (_results.Count > 0)
            {
                EmptyState.Visibility = Visibility.Collapsed;
                return;
            }

            EmptyState.Visibility = Visibility.Visible;
            EmptyState.Children.Clear();

            if (_hasSearched && !string.IsNullOrWhiteSpace(_query))
                BuildNoResultsState();
            else
                BuildWelcomeState();
        }

        private void BuildWelcomeState()
        {
            EmptyState.Children.Add(new TextBlock
            {
                Text = "Enter a word or phrase\nto find relevant verses",
                FontSize = 16,
                Foreground = _brushes.Muted,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 24),
            });
            EmptyState.Children.Add(BuildPopularTermsCard());
        }

        private Border BuildPopularTermsCard()
        {
            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = "Popular Search Terms",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = _brushes.Primary,
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
            });

            var chips = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };

            foreach (var term in SearchHelpers.PopularSearchTerms)
            {
                var chip = new Border
                {
                    Background = _brushes.Card,
                    BorderBrush = _brushes.PrimaryBorderSoft,
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(14),
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(3),
                    Cursor = Cursors.Hand,
                    Tag = term,
                    Child = new TextBlock
                    {
                        Text = term,
                        FontSize = 12,
                        Foreground = _brushes.Primary,
                    }
                };
                chip.MouseLeftButtonUp += (_, _) =>
                {
                    if (chip.Tag is string t)
                    {
                        SearchBox.Text = t;
                        _query = t;
                        RunSearch();
                    }
                };
                chips.Children.Add(chip);
            }

            stack.Children.Add(chips);

            return new Border
            {
                Background = _brushes.PrimaryBgSoft,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 8),
                Child = stack,
            };
        }

        private void BuildNoResultsState()
        {
            EmptyState.Children.Add(new TextBlock
            {
                Text = $"No results found {BuildTermDescription("")}",
                FontSize = 16,
                Foreground = _brushes.Text,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8),
            });

            EmptyState.Children.Add(new TextBlock
            {
                Text = "Try different keywords or check spelling",
                FontSize = 13,
                Foreground = _brushes.Muted,
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 20),
            });

            var tips = new StackPanel();
            tips.Children.Add(new TextBlock
            {
                Text = "Search tips",
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = _brushes.Text,
                Margin = new Thickness(0, 0, 0, 8),
                HorizontalAlignment = HorizontalAlignment.Center,
            });
            foreach (var tip in new[]
            {
                "Try simpler or more common words",
                "Check for typos",
                "Search for single words first",
            })
            {
                tips.Children.Add(new TextBlock
                {
                    Text = $"•  {tip}",
                    FontSize = 12,
                    Foreground = _brushes.Muted,
                    Margin = new Thickness(0, 2, 0, 2),
                });
            }

            EmptyState.Children.Add(new Border
            {
                Background = _brushes.Card,
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16),
                Child = tips,
            });
        }

        private void ScopeCard_Click(object sender, MouseButtonEventArgs e)
        {
            var dialog = new SearchScopeDialog(
                currentScope: _scope,
                onScopeSelected: newScope =>
                {
                    _scope = newScope;
                    RefreshScopeCard();
                    _results.Clear();
                    _hasSearched = false;
                    RebuildResults();
                    UpdateEmptyState();
                    UpdateSearchButtonLabel();
                },
                colors: _colors)
            {
                Owner = Window.GetWindow(this),
            };
            dialog.Show();
        }

        private void VersionChip_Click(object sender, RoutedEventArgs e)
        {
            var modal = new VersionSelectionModal(
                viewModel: _viewModel,
                currentVersionKey: _viewModel.CurrentDbName,
                isSecondary: false,
                onVersionSelected: file => _onVersionChange(file),
                onDismiss: () => { })
            {
                Owner = Window.GetWindow(this),
            };
            modal.Show();
        }

        private void VersionInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowVersionInfoDialog = true;
            _viewModel.VersionInfoForDialog = _viewModel.CurrentDbName;
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                RunSearch();
                e.Handled = true;
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _query = SearchBox.Text ?? "";
            _saveQueryTimer.Stop();
            _saveQueryTimer.Start();
            ClearBtn.Visibility = _query.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateSearchButtonLabel();
        }

        private void ClearBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            _query = "";
            _results.Clear();
            _hasSearched = false;
            InverseCheck.IsChecked = false;
            ExactCheck.IsChecked = false;
            ClearBtn.Visibility = Visibility.Collapsed;
            RebuildResults();
            UpdateEmptyState();
            UpdateSearchButtonLabel();
            SearchBox.Focus();
        }

        private void Options_Changed(object sender, RoutedEventArgs e)
        {
            if (_hasSearched && !string.IsNullOrWhiteSpace(_query) && !_loading)
                RunSearch();
            else
                UpdateSearchButtonLabel();
        }

        private void SearchBtn_Click(object sender, RoutedEventArgs e)
        {
            RunSearch();
        }

        private void RetryBtn_Click(object sender, RoutedEventArgs e)
        {
            RunSearch();
        }

        private void BackToTopBtn_Click(object sender, RoutedEventArgs e)
        {
            ResultsScroller.ScrollToTop();
        }

        private void ResultsScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            var backToTopTarget = ResultsScroller.VerticalOffset > 300
                ? Visibility.Visible
                : Visibility.Collapsed;
            if (BackToTopBtn.Visibility != backToTopTarget)
                BackToTopBtn.Visibility = backToTopTarget;

            if (_loading || _results.Count == 0 || _renderedCount >= _results.Count)
                return;

            const double threshold = 400;
            var distanceFromBottom = ResultsScroller.ScrollableHeight - ResultsScroller.VerticalOffset;
            if (distanceFromBottom < threshold)
                RenderNextBatch();
        }

        private sealed record SearchBrushes(
            SolidColorBrush Text,
            SolidColorBrush Muted,
            SolidColorBrush Card,
            SolidColorBrush Primary,
            SolidColorBrush PrimaryBorderSoft,
            SolidColorBrush PrimaryBgSoft);
    }
}