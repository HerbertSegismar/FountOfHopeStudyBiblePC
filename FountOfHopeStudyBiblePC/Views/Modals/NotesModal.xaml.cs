// Views/Modals/NotesModal.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class NotesModal : Window
    {
        // ── Dependencies ────────────────────────────────────────────
        private readonly AppViewModel _viewModel;
        private readonly List<Verse> _verses;
        private readonly PassageSelection? _passage;
        private readonly DatabaseHelper? _db;
        private readonly Action _onSave;

        // ── Derived values ──────────────────────────────────────────
        private readonly string _book;
        private readonly int _chapter;
        private readonly int _startVerse;
        private readonly int _endVerse;

        public NotesModal(
            AppViewModel viewModel,
            List<Verse> verses,
            PassageSelection? passage,
            DatabaseHelper? databaseHelper,
            Action? onSave = null)
        {
            // ── 1. Assign fields BEFORE InitializeComponent() ───────
            _viewModel = viewModel;
            _verses = verses ?? new List<Verse>();
            _passage = passage;
            _db = databaseHelper;
            _onSave = onSave ?? (() => { });

            _book = _verses.FirstOrDefault()?.BookName
                          ?? passage?.BookName
                          ?? "";
            _chapter = _verses.FirstOrDefault()?.Chapter
                          ?? passage?.Chapter
                          ?? 0;
            _startVerse = _verses.Count > 0
                          ? _verses.Min(v => v.VerseNumber)
                          : 0;
            _endVerse = _verses.Count > 0
                          ? _verses.Max(v => v.VerseNumber)
                          : 0;

            // ── 2. Parse XAML ───────────────────────────────────────
            InitializeComponent();

            // ── 3. Populate UI ──────────────────────────────────────
            ApplyTheme();
            PopulateHeader();
            PopulatePreview();
            LoadExistingNote();

            // Wire the TextBox to update Delete button visibility
            NoteTextBox.TextChanged += (_, _) => UpdateDeleteButtonVisibility();
            UpdateDeleteButtonVisibility();
        }

        // ═══════════════════════════════════════════════════════════
        //  Setup
        // ═══════════════════════════════════════════════════════════
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
            if (_db == null
                || string.IsNullOrWhiteSpace(_book)
                || _chapter <= 0
                || _startVerse <= 0)
            {
                NoteTextBox.Text = "";
                return;
            }

            var existing = _db.GetElement(_book, _chapter, _startVerse, _endVerse);
            NoteTextBox.Text = existing ?? "";
            // Move caret to end
            NoteTextBox.CaretIndex = NoteTextBox.Text.Length;
        }

        private void UpdateDeleteButtonVisibility()
        {
            DeleteButton.Visibility = string.IsNullOrWhiteSpace(NoteTextBox.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        // ═══════════════════════════════════════════════════════════
        //  Save / Delete / Cancel
        // ═══════════════════════════════════════════════════════════
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (_db != null
                && !string.IsNullOrWhiteSpace(_book)
                && _chapter > 0
                && _startVerse > 0)
            {
                var text = NoteTextBox.Text ?? "";

                if (string.IsNullOrWhiteSpace(text))
                {
                    // Empty → remove any existing note
                    _db.DeleteNote(_book, _chapter, _startVerse, _endVerse);
                    App.Log($"Notes: deleted note for {_book} {_chapter}:{_startVerse}-{_endVerse}");
                }
                else
                {
                    _db.AddOrUpdateNote(_book, _chapter, _startVerse, _endVerse, text);
                    App.Log($"Notes: saved note for {_book} {_chapter}:{_startVerse}-{_endVerse} " +
                            $"({text.Length} chars)");
                }

                _onSave();
            }
            Close();
        }

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            if (_db != null
                && !string.IsNullOrWhiteSpace(_book)
                && _chapter > 0
                && _startVerse > 0)
            {
                _db.DeleteNote(_book, _chapter, _startVerse, _endVerse);
                App.Log($"Notes: deleted note for {_book} {_chapter}:{_startVerse}-{_endVerse}");
                _onSave();
            }
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => Close();
    }
}