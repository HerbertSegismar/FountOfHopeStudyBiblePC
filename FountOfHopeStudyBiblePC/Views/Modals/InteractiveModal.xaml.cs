using System.IO;
using System.Text;
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
        private readonly AppViewModel _viewModel;
        private readonly DatabaseHelper? _bibleDb;
        private readonly Action<PassageSelection> _onNavigateToReader;
        private readonly ModalOverlayController _overlay;

        private readonly List<ModalPage> _stack = [];
        private readonly bool _useSecondaryDictionary;

        private DatabaseHelper? _dictionaryDb;
        private DatabaseHelper? _secondaryDictionaryDb;
        private DatabaseHelper? _strongDb;
        private DatabaseHelper? _commentaryDb;
        private DatabaseHelper? _verseCommentaryDb;
        private DatabaseHelper? _crossRefDb;

        private readonly Dictionary<int, int> _crossRefCounts = [];

        public InteractiveModal(
            AppViewModel viewModel,
            DatabaseHelper? bibleDb,
            Action<PassageSelection> onNavigateToReader,
            string initialType,
            bool isOldTestament,
            bool useSecondaryDictionary = false,
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

            BodyText.MouseLeftButtonUp += BodyText_MouseLeftButtonUp;

            _overlay = new ModalOverlayController(this) { CloseOnOwnerResize = false };

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

        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Window_KeyDown(object sender, KeyEventArgs e)
            => _overlay.HandleKeyDown(sender, e);

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

            _commentaryDb?.Dispose();
            _commentaryDb = CreateCommentaryHelperIfExists(_bibleDb?.DatabaseName);

            _verseCommentaryDb?.Dispose();
            _verseCommentaryDb = !string.IsNullOrWhiteSpace(_viewModel.SelectedVerseCommentary)
                ? new DatabaseHelper($"{_viewModel.SelectedVerseCommentary}.commentaries.sqlite3")
                : null;

            _crossRefDb?.Dispose();
            _crossRefDb = !string.IsNullOrWhiteSpace(_viewModel.SelectedCrossReferenceDatabase)
                ? new DatabaseHelper($"{_viewModel.SelectedCrossReferenceDatabase}.crossreferences.sqlite3")
                : null;
        }

        private DatabaseHelper? CreateCommentaryHelperIfExists(string? bibleDbName)
        {
            if (string.IsNullOrWhiteSpace(bibleDbName)) return null;

            var comDbName = bibleDbName.Replace(".sqlite3", "com.sqlite3");

            var localPath = DatabaseHelper.GetDatabaseFilePath(comDbName);
            if (File.Exists(localPath))
                return new DatabaseHelper(comDbName);

            var assetCandidates = new[]
            {
                Path.Combine(AppContext.BaseDirectory, "Assets", "databases", comDbName),
                Path.Combine(AppContext.BaseDirectory, "Assets", comDbName),
            };
            foreach (var asset in assetCandidates)
                if (File.Exists(asset))
                    return new DatabaseHelper(comDbName);

            return null;
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
            _crossRefCounts.Clear();

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
                    BuildVerseCommentaryInitial(
                        bookNumber ?? 10, chapter ?? 1, verse ?? 1, isOldTestament);
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

                case "verses":
                    {
                        var bookNum = bookNumber ?? 1;
                        var chap = chapter ?? 1;
                        var vers = verse ?? 1;
                        var bookName = BibleData.GetBookByCustomNumber(bookNum)?.Name ?? "Book";

                        var passage = new PassageSelection
                        {
                            BookNumber = bookNum,
                            BookName = bookName,
                            Chapter = chap,
                            Verse = vers,
                        };

                        var verses = ModalContentHelpers.FetchVerses(passage, _bibleDb);
                        LoadCrossRefCounts(bookNum, verses);

                        _stack.Add(new ModalPage
                        {
                            Title = $"{bookName} {chap}:{vers}",
                            Type = "verses",
                            Content = "",
                            Verses = verses,
                            Passage = passage,
                            IsOldTestament = isOldTestament,
                            BookNumber = bookNum,
                            Chapter = chap,
                            Verse = vers,
                        });
                        break;
                    }
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

        private void BuildVerseCommentaryInitial(
            int book, int chapter, int verse, bool isOldTestament)
        {
            var selected = _viewModel.SelectedVerseCommentary;
            var bookName = BibleData.GetBookByCustomNumber(book)?.Name ?? "Book";

            string content;
            if (string.IsNullOrWhiteSpace(selected))
            {
                content = "No verse commentary is selected in Settings.";
            }
            else if (_verseCommentaryDb == null)
            {
                content = $"Could not open '{selected}.commentaries.sqlite3'.";
            }
            else
            {
                var commentaries = _verseCommentaryDb
                    .GetCommentariesForVerse(book, chapter, verse);

                content = commentaries == null || commentaries.Count == 0
                    ? "No commentaries available for this verse."
                    : ModalContentHelpers.SanitizeHtmlContent(
                        string.Join("<br><br>──────────<br><br>",
                            commentaries.Select(c => c.Text)));
            }

            _stack.Add(new ModalPage
            {
                Title = $"Notes on {bookName} {chapter}:{verse}",
                Type = "versecommentary",
                Content = content,
                Description = InteractiveModalUtils.VerseCommentaryDisplayNames
                    .GetValueOrDefault(selected, selected ?? ""),
                IsOldTestament = isOldTestament,
                BookNumber = book,
                Chapter = chapter,
                Verse = verse,
            });
        }

        // ═══════════════════════════════════════════════════════════
        //  View refresh
        // ═══════════════════════════════════════════════════════════
        private void UpdateView()
        {
            if (_stack.Count == 0) { Close(); return; }
            var page = _stack[^1];

            if (page.Type == "definition" && !string.IsNullOrWhiteSpace(page.Word))
            {
                var selectedDict = _useSecondaryDictionary
                    ? _viewModel.SelectedSecondaryDictionary
                    : _viewModel.SelectedPrimaryDictionary;
                var isTopical = selectedDict == "topical";

                string prefix;
                if (isTopical) prefix = "References for ";
                else if (page.Title.StartsWith("Definition of ", StringComparison.OrdinalIgnoreCase))
                    prefix = "Definition of ";
                else if (page.Title.StartsWith("Closest match for ", StringComparison.OrdinalIgnoreCase))
                    prefix = "Closest match for ";
                else if (page.Title.StartsWith("Matches for ", StringComparison.OrdinalIgnoreCase))
                    prefix = "Matches for ";
                else prefix = "Definition of ";

                TitleText.Text = prefix;
                TitleWordLink.Text = Capitalize(page.Word);
                TitleWordLink.Visibility = Visibility.Visible;
            }
            else
            {
                TitleText.Text = page.Title;
                TitleWordLink.Visibility = Visibility.Collapsed;
            }

            PlayInReaderButton.Visibility = page.Type == "verses" && page.Passage != null
                ? Visibility.Visible
                : Visibility.Collapsed;

            if (!string.IsNullOrWhiteSpace(page.Description))
            {
                DescriptionPanel.Visibility = Visibility.Visible;
                DescriptionText.Text = page.Description;
                NextDbButton.Visibility =
                    page.Type is "definition" or "versecommentary" or "crossreference"
                        ? Visibility.Visible
                        : Visibility.Collapsed;
            }
            else
            {
                DescriptionPanel.Visibility = Visibility.Collapsed;
                NextDbButton.Visibility = Visibility.Collapsed;
            }

            BodyText.Inlines.Clear();
            RenderBody(page);

            BackButton.Visibility = _stack.Count > 1
                ? Visibility.Visible
                : Visibility.Collapsed;

            BodyScroller?.ScrollToTop();
        }

        private void RenderBody(ModalPage page)
        {
            if (page.Type == "verses" && page.Verses != null)
            {
                RenderVerseList(page);
                return;
            }
            RenderInlineHtml(page.Content);
        }

        // ═══════════════════════════════════════════════════════════
        //  Verse list
        // ═══════════════════════════════════════════════════════════
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

            var showChapterHeaders = page.Verses
                .Select(v => v.Chapter is int c ? c : 0)
                .Where(c => c > 0)
                .Distinct()
                .Count() > 1;

            int? lastChapter = null;

            foreach (var verse in page.Verses)
            {
                int currentChapter = verse.Chapter is int cc ? cc : 0;

                if (showChapterHeaders && currentChapter != lastChapter)
                {
                    BodyText.Inlines.Add(new Run($"Chapter {currentChapter}")
                    {
                        FontWeight = FontWeights.Bold,
                        FontSize = 14,
                        Foreground = new SolidColorBrush(themeColors.Primary),
                    });
                    BodyText.Inlines.Add(new LineBreak());
                    lastChapter = currentChapter;
                }

                BodyText.Inlines.Add(new Run($" {verse.VerseNumber} ")
                {
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(themeColors.VerseNumber),
                });

                int vBook = passage.BookNumber is int pb && pb > 0 ? pb : 1;
                int vChapter =
                    verse.Chapter is int vcc && vcc > 0 ? vcc
                    : passage.Chapter is int pcc && pcc > 0 ? pcc
                    : 1;
                var vVerse = verse.VerseNumber;
                var vIsOld = page.IsOldTestament;

                var processed = processor.ProcessVerse(
                    verseText: verse.Text,
                    baseFontSize: 12,
                    themeColors: themeColors,
                    textColor: themeColors.TextColor,
                    isOldTestament: vIsOld,
                    options: new ProcessingOptions
                    {
                        ShowStrongs = _viewModel.ShowStrongs,
                        EnableWordClick = true,
                        EnableStrongsClick = true,
                        EnableTagClick = true,
                    },
                    onTagPress: marker =>
                        OpenFootnoteCommentary(marker, vBook, vChapter, vVerse, vIsOld),
                    onWordPress: w =>
                        OnWordPressInVerse(w, vBook, vChapter, vVerse, vIsOld),
                    onStrongsPress: num =>
                        OnStrongsPressInVerse(num, vBook, vChapter, vVerse, vIsOld));

                foreach (var inline in processed.Body) BodyText.Inlines.Add(inline);

                if (_crossRefCounts.TryGetValue(vVerse, out var refCount) && refCount > 0)
                {
                    BodyText.Inlines.Add(new Run(" "));
                    var refLink = new Hyperlink(new Run($"[{refCount}]"))
                    {
                        Foreground = new SolidColorBrush(themeColors.Primary),
                        FontWeight = FontWeights.Bold,
                        TextDecorations = new TextDecorationCollection(),
                        Cursor = Cursors.Hand,
                        ToolTip = "Open cross-references",
                        Tag = new VerseCommentaryTagInfo
                        {
                            BookNumber = vBook,
                            Chapter = vChapter,
                            VerseNumber = vVerse,
                            IsOldTestament = vIsOld,
                        },
                    };
                    refLink.Click += CrossRefLink_Click;
                    BodyText.Inlines.Add(refLink);
                }

                BodyText.Inlines.Add(new Run(" "));
                var star = new Hyperlink(new Run("*"))
                {
                    Foreground = new SolidColorBrush(themeColors.VerseNumber),
                    FontWeight = FontWeights.Bold,
                    TextDecorations = new TextDecorationCollection(),
                    Cursor = Cursors.Hand,
                    ToolTip = "Open verse commentary",
                    Tag = new VerseCommentaryTagInfo
                    {
                        BookNumber = vBook,
                        Chapter = vChapter,
                        VerseNumber = vVerse,
                        IsOldTestament = vIsOld,
                    },
                };
                star.Click += VerseCommentaryLink_Click;
                BodyText.Inlines.Add(star);

                BodyText.Inlines.Add(new LineBreak());
            }
        }

        private void BodyText_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Handled) return;
            if (sender is not TextBlock tb) return;
            if (_stack.Count == 0) return;

            var page = _stack[^1];
            if (page.Type != "verses") return;

            var pt = e.GetPosition(tb);
            var pos = tb.GetPositionFromPoint(pt, snapToText: true);
            if (pos == null) return;

            var run = pos.Parent as Run ?? FindRunAtPosition(tb, pos);
            if (run == null) return;

            if (run.Tag is not VerseAnnotation ann) return;

            var passage = page.Passage;
            int book = passage?.BookNumber is int b && b > 0 ? b : 1;
            int chapter = passage?.Chapter is int c && c > 0 ? c : 1;

            System.Diagnostics.Debug.WriteLine(
                $"[ModalBody click] tag='{ann.Tag}' value='{ann.Value}' " +
                $"b={book} c={chapter} old={page.IsOldTestament}");

            switch (ann.Tag)
            {
                case "word":
                    OnWordPressInVerse(ann.Value, book, chapter, 1, page.IsOldTestament);
                    e.Handled = true;
                    break;

                case "strong":
                    OnStrongsPressInVerse(ann.Value, book, chapter, 1, page.IsOldTestament);
                    e.Handled = true;
                    break;
            }
        }

        private static Run? FindRunAtPosition(TextBlock tb, TextPointer pos)
        {
            var offset = tb.ContentStart.GetOffsetToPosition(pos);
            if (offset < 0) return null;

            int cursor = 0;
            foreach (var inline in tb.Inlines)
            {
                if (inline is not Run r) continue;
                int len = r.Text?.Length ?? 0;
                if (offset >= cursor && offset < cursor + len) return r;
                cursor += len;
            }
            return null;
        }

        private void LoadCrossRefCounts(int book, List<Verse> verses)
        {
            _crossRefCounts.Clear();
            if (_crossRefDb == null || verses == null) return;

            var chapters = verses
                .Select(v => v.Chapter is int c ? c : 0)
                .Where(c => c > 0)
                .Distinct()
                .ToList();

            foreach (var chapter in chapters)
            {
                try
                {
                    var counts = _crossRefDb.GetCrossReferenceCountsForChapter(book, chapter);
                    if (counts == null) continue;
                    foreach (var kv in counts)
                        _crossRefCounts[kv.Key] = kv.Value;
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(
                        $"[LoadCrossRefCounts] ch={chapter} failed: {ex.Message}");
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Inline HTML → WPF Inlines
        //  Preserves <b>/<i>/<u>/<sup>/<sub>/<p>/<br>/<hr>/<h1-6>/
        //  <ul>/<ol>/<li>/<blockquote>, decodes entities, handles
        //  <a href> links with nested formatting.
        //  Self-closing variants (<p/>, <h1/>, <blockquote/>) are
        //  treated the same as their closing counterparts — some
        //  commentary DBs use `<p/>` as a paragraph separator.
        // ═══════════════════════════════════════════════════════════
        private void RenderInlineHtml(string content)
        {
            if (string.IsNullOrEmpty(content)) return;

            int i = 0;
            var buffer = new StringBuilder();
            int bold = 0, italic = 0, underline = 0, sup = 0, sub = 0;
            Hyperlink? link = null;
            InlineCollection target = BodyText.Inlines;
            bool inOrderedList = false;
            int olCounter = 0;

            void Flush()
            {
                if (buffer.Length == 0) return;
                var decoded = DecodeEntities(buffer.ToString());
                buffer.Clear();
                if (decoded.Length == 0) return;

                var run = new Run(decoded);
                if (bold > 0) run.FontWeight = FontWeights.Bold;
                if (italic > 0) run.FontStyle = FontStyles.Italic;
                if (underline > 0) run.TextDecorations = TextDecorations.Underline;
                if (sup > 0) run.BaselineAlignment = BaselineAlignment.Superscript;
                else if (sub > 0) run.BaselineAlignment = BaselineAlignment.Subscript;
                target.Add(run);
            }

            void EnsureTrailingLineBreaks(int count)
            {
                var last = target.Count > 0 ? target.LastInline : null;
                while (last is LineBreak)
                {
                    target.Remove(last);
                    last = target.Count > 0 ? target.LastInline : null;
                }
                for (int k = 0; k < count; k++)
                    target.Add(new LineBreak());
            }

            void CloseLink()
            {
                if (link == null) return;
                Flush();
                if (link.Inlines.Count > 0)
                    BodyText.Inlines.Add(link);
                link = null;
                target = BodyText.Inlines;
            }

            while (i < content.Length)
            {
                var c = content[i];

                if (c == '<')
                {
                    var end = content.IndexOf('>', i);
                    if (end == -1) { buffer.Append(content, i, content.Length - i); break; }

                    var rawTag = content.Substring(i + 1, end - i - 1).Trim();
                    Flush();

                    var closing = rawTag.StartsWith("/");
                    if (closing) rawTag = rawTag.Substring(1).TrimStart();
                    var selfClosing = rawTag.EndsWith("/");
                    if (selfClosing) rawTag = rawTag.Substring(0, rawTag.Length - 1).TrimEnd();
                    var sp = rawTag.IndexOfAny(new[] { ' ', '\t', '\n', '\r' });
                    var name = (sp >= 0 ? rawTag.Substring(0, sp) : rawTag).ToLowerInvariant();
                    var attrs = sp >= 0 ? rawTag.Substring(sp) : "";

                    switch (name)
                    {
                        case "a":
                            if (closing || selfClosing)
                            {
                                CloseLink();
                            }
                            else
                            {
                                var m = Regex.Match(attrs,
                                    "\\bhref\\s*=\\s*[\"']([^\"']+)[\"']",
                                    RegexOptions.IgnoreCase);
                                if (m.Success)
                                {
                                    link = new Hyperlink
                                    {
                                        Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                                        Tag = DecodeEntities(m.Groups[1].Value),
                                        TextDecorations = TextDecorations.Underline,
                                        Cursor = Cursors.Hand,
                                    };
                                    link.Click += Link_Click;
                                    target = link.Inlines;
                                }
                            }
                            break;

                        case "br":
                            target.Add(new LineBreak());
                            break;

                        case "p":
                        case "div":
                            if (closing || selfClosing) EnsureTrailingLineBreaks(2);
                            break;

                        case "h1":
                        case "h2":
                        case "h3":
                        case "h4":
                        case "h5":
                        case "h6":
                            if (closing || selfClosing)
                            {
                                bold = Math.Max(0, bold - 1);
                                EnsureTrailingLineBreaks(2);
                            }
                            else
                            {
                                EnsureTrailingLineBreaks(1);
                                bold++;
                            }
                            break;

                        case "b":
                        case "strong":
                            if (closing) bold = Math.Max(0, bold - 1); else bold++;
                            break;

                        case "i":
                        case "em":
                            if (closing) italic = Math.Max(0, italic - 1); else italic++;
                            break;

                        case "u":
                            if (closing) underline = Math.Max(0, underline - 1); else underline++;
                            break;

                        case "sup":
                            if (closing) sup = Math.Max(0, sup - 1); else sup++;
                            break;

                        case "sub":
                            if (closing) sub = Math.Max(0, sub - 1); else sub++;
                            break;

                        case "ol":
                            if (closing) { inOrderedList = false; olCounter = 0; EnsureTrailingLineBreaks(2); }
                            else { inOrderedList = true; olCounter = 0; EnsureTrailingLineBreaks(1); }
                            break;

                        case "ul":
                            if (closing) EnsureTrailingLineBreaks(2);
                            else EnsureTrailingLineBreaks(1);
                            break;

                        case "li":
                            if (closing)
                            {
                                EnsureTrailingLineBreaks(1);
                            }
                            else
                            {
                                EnsureTrailingLineBreaks(1);
                                if (inOrderedList) { olCounter++; target.Add(new Run($"{olCounter}. ")); }
                                else target.Add(new Run("•  "));
                            }
                            break;

                        case "hr":
                            EnsureTrailingLineBreaks(1);
                            target.Add(new Run("──────────────"));
                            EnsureTrailingLineBreaks(1);
                            break;

                        case "blockquote":
                            if (closing || selfClosing) EnsureTrailingLineBreaks(2);
                            else EnsureTrailingLineBreaks(1);
                            break;

                            // <span>, <font>, <style>, unknown → silently ignored
                    }

                    i = end + 1;
                }
                else if (c == '&')
                {
                    var semi = content.IndexOf(';', i);
                    if (semi != -1 && semi - i <= 12)
                    {
                        buffer.Append(content, i, semi - i + 1);
                        i = semi + 1;
                    }
                    else
                    {
                        buffer.Append(c);
                        i++;
                    }
                }
                else
                {
                    buffer.Append(c);
                    i++;
                }
            }

            Flush();
            CloseLink();

            var lastInline = BodyText.Inlines.Count > 0 ? BodyText.Inlines.LastInline : null;
            while (lastInline is LineBreak)
            {
                BodyText.Inlines.Remove(lastInline);
                lastInline = BodyText.Inlines.Count > 0 ? BodyText.Inlines.LastInline : null;
            }
        }

        private static string DecodeEntities(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            text = text
                .Replace("&lArr;", "←").Replace("&rArr;", "→").Replace("&hArr;", "↔")
                .Replace("&larr;", "←").Replace("&rarr;", "→").Replace("&harr;", "↔");
            return System.Net.WebUtility.HtmlDecode(text);
        }

        private void Link_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Hyperlink link) return;
            var href = link.Tag as string ?? "";

            var linkText = string.Concat(
                link.Inlines.OfType<Run>().Select(r => r.Text ?? ""));

            if (href.StartsWith("B:", StringComparison.OrdinalIgnoreCase))
            {
                NavigateToPassage(href, linkText);
                return;
            }
            if (href.StartsWith("S:", StringComparison.OrdinalIgnoreCase))
            {
                HandleSeeLink(href, linkText);
                return;
            }
        }

        private void VerseCommentaryLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Hyperlink link) return;
            if (link.Tag is not VerseCommentaryTagInfo tag) return;

            BuildVerseCommentaryInitial(
                tag.BookNumber, tag.Chapter, tag.VerseNumber, tag.IsOldTestament);
            UpdateView();
        }

        private void CrossRefLink_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Hyperlink link) return;
            if (link.Tag is not VerseCommentaryTagInfo tag) return;
            if (_crossRefDb == null)
            {
                _stack.Add(new ModalPage
                {
                    Title = "Cross-references unavailable",
                    Type = "crossreference",
                    Content = "No cross-reference database is selected.",
                    IsOldTestament = tag.IsOldTestament,
                });
                UpdateView();
                return;
            }

            var refs = _crossRefDb.GetCrossReferences(
                tag.BookNumber, tag.Chapter, tag.VerseNumber) ?? new List<CrossReference>();
            var bookName = BibleData.GetBookByCustomNumber(tag.BookNumber)?.Name ?? "Book";

            var html = refs.Count == 0
                ? "No references available."
                : string.Join("<br>", refs.Select(r =>
                {
                    var toBook = BibleData.GetBookByCustomNumber(r.BookTo)?.Name ?? r.BookTo.ToString();
                    var vr = r.VerseToStart == r.VerseToEnd
                        ? r.VerseToStart.ToString()
                        : $"{r.VerseToStart}-{r.VerseToEnd}";
                    var href = $"B:{r.BookTo} {r.ChapterTo}:{vr}";
                    return $"<a href=\"{href}\">{toBook} {r.ChapterTo}:{vr}</a>";
                }));

            html = ModalContentHelpers.SanitizeHtmlContent(html);

            _stack.Add(new ModalPage
            {
                Title = $"References for {bookName} {tag.Chapter}:{tag.VerseNumber}",
                Type = "crossreference",
                Content = html,
                Description = InteractiveModalUtils
                    .CrossReferenceDatabaseDisplayNames
                    .GetValueOrDefault(_viewModel.SelectedCrossReferenceDatabase, ""),
                IsOldTestament = tag.IsOldTestament,
                BookNumber = tag.BookNumber,
                Chapter = tag.Chapter,
                Verse = tag.VerseNumber,
            });
            UpdateView();
        }

        private void NavigateToPassage(string href, string linkText)
        {
            var passage = ModalContentHelpers.ParseVerseLink(href, linkText);
            if (passage == null) return;

            int passageBook = passage.BookNumber is int pb && pb > 0 ? pb : 1;
            int passageChapter = passage.Chapter is int pc && pc > 0 ? pc : 1;
            int passageVerse = passage.Verse is int pv && pv > 0 ? pv : 1;

            var verses = ModalContentHelpers.FetchVerses(passage, _bibleDb);
            LoadCrossRefCounts(passageBook, verses);

            _stack.Add(new ModalPage
            {
                Title = $"{passage.BookName} {passage.Chapter}:{passage.Verse}",
                Type = "verses",
                Verses = verses,
                Passage = passage,
                IsOldTestament = BibleData.GetBookByCustomNumber(passageBook)?.Testament == Testament.Old,
                BookNumber = passageBook,
                Chapter = passageChapter,
                Verse = passageVerse,
            });
            UpdateView();
        }

        private void HandleSeeLink(string href, string linkText)
        {
            var seeContent = href.Substring(2).Trim();

            if (seeContent.StartsWith("B:"))
            {
                NavigateToPassage(seeContent, linkText);
                return;
            }
            if (Regex.IsMatch(seeContent, @"^[HG]\d+"))
            {
                ShowStrongDefinition(seeContent);
                return;
            }
            if (Regex.IsMatch(seeContent, @"^\d+"))
            {
                var isOld = _stack[^1].IsOldTestament;
                ShowCombinedStrongDefinition("H" + seeContent, "G" + seeContent, isOld);
                return;
            }

            var wordToFetch = Regex.Replace(linkText, @"^See\s+", "",
                RegexOptions.IgnoreCase).Trim();
            if (string.IsNullOrEmpty(wordToFetch)) wordToFetch = seeContent;

            BuildDefinitionInitial(wordToFetch, "", _stack[^1].IsOldTestament);
            UpdateView();
        }

        private void OnWordPressInVerse(string word, int book, int chapter, int verse, bool isOldTestament)
        {
            var trimmed = (word ?? "").Trim();
            if (string.IsNullOrEmpty(trimmed) || Regex.IsMatch(trimmed, "\\d")) return;

            BuildDefinitionInitial(trimmed, "", isOldTestament);
            UpdateView();
        }

        private void OnStrongsPressInVerse(string strongNumber, int book, int chapter, int verse, bool isOldTestament)
        {
            var trimmed = (strongNumber ?? "").Trim();
            if (string.IsNullOrEmpty(trimmed)) return;

            string prefixed;
            if (char.IsLetter(trimmed[0]))
                prefixed = trimmed.ToUpperInvariant();
            else
                prefixed = (isOldTestament ? "H" : "G") + trimmed;

            if (!Regex.IsMatch(prefixed, @"^[HG]\d+")) return;

            ShowStrongDefinition(prefixed);
        }

        private void ShowStrongDefinition(string strongNumber)
        {
            var isOld = strongNumber.StartsWith("H");
            var definition = _strongDb?.GetStrongDefinition(strongNumber)
                ?? "Strong's definition not found.";
            _stack.Add(new ModalPage
            {
                Title = $"Strong's Definition for {strongNumber}",
                Type = "strong",
                Content = ModalContentHelpers.PrepareStrongContent(definition),
                StrongNumber = strongNumber,
                IsOldTestament = isOld,
            });
            UpdateView();
        }

        private void ShowCombinedStrongDefinition(string hNum, string gNum, bool isOldTestament)
        {
            var hDef = _strongDb?.GetStrongDefinition(hNum) ?? "";
            var gDef = _strongDb?.GetStrongDefinition(gNum) ?? "";
            var preparedH = !string.IsNullOrWhiteSpace(hDef) ? ModalContentHelpers.PrepareStrongContent(hDef) : "";
            var preparedG = !string.IsNullOrWhiteSpace(gDef) ? ModalContentHelpers.PrepareStrongContent(gDef) : "";

            string title, content, strongNum;
            if (preparedH.Length > 0 && preparedG.Length > 0)
            {
                title = $"Strong's Definition for {hNum} and {gNum}";
                content = $"{hNum}:<br>{preparedH}<br><br>{gNum}:<br>{preparedG}";
                strongNum = $"{hNum},{gNum}";
            }
            else if (preparedH.Length > 0)
            { title = $"Strong's Definition for {hNum}"; content = preparedH; strongNum = hNum; }
            else if (preparedG.Length > 0)
            { title = $"Strong's Definition for {gNum}"; content = preparedG; strongNum = gNum; }
            else { title = "Strong's Definition not found"; content = "No definition found."; strongNum = ""; }

            _stack.Add(new ModalPage
            {
                Title = title,
                Type = "strong",
                Content = content,
                StrongNumber = strongNum,
                IsOldTestament = isOldTestament,
            });
            UpdateView();
        }

        private void OpenFootnoteCommentary(
            string marker, int book, int chapter, int verse, bool isOldTestament)
        {
            if (string.IsNullOrEmpty(marker)) return;

            var bookName = BibleData.GetBookByCustomNumber(book)?.Name ?? $"Book {book}";

            string content;
            if (_commentaryDb == null)
            {
                var expected = (_bibleDb?.DatabaseName ?? "").Replace(".sqlite3", "com.sqlite3");
                content =
                    $"No footnote database is available for this Bible version " +
                    $"(expected '{expected}' in Assets/databases/).";
            }
            else
            {
                var text = _commentaryDb.GetCommentary(book, chapter, verse, marker);
                content = string.IsNullOrWhiteSpace(text)
                    ? $"No commentary found for marker \"{marker}\" at {bookName} {chapter}:{verse}."
                    : ModalContentHelpers.SanitizeHtmlContent(text);
            }

            _stack.Add(new ModalPage
            {
                Title = $"Notes on {bookName} {chapter}:{verse}{marker}",
                Type = "commentary",
                Content = content,
                IsOldTestament = isOldTestament,
                BookNumber = book,
                Chapter = chapter,
                Verse = verse,
            });
            UpdateView();
        }

        private void TitleWordLink_Click(object sender, MouseButtonEventArgs e)
        {
            var page = _stack[^1];
            if (page.Type != "definition" || string.IsNullOrWhiteSpace(page.Word)) return;

            var newWord = PromptForWord(page.Word);
            if (string.IsNullOrWhiteSpace(newWord) ||
                newWord.Equals(page.Word, StringComparison.OrdinalIgnoreCase))
                return;

            var capitalized = Capitalize(newWord);
            var loading = page with
            {
                Title = $"Searching for {capitalized}...",
                Content = "Loading...",
                Word = newWord,
            };
            _stack[^1] = loading;
            UpdateView();

            var dict = _useSecondaryDictionary
                ? _viewModel.SelectedSecondaryDictionary
                : _viewModel.SelectedPrimaryDictionary;
            var db = _useSecondaryDictionary ? _secondaryDictionaryDb : _dictionaryDb;
            var display = InteractiveModalUtils.DictionaryDisplayNames
                .GetValueOrDefault(dict, dict);

            var pairs = ModalContentHelpers.GetDefinitionOrClosest(db, newWord);
            string title, content;
            if (pairs != null && pairs.Count > 0)
            {
                var isExact = pairs.Count == 1 &&
                              pairs[0].topic.Equals(newWord, StringComparison.OrdinalIgnoreCase);
                var isTopical = dict == "topical";
                title = isTopical ? $"References for {capitalized}"
                      : isExact ? $"Definition of {Capitalize(pairs[0].topic)}"
                      : pairs.Count == 1 ? $"Closest match for {Capitalize(pairs[0].topic)}"
                      : $"Matches for \"{capitalized}\"";
                content = ModalContentHelpers.BuildDefinitionContent(
                    newWord, pairs, dict == "oxford", isTopical);
            }
            else
            {
                title = $"Definition of {capitalized} not found";
                content = $"No results for \"{newWord}\".";
            }

            _stack[^1] = page with
            {
                Title = title,
                Content = content,
                Word = newWord,
                Description = display,
            };
            UpdateView();
        }

        private string? PromptForWord(string initial)
        {
            var dlg = new Window
            {
                Title = "Edit Word",
                Width = 320,
                Height = 160,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                ResizeMode = ResizeMode.NoResize,
            };

            var panel = new StackPanel { Margin = new Thickness(12) };
            var tb = new TextBox { Text = initial, Margin = new Thickness(0, 4, 0, 12), FontSize = 14 };
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
            };
            var ok = new Button { Content = "Update", Width = 80, Margin = new Thickness(4, 0, 0, 0) };
            var cancel = new Button { Content = "Cancel", Width = 80 };

            ok.Click += (_, _) => { dlg.DialogResult = true; dlg.Close(); };
            cancel.Click += (_, _) => { dlg.DialogResult = false; dlg.Close(); };

            buttons.Children.Add(cancel);
            buttons.Children.Add(ok);
            panel.Children.Add(new TextBlock { Text = "Enter word:" });
            panel.Children.Add(tb);
            panel.Children.Add(buttons);
            dlg.Content = panel;

            return dlg.ShowDialog() == true ? tb.Text.Trim() : null;
        }

        private void DescriptionButton_Click(object sender, RoutedEventArgs e)
        {
            var page = _stack[^1];
            var menu = new ContextMenu();

            switch (page.Type)
            {
                case "definition":
                    {
                        var lang = _useSecondaryDictionary
                            ? _viewModel.SelectedSecondaryDictLanguage
                            : _viewModel.SelectedPrimaryDictLanguage;
                        var dicts = InteractiveModalUtils.DictionariesByLanguage
                            .GetValueOrDefault(lang, new List<string>());
                        foreach (var key in dicts)
                        {
                            var item = new MenuItem
                            {
                                Header = InteractiveModalUtils.DictionaryDisplayNames
                                    .GetValueOrDefault(key, key),
                                IsChecked = key == (_useSecondaryDictionary
                                    ? _viewModel.SelectedSecondaryDictionary
                                    : _viewModel.SelectedPrimaryDictionary),
                            };
                            var captured = key;
                            item.Click += (_, _) => SwitchToDictionary(captured);
                            menu.Items.Add(item);
                        }
                        break;
                    }
                case "versecommentary":
                    {
                        foreach (var key in InteractiveModalUtils.VerseCommentaryDisplayNames.Keys)
                        {
                            var item = new MenuItem
                            {
                                Header = InteractiveModalUtils.VerseCommentaryDisplayNames[key],
                                IsChecked = key == _viewModel.SelectedVerseCommentary,
                            };
                            var captured = key;
                            item.Click += (_, _) => SwitchToVerseCommentary(captured);
                            menu.Items.Add(item);
                        }
                        break;
                    }
                case "crossreference":
                    {
                        foreach (var key in InteractiveModalUtils.CrossReferenceDatabaseDisplayNames.Keys)
                        {
                            var item = new MenuItem
                            {
                                Header = InteractiveModalUtils.CrossReferenceDatabaseDisplayNames[key],
                                IsChecked = key == _viewModel.SelectedCrossReferenceDatabase,
                            };
                            var captured = key;
                            item.Click += (_, _) => SwitchToCrossReference(captured);
                            menu.Items.Add(item);
                        }
                        break;
                    }
                default:
                    return;
            }

            if (menu.Items.Count == 0) return;
            menu.PlacementTarget = sender as UIElement ?? DescriptionPanel;
            menu.IsOpen = true;
        }

        private void PlayInReaderButton_Click(object sender, RoutedEventArgs e)
        {
            var page = _stack[^1];
            if (page.Type != "verses" || page.Passage == null) return;
            _onNavigateToReader?.Invoke(page.Passage);
            Close();
        }

        private void NextDbButton_Click(object sender, RoutedEventArgs e)
        {
            var page = _stack[^1];
            switch (page.Type)
            {
                case "definition":
                    {
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
                        SwitchToDictionary(next);
                        break;
                    }
                case "versecommentary":
                    {
                        var keys = InteractiveModalUtils.VerseCommentaryDisplayNames.Keys.ToList();
                        if (keys.Count <= 1) return;
                        var idx = keys.IndexOf(_viewModel.SelectedVerseCommentary);
                        var next = keys[(idx + 1) % keys.Count];
                        SwitchToVerseCommentary(next);
                        break;
                    }
                case "crossreference":
                    {
                        var keys = InteractiveModalUtils.CrossReferenceDatabaseDisplayNames.Keys.ToList();
                        if (keys.Count <= 1) return;
                        var idx = keys.IndexOf(_viewModel.SelectedCrossReferenceDatabase);
                        var next = keys[(idx + 1) % keys.Count];
                        SwitchToCrossReference(next);
                        break;
                    }
            }
        }

        private void SwitchToDictionary(string newDict)
        {
            if (_useSecondaryDictionary) _viewModel.SelectedSecondaryDictionary = newDict;
            else _viewModel.SelectedPrimaryDictionary = newDict;
            LoadAllHelpers();

            var page = _stack[^1];
            if (page.Type == "definition" && !string.IsNullOrWhiteSpace(page.Word))
            {
                _stack.RemoveAt(_stack.Count - 1);
                BuildDefinitionInitial(page.Word, "", page.IsOldTestament);
                UpdateView();
            }
        }

        private void SwitchToVerseCommentary(string newComKey)
        {
            _viewModel.SelectedVerseCommentary = newComKey;
            LoadAllHelpers();

            var page = _stack[^1];
            if (page.BookNumber is int book && page.Chapter is int chap && page.Verse is int vers)
            {
                _stack.RemoveAt(_stack.Count - 1);
                BuildVerseCommentaryInitial(book, chap, vers, page.IsOldTestament);
                UpdateView();
            }
        }

        private void SwitchToCrossReference(string newDbKey)
        {
            _viewModel.SelectedCrossReferenceDatabase = newDbKey;
            LoadAllHelpers();

            var page = _stack[^1];
            if (page.BookNumber is int book && page.Chapter is int chap && page.Verse is int vers)
            {
                _stack.RemoveAt(_stack.Count - 1);
                var tag = new VerseCommentaryTagInfo
                {
                    BookNumber = book,
                    Chapter = chap,
                    VerseNumber = vers,
                    IsOldTestament = page.IsOldTestament,
                };
                CrossRefLink_Click(new Hyperlink { Tag = tag }, new RoutedEventArgs());
            }
        }

        private void ColorSwatchButton_Click(object sender, RoutedEventArgs e)
        {
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
            => _overlay.DismissWithFade();

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

        private static string Capitalize(string s)
            => string.IsNullOrEmpty(s)
                ? s
                : char.ToUpperInvariant(s[0]) + s.Substring(1);

        private sealed class VerseCommentaryTagInfo
        {
            public int BookNumber { get; init; }
            public int Chapter { get; init; }
            public int VerseNumber { get; init; }
            public bool IsOldTestament { get; init; }
        }
    }
}