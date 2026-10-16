// Views/Modals/InteractiveModal.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class InteractiveModal : Window
    {
        // ── Dependencies ────────────────────────────────────────────
        private readonly AppViewModel _viewModel;
        private readonly DatabaseHelper? _bibleDb;
        private readonly Action<PassageSelection> _onNavigateToReader;

        private readonly List<ModalPage> _stack = new();
        private readonly bool _useSecondaryDictionary;

        private DatabaseHelper? _dictionaryDb;
        private DatabaseHelper? _secondaryDictionaryDb;
        private DatabaseHelper? _strongDb;
        private DatabaseHelper? _commentaryDb;
        private DatabaseHelper? _verseCommentaryDb;
        private DatabaseHelper? _crossRefDb;

        // ── Constructor ─────────────────────────────────────────────
        public InteractiveModal(
            AppViewModel viewModel,
            DatabaseHelper? bibleDb,
            Action<PassageSelection> onNavigateToReader,
            string initialType,
            bool isOldTestament,
            bool useSecondaryDictionary = false,
            // Optional content depending on type
            string initialTitle = "",
            string initialContent = "",
            string word = "",
            string definition = "",
            string strongNumber = "",
            string strongDefinition = "",
            int? bookNumber = null,
            int? chapter = null,
            int? verse = null)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _bibleDb = bibleDb;
            _onNavigateToReader = onNavigateToReader;
            _useSecondaryDictionary = useSecondaryDictionary;

            LoadAllHelpers();

            ApplyTheme();
            BuildInitialPage(initialType, isOldTestament, initialTitle,
                initialContent, word, definition, strongNumber, strongDefinition,
                bookNumber, chapter, verse);

            UpdateView();
        }

        // ═══════════════════════════════════════════════════════════
        //  DB / helper loading
        // ═══════════════════════════════════════════════════════════
        private void LoadAllHelpers()
        {
            _dictionaryDb?.Dispose();
            _dictionaryDb = !string.IsNullOrWhiteSpace(_viewModel.SelectedPrimaryDictionary)
                ? new DatabaseHelper($"{_viewModel.SelectedPrimaryDictionary}.dictionary.sqlite3")
                : null;

            _secondaryDictionaryDb?.Dispose();
            _secondaryDictionaryDb = !string.IsNullOrWhiteSpace(_viewModel.SelectedSecondaryDictionary)
                ? new DatabaseHelper($"{_viewModel.SelectedSecondaryDictionary}.dictionary.sqlite3")
                : null;

            _strongDb?.Dispose();
            _strongDb = new DatabaseHelper("secedictionary.sqlite3");

            if (!string.IsNullOrWhiteSpace(_bibleDb?.DatabaseName))
            {
                var comName = _bibleDb!.DatabaseName.Replace(".sqlite3", "com.sqlite3");
                _commentaryDb?.Dispose();
                _commentaryDb = new DatabaseHelper(comName);
            }

            _verseCommentaryDb?.Dispose();
            _verseCommentaryDb = !string.IsNullOrWhiteSpace(_viewModel.SelectedVerseCommentary)
                ? new DatabaseHelper($"{_viewModel.SelectedVerseCommentary}.commentaries.sqlite3")
                : null;

            _crossRefDb?.Dispose();
            _crossRefDb = !string.IsNullOrWhiteSpace(_viewModel.SelectedCrossReferenceDatabase)
                ? new DatabaseHelper($"{_viewModel.SelectedCrossReferenceDatabase}.crossreferences.sqlite3")
                : null;
        }

        private void ApplyTheme()
        {
            var bg = _viewModel.DarkTheme
                ? _viewModel.DarkModalBackgroundColor
                : _viewModel.LightModalBackgroundColor;
            RootBorder.Background = new SolidColorBrush(bg);

            var fg = _viewModel.DarkTheme ? Colors.White : Colors.Black;
            BodyText.Foreground = new SolidColorBrush(fg);
            TitleText.Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor);
        }

        // ═══════════════════════════════════════════════════════════
        //  Initial page
        // ═══════════════════════════════════════════════════════════
        private void BuildInitialPage(
            string type, bool isOldTestament,
            string initialTitle, string initialContent,
            string word, string definition,
            string strongNumber, string strongDefinition,
            int? bookNumber, int? chapter, int? verse)
        {
            _stack.Clear();

            switch (type)
            {
                case "commentary":
                    _stack.Add(new ModalPage
                    {
                        Title = initialTitle,
                        Type = "commentary",
                        Content = ModalContentHelpers.SanitizeHtmlContent(initialContent),
                        IsOldTestament = isOldTestament,
                    });
                    break;

                case "strong":
                    _stack.Add(new ModalPage
                    {
                        Title = $"Strong's Definition for {strongNumber}",
                        Type = "strong",
                        Content = ModalContentHelpers.PrepareStrongContent(strongDefinition),
                        StrongNumber = strongNumber,
                        IsOldTestament = isOldTestament,
                    });
                    break;

                case "definition":
                    BuildDefinitionInitial(word, definition, isOldTestament);
                    break;

                case "versecommentary":
                    BuildVerseCommentaryInitial(bookNumber ?? 10, chapter ?? 1, verse ?? 1,
                        isOldTestament);
                    break;

                case "crossreference":
                    _stack.Add(new ModalPage
                    {
                        Title = initialTitle,
                        Type = "crossreference",
                        Content = ModalContentHelpers.SanitizeHtmlContent(initialContent),
                        Description = InteractiveModalUtils
                            .CrossReferenceDatabaseDisplayNames
                            .GetValueOrDefault(_viewModel.SelectedCrossReferenceDatabase, ""),
                        IsOldTestament = isOldTestament,
                        BookNumber = bookNumber,
                        Chapter = chapter,
                        Verse = verse,
                    });
                    break;
            }
        }

        private void BuildDefinitionInitial(string word, string definition, bool isOldTestament)
        {
            var dict = _useSecondaryDictionary
                ? _viewModel.SelectedSecondaryDictionary
                : _viewModel.SelectedPrimaryDictionary;

            var db = _useSecondaryDictionary ? _secondaryDictionaryDb : _dictionaryDb;
            var dbDisplay = InteractiveModalUtils.DictionaryDisplayNames
                .GetValueOrDefault(dict, dict);

            if (string.IsNullOrWhiteSpace(dict))
            {
                _stack.Add(new ModalPage
                {
                    Title = "No Dictionary Available",
                    Type = "definition",
                    Content = "There are no dictionaries available for this language.",
                    IsOldTestament = isOldTestament,
                });
                return;
            }

            if (!string.IsNullOrWhiteSpace(definition) &&
                !definition.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                _stack.Add(new ModalPage
                {
                    Title = $"Definition of {Capitalize(word)}",
                    Type = "definition",
                    Content = definition,
                    Word = word,
                    Description = dbDisplay,
                    IsOldTestament = isOldTestament,
                });
                return;
            }

            // Look up in DB
            var pairs = ModalContentHelpers.GetDefinitionOrClosest(db, word);
            if (pairs != null && pairs.Count > 0)
            {
                _stack.Add(new ModalPage
                {
                    Title = pairs.Count == 1 && pairs[0].topic.Equals(word, StringComparison.OrdinalIgnoreCase)
                        ? $"Definition of {Capitalize(pairs[0].topic)}"
                        : $"Matches for \"{Capitalize(word)}\"",
                    Type = "definition",
                    Content = ModalContentHelpers.BuildDefinitionContent(
                        word, pairs, dict == "oxford", dict == "topical"),
                    Word = word,
                    Description = dbDisplay,
                    IsOldTestament = isOldTestament,
                });
            }
            else
            {
                _stack.Add(new ModalPage
                {
                    Title = $"Definition of {Capitalize(word)} not found",
                    Type = "definition",
                    Content = $"No results for \"{word}\".",
                    Word = word,
                    Description = dbDisplay,
                    IsOldTestament = isOldTestament,
                });
            }
        }

        private void BuildVerseCommentaryInitial(int book, int chapter, int verse, bool isOldTestament)
        {
            var dbDisplay = InteractiveModalUtils.VerseCommentaryDisplayNames
                .GetValueOrDefault(_viewModel.SelectedVerseCommentary, "");

            var bookName = BibleData.GetBookByCustomNumber(book)?.Name ?? "Book";
            var commentaries = ModalContentHelpers.GetVerseCommentaries(
                _verseCommentaryDb, book, chapter, verse);

            var content = commentaries == null || commentaries.Count == 0
                ? "No commentaries available for this verse."
                : string.Join("\n\n──────────\n\n", commentaries.Select(c => c.Text));

            _stack.Add(new ModalPage
            {
                Title = $"Notes on {bookName} {chapter}:{verse}",
                Type = "versecommentary",
                Content = content,
                Description = dbDisplay,
                IsOldTestament = isOldTestament,
                BookNumber = book,
                Chapter = chapter,
                Verse = verse,
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  View updates
        // ═══════════════════════════════════════════════════════════
        private void UpdateView()
        {
            if (_stack.Count == 0) { Close(); return; }
            var page = _stack[^1];

            // Title row
            if (page.Type == "definition" && !string.IsNullOrWhiteSpace(page.Word))
            {
                TitleText.Text = "Definition of";
                TitleWordLink.Text = Capitalize(page.Word);
                TitleWordLink.Visibility = Visibility.Visible;
            }
            else
            {
                TitleText.Text = page.Title;
                TitleWordLink.Visibility = Visibility.Collapsed;
            }

            // Description row
            if (!string.IsNullOrWhiteSpace(page.Description))
            {
                DescriptionPanel.Visibility = Visibility.Visible;
                DescriptionText.Text = page.Description;
                NextDbButton.Visibility = page.Type == "definition"
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
            else DescriptionPanel.Visibility = Visibility.Collapsed;

            // Body
            BodyText.Inlines.Clear();
            RenderBody(page);

            // Buttons
            BackButton.Visibility = _stack.Count > 1
                ? Visibility.Visible : Visibility.Collapsed;
        }

        private void RenderBody(ModalPage page)
        {
            if (page.Type == "verses" && page.Verses != null)
            {
                RenderVerseList(page);
                return;
            }

            // Default: render text with hyperlinks
            RenderInlineHtml(page.Content);
        }

        private void RenderVerseList(ModalPage page)
        {
            var passage = page.Passage;
            if (passage == null || page.Verses == null) return;

            var processor = new VerseTextProcessor();
            var themeColors = new ThemeColors(
                TextColor: _viewModel.DarkTheme
                    ? _viewModel.DarkThemeReaderFontColor
                    : _viewModel.LightThemeReaderFontColor,
                VerseNumber: _viewModel.HeaderButtonsColor,
                Primary: _viewModel.HeaderButtonsColor,
                TagColor: Colors.MediumPurple,
                TagBg: Color.FromArgb(0x33, 0x7F, 0xB2, 0xFF),
                WordsOfJesus: _viewModel.WordsOfJesus,
                SearchHighlightBg: Color.FromArgb(0x55, 0x7F, 0xB2, 0xFF),
                HighlightIcon: _viewModel.HeaderButtonsColor,
                WordHighlightBg: _viewModel.WordMarkerColor);

            foreach (var verse in page.Verses)
            {
                // Bold verse number
                BodyText.Inlines.Add(new Run($" {verse.VerseNumber} ")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(themeColors.VerseNumber),
                });

                // Processed body
                var processed = processor.ProcessVerse(
                    verseText: verse.Text,
                    baseFontSize: 12,
                    themeColors: themeColors,
                    textColor: themeColors.TextColor,
                    isOldTestament: page.IsOldTestament,
                    options: new ProcessingOptions { ShowStrongs = _viewModel.ShowStrongs });

                foreach (var inline in processed.Body) BodyText.Inlines.Add(inline);

                BodyText.Inlines.Add(new LineBreak());
            }
        }

        private void RenderInlineHtml(string content)
        {
            if (string.IsNullOrEmpty(content)) return;

            // Find and convert <a href="...">text</a> into clickable Hyperlinks
            var linkRegex = new Regex("<a\\s+href=\"([^\"]+)\"[^>]*>(.*?)</a>",
                RegexOptions.IgnoreCase);

            int cursor = 0;
            foreach (Match m in linkRegex.Matches(content))
            {
                if (m.Index > cursor)
                {
                    var prefix = StripTags(content.Substring(cursor, m.Index - cursor));
                    BodyText.Inlines.Add(new Run(prefix));
                }

                var href = m.Groups[1].Value;
                var text = StripTags(m.Groups[2].Value);

                var link = new Hyperlink(new Run(text))
                {
                    Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                    Tag = href,
                    TextDecorations = TextDecorations.Underline,
                };
                link.Click += Link_Click;
                BodyText.Inlines.Add(link);

                cursor = m.Index + m.Length;
            }

            if (cursor < content.Length)
            {
                var tail = StripTags(content.Substring(cursor));
                BodyText.Inlines.Add(new Run(tail));
            }
        }

        private static string StripTags(string text)
            => Regex.Replace(text, "<[^>]+>", "");

        // ═══════════════════════════════════════════════════════════
        //  Click handlers
        // ═══════════════════════════════════════════════════════════
        private void Link_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Hyperlink link) return;
            var href = link.Tag as string ?? "";

            if (href.StartsWith("B:"))
            {
                var passage = ModalContentHelpers.ParseVerseLink(href, link.Inlines
                    .OfType<Run>().FirstOrDefault()?.Text ?? "");
                if (passage != null)
                {
                    var verses = ModalContentHelpers.FetchVerses(passage, _bibleDb);
                    _stack.Add(new ModalPage
                    {
                        Title = $"{passage.BookName} {passage.Chapter}:{passage.Verse}",
                        Type = "verses",
                        Verses = verses,
                        Passage = passage,
                        IsOldTestament = BibleData.GetBookByCustomNumber(passage.BookNumber)?.Testament
                            == Testament.Old,
                    });
                    UpdateView();
                }
            }
        }

        private void TitleWordLink_Click(object sender, MouseButtonEventArgs e)
        {
            // TODO: open edit-word dialog. For now, no-op.
        }

        private void DescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: open the description dropdown (dictionary/commentary switcher).
        }

        private void NextDbButton_Click(object sender, RoutedEventArgs e)
        {
            var page = _stack[^1];
            if (page.Type == "definition" && !string.IsNullOrWhiteSpace(page.Word))
            {
                // Cycle through dictionaries for the current language
                var lang = _useSecondaryDictionary
                    ? _viewModel.SelectedSecondaryDictLanguage
                    : _viewModel.SelectedPrimaryDictLanguage;
                var dicts = InteractiveModalUtils.DictionariesByLanguage
                    .GetValueOrDefault(lang, new List<string>());
                if (dicts.Count <= 1) return;

                var current = _useSecondaryDictionary
                    ? _viewModel.SelectedSecondaryDictionary
                    : _viewModel.SelectedPrimaryDictionary;
                var idx = dicts.IndexOf(current);
                var next = dicts[(idx + 1) % dicts.Count];

                if (_useSecondaryDictionary) _viewModel.SelectedSecondaryDictionary = next;
                else _viewModel.SelectedPrimaryDictionary = next;

                LoadAllHelpers();
                BuildDefinitionInitial(page.Word, "", page.IsOldTestament);
                UpdateView();
            }
        }

        private void ColorSwatchButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: open ColorWheelDialog. For now, alternate between light/dark.
            _viewModel.DarkTheme = !_viewModel.DarkTheme;
            ApplyTheme();
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            if (_stack.Count > 1)
            {
                _stack.RemoveAt(_stack.Count - 1);
                UpdateView();
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _dictionaryDb?.Dispose();
            _secondaryDictionaryDb?.Dispose();
            _strongDb?.Dispose();
            _commentaryDb?.Dispose();
            _verseCommentaryDb?.Dispose();
            _crossRefDb?.Dispose();
        }

        // ═══════════════════════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════════════════════
        private static string Capitalize(string s)
            => string.IsNullOrEmpty(s) ? s
               : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}