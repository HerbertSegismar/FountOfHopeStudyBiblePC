using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class NotesModal : Window
    {
        private static readonly Action NoOp = () => { };

        private readonly AppViewModel _viewModel;
        private readonly List<Verse> _verses;
        private readonly DatabaseHelper? _db;
        private readonly Action _onSave;
        private readonly ModalOverlayController _overlay;

        private readonly string _book;
        private readonly int _chapter;
        private readonly int _startVerse;
        private readonly int _endVerse;
        private readonly bool _canPersist;

        public NotesModal(
            AppViewModel viewModel,
            List<Verse> verses,
            PassageSelection? passage,
            DatabaseHelper? databaseHelper,
            Action? onSave = null)
        {
            _viewModel = viewModel;
            _verses = verses ?? [];
            _db = databaseHelper;
            _onSave = onSave ?? NoOp;

            var first = _verses.FirstOrDefault();
            _book = first?.BookName ?? passage?.BookName ?? "";
            _chapter = first?.Chapter ?? passage?.Chapter ?? 0;

            if (_verses.Count > 0)
            {
                _startVerse = _verses.Min(v => v.VerseNumber);
                _endVerse = _verses.Max(v => v.VerseNumber);
            }

            _canPersist = _db != null
                          && !string.IsNullOrWhiteSpace(_book)
                          && _chapter > 0
                          && _startVerse > 0;

            InitializeComponent();

            _overlay = new ModalOverlayController(this)
            {
                CloseOnOwnerResize = true,
                RevealOnLoad = true,
            };

            ApplyTheme();
            PopulateHeader();
            PopulatePreview();
            LoadExistingNote();

            NoteTextBox.TextChanged += (_, _) => UpdateDeleteButtonVisibility();
            UpdateDeleteButtonVisibility();
        }

        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Window_KeyDown(object sender, KeyEventArgs e)
            => _overlay.HandleKeyDown(sender, e);

        private void ApplyTheme()
        {
            var bg = _viewModel.DarkTheme
                ? _viewModel.DarkModalBackgroundColor
                : _viewModel.LightModalBackgroundColor;
            RootBorder.Background = new SolidColorBrush(bg);
        }

        private void PopulateHeader()
        {
            var rangeStr = _startVerse == _endVerse
                ? $"{_startVerse}"
                : $"{_startVerse}-{_endVerse}";
            PassageHeaderText.Text = $"{_book} {_chapter}:{rangeStr}";
        }

        private void PopulatePreview()
        {
            if (_verses.Count == 0)
            {
                PreviewText.Text = "(no verses selected)";
                return;
            }

            var sb = new StringBuilder();
            foreach (var v in _verses.OrderBy(v => v.VerseNumber))
            {
                var clean = SimpleVerseProcessor.StripXmlTags(v.Text);
                sb.AppendLine($"{v.VerseNumber} {clean}");
            }
            PreviewText.Text = sb.ToString().TrimEnd();
        }

        private void LoadExistingNote()
        {
            if (!_canPersist)
            {
                NoteTextBox.Text = "";
                return;
            }

            NoteTextBox.Text = _db!.GetElement(_book, _chapter, _startVerse, _endVerse) ?? "";
            NoteTextBox.CaretIndex = NoteTextBox.Text.Length;
        }

        private void UpdateDeleteButtonVisibility()
        {
            DeleteButton.Visibility = string.IsNullOrWhiteSpace(NoteTextBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_canPersist)
            {
                var text = NoteTextBox.Text ?? "";
                if (string.IsNullOrWhiteSpace(text))
                    _db!.DeleteNote(_book, _chapter, _startVerse, _endVerse);
                else
                    _db!.AddOrUpdateNote(_book, _chapter, _startVerse, _endVerse, text);

                _onSave();
            }

            Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_canPersist)
            {
                _db!.DeleteNote(_book, _chapter, _startVerse, _endVerse);
                _onSave();
            }

            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
            => _overlay.DismissWithFade();
    }
}