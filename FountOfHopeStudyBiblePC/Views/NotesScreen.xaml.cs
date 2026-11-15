using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Views
{
    public partial class NotesScreen : UserControl
    {
        private static readonly SolidColorBrush SelectedBgBrush = Frozen(Color.FromArgb(0x55, 0x7F, 0xB2, 0xFF));
        private static readonly SolidColorBrush CardDarkBrush = Frozen(Color.FromRgb(0x23, 0x2A, 0x33));
        private static readonly SolidColorBrush CardLightBrush = Frozen(Color.FromRgb(0xEA, 0xE7, 0xE3));
        private static readonly SolidColorBrush TitleBrush = Frozen(Color.FromRgb(0x7F, 0xB2, 0xFF));
        private static readonly SolidColorBrush BodyDarkBrush = Frozen(Colors.White);
        private static readonly SolidColorBrush BodyLightBrush = Frozen(Color.FromRgb(0x1A, 0x1A, 0x1A));
        private static readonly SolidColorBrush BookChipBgBrush = Frozen(Color.FromArgb(0x33, 0x7F, 0xB2, 0xFF));
        private static readonly SolidColorBrush ChapterChipBgBrush = Frozen(Color.FromArgb(0x33, 0xB1, 0x7D, 0xF5));
        private static readonly SolidColorBrush ChipFgBrush = Frozen(Colors.White);
        private static readonly SolidColorBrush DateBrush = Frozen(Color.FromArgb(0x88, 0xAA, 0xB4, 0xC0));
        private static readonly SolidColorBrush DeleteIconBrush = Frozen(Color.FromRgb(0xEB, 0x4E, 0x4E));

        private static SolidColorBrush Frozen(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        private readonly Action<PassageSelection> _onNavigateToReader;
        private readonly AppViewModel _viewModel;
        private readonly DispatcherTimer _undoTimer;
        private DatabaseHelper? _db;

        private List<Note> _notes = [];
        private readonly List<Note> _selectedNotes = [];
        private bool _multiSelectMode;
        private bool _searchActive;
        private string _searchQuery = "";
        private NoteSortOrder _sortOrder = NoteSortOrder.DateNewest;
        private string _selectedDbName;

        private List<Note>? _lastDeleted;

        public NotesScreen(
            Action<PassageSelection> onNavigateToReader,
            AppViewModel viewModel,
            string initialDbName)
        {
            InitializeComponent();

            _onNavigateToReader = onNavigateToReader;
            _viewModel = viewModel;
            _selectedDbName = initialDbName;

            _undoTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
            _undoTimer.Tick += (_, _) =>
            {
                _undoTimer.Stop();
                UndoBar.Visibility = Visibility.Collapsed;
                _lastDeleted = null;
            };

            _viewModel.PropertyChanged += OnViewModelPropertyChanged;

            Loaded += NotesScreen_Loaded;
            Unloaded += NotesScreen_Unloaded;
        }

        private void NotesScreen_Loaded(object sender, RoutedEventArgs e)
        {
            _db = new DatabaseHelper(_selectedDbName);
            RefreshNotes();
            UpdateVersionCard();
        }

        private void NotesScreen_Unloaded(object sender, RoutedEventArgs e)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _undoTimer.Stop();
            _db?.Dispose();
        }

        private void OnViewModelPropertyChanged(object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppViewModel.DarkTheme))
                RebuildNotesList();

            if (e.PropertyName == nameof(AppViewModel.CurrentDbName)
                && _viewModel.CurrentDbName != _selectedDbName)
            {
                _selectedDbName = _viewModel.CurrentDbName;
                _db?.Dispose();
                _db = new DatabaseHelper(_selectedDbName);
                RefreshNotes();
                UpdateVersionCard();
            }
        }

        private void RefreshNotes()
        {
            if (_db == null) return;
            var raw = _db.GetAllNotes();
            _notes = SortNotes(raw, _sortOrder);
            RebuildNotesList();
            UpdateTopBar();
        }

        private static List<Note> SortNotes(List<Note> notes, NoteSortOrder order)
        {
            return order switch
            {
                NoteSortOrder.DateNewest => [.. notes.OrderByDescending(n => n.Timestamp)],
                NoteSortOrder.DateOldest => [.. notes.OrderBy(n => n.Timestamp)],
                NoteSortOrder.Book => notes
                    .OrderBy(n => BibleData.GetBookByName(n.BookName)?.CustomNumber ?? 0)
                    .ThenBy(n => n.Chapter)
                    .ThenBy(n => n.StartVerse)
                    .ToList(),
                NoteSortOrder.Chapter => [.. notes
                    .OrderBy(n => n.Chapter)
                    .ThenBy(n => BibleData.GetBookByName(n.BookName)?.CustomNumber ?? 0)
                    .ThenBy(n => n.StartVerse)],
                _ => notes
            };
        }

        private List<Note> GetFilteredNotes()
        {
            if (string.IsNullOrWhiteSpace(_searchQuery)) return _notes;
            var q = _searchQuery;
            return [.. _notes.Where(n =>
                n.NoteText.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                n.BookName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                n.Chapter.ToString().Contains(q) ||
                n.StartVerse.ToString().Contains(q) ||
                n.EndVerse.ToString().Contains(q))];
        }

        private void UpdateTopBar()
        {
            TopBarTitle.Text = $"Notes ({_notes.Count})";
            SelectedCountText.Text = $"{_selectedNotes.Count} selected";

            NormalButtons.Visibility = _multiSelectMode ? Visibility.Collapsed : Visibility.Visible;
            MultiSelectButtons.Visibility = _multiSelectMode ? Visibility.Visible : Visibility.Collapsed;
            VersionCard.Visibility = _searchActive ? Visibility.Collapsed : Visibility.Visible;
            SelectionHintText.Visibility = _multiSelectMode ? Visibility.Visible : Visibility.Collapsed;

            if (_multiSelectMode)
                SelectionHintText.Text = "Tap a note to select. Tap again to deselect.";

            var hasSelection = _selectedNotes.Count > 0;
            ShareSelectedBtn.IsEnabled = hasSelection;
            DeleteSelectedBtn.IsEnabled = hasSelection;
        }

        private void UpdateVersionCard()
        {
            var abbr = BibleVersionUtils.VersionMap.GetValueOrDefault(_selectedDbName, "Bible");
            var desc = BibleVersionUtils.GetDescriptionForVersion(_selectedDbName);
            VersionDescriptionText.Text = $"{abbr} — {desc}";
        }

        private void RebuildNotesList()
        {
            NotesListPanel.Children.Clear();

            var filtered = GetFilteredNotes();

            if (filtered.Count == 0)
            {
                NotesScrollViewer.Visibility = Visibility.Collapsed;
                EmptyStatePanel.Visibility = Visibility.Visible;
                EmptyStateTitle.Text = _searchQuery.Length > 0 ? "No matching notes" : "No notes yet";
                EmptyStateSubtitle.Text = _searchQuery.Length > 0
                    ? "Try a different search term"
                    : "Add notes to verses to see them here";
                return;
            }

            NotesScrollViewer.Visibility = Visibility.Visible;
            EmptyStatePanel.Visibility = Visibility.Collapsed;

            var selectedSet = _selectedNotes.ToHashSet();
            foreach (var note in filtered)
                NotesListPanel.Children.Add(BuildNoteCard(note, selectedSet.Contains(note)));
        }

        private Border BuildNoteCard(Note note, bool isSelected)
        {
            var isDark = _viewModel.DarkTheme;

            var rangeStr = note.StartVerse == note.EndVerse
                ? $"{note.StartVerse}"
                : $"{note.StartVerse}-{note.EndVerse}";

            var border = new Border
            {
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 10),
                Background = isSelected ? SelectedBgBrush : (isDark ? CardDarkBrush : CardLightBrush),
                Tag = note,
                Cursor = Cursors.Hand,
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var headerGrid = new Grid();
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            headerGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            if (_multiSelectMode)
            {
                var cb = new CheckBox
                {
                    IsChecked = isSelected,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0),
                };
                cb.Checked += (_, _) => ToggleNoteSelection(note);
                cb.Unchecked += (_, _) => ToggleNoteSelection(note);
                Grid.SetColumn(cb, 0);
                headerGrid.Children.Add(cb);
            }

            var title = new TextBlock
            {
                Text = $"{note.BookName} {note.Chapter}:{rangeStr}",
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                Foreground = TitleBrush,
                TextTrimming = TextTrimming.CharacterEllipsis,
                VerticalAlignment = VerticalAlignment.Center,
            };
            Grid.SetColumn(title, 1);
            headerGrid.Children.Add(title);

            if (!_multiSelectMode)
            {
                var actions = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    VerticalAlignment = VerticalAlignment.Center,
                };

                var editBtn = BuildIconButton("✏", TitleBrush, "Edit note");
                editBtn.Click += (_, _) => EditNote(note);
                actions.Children.Add(editBtn);

                var delBtn = BuildIconButton("🗑", DeleteIconBrush, "Delete note");
                delBtn.Click += (_, _) => DeleteSingleNote(note);
                actions.Children.Add(delBtn);

                Grid.SetColumn(actions, 2);
                headerGrid.Children.Add(actions);
            }

            Grid.SetRow(headerGrid, 0);
            grid.Children.Add(headerGrid);

            var body = new TextBlock
            {
                Text = note.NoteText,
                FontSize = 13,
                LineHeight = 20,
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 72,
                TextTrimming = TextTrimming.CharacterEllipsis,
                Margin = new Thickness(0, 8, 0, 0),
                Foreground = isDark ? BodyDarkBrush : BodyLightBrush,
            };
            Grid.SetRow(body, 1);
            grid.Children.Add(body);

            var footer = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(0, 10, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };

            footer.Children.Add(BuildChip(
                note.BookName.Length >= 3 ? note.BookName[..3].ToUpperInvariant() : note.BookName.ToUpperInvariant(),
                BookChipBgBrush));
            footer.Children.Add(BuildChip($"Ch {note.Chapter}", ChapterChipBgBrush));

            var dt = DateTimeOffset.FromUnixTimeSeconds(note.Timestamp).LocalDateTime;
            footer.Children.Add(new TextBlock
            {
                Text = $"{dt:MMM dd, yyyy} at {dt:hh:mm tt}",
                FontSize = 11,
                Margin = new Thickness(8, 0, 0, 0),
                Foreground = DateBrush,
                VerticalAlignment = VerticalAlignment.Center,
            });

            Grid.SetRow(footer, 2);
            grid.Children.Add(footer);

            border.Child = grid;

            border.MouseLeftButtonUp += (_, e) =>
            {
                if (e.OriginalSource is Button) return;
                if (_multiSelectMode) ToggleNoteSelection(note);
                else NavigateToNote(note);
            };
            border.MouseRightButtonUp += (_, e) =>
            {
                e.Handled = true;
                if (_multiSelectMode) return;
                _multiSelectMode = true;
                _selectedNotes.Add(note);
                UpdateTopBar();
                RebuildNotesList();
            };

            return border;
        }

        private static Button BuildIconButton(string glyph, SolidColorBrush fg, string tooltip) => new()
        {
            Width = 30,
            Height = 30,
            Background = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Cursor = Cursors.Hand,
            ToolTip = tooltip,
            Content = new TextBlock
            {
                Text = glyph,
                FontSize = 14,
                Foreground = fg,
            }
        };

        private static Border BuildChip(string text, SolidColorBrush bg) => new()
        {
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(8, 3, 8, 3),
            Margin = new Thickness(0, 0, 6, 0),
            Background = bg,
            Child = new TextBlock
            {
                Text = text,
                FontSize = 10,
                FontWeight = FontWeights.SemiBold,
                Foreground = ChipFgBrush,
            },
        };

        private void ToggleNoteSelection(Note note)
        {
            if (_selectedNotes.Contains(note)) _selectedNotes.Remove(note);
            else _selectedNotes.Add(note);

            if (_selectedNotes.Count == 0) _multiSelectMode = false;

            UpdateTopBar();
            RebuildNotesList();
        }

        private void NavigateToNote(Note note)
        {
            var book = BibleData.GetBookByName(note.BookName);
            if (book == null) return;

            _onNavigateToReader(new PassageSelection(
                book.CustomNumber, note.BookName, note.Chapter, note.StartVerse));
        }

        private void EditNote(Note note)
        {
            if (_db == null) return;

            var book = BibleData.GetBookByName(note.BookName);
            if (book == null) return;

            var verses = _db.GetVerses(book.CustomNumber, note.Chapter)
                .Where(v => v.VerseNumber >= note.StartVerse && v.VerseNumber <= note.EndVerse)
                .ToList();

            new Modals.NotesModal(
                viewModel: _viewModel,
                verses: verses,
                passage: new PassageSelection(book.CustomNumber, note.BookName,
                                              note.Chapter, note.StartVerse),
                databaseHelper: _db,
                onSave: RefreshNotes)
            {
                Owner = Window.GetWindow(this)
            }.Show();
        }

        private void DeleteSingleNote(Note note)
        {
            if (_db == null) return;

            var result = MessageBox.Show(
                $"Delete note on {note.BookName} {note.Chapter}:{note.StartVerse}?",
                "Delete Note", MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            _db.DeleteNote(note.BookName, note.Chapter, note.StartVerse, note.EndVerse);
            RefreshNotes();
        }

        private void DeleteSelectedNotes()
        {
            if (_db == null || _selectedNotes.Count == 0) return;

            var count = _selectedNotes.Count;
            var result = MessageBox.Show(
                count == 1 ? "Delete this note?" : $"Delete these {count} notes?",
                "Delete Notes", MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            foreach (var n in _selectedNotes)
                _db.DeleteNote(n.BookName, n.Chapter, n.StartVerse, n.EndVerse);

            _lastDeleted = [.. _selectedNotes];
            ShowUndoBar(count);
            _selectedNotes.Clear();
            _multiSelectMode = false;

            RefreshNotes();
        }

        private void ShareSelectedNotes()
        {
            if (_selectedNotes.Count == 0) return;

            var sb = new System.Text.StringBuilder();
            foreach (var n in _selectedNotes.OrderBy(n => n.BookName).ThenBy(n => n.Chapter))
            {
                var range = n.StartVerse == n.EndVerse
                    ? $"{n.StartVerse}"
                    : $"{n.StartVerse}-{n.EndVerse}";
                sb.AppendLine($"{n.BookName} {n.Chapter}:{range}");
                sb.AppendLine(n.NoteText);
                sb.AppendLine();
            }

            try
            {
                Clipboard.SetText(sb.ToString().TrimEnd());
                MessageBox.Show($"{_selectedNotes.Count} note(s) copied to clipboard.",
                    "Share", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch { }

            _selectedNotes.Clear();
            _multiSelectMode = false;
            UpdateTopBar();
            RebuildNotesList();
        }

        private void ShowUndoBar(int count)
        {
            UndoText.Text = $"{count} note{(count == 1 ? "" : "s")} removed";
            UndoBar.Visibility = Visibility.Visible;
            _undoTimer.Stop();
            _undoTimer.Start();
        }

        private void UndoButton_Click(object sender, RoutedEventArgs e)
        {
            if (_db == null || _lastDeleted == null) return;

            foreach (var n in _lastDeleted)
                _db.AddOrUpdateNote(n.BookName, n.Chapter, n.StartVerse, n.EndVerse, n.NoteText);

            _lastDeleted = null;
            _undoTimer.Stop();
            UndoBar.Visibility = Visibility.Collapsed;
            RefreshNotes();
        }

        private void SearchToggleBtn_Click(object sender, RoutedEventArgs e)
        {
            _searchActive = !_searchActive;
            SearchBarPanel.Visibility = _searchActive ? Visibility.Visible : Visibility.Collapsed;
            if (_searchActive) SearchBox.Focus();
            else
            {
                _searchQuery = "";
                SearchBox.Text = "";
                ClearSearchBtn.Visibility = Visibility.Collapsed;
                RefreshNotes();
            }
            UpdateTopBar();
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = SearchBox.Text ?? "";
            ClearSearchBtn.Visibility = _searchQuery.Length > 0 ? Visibility.Visible : Visibility.Collapsed;
            RebuildNotesList();
        }

        private void ClearSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            _searchQuery = "";
            ClearSearchBtn.Visibility = Visibility.Collapsed;
            RebuildNotesList();
        }

        private void SortBtn_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Modals.NoteSortDialog(_sortOrder) { Owner = Window.GetWindow(this) };
            if (dlg.ShowDialog() == true && dlg.SelectedOrder != _sortOrder)
            {
                _sortOrder = dlg.SelectedOrder;
                RefreshNotes();
            }
        }

        private void MoreBtn_Click(object sender, RoutedEventArgs e)
        {
            _multiSelectMode = true;
            _selectedNotes.Clear();
            UpdateTopBar();
            RebuildNotesList();
        }

        private void CancelMultiSelectBtn_Click(object sender, RoutedEventArgs e)
        {
            _multiSelectMode = false;
            _selectedNotes.Clear();
            UpdateTopBar();
            RebuildNotesList();
        }

        private void ShareSelectedBtn_Click(object sender, RoutedEventArgs e) => ShareSelectedNotes();

        private void DeleteSelectedBtn_Click(object sender, RoutedEventArgs e) => DeleteSelectedNotes();

        private void VersionInfoBtn_Click(object sender, RoutedEventArgs e)
        {
            _viewModel.ShowVersionInfoDialog = true;
            _viewModel.VersionInfoForDialog = _viewModel.CurrentDbName;
        }
    }
}