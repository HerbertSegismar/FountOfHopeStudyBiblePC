using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class NavigationModal : Window
    {
        private static readonly SolidColorBrush FocusBorderBrush = Frozen(Color.FromRgb(0x7F, 0xB2, 0xFF));
        private static readonly SolidColorBrush ErrorBorderBrush = Frozen(Color.FromRgb(0xEB, 0x4E, 0x4E));
        private static readonly ControlTemplate RoundedButtonTemplate = BuildRoundedTemplate();

        private readonly AppViewModel _viewModel;
        private readonly DatabaseHelper? _db;
        private readonly Action<PassageSelection> _onPassageSelected;
        private readonly Action _onDismiss;
        private readonly ModalOverlayController _overlay;

        private BookUi? _selectedBook;
        private string _chapterInput = "";
        private string _verseInput = "";
        private string _focusedInput = "chapter";
        private int _maxVerse;

        public NavigationModal(
            AppViewModel viewModel,
            DatabaseHelper? databaseHelper,
            Action<PassageSelection> onPassageSelected,
            Action onDismiss,
            int? initialBookNumber = null,
            int initialChapter = 1,
            int? initialVerse = 1)
        {
            _viewModel = viewModel;
            _db = databaseHelper;
            _onPassageSelected = onPassageSelected ?? (_ => { });
            _onDismiss = onDismiss ?? (() => { });

            InitializeComponent();

            _overlay = new ModalOverlayController(this) { CloseOnOwnerResize = true };

            BuildBookGrids();
            ApplyInitialSelection(initialBookNumber, initialChapter, initialVerse);
            UpdateUI();
        }

        private static SolidColorBrush Frozen(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleCardMouseDown(sender, e);

        private void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleCardMouseUp(sender, e);

        private void Window_KeyDown(object sender, KeyEventArgs e)
            => _overlay.HandleKeyDown(sender, e);

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _onDismiss();
        }

        private void BuildBookGrids()
        {
            OldTestamentPanel.Children.Clear();
            NewTestamentPanel.Children.Clear();

            foreach (var b in BibleData.OldTestamentBooks)
                OldTestamentPanel.Children.Add(BuildBookCard(b));
            foreach (var b in BibleData.NewTestamentBooks)
                NewTestamentPanel.Children.Add(BuildBookCard(b));
        }

        private Button BuildBookCard(BibleBook book)
        {
            var bookUi = new BookUi(
                book.CustomNumber,
                book.Name,
                book.Abbreviation,
                book.Testament,
                book.Chapters);

            var btn = new Button
            {
                Width = 54,
                Height = 44,
                Margin = new Thickness(2),
                Background = new SolidColorBrush(GetBookColor(bookUi, _viewModel.DarkTheme)),
                BorderBrush = FocusBorderBrush,
                BorderThickness = new Thickness(0),
                Cursor = Cursors.Hand,
                Tag = bookUi,
                ToolTip = $"{book.Name} — {book.Chapters} chapters",
                Template = RoundedButtonTemplate,
                Content = new TextBlock
                {
                    Text = book.Abbreviation,
                    FontSize = 13,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x44, 0x4C, 0x69)),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            };
            btn.Click += (_, _) => SelectBook(bookUi);
            return btn;
        }

        private static ControlTemplate BuildRoundedTemplate()
        {
            var t = new ControlTemplate(typeof(Button));

            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "B";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(5));
            border.SetValue(Border.BackgroundProperty,
                new System.Windows.Data.Binding(nameof(Button.Background))
                { RelativeSource = System.Windows.Data.RelativeSource.TemplatedParent });

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            t.VisualTree = border;

            var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(UIElement.OpacityProperty, 0.8, "B"));
            t.Triggers.Add(trigger);
            t.Seal();
            return t;
        }

        private static string? GetScopeForBookNumber(int n)
        {
            if (n >= 10 && n <= 50) return SearchScopes.Law;
            if (n >= 60 && n <= 190) return SearchScopes.Historical;
            if (n >= 220 && n <= 260) return SearchScopes.Poetic;
            if (n >= 290 && n <= 340) return SearchScopes.MajorProphets;
            if (n >= 350 && n <= 460) return SearchScopes.MinorProphets;
            if (n >= 470 && n <= 500) return SearchScopes.Gospels;
            if (n == 510) return SearchScopes.HistoricalNt;
            if (n >= 520 && n <= 640) return SearchScopes.PaulineLetters;
            if (n >= 520 && n <= 720) return SearchScopes.Letters;
            if (n == 730) return SearchScopes.Vision;
            return null;
        }

        private static Color GetBookColor(BookUi book, bool isDarkTheme)
        {
            try
            {
                var scope = GetScopeForBookNumber(book.BookNumber);
                var baseColor = scope != null && SearchScopes.ScopeColors.TryGetValue(scope, out var hex)
                    ? (Color)ColorConverter.ConvertFromString(hex)
                    : book.Testament == Testament.Old
                        ? (Color)ColorConverter.ConvertFromString("#DC2626")
                        : (Color)ColorConverter.ConvertFromString("#059669");

                if (isDarkTheme) return baseColor;

                return Color.FromArgb(
                    (byte)(255 + (baseColor.A - 255) * 0.85),
                    (byte)(255 + (baseColor.R - 255) * 0.85),
                    (byte)(255 + (baseColor.G - 255) * 0.85),
                    (byte)(255 + (baseColor.B - 255) * 0.85));
            }
            catch
            {
                return Colors.Gray;
            }
        }

        private void SelectBook(BookUi book)
        {
            _selectedBook = book;
            _chapterInput = "";
            _verseInput = "";
            _maxVerse = 0;
            _focusedInput = "chapter";

            if (book.TotalChapters == 1)
            {
                _chapterInput = "1";
                _focusedInput = "verse";
            }

            UpdateUI();
        }

        private void ApplyInitialSelection(int? bookNumber, int chapter, int? verse)
        {
            if (bookNumber is null) return;

            var book = BibleData.GetBookByCustomNumber(bookNumber.Value);
            if (book is null) return;

            _selectedBook = new BookUi(
                book.CustomNumber,
                book.Name,
                book.Abbreviation,
                book.Testament,
                book.Chapters);

            _chapterInput = chapter > 0 ? chapter.ToString() : "";
            _verseInput = verse is > 0 ? verse.Value.ToString() : "";
            _focusedInput = "chapter";

            LoadMaxVerse();
        }

        private void LoadMaxVerse()
        {
            if (_selectedBook is null || _chapterInput == "") { _maxVerse = 0; return; }
            if (!int.TryParse(_chapterInput, out var chapter)) { _maxVerse = 0; return; }
            if (chapter < 1 || chapter > _selectedBook.TotalChapters) { _maxVerse = 0; return; }

            try { _maxVerse = _db?.GetVerseCount(_selectedBook.BookNumber, chapter) ?? 0; }
            catch { _maxVerse = 0; }
        }

        private void NumpadDigit_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedBook is null) return;
            if (sender is not Button btn || btn.Tag is not string digit) return;

            if (_focusedInput == "chapter")
            {
                var newValue = _chapterInput + digit;
                var num = int.TryParse(newValue, out var n) ? n : 0;
                var maxChapters = _selectedBook.TotalChapters;

                if (num >= 1 && num <= maxChapters && num.ToString() == newValue)
                {
                    _chapterInput = newValue;
                    LoadMaxVerse();
                    if (num * 10 > maxChapters)
                        _focusedInput = "verse";
                }
                else if (!string.IsNullOrEmpty(_chapterInput)
                         && int.TryParse(_chapterInput, out var currentNum)
                         && currentNum >= 1 && currentNum <= maxChapters)
                {
                    _focusedInput = "verse";
                    _verseInput = "";
                    LoadMaxVerse();
                    ApplyPendingVerseDigit(digit);
                }
            }
            else
            {
                var newValue = _verseInput + digit;
                var num = int.TryParse(newValue, out var n) ? n : 0;

                if (num >= 1 && num <= _maxVerse && num.ToString() == newValue)
                {
                    _verseInput = newValue;
                    if (num * 10 > _maxVerse)
                        _ = DelayedConfirm();
                }
                else if (!string.IsNullOrEmpty(_verseInput)
                         && int.TryParse(_verseInput, out var currentNum)
                         && currentNum >= 1 && currentNum <= _maxVerse)
                {
                    _ = DelayedConfirm();
                }
            }

            UpdateUI();
        }

        private void ApplyPendingVerseDigit(string digit)
        {
            if (_focusedInput != "verse" || _maxVerse <= 0) return;

            if (int.TryParse(digit, out var v) && v >= 1 && v <= _maxVerse)
            {
                _verseInput = digit;
                if (v * 10 > _maxVerse)
                    _ = DelayedConfirm();
            }
        }

        private async Task DelayedConfirm()
        {
            await Task.Delay(300);
            ConfirmSelection();
        }

        private void Backspace_Click(object sender, RoutedEventArgs e)
        {
            if (_focusedInput == "chapter" && _chapterInput.Length > 0)
            {
                _chapterInput = _chapterInput[..^1];
                LoadMaxVerse();
            }
            else if (_focusedInput == "verse" && _verseInput.Length > 0)
            {
                _verseInput = _verseInput[..^1];
            }

            UpdateUI();
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            _chapterInput = "";
            _verseInput = "";
            _maxVerse = 0;
            _focusedInput = "chapter";
            UpdateUI();
        }

        private void Confirm_Click(object sender, RoutedEventArgs e) => ConfirmSelection();

        private void ConfirmSelection()
        {
            if (!IsInputValid() || _selectedBook is null) return;

            var chapter = int.Parse(_chapterInput);
            var verse = string.IsNullOrEmpty(_verseInput) ? 1 : int.Parse(_verseInput);

            var passage = new PassageSelection(
                _selectedBook.BookNumber,
                _selectedBook.LongName,
                chapter,
                verse);

            _onPassageSelected(passage);
            Close();
        }

        private void ChapterInput_Click(object sender, MouseButtonEventArgs e)
        {
            _focusedInput = "chapter";
            UpdateUI();
        }

        private void VerseInput_Click(object sender, MouseButtonEventArgs e)
        {
            if (_chapterInput.Length > 0)
            {
                _focusedInput = "verse";
                UpdateUI();
            }
        }

        private bool IsChapterValid()
        {
            if (string.IsNullOrEmpty(_chapterInput) || _selectedBook is null) return false;
            return int.TryParse(_chapterInput, out var c)
                && c >= 1 && c <= _selectedBook.TotalChapters;
        }

        private bool IsVerseValid()
        {
            if (string.IsNullOrEmpty(_verseInput)) return true;
            return int.TryParse(_verseInput, out var v) && v >= 1 && v <= _maxVerse;
        }

        private bool IsInputValid() => IsChapterValid() && IsVerseValid();

        private void UpdateUI()
        {
            if (_selectedBook != null)
            {
                BookHeaderCard.Visibility = Visibility.Visible;
                BookHeaderTitle.Text = _selectedBook.LongName;
                BookHeaderSubtitle.Text = _selectedBook.TotalChapters == 1
                    ? "1 chapter"
                    : $"{_selectedBook.TotalChapters} chapters";
                BookHeaderShort.Text = _selectedBook.ShortName;
            }
            else
            {
                BookHeaderCard.Visibility = Visibility.Collapsed;
            }

            ChapterValue.Text = _chapterInput;
            ChapterHint.Text = _selectedBook != null ? $"1-{_selectedBook.TotalChapters}" : "";

            var chapterError = _chapterInput.Length > 0 && !IsChapterValid();
            ChapterInputBorder.BorderBrush = chapterError
                ? ErrorBorderBrush
                : _focusedInput == "chapter" && _selectedBook != null
                    ? FocusBorderBrush
                    : Brushes.Transparent;

            VerseValue.Text = _verseInput;
            VerseHint.Text = _focusedInput == "verse" && _maxVerse > 0 ? $"1-{_maxVerse}" : "";

            var verseError = _verseInput.Length > 0 && !IsVerseValid() && _maxVerse > 0;
            VerseInputBorder.BorderBrush = verseError
                ? ErrorBorderBrush
                : _focusedInput == "verse"
                    ? FocusBorderBrush
                    : Brushes.Transparent;

            var valid = IsInputValid();
            GoToButton.IsEnabled = valid;
            GoToButton.Opacity = valid ? 1.0 : 0.5;

            if (valid && _selectedBook != null)
            {
                var verseSuffix = string.IsNullOrEmpty(_verseInput) ? "" : $":{_verseInput}";
                GoToButton.Content = $"Go to {_selectedBook.LongName} {_chapterInput}{verseSuffix}";
            }
            else
            {
                GoToButton.Content = "Go to…";
            }

            HighlightSelectedBook();
        }

        private void HighlightSelectedBook()
        {
            foreach (var panel in new[] { OldTestamentPanel, NewTestamentPanel })
            {
                foreach (var child in panel.Children)
                {
                    if (child is not Button btn || btn.Tag is not BookUi b) continue;

                    var isSelected = _selectedBook != null && _selectedBook.BookNumber == b.BookNumber;
                    btn.BorderThickness = new Thickness(isSelected ? 3 : 0);
                }
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => _overlay.DismissWithFade();
    }
}