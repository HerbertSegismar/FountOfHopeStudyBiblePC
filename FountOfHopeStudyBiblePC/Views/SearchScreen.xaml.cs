// Views/SearchScreen.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
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
        // ── Dependencies ──────────────────────────────────────────
        private readonly Action<PassageSelection> _onPassageSelected;
        private readonly Action<string> _onVersionChange;
        private readonly AppViewModel _viewModel;
        private readonly PreferencesService _prefs = new();
        private readonly VerseTextProcessor _processor = new();

        // ── Own DB instance ───────────────────────────────────────
        private DatabaseHelper? _db;

        // ── State ─────────────────────────────────────────────────
        private string _query = "";
        private string _scope = SearchScopes.Whole;
        private bool _hasSearched;
        private bool _loading;
        private List<SearchVerse> _results = new();

        private const int MaxRenderResults = 500;

        // ── Colors ────────────────────────────────────────────────
        private SearchColors _colors = null!;

        // ═══════════════════════════════════════════════════════════
        //  Constructor
        // ═══════════════════════════════════════════════════════════
        public SearchScreen(
            DatabaseHelper? databaseHelper,
            Action<PassageSelection> onPassageSelected,
            string currentVersionKey,
            Action<string> onVersionChange)
        {
            InitializeComponent();

            _viewModel = ViewModels.AppViewModelAccessor.Current
                                 ?? new AppViewModel();
            _onPassageSelected = onPassageSelected ?? (_ => { });
            _onVersionChange = onVersionChange ?? (_ => { });

            _ = databaseHelper;

            _query = _prefs.GetString("search_query", "");

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        // ═══════════════════════════════════════════════════════════
        //  Lifecycle
        // ═══════════════════════════════════════════════════════════
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            App.Log("SearchScreen: Loaded");
            RebuildDb();
            RebuildColors();
            RefreshScopeCard();
            RefreshVersionChip();
            SearchBox.Text = _query;
            ClearBtn.Visibility = _query.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            UpdateEmptyState();
            UpdateSearchButtonLabel();

            // ── NEW: re-apply the label after WPF has applied the template ──
            Dispatcher.BeginInvoke(new Action(UpdateSearchButtonLabel),
                System.Windows.Threading.DispatcherPriority.Loaded);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
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
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  DB / Colors
        // ═══════════════════════════════════════════════════════════
        private void RebuildDb()
        {
            _db?.Dispose();
            _db = new DatabaseHelper(_viewModel.CurrentDbName);
            App.Log($"SearchScreen: DB rebuilt for {_viewModel.CurrentDbName}");
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

            ScopeCard.Background = new SolidColorBrush(_colors.Primary);
        }

        // ═══════════════════════════════════════════════════════════
        //  Refresh labels
        // ═══════════════════════════════════════════════════════════
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

        private void UpdateSearchButtonLabel()
        {
            // ═══ FIX 2: removed unused `mode` variable ═══
            var scopeConfig = SearchScopes.GetScopeConfig(_scope);

            string label;
            if (!_hasSearched || _loading)
            {
                var modeSuffix =
                    ExactCheck.IsChecked == true && InverseCheck.IsChecked == true ? " (exact phrase & opposite)" :
                    ExactCheck.IsChecked == true ? " (exact phrase)" :
                    InverseCheck.IsChecked == true ? " (opposite)" : "";
                label = $"Search {scopeConfig.Label}{modeSuffix}";
            }
            else if (_results.Count == 0)
            {
                var desc =
                    InverseCheck.IsChecked == true && ExactCheck.IsChecked == true
                        ? $"without exact term \"{_query}\"" :
                    InverseCheck.IsChecked == true
                        ? $"without \"{_query}\"" :
                    ExactCheck.IsChecked == true
                        ? $"with exact term \"{_query}\"" :
                        $"for \"{_query}\"";
                label = $"No results found {desc} in {scopeConfig.Label}";
            }
            else
            {
                var bookCount = _results.Select(v => v.BookNumber).Distinct().Count();
                var found = $"Found {_results.Count} result{(_results.Count == 1 ? "" : "s")}";
                var term = InverseCheck.IsChecked == true && ExactCheck.IsChecked == true
                    ? $" without exact term \"{_query}\"" :
                    InverseCheck.IsChecked == true
                        ? $" without \"{_query}\"" :
                    ExactCheck.IsChecked == true
                        ? $" with exact term \"{_query}\"" :
                        $" for \"{_query}\"";
                var scopeStr = SearchScopes.IsBookScope(_scope)
                    ? $" in {scopeConfig.Label}"
                    : $" in {bookCount} book{(bookCount == 1 ? "" : "s")}";
                label = $"{found}{term}{scopeStr}";
            }

            // ═══ FIX 3: use SetSearchButtonLabel which finds the template TextBlock ═══
            SetSearchButtonLabel(label);
            SearchBtn.IsEnabled = _query.Trim().Length > 0;
        }

        /// <summary>
        /// Updates the label inside the SearchBtn's ControlTemplate.
        /// The template has a TextBlock x:Name="Label" — we reach in via FindName.
        /// </summary>
        private void SetSearchButtonLabel(string text)
        {
            if (SearchBtn.Template == null) return;
            if (SearchBtn.Template.FindName("Label", SearchBtn) is TextBlock tb)
                tb.Text = text;
        }

        // ═══════════════════════════════════════════════════════════
        //  Search
        // ═══════════════════════════════════════════════════════════
        private async void RunSearch()
        {
            _hasSearched = true;
            _loading = true;
            ShowLoading($"Searching {SearchScopes.GetScopeConfig(_scope).Label}…");

            try
            {
                var options = new SearchOptions { Scope = _scope };
                var inverse = InverseCheck.IsChecked == true;
                var exact = ExactCheck.IsChecked == true;
                var query = _query;

                var results = await System.Threading.Tasks.Task.Run(() =>
                    _db?.SearchVerses(query, options, inverse, exact) ?? new List<SearchVerse>());

                var enhanced = results.Select(v =>
                    v with { BookColor = SearchHelpers.GetBookColor(v.BookName ?? "", v) })
                    .ToList();

                _results = enhanced;
                _loading = false;

                HideOverlays();
                RebuildResults();
                UpdateEmptyState();
                UpdateSearchButtonLabel();
                ResultsScroller.ScrollToTop();
            }
            catch (Exception ex)
            {
                App.Log($"SearchScreen: search failed — {ex.Message}");
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

        // ═══════════════════════════════════════════════════════════
        //  Results rendering
        // ═══════════════════════════════════════════════════════════
        private void RebuildResults()
        {
            ResultsPanel.Children.Clear();

            if (_results.Count == 0)
            {
                ResultsScroller.Visibility = Visibility.Collapsed;
                return;
            }

            ResultsScroller.Visibility = Visibility.Visible;

            var toRender = _results.Count > MaxRenderResults
                ? _results.Take(MaxRenderResults).ToList()
                : _results;

            foreach (var verse in toRender)
                ResultsPanel.Children.Add(BuildResultCard(verse));

            if (_results.Count > MaxRenderResults)
            {
                ResultsPanel.Children.Add(new TextBlock
                {
                    Text = $"Showing first {MaxRenderResults} of {_results.Count} results",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(_colors.Muted),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Margin = new Thickness(0, 12, 0, 0),
                });
            }
        }

        private FrameworkElement BuildResultCard(SearchVerse verse)
        {
            var bookName = verse.BookName ?? "Unknown";
            var bookColorStr = verse.BookColor
                ?? SearchHelpers.GetBookColor(bookName, verse);
            var bookColor = (Color)ColorConverter.ConvertFromString(bookColorStr);

            var themeColors = new ThemeColors(
                TextColor: _colors.Text,
                VerseNumber: _colors.Primary,
                Primary: _colors.Primary,
                TagColor: _colors.Muted,
                TagBg: _colors.Card,
                WordsOfJesus: _viewModel.WordsOfJesus,
                SearchHighlightBg: Color.FromArgb(0x55, _colors.Primary.R, _colors.Primary.G, _colors.Primary.B),
                HighlightIcon: _colors.Primary,
                WordHighlightBg: Color.FromArgb(0x33, _colors.Primary.R, _colors.Primary.G, _colors.Primary.B));

            var processed = _processor.ProcessVerse(
                verseText: verse.Text ?? "",
                baseFontSize: 14,
                themeColors: themeColors,
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
                Foreground = new SolidColorBrush(_colors.Text),
            };
            foreach (var inline in processed.Body)
                bodyText.Inlines.Add(inline);

            var stack = new StackPanel();
            stack.Children.Add(new TextBlock
            {
                Text = $"{bookName} {verse.Chapter}:{verse.VerseNumber}",
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(bookColor),
            });
            stack.Children.Add(bodyText);

            var card = new Border
            {
                Background = new SolidColorBrush(_colors.Card),
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

        // ═══════════════════════════════════════════════════════════
        //  Empty state
        // ═══════════════════════════════════════════════════════════
        private void UpdateEmptyState()
        {
            if (_results.Count > 0)
            {
                EmptyState.Visibility = Visibility.Collapsed;
                return;
            }

            EmptyState.Visibility = Visibility.Visible;
            EmptyState.Children.Clear();

            if (string.IsNullOrWhiteSpace(_query))
                BuildPopularTermsState();
            else if (_hasSearched)
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
                Foreground = new SolidColorBrush(_colors.Muted),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 24),
            });
            EmptyState.Children.Add(BuildPopularTermsCard());
        }

        private void BuildPopularTermsState()
        {
            EmptyState.Children.Add(new TextBlock
            {
                Text = "Enter a word or phrase\nto find relevant verses",
                FontSize = 16,
                Foreground = new SolidColorBrush(_colors.Muted),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 24),
            });
            EmptyState.Children.Add(BuildPopularTermsCard());
        }

        private FrameworkElement BuildPopularTermsCard()
        {
            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = "Popular Search Terms",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(_colors.Primary),
                Margin = new Thickness(0, 0, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center,
            });

            var chips = new WrapPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center,
            };

            foreach (var term in SearchHelpers.PopularSearchTerms)
            {
                var chip = new Border
                {
                    Background = new SolidColorBrush(_colors.Card),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(
                        0x33, _colors.Primary.R, _colors.Primary.G, _colors.Primary.B)),
                    BorderThickness = new Thickness(1),
                    CornerRadius = new CornerRadius(14),
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(3),
                    Cursor = Cursors.Hand,
                    Tag = term,
                };
                chip.Child = new TextBlock
                {
                    Text = term,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(_colors.Primary),
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

            // ═══ FIX 1: was broken — `{ Child = stack };` dangling after the object initializer ═══
            return new Border
            {
                Background = new SolidColorBrush(Color.FromArgb(
                    0x1A, _colors.Primary.R, _colors.Primary.G, _colors.Primary.B)),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 8),
                Child = stack,   // ← Child now set inside the initializer
            };
        }

        private void BuildNoResultsState()
        {
            var inverse = InverseCheck.IsChecked == true;
            var exact = ExactCheck.IsChecked == true;

            var noResultsText =
                inverse && exact ? $"No verses without the exact phrase \"{_query}\" found" :
                inverse ? $"No verses without \"{_query}\" found" :
                exact ? $"No verses with the exact phrase \"{_query}\" found" :
                                   $"No results found for \"{_query}\"";

            EmptyState.Children.Add(new TextBlock
            {
                Text = noResultsText,
                FontSize = 16,
                Foreground = new SolidColorBrush(_colors.Text),
                TextAlignment = TextAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 8),
            });

            EmptyState.Children.Add(new TextBlock
            {
                Text = "Try different keywords or check spelling",
                FontSize = 13,
                Foreground = new SolidColorBrush(_colors.Muted),
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
                Foreground = new SolidColorBrush(_colors.Text),
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
                    Foreground = new SolidColorBrush(_colors.Muted),
                    Margin = new Thickness(0, 2, 0, 2),
                });
            }

            EmptyState.Children.Add(new Border
            {
                Background = new SolidColorBrush(_colors.Card),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(16),
                Child = tips,
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  Event handlers
        // ═══════════════════════════════════════════════════════════
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
            dialog.ShowDialog();
        }

        private void VersionChip_Click(object sender, RoutedEventArgs e)
        {
            var modal = new VersionSelectionModal(
                viewModel: _viewModel,
                currentVersionKey: _viewModel.CurrentDbName,
                isSecondary: false,
                onVersionSelected: file => { _onVersionChange(file); },
                onDismiss: () => App.Log("SearchScreen: version modal dismissed"))
            {
                Owner = Window.GetWindow(this),
            };
            modal.ShowDialog();
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
            _prefs.Set("search_query", _query);
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
            BackToTopBtn.Visibility = ResultsScroller.VerticalOffset > 300
                ? Visibility.Visible
                : Visibility.Collapsed;
        }
    }
}