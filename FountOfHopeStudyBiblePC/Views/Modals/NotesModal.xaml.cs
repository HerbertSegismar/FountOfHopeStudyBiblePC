// Views/Modals/NotesModal.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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

        // ── Scrim dismiss state ─────────────────────────────────────
        private bool _isDismissing;
        private bool _dismissArmed;

        /// <summary>
        /// If true, the dialog dismisses when the owner window is resized.
        /// Default is true — the card is small and centred, keeping it on
        /// screen while the owner resizes feels odd.
        /// </summary>
        public bool CloseOnOwnerResize { get; set; } = true;

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

            Loaded += OnLoaded;
        }

        // ═══════════════════════════════════════════════════════════
        //  Scrim dismiss infrastructure
        // ═══════════════════════════════════════════════════════════
        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only arm dismissal when the press starts on the scrim.
            // TextBox, buttons, scrollviewer, etc. keep their own
            // mouse lifecycle untouched — otherwise typing/selection breaks.
            if (!ReferenceEquals(e.OriginalSource, Scrim))
            {
                _dismissArmed = false;
                return;
            }

            _dismissArmed = true;
            Scrim.CaptureMouse();
            e.Handled = true;
        }

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            bool shouldDismiss = _dismissArmed;
            _dismissArmed = false;

            if (Scrim.IsMouseCaptured)
                Scrim.ReleaseMouseCapture();

            if (shouldDismiss)
                DismissWithFade();

            e.Handled = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DismissWithFade();
                e.Handled = true;
            }
        }

        private void DismissWithFade()
        {
            if (_isDismissing) return;
            _isDismissing = true;

            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120));
            fade.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fade);
        }

        // ═══════════════════════════════════════════════════════════
        //  Owner bounds tracking
        // ═══════════════════════════════════════════════════════════
        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (Owner is not null)
            {
                Owner.LocationChanged += Owner_LocationChanged;
                Owner.SizeChanged += Owner_SizeChanged;
                Owner.StateChanged += Owner_StateChanged;
                Owner.Closed += Owner_Closed;
            }

            SyncBoundsToOwner();

            // Reveal after bounds are correct (avoids pre-layout flash).
            Opacity = 1;
        }

        private void SyncBoundsToOwner()
        {
            if (Owner is null) return;
            WindowStartupLocation = WindowStartupLocation.Manual;

            if (Owner.WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Maximized;
                return;
            }
            WindowState = WindowState.Normal;
            Left = Owner.Left;
            Top = Owner.Top;
            Width = Owner.ActualWidth;
            Height = Owner.ActualHeight;
        }

        private void Owner_LocationChanged(object? sender, EventArgs e)
        {
            if (Owner is null) return;
            Left = Owner.Left;
            Top = Owner.Top;
        }

        private void Owner_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (CloseOnOwnerResize) { DismissWithFade(); return; }
            if (Owner is null) return;
            Left = Owner.Left;
            Top = Owner.Top;
            Width = Owner.ActualWidth;
            Height = Owner.ActualHeight;
        }

        private void Owner_StateChanged(object? sender, EventArgs e)
        {
            if (Owner?.WindowState == WindowState.Minimized) DismissWithFade();
        }

        private void Owner_Closed(object? sender, EventArgs e) => DismissWithFade();

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

            // Confirm action → immediate close, no fade.
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

            // Confirm action → immediate close, no fade.
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) => DismissWithFade();

        // ═══════════════════════════════════════════════════════════
        //  Cleanup
        // ═══════════════════════════════════════════════════════════
        protected override void OnClosed(EventArgs e)
        {
            if (Owner is not null)
            {
                Owner.LocationChanged -= Owner_LocationChanged;
                Owner.SizeChanged -= Owner_SizeChanged;
                Owner.StateChanged -= Owner_StateChanged;
                Owner.Closed -= Owner_Closed;
            }

            base.OnClosed(e);
        }
    }
}