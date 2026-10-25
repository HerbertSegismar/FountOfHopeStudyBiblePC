// Views/Modals/VerseOptionsModal.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class VerseOptionsModal : Window
    {
        // ── Dependencies ────────────────────────────────────────────
        private readonly AppViewModel _viewModel;
        private readonly PassageSelection _passage;
        private readonly Verse _verse;
        private readonly List<Verse> _chapterVerses;
        private readonly DatabaseHelper? _db;
        private readonly Action _onAddBookmark;
        private readonly Action _onAddHighlight;
        private readonly Action<List<Verse>> _onShare;
        private readonly Action<List<Verse>> _onAddNote;

        // ── State ───────────────────────────────────────────────────
        private int _currentStart;
        private int _currentEnd;
        private int _maxVerse;
        private bool _isBookmarked;
        private bool _isHighlighted;
        private bool _suppressSliderEvents;

        // ── Scrim dismiss state ─────────────────────────────────────
        private bool _isDismissing;
        private bool _dismissArmed;

        /// <summary>
        /// If true, the dialog dismisses when the owner window is resized.
        /// Default is true — the card is centred and fixed-size.
        /// </summary>
        public bool CloseOnOwnerResize { get; set; } = true;

        public VerseOptionsModal(
            AppViewModel viewModel,
            PassageSelection passage,
            Verse verse,
            List<Verse> chapterVerses,
            DatabaseHelper? databaseHelper,
            Action onAddBookmark,
            Action onAddHighlight,
            Action<List<Verse>> onShare,
            Action<List<Verse>> onAddNote)
        {
            // ── 1. Initialize fields FIRST, before InitializeComponent ──
            _viewModel = viewModel;
            _passage = passage;
            _verse = verse;
            _chapterVerses = chapterVerses ?? new List<Verse>();
            _db = databaseHelper;
            _onAddBookmark = onAddBookmark ?? (() => { });
            _onAddHighlight = onAddHighlight ?? (() => { });
            _onShare = onShare ?? (_ => { });
            _onAddNote = onAddNote ?? (_ => { });

            _currentStart = verse.VerseNumber;
            _currentEnd = verse.VerseNumber;
            _maxVerse = _chapterVerses.Count > 0
                ? _chapterVerses.Max(v => v.VerseNumber)
                : verse.VerseNumber;

            // Suppress slider events while XAML loads
            _suppressSliderEvents = true;

            // ── 2. Now parse the XAML ──
            InitializeComponent();

            // ── 3. Real setup ──
            ApplyTheme();
            InitializeRangeSlider();
            _suppressSliderEvents = false;
            RefreshState();

            Loaded += OnLoaded;
        }

        // ═══════════════════════════════════════════════════════════
        //  Scrim dismiss infrastructure
        // ═══════════════════════════════════════════════════════════
        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only arm dismissal when the press starts on the scrim.
            // Sliders, action-item Borders, buttons and the ScrollViewer
            // all need their own mouse lifecycle untouched.
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

        private void InitializeRangeSlider()
        {
            _suppressSliderEvents = true;

            StartSlider.Minimum = 1;
            StartSlider.Maximum = _maxVerse;
            StartSlider.Value = _currentStart;

            EndSlider.Minimum = 1;
            EndSlider.Maximum = _maxVerse;
            EndSlider.Value = _currentEnd;

            RangeSection.Visibility = _chapterVerses.Count > 0
                ? Visibility.Visible
                : Visibility.Collapsed;

            _suppressSliderEvents = false;
        }

        // ═══════════════════════════════════════════════════════════
        //  State refresh
        // ═══════════════════════════════════════════════════════════
        private void RefreshState()
        {
            var minV = Math.Min(_currentStart, _currentEnd);
            var maxV = Math.Max(_currentStart, _currentEnd);

            var currentVerses = _chapterVerses
                .Where(v => v.VerseNumber >= minV && v.VerseNumber <= maxV)
                .OrderBy(v => v.VerseNumber)
                .ToList();

            if (_db != null && currentVerses.Count > 0)
            {
                _isBookmarked = currentVerses.All(v =>
                {
                    var enriched = v with { BookName = _passage.BookName, Chapter = _passage.Chapter };
                    return _db.IsBookmarked(enriched);
                });

                _isHighlighted = currentVerses.All(v =>
                {
                    var enriched = v with { BookName = _passage.BookName, Chapter = _passage.Chapter };
                    return _db.IsHighlighted(enriched);
                });
            }
            else
            {
                _isBookmarked = false;
                _isHighlighted = false;
            }

            var rangeStr = minV == maxV ? $"{minV}" : $"{minV}-{maxV}";
            PassageHeaderText.Text = $"{_passage.BookName} {_passage.Chapter}:{rangeStr}";

            var count = maxV - minV + 1;
            SelectedCountText.Text = $"Selected {count} verse{(count == 1 ? "" : "s")}";

            var preview = currentVerses.Count > 0
                ? string.Join("\n", currentVerses.Select(v =>
                    $"{v.VerseNumber} {SimpleVerseProcessor.StripXmlTags(v.Text)}"))
                : SimpleVerseProcessor.StripXmlTags(_verse.Text);
            PreviewText.Text = preview;

            BookmarkIcon.Text = _isBookmarked ? "🔖" : "🔖";
            BookmarkTitle.Text = _isBookmarked ? "Bookmarked" : "Add Bookmark";
            BookmarkSubtitle.Text = _isBookmarked ? "Tap to remove" : "Save this verse for later";
            BookmarkButton.Background = _isBookmarked
                ? new SolidColorBrush(Color.FromArgb(0x33, 0x7F, 0xB2, 0xFF))
                : new SolidColorBrush(Color.FromRgb(0x23, 0x2A, 0x33));
            BookmarkButton.Opacity = _isBookmarked ? 1.0 : 0.85;

            HighlightIcon.Text = _isHighlighted ? "★" : "☆";
            HighlightTitle.Text = _isHighlighted ? "Highlighted" : "Add Highlight";
            HighlightSubtitle.Text = _isHighlighted ? "Tap to remove" : "Mark this verse as important";
            HighlightButton.Background = _isHighlighted
                ? new SolidColorBrush(Color.FromArgb(0x33, 0x7F, 0xB2, 0xFF))
                : new SolidColorBrush(Color.FromRgb(0x23, 0x2A, 0x33));
            HighlightButton.Opacity = _isHighlighted ? 1.0 : 0.85;

            ShareTitle.Text = minV == maxV ? "Share Verse" : "Share Verses";
            NoteTitle.Text = minV == maxV ? "Add Note" : "Add Notes";

            StartDecButton.IsEnabled = _currentStart > 1;
            StartIncButton.IsEnabled = _currentStart < _currentEnd;
            EndDecButton.IsEnabled = _currentEnd > _currentStart;
            EndIncButton.IsEnabled = _currentEnd < _maxVerse;
        }

        // ═══════════════════════════════════════════════════════════
        //  Range slider handlers
        // ═══════════════════════════════════════════════════════════
        private void StartSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvents) return;
            var v = (int)Math.Round(e.NewValue);
            if (v > _currentEnd) v = _currentEnd;
            if (v < 1) v = 1;
            _currentStart = v;
            RefreshState();
        }

        private void EndSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvents) return;
            var v = (int)Math.Round(e.NewValue);
            if (v < _currentStart) v = _currentStart;
            if (v > _maxVerse) v = _maxVerse;
            _currentEnd = v;
            RefreshState();
        }

        private void StartDecButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStart > 1)
            {
                _currentStart--;
                _suppressSliderEvents = true;
                StartSlider.Value = _currentStart;
                _suppressSliderEvents = false;
                RefreshState();
            }
        }

        private void StartIncButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentStart < _currentEnd)
            {
                _currentStart++;
                _suppressSliderEvents = true;
                StartSlider.Value = _currentStart;
                _suppressSliderEvents = false;
                RefreshState();
            }
        }

        private void EndDecButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEnd > _currentStart)
            {
                _currentEnd--;
                _suppressSliderEvents = true;
                EndSlider.Value = _currentEnd;
                _suppressSliderEvents = false;
                RefreshState();
            }
        }

        private void EndIncButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentEnd < _maxVerse)
            {
                _currentEnd++;
                _suppressSliderEvents = true;
                EndSlider.Value = _currentEnd;
                _suppressSliderEvents = false;
                RefreshState();
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Action buttons (all confirm actions → immediate Close)
        // ═══════════════════════════════════════════════════════════
        private List<Verse> GetCurrentVerses()
        {
            var minV = Math.Min(_currentStart, _currentEnd);
            var maxV = Math.Max(_currentStart, _currentEnd);
            var currentVerses = _chapterVerses
                .Where(v => v.VerseNumber >= minV && v.VerseNumber <= maxV)
                .OrderBy(v => v.VerseNumber)
                .ToList();

            for (int i = 0; i < currentVerses.Count; i++)
                currentVerses[i] = currentVerses[i] with
                {
                    BookName = _passage.BookName,
                    Chapter = _passage.Chapter,
                };

            if (currentVerses.Count == 0)
            {
                currentVerses.Add(_verse with
                {
                    BookName = _passage.BookName,
                    Chapter = _passage.Chapter,
                });
            }

            return currentVerses;
        }

        private void BookmarkButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (_db == null) { Close(); return; }
            var verses = GetCurrentVerses();

            if (_isBookmarked)
            {
                foreach (var v in verses) _db.RemoveBookmark(v);
            }
            else
            {
                foreach (var v in verses) _db.AddBookmark(v);
            }

            _onAddBookmark();
            Close();   // confirm action → immediate
        }

        private void HighlightButton_Click(object sender, MouseButtonEventArgs e)
        {
            if (_db == null) { Close(); return; }
            var verses = GetCurrentVerses();

            if (_isHighlighted)
            {
                foreach (var v in verses) _db.RemoveHighlight(v);
            }
            else
            {
                var colorArgb = (int)(
                    (_viewModel.VerseMarkerColor.A << 24) |
                    (_viewModel.VerseMarkerColor.R << 16) |
                    (_viewModel.VerseMarkerColor.G << 8) |
                     _viewModel.VerseMarkerColor.B);
                foreach (var v in verses) _db.AddHighlight(v, colorArgb);
            }

            _onAddHighlight();
            Close();   // confirm action → immediate
        }

        private void ShareButton_Click(object sender, MouseButtonEventArgs e)
        {
            var verses = GetCurrentVerses();
            _onShare(verses);
            Close();   // hands off to caller → immediate
        }

        private void NoteButton_Click(object sender, MouseButtonEventArgs e)
        {
            var verses = GetCurrentVerses();
            _onAddNote(verses);
            Close();   // hands off to caller → immediate
        }

        // ═══════════════════════════════════════════════════════════
        //  Dismiss
        // ═══════════════════════════════════════════════════════════
        private void CloseButton_Click(object sender, RoutedEventArgs e) => DismissWithFade();

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