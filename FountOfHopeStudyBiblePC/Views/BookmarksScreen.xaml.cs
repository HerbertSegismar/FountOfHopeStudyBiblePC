// Views/BookmarksScreen.xaml.cs
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;
using FountOfHopeStudyBiblePC.Views.Modals;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;

namespace FountOfHopeStudyBiblePC.Views
{
    public partial class BookmarksScreen : UserControl
    {
        // ── Dependencies ──────────────────────────────────────────
        private readonly Action<PassageSelection> _onNavigateToReader;
        private readonly AppViewModel _viewModel;

        // ── Local DB (per selected version) ────────────────────────
        private string _selectedDbName;
        private DatabaseHelper? _db;

        // ── State ─────────────────────────────────────────────────
        private List<Verse> _bookmarks = new();
        private readonly List<Verse> _selectedVerses = new();
        private bool _multiSelectMode;
        private bool _searchActive;
        private string _searchQuery = "";
        private BookmarkSortOrder _sortOrder = BookmarkSortOrder.RecentlyAdded;

        // ── Undo ──────────────────────────────────────────────────
        private List<Verse>? _lastDeleted;
        private DispatcherTimer? _undoTimer;

        // ═══════════════════════════════════════════════════════════
        //  Constructor
        // ═══════════════════════════════════════════════════════════
        public BookmarksScreen(Action<PassageSelection> onNavigateToReader)
        {
            InitializeComponent();

            _onNavigateToReader = onNavigateToReader ?? (_ => { });
            _viewModel = ViewModels.AppViewModelAccessor.Current
                         ?? new AppViewModel();

            _selectedDbName = _viewModel.CurrentDbName;

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;
        }

        // ═══════════════════════════════════════════════════════════
        //  Lifecycle
        // ═══════════════════════════════════════════════════════════
        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            App.Log("BookmarksScreen: Loaded");
            RebuildDb();
            RefreshBookmarks();
            UpdateTopBar();
            UpdateVersionCard();
            UpdateEmptyState();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _db?.Dispose();
        }

        private void OnViewModelPropertyChanged(object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppViewModel.CurrentDbName)
                && _viewModel.CurrentDbName != _selectedDbName)
            {
                _selectedDbName = _viewModel.CurrentDbName;
                RebuildDb();
                RefreshBookmarks();
                UpdateVersionCard();
            }

            if (e.PropertyName == nameof(AppViewModel.DarkTheme))
            {
                UpdateVersionCard();
                RebuildBookmarksList();
            }

            if (e.PropertyName == nameof(AppViewModel.SelectedColor))
            {
                // Just relabel the top bar; underlying list uses cached colors
                UpdateTopBar();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  DB / data
        // ═══════════════════════════════════════════════════════════
        private void RebuildDb()
        {
            _db?.Dispose();
            _db = new DatabaseHelper(_selectedDbName);
            App.Log($"BookmarksScreen: DB rebuilt for {_selectedDbName}");
        }

        private void RefreshBookmarks()
        {
            if (_db == null) return;
            var raw = _db.GetBookmarks();
            _bookmarks = SortVerses(raw, _sortOrder);
            RebuildBookmarksList();
        }

        private static List<Verse> SortVerses(List<Verse> verses, BookmarkSortOrder order)
        {
            return order switch
            {
                BookmarkSortOrder.ByBook => verses
                    .OrderBy(v => BibleData.GetBookByName(v.BookName ?? "")?.CustomNumber ?? 0)
                    .ThenBy(v => v.Chapter ?? 0)
                    .ThenBy(v => v.VerseNumber)
                    .ToList(),
                BookmarkSortOrder.ByChapter => verses
                    .OrderBy(v => v.Chapter ?? 0)
                    .ThenBy(v => BibleData.GetBookByName(v.BookName ?? "")?.CustomNumber ?? 0)
                    .ThenBy(v => v.VerseNumber)
                    .ToList(),
                BookmarkSortOrder.RecentlyAdded => verses,
                _ => verses
            };
        }

        private List<Verse> GetFilteredBookmarks()
        {
            if (string.IsNullOrWhiteSpace(_searchQuery)) return _bookmarks;
            var q = _searchQuery;
            return _bookmarks.Where(v =>
                (v.Text ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                (v.BookName ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                v.VerseNumber.ToString().Contains(q) ||
                (v.Chapter?.ToString() ?? "").Contains(q)
            ).ToList();
        }

        // ═══════════════════════════════════════════════════════════
        //  Top bar state
        // ═══════════════════════════════════════════════════════════
        private void UpdateTopBar()
        {
            NormalTitleText.Text = $"Bookmarks ({_bookmarks.Count})";
            SelectedCountText.Text = $"{_selectedVerses.Count} selected";

            NormalButtons.Visibility = _multiSelectMode ? Visibility.Collapsed : Visibility.Visible;
            MultiSelectButtons.Visibility = _multiSelectMode ? Visibility.Visible : Visibility.Collapsed;

            if (_multiSelectMode)
                NormalTitleText.Visibility = Visibility.Collapsed;
            else
                NormalTitleText.Visibility = Visibility.Visible;

            VersionCard.Visibility = _searchActive ? Visibility.Collapsed : Visibility.Visible;

            // Sort badge indicator — small dot on SortBtn when non-default sort is active
            SortBtn.Opacity = _sortOrder == BookmarkSortOrder.RecentlyAdded ? 1.0 : 0.8;
        }

        private void UpdateVersionCard()
        {
            var abbr = BibleVersionUtils.VersionMap.GetValueOrDefault(_selectedDbName, "Bible");
            var desc = BibleVersionUtils.GetDescriptionForVersion(_selectedDbName);
            VersionDescriptionText.Text = $"{abbr} — {desc}";
        }

        // ═══════════════════════════════════════════════════════════
        //  Bookmarks list rendering
        // ═══════════════════════════════════════════════════════════
        private void RebuildBookmarksList()
        {
            BookmarksPanel.Children.Clear();

            var filtered = GetFilteredBookmarks();

            if (filtered.Count == 0)
            {
                BookmarksScroller.Visibility = Visibility.Collapsed;
                EmptyState.Visibility = Visibility.Visible;
                EmptyStateTitle.Text = _searchQuery.Length > 0
                    ? "No matching bookmarks"
                    : "No bookmarks yet";
                EmptyStateSubtitle.Text = _searchQuery.Length > 0
                    ? "Try a different search term"
                    : "Bookmark verses to see them here";
                return;
            }

            BookmarksScroller.Visibility = Visibility.Visible;
            EmptyState.Visibility = Visibility.Collapsed;

            foreach (var verse in filtered)
                BookmarksPanel.Children.Add(BuildBookmarkCard(verse));
        }

        private void UpdateEmptyState() => RebuildBookmarksList();

        private FrameworkElement BuildBookmarkCard(Verse verse)
        {
            var isSelected = _selectedVerses.Contains(verse);
            var primary = _viewModel.SelectedColor
                          ?? (Color)ColorConverter.ConvertFromString("#FF7FB2FF");

            var cardBg = isSelected
                ? Color.FromArgb(0x4D, primary.R, primary.G, primary.B) // 30% primary
                : (_viewModel.DarkTheme
                    ? Color.FromRgb(0x23, 0x2A, 0x33)
                    : Color.FromRgb(0xEA, 0xE7, 0xE3));

            var textColor = _viewModel.DarkTheme
                ? Colors.White
                : Color.FromRgb(0x1A, 0x1A, 0x1A);

            // Card container
            var card = new Border
            {
                Background = new SolidColorBrush(cardBg),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 10),
                Tag = verse,
                Cursor = Cursors.Hand,
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Header row: [checkbox] title ......... [delete icon]
            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            if (_multiSelectMode)
            {
                var cb = new CheckBox
                {
                    IsChecked = isSelected,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0),
                };
                cb.Checked += (_, _) => ToggleSelection(verse);
                cb.Unchecked += (_, _) => ToggleSelection(verse);
                Grid.SetColumn(cb, 0);
                header.Children.Add(cb);
            }

            var title = new TextBlock
            {
                Text = $"{verse.BookName} {verse.Chapter}:{verse.VerseNumber}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(primary),
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(title, 1);
            header.Children.Add(title);

            if (!_multiSelectMode)
            {
                var delBtn = new Button
                {
                    Width = 30,
                    Height = 30,
                    Background = Brushes.Transparent,
                    BorderThickness = new Thickness(0),
                    Cursor = Cursors.Hand,
                    ToolTip = "Remove bookmark",
                };
                delBtn.Content = new TextBlock
                {
                    Text = "🗑",
                    FontSize = 14,
                    Foreground = new SolidColorBrush(
                        (Color)ColorConverter.ConvertFromString("#FFEB4E4E")),
                };
                delBtn.Click += (_, _) => DeleteSingleBookmark(verse);
                Grid.SetColumn(delBtn, 2);
                header.Children.Add(delBtn);
            }

            Grid.SetRow(header, 0);
            grid.Children.Add(header);

            // Body: bold verse number + stripped verse text
            var body = new TextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                FontSize = 13,
                LineHeight = 20,
                MaxHeight = 72,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 8, 0, 0),
                Foreground = new SolidColorBrush(textColor),
            };
            body.Inlines.Add(new Run($"{verse.VerseNumber} ")
            {
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(primary),
                FontSize = 14,
            });
            body.Inlines.Add(new Run(
                SimpleVerseProcessor.StripXmlTags(verse.Text ?? "")));
            Grid.SetRow(body, 1);
            grid.Children.Add(body);

            // Footer: book chip + chapter chip
            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0),
            };
            var bookChip = (verse.BookName?.Length ?? 0) >= 3
                ? verse.BookName!.Substring(0, 3).ToUpperInvariant()
                : (verse.BookName ?? "GEN").ToUpperInvariant();
            footer.Children.Add(BuildChip(bookChip, primary, 0x1A));
            footer.Children.Add(BuildChip($"Ch {verse.Chapter}",
                Color.FromRgb(0xB1, 0x7D, 0xF5), 0x1A));

            Grid.SetRow(footer, 2);
            grid.Children.Add(footer);

            card.Child = grid;

            // Click / long-press handling
            card.MouseLeftButtonUp += (_, e) =>
            {
                // Ignore if a child button was clicked
                if (e.OriginalSource is Button) return;

                if (_multiSelectMode)
                    ToggleSelection(verse);
                else
                    NavigateToBookmark(verse);
            };
            card.MouseRightButtonUp += (_, e) =>
            {
                e.Handled = true;
                if (!_multiSelectMode)
                {
                    _multiSelectMode = true;
                    _selectedVerses.Clear();
                    _selectedVerses.Add(verse);
                    UpdateTopBar();
                    RebuildBookmarksList();
                }
            };

            return card;
        }

        private FrameworkElement BuildChip(string text, Color baseColor, byte alphaPercent)
        {
            return new Border
            {
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8, 3, 8, 3),
                Margin = new Thickness(0, 0, 6, 0),
                Background = new SolidColorBrush(Color.FromArgb(alphaPercent,
                    baseColor.R, baseColor.G, baseColor.B)),
                Child = new TextBlock
                {
                    Text = text,
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(baseColor),
                },
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  Selection
        // ═══════════════════════════════════════════════════════════
        private void ToggleSelection(Verse verse)
        {
            if (_selectedVerses.Contains(verse))
                _selectedVerses.Remove(verse);
            else
                _selectedVerses.Add(verse);

            if (_selectedVerses.Count == 0)
                _multiSelectMode = false;

            UpdateTopBar();
            RebuildBookmarksList();
        }

        // ═══════════════════════════════════════════════════════════
        //  Actions
        // ═══════════════════════════════════════════════════════════
        private void NavigateToBookmark(Verse verse)
        {
            var book = BibleData.GetBookByName(verse.BookName ?? "");
            if (book == null)
            {
                App.Log($"BookmarksScreen: unknown book '{verse.BookName}'");
                return;
            }

            // Sync the view model to the version we're reading from
            _viewModel.CurrentDbName = _selectedDbName;

            _onNavigateToReader(new PassageSelection(
                book.CustomNumber,
                book.Name,
                verse.Chapter ?? 1,
                verse.VerseNumber));
        }

        private void DeleteSingleBookmark(Verse verse)
        {
            if (_db == null) return;

            var result = MessageBox.Show(
                $"Remove bookmark on {verse.BookName} {verse.Chapter}:{verse.VerseNumber}?",
                "Remove Bookmark", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            _db.RemoveBookmark(verse);
            _lastDeleted = new List<Verse> { verse };
            ShowUndoBar(1);
            RefreshBookmarks();
            UpdateTopBar();
        }

        private void DeleteSelectedBookmarks()
        {
            if (_db == null || _selectedVerses.Count == 0) return;

            var count = _selectedVerses.Count;
            var result = MessageBox.Show(
                count == 1
                    ? "Delete this bookmark?"
                    : $"Delete these {count} bookmarks?",
                "Delete Bookmarks", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            var toDelete = _selectedVerses.ToList();
            foreach (var v in toDelete)
                _db.RemoveBookmark(v);

            _lastDeleted = toDelete;
            ShowUndoBar(count);
            _selectedVerses.Clear();
            _multiSelectMode = false;

            RefreshBookmarks();
            UpdateTopBar();
        }

        private void ShareSelectedBookmarks()
        {
            if (_selectedVerses.Count == 0) return;

            var sb = new System.Text.StringBuilder();
            foreach (var v in _selectedVerses)
            {
                sb.AppendLine($"{v.BookName} {v.Chapter}:{v.VerseNumber}");
                sb.AppendLine(SimpleVerseProcessor.StripXmlTags(v.Text ?? ""));
                sb.AppendLine();
            }

            try
            {
                Clipboard.SetText(sb.ToString().TrimEnd());
                MessageBox.Show($"{_selectedVerses.Count} bookmark(s) copied to clipboard.",
                    "Share", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { }

            _selectedVerses.Clear();
            _multiSelectMode = false;
            UpdateTopBar();
            RebuildBookmarksList();
        }

        // ═══════════════════════════════════════════════════════════
        //  Undo
        // ═══════════════════════════════════════════════════════════
        private void ShowUndoBar(int count)
        {
            UndoText.Text = $"{count} bookmark{(count == 1 ? "" : "s")} removed";
            UndoBar.Visibility = Visibility.Visible;

            _undoTimer?.Stop();
            _undoTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(6)
            };
            _undoTimer.Tick += (_, _) =>
            {
                _undoTimer!.Stop();
                UndoBar.Visibility = Visibility.Collapsed;
                _lastDeleted = null;
            };
            _undoTimer.Start();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null || _lastDeleted == null) return;

            foreach (var v in _lastDeleted)
                _db.AddBookmark(v);

            _lastDeleted = null;
            _undoTimer?.Stop();
            UndoBar.Visibility = Visibility.Collapsed;
            RefreshBookmarks();
            UpdateTopBar();
        }

        // ═══════════════════════════════════════════════════════════
        //  Top-bar button handlers
        // ═══════════════════════════════════════════════════════════
        private void SearchToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            _searchActive = !_searchActive;
            SearchBarPanel.Visibility = _searchActive ? Visibility.Visible : Visibility.Collapsed;
            if (_searchActive)
            {
                SearchBox.Focus();
            }
            else
            {
                _searchQuery = "";
                SearchBox.Text = "";
                ClearSearchBtn.Visibility = Visibility.Collapsed;
                RefreshBookmarks();
            }
            UpdateTopBar();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = SearchBox.Text ?? "";
            ClearSearchBtn.Visibility = _searchQuery.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            RebuildBookmarksList();
        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                _searchActive = false;
                SearchBarPanel.Visibility = Visibility.Collapsed;
                UpdateTopBar();
            }
        }

        private void ClearSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            _searchQuery = "";
            ClearSearchBtn.Visibility = Visibility.Collapsed;
            RebuildBookmarksList();
        }

        private void SortBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new BookmarkSortDialog(_sortOrder)
            {
                Owner = Window.GetWindow(this)
            };
            if (dlg.ShowDialog() == true && dlg.SelectedOrder != _sortOrder)
            {
                _sortOrder = dlg.SelectedOrder;
                RefreshBookmarks();
                UpdateTopBar();
            }
        }

        private void MoreBtn_Click(object sender, RoutedEventArgs e)
        {
            _multiSelectMode = true;
            _selectedVerses.Clear();
            UpdateTopBar();
            RebuildBookmarksList();
        }

        private void CancelMultiSelectBtn_Click(object sender, RoutedEventArgs e)
        {
            _multiSelectMode = false;
            _selectedVerses.Clear();
            UpdateTopBar();
            RebuildBookmarksList();
        }

        private void ShareSelectedBtn_Click(object sender, RoutedEventArgs e)
            => ShareSelectedBookmarks();

        private void DeleteSelectedBtn_Click(object sender, RoutedEventArgs e)
            => DeleteSelectedBookmarks();

        private void VersionInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowVersionInfoDialog = true;
            _viewModel.VersionInfoForDialog = _viewModel.CurrentDbName;
        }

        private void VersionCard_Click(object sender, MouseButtonEventArgs e)
        {
            // Ignore click if it came from the info button
            if (e.OriginalSource is Button) return;

            var modal = new VersionSelectionModal(
                viewModel: _viewModel,
                currentVersionKey: _selectedDbName,
                isSecondary: false,
                onVersionSelected: file =>
                {
                    App.Log($"BookmarksScreen: version picked: {file}");
                    _selectedDbName = file;
                    _viewModel.CurrentDbName = file;
                    _viewModel.CurrentVersionAbbr =
                        BibleVersionUtils.VersionMap.GetValueOrDefault(file, "Bible");

                    RebuildDb();
                    RefreshBookmarks();
                    UpdateVersionCard();
                },
                onDismiss: () => App.Log("BookmarksScreen: version modal dismissed"))
            {
                Owner = Window.GetWindow(this),
            };
            modal.ShowDialog();
        }

        // ═══════════════════════════════════════════════════════════
        //  Back to top
        // ═══════════════════════════════════════════════════════════
        private void BookmarksScroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            BackToTopBtn.Visibility = BookmarksScroller.VerticalOffset > 300
                ? Visibility.Visible
                : Visibility.Collapsed;
        }

        private void BackToTopBtn_Click(object sender, RoutedEventArgs e)
        {
            BookmarksScroller.ScrollToTop();
        }
    }
}