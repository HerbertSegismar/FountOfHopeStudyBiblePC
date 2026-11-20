using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using Microsoft.Data.Sqlite;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Services
{
    public partial class DatabaseHelper : IDisposable
    {
        private const string VersesTable = "verses";
        private const string ColumnText = "text";
        private const string ColumnBookNumber = "book_number";
        private const string ColumnChapter = "chapter";
        private const string ColumnVerse = "verse";
        public const string BookmarksTable = "bookmarks";
        public const string HighlightsTable = "highlights";
        public const string ElementsTable = "elements";
        public const string ColumnBookName = "book_name";
        public const string ColumnVerseNumber = "verse_number";
        public const string ColumnStartVerse = "start_verse";
        public const string ColumnEndVerse = "end_verse";
        public const string ColumnElement = "element";
        public const string ColumnTimestamp = "timestamp";
        public const string ColumnHighlightColor = "highlight_color";
        public const string WordHighlightsTable = "word_highlights";
        public const string ColumnStartPos = "start_pos";
        public const string ColumnEndPos = "end_pos";

        private static readonly Dictionary<int, BibleBook> BookByNumber = BuildBookIndex();
        private static readonly char[] TermSplitters = [' ', '\t', '\n', '\r', '\f', '\v'];

        [GeneratedRegex("<script[\\s\\S]*?</script>")]
        private static partial Regex ScriptTagRegex();

        [GeneratedRegex("</i>(\\w)", RegexOptions.IgnoreCase)]
        private static partial Regex ItalicWordBreakRegex();

        [GeneratedRegex("</em>(\\w)", RegexOptions.IgnoreCase)]
        private static partial Regex EmphasisWordBreakRegex();

        private SqliteConnection? _connection;

        public string DatabaseName { get; }
        public SqliteConnection? Connection => _connection;

        public DatabaseHelper(string databaseName)
        {
            DatabaseName = databaseName;
            OpenDatabase();
        }

        public bool IsEmpty()
            => _connection == null || _connection.State != System.Data.ConnectionState.Open;

        private static Dictionary<int, BibleBook> BuildBookIndex()
        {
            var map = new Dictionary<int, BibleBook>(BibleData.AllBooks.Count);
            foreach (var b in BibleData.AllBooks)
                if (!map.ContainsKey(b.CustomNumber))
                    map[b.CustomNumber] = b;
            return map;
        }

        private static BibleBook? GetBookInfo(int bookNumber)
            => BookByNumber.TryGetValue(bookNumber, out var b) ? b : null;

        private void OpenDatabase()
        {
            try
            {
                var dbFile = GetDatabaseFilePath(DatabaseName);
                if (!File.Exists(dbFile))
                    CopyDatabaseFromAssets(dbFile);

                _connection = new SqliteConnection($"Data Source={dbFile}");
                _connection.Open();

                CreateBookmarksTable();
                CreateHighlightsTable();
                CreateElementsTable();
                CreateWordHighlightsTable();
                MigrateHighlightsTable();
            }
            catch
            {
                _connection?.Dispose();
                _connection = null;
            }
        }

        public bool RefreshDatabase()
        {
            try
            {
                _connection?.Close();
                _connection?.Dispose();
                _connection = null;

                var dbFile = GetDatabaseFilePath(DatabaseName);
                if (File.Exists(dbFile)) File.Delete(dbFile);

                Thread.Sleep(100);
                CopyDatabaseFromAssets(dbFile);

                _connection = new SqliteConnection($"Data Source={dbFile}");
                _connection.Open();

                CreateBookmarksTable();
                CreateHighlightsTable();
                CreateElementsTable();
                CreateWordHighlightsTable();
                return true;
            }
            catch
            {
                try { _connection?.Dispose(); } catch { }
                _connection = null;
                return false;
            }
        }

        public void Dispose()
        {
            try { _connection?.Close(); _connection?.Dispose(); } catch { }
            _connection = null;
            GC.SuppressFinalize(this);
        }

        public void Close() => Dispose();

        private void CreateBookmarksTable() => ExecuteNonQuery($"""
            CREATE TABLE IF NOT EXISTS {BookmarksTable} (
                {ColumnBookName} TEXT,
                {ColumnChapter} INTEGER,
                {ColumnVerseNumber} INTEGER,
                {ColumnText} TEXT,
                PRIMARY KEY ({ColumnBookName}, {ColumnChapter}, {ColumnVerseNumber})
            )
            """);

        private void CreateHighlightsTable() => ExecuteNonQuery($"""
            CREATE TABLE IF NOT EXISTS {HighlightsTable} (
                {ColumnBookName} TEXT,
                {ColumnChapter} INTEGER,
                {ColumnVerseNumber} INTEGER,
                {ColumnText} TEXT,
                PRIMARY KEY ({ColumnBookName}, {ColumnChapter}, {ColumnVerseNumber})
            )
            """);

        private void MigrateHighlightsTable()
        {
            try
            {
                var hasColor = false;
                using (var cmd = _connection!.CreateCommand())
                {
                    cmd.CommandText = $"PRAGMA table_info({HighlightsTable})";
                    using var r = cmd.ExecuteReader();
                    while (r.Read())
                        if (r.GetString(r.GetOrdinal("name")) == ColumnHighlightColor)
                            hasColor = true;
                }
                if (!hasColor)
                    ExecuteNonQuery(
                        $"ALTER TABLE {HighlightsTable} ADD COLUMN {ColumnHighlightColor} INTEGER");
            }
            catch { }
        }

        private void CreateElementsTable() => ExecuteNonQuery($"""
            CREATE TABLE IF NOT EXISTS {ElementsTable} (
                {ColumnBookName} TEXT,
                {ColumnChapter} INTEGER,
                {ColumnStartVerse} INTEGER,
                {ColumnEndVerse} INTEGER,
                {ColumnElement} TEXT,
                {ColumnTimestamp} INTEGER DEFAULT (strftime('%s','now')),
                PRIMARY KEY ({ColumnBookName}, {ColumnChapter}, {ColumnStartVerse}, {ColumnEndVerse})
            )
            """);

        private void CreateWordHighlightsTable() => ExecuteNonQuery($"""
            CREATE TABLE IF NOT EXISTS {WordHighlightsTable} (
                {ColumnBookName} TEXT,
                {ColumnChapter} INTEGER,
                {ColumnVerseNumber} INTEGER,
                {ColumnStartPos} INTEGER,
                {ColumnEndPos} INTEGER,
                {ColumnHighlightColor} INTEGER,
                PRIMARY KEY (
                    {ColumnBookName}, {ColumnChapter},
                    {ColumnVerseNumber}, {ColumnStartPos}, {ColumnEndPos})
            )
            """);

        private static string GetDatabaseDirectory()
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "FountOfHopeStudyBiblePC", "Databases");
            Directory.CreateDirectory(dir);
            return dir;
        }

        public static string GetDatabaseFilePath(string databaseName)
            => Path.Combine(GetDatabaseDirectory(), databaseName);

        private static string GetAssetPath(string databaseName)
        {
            var subDir = databaseName switch
            {
                var n when n.EndsWith("dictionary.sqlite3") => "dictionaries",
                var n when n.EndsWith("kjvsubheadings.sqlite3") => "subheadings",
                var n when n.EndsWith("crossreferences.sqlite3") => "cross-references",
                var n when n.EndsWith("commentaries.sqlite3") => "commentaries",
                _ => "databases"
            };
            return Path.Combine(AppContext.BaseDirectory, "Assets", subDir, databaseName);
        }

        private void CopyDatabaseFromAssets(string destPath)
        {
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(destPath)!);
                var src = GetAssetPath(DatabaseName);
                if (!File.Exists(src)) return;
                File.Copy(src, destPath, overwrite: true);
            }
            catch { }
        }

        public Dictionary<int, int> GetCrossReferenceCountsForChapter(int book, int? chapter)
        {
            var map = new Dictionary<int, int>();
            try
            {
                ExecuteReader(
                    "SELECT verse, COUNT(*) FROM cross_references WHERE book = @p0 AND chapter = @p1 GROUP BY verse",
                    r => { while (r.Read()) map[r.GetInt32(0)] = r.GetInt32(1); },
                    book, chapter);
            }
            catch { }
            return map;
        }

        public List<CrossReference> GetCrossReferences(int book, int chapter, int verse)
        {
            var refs = new List<CrossReference>();
            try
            {
                ExecuteReader(
                    "SELECT book_to, chapter_to, verse_to_start, verse_to_end " +
                    "FROM cross_references WHERE book = @p0 AND chapter = @p1 AND verse = @p2",
                    r =>
                    {
                        while (r.Read())
                        {
                            var start = r.GetInt32(r.GetOrdinal("verse_to_start"));
                            var end = r.GetInt32(r.GetOrdinal("verse_to_end"));
                            if (end == 0) end = start;
                            refs.Add(new CrossReference
                            {
                                BookTo = r.GetInt32(r.GetOrdinal("book_to")),
                                ChapterTo = r.GetInt32(r.GetOrdinal("chapter_to")),
                                VerseToStart = start,
                                VerseToEnd = end
                            });
                        }
                    },
                    book, chapter, verse);
            }
            catch { }
            return refs;
        }

        public List<VerseCommentary> GetCommentariesForVerse(int bookNumber, int chapter, int verse)
        {
            var commentaries = new List<VerseCommentary>();
            try
            {
                ExecuteReader(
                    """
                    SELECT text, chapter_number_from, verse_number_from,
                           chapter_number_to, verse_number_to
                    FROM commentaries
                    WHERE book_number = @p0
                      AND chapter_number_from <= @p1
                      AND (chapter_number_to IS NULL OR chapter_number_to >= @p1)
                    """,
                    r =>
                    {
                        var textIdx = r.GetOrdinal("text");
                        var chFromIdx = r.GetOrdinal("chapter_number_from");
                        var vFromIdx = r.GetOrdinal("verse_number_from");
                        var chToIdx = r.GetOrdinal("chapter_number_to");
                        var vToIdx = r.GetOrdinal("verse_number_to");

                        while (r.Read())
                        {
                            var chapterFrom = r.GetInt32(chFromIdx);
                            var safeChapterFrom = chapterFrom <= 0 ? 1 : chapterFrom;
                            var verseFrom = r.IsDBNull(vFromIdx) || r.GetInt32(vFromIdx) <= 0
                                ? 1 : r.GetInt32(vFromIdx);
                            var chapterToRaw = r.IsDBNull(chToIdx) ? (int?)null : r.GetInt32(chToIdx);
                            var verseToRaw = r.IsDBNull(vToIdx) ? (int?)null : r.GetInt32(vToIdx);
                            var chapterTo = chapterToRaw is > 0 ? chapterToRaw : null;
                            var verseTo = verseToRaw is > 0 ? verseToRaw : null;

                            if (chapterTo == null && verseTo == null)
                            {
                                if (chapter == safeChapterFrom && verse == verseFrom)
                                {
                                    var text = CleanCommentaryText(r.GetString(textIdx));
                                    commentaries.Add(new VerseCommentary(text, safeChapterFrom, verseFrom, null, null));
                                }
                                continue;
                            }

                            var inChapterRange = chapter >= safeChapterFrom &&
                                                 (chapterTo == null || chapter <= chapterTo);
                            if (!inChapterRange) continue;

                            var lowerMet = chapter != safeChapterFrom || verse >= verseFrom;
                            var upperMet = chapterTo == null || chapter != chapterTo ||
                                           verseTo == null || verse <= verseTo;

                            if (lowerMet && upperMet)
                            {
                                var text = CleanCommentaryText(r.GetString(textIdx));
                                commentaries.Add(new VerseCommentary(text, safeChapterFrom, verseFrom, chapterTo, verseTo));
                            }
                        }
                    },
                    bookNumber, chapter);
            }
            catch { }
            return commentaries;
        }

        private static string CleanCommentaryText(string text)
        {
            text = ScriptTagRegex().Replace(text, "");
            text = ItalicWordBreakRegex().Replace(text, "</i> $1");
            text = EmphasisWordBreakRegex().Replace(text, "</em> $1");
            return text;
        }

        public string? GetCommentary(int bookNumber, int chapterNumber, int verseNumber, string marker)
        {
            string? commentary = null;
            try
            {
                ExecuteReader(
                    "SELECT text FROM commentaries " +
                    "WHERE book_number = @p0 AND chapter_number_from = @p1 " +
                    "  AND verse_number_from = @p2 AND marker = @p3",
                    r =>
                    {
                        if (r.Read())
                            commentary = ScriptTagRegex().Replace(
                                r.GetString(r.GetOrdinal("text")), "");
                    },
                    bookNumber, chapterNumber, verseNumber, marker);
            }
            catch { }
            return commentary;
        }

        public int GetVerseCount(int bookNumber, int chapter)
        {
            var count = 0;
            try
            {
                ExecuteReader(
                    $"SELECT COUNT(*) FROM {VersesTable} " +
                    $"WHERE {ColumnBookNumber} = @p0 AND {ColumnChapter} = @p1",
                    r => { if (r.Read()) count = r.GetInt32(0); },
                    bookNumber, chapter);
            }
            catch { }
            return count;
        }

        public List<Verse> GetVerses(int bookNumber, int chapter)
        {
            var verses = new List<Verse>();
            try
            {
                if (_connection == null ||
                    _connection.State != System.Data.ConnectionState.Open)
                    return verses;

                ExecuteReader(
                    $"SELECT {ColumnVerse}, {ColumnText} FROM {VersesTable} " +
                    $"WHERE {ColumnBookNumber} = @p0 AND {ColumnChapter} = @p1 " +
                    $"ORDER BY {ColumnVerse} ASC",
                    r =>
                    {
                        while (r.Read())
                        {
                            var num = r.GetInt32(r.GetOrdinal(ColumnVerse));
                            var textIdx = r.GetOrdinal(ColumnText);
                            var text = r.IsDBNull(textIdx) ? "" : r.GetString(textIdx);
                            verses.Add(new Verse(num, text));
                        }
                    },
                    bookNumber, chapter);
            }
            catch { }
            return verses;
        }

        public List<Subheading> GetSubheadings(int bookNumber, int chapter)
        {
            var subheadings = new List<Subheading>();
            try
            {
                if (_connection == null ||
                    _connection.State != System.Data.ConnectionState.Open)
                    return subheadings;

                ExecuteReader(
                    "SELECT verse, subheading FROM subheadings " +
                    "WHERE book_number = @p0 AND chapter = @p1 ORDER BY verse ASC",
                    r =>
                    {
                        while (r.Read())
                        {
                            try
                            {
                                var v = r.GetInt32(r.GetOrdinal("verse"));
                                var t = r.GetString(r.GetOrdinal("subheading"));
                                subheadings.Add(new Subheading(v, t));
                            }
                            catch { }
                        }
                    },
                    bookNumber, chapter);
            }
            catch { }
            return subheadings;
        }

        public List<string> GetRandomDistractors(
            string excludeWord, int count, HashSet<string>? additionalExcludeWords = null)
        {
            additionalExcludeWords ??= new HashSet<string>();
            var distractors = new List<string>(count);
            var backupDistractors = new List<string>();
            var usedVersesText = new HashSet<string>();
            var usedWords = new HashSet<string>();
            var backupWordsSet = new HashSet<string>();
            if (_connection == null) return distractors;

            var targetLength = excludeWord.Length;
            var isLatin = IsAllAsciiLetters(excludeWord);
            var answerFirstCharIsUpper = isLatin && char.IsUpper(excludeWord[0]);
            var excludedLower = new HashSet<string>(
                new[] { excludeWord }.Concat(additionalExcludeWords).Select(w => w.ToLowerInvariant()));

            const int maxStrictBatches = 10;
            var strictPhase = true;
            var batchesScanned = 0;

            while (distractors.Count < count)
            {
                var any = false;
                ExecuteReader(
                    $"SELECT text FROM {VersesTable} ORDER BY RANDOM() LIMIT 5",
                    r =>
                    {
                        while (r.Read() && distractors.Count < count)
                        {
                            any = true;
                            var raw = r.GetString(0) ?? "";
                            var clean = SimpleVerseProcessor.StripXmlTags(raw);
                            if (string.IsNullOrEmpty(clean) || !usedVersesText.Add(clean)) continue;

                            foreach (var rawWord in clean.Split(' '))
                            {
                                var word = StripNonLetters(rawWord);
                                if (word.Length != targetLength) continue;
                                var wordLower = word.ToLowerInvariant();
                                if (excludedLower.Contains(wordLower)) continue;
                                if (usedWords.Contains(wordLower)) continue;

                                var suffixOk = true;
                                if (isLatin)
                                {
                                    if (excludeWord.EndsWith("ing", StringComparison.OrdinalIgnoreCase))
                                        suffixOk = word.EndsWith("ing", StringComparison.OrdinalIgnoreCase);
                                    else if (excludeWord.EndsWith("ed", StringComparison.OrdinalIgnoreCase))
                                        suffixOk = word.EndsWith("ed", StringComparison.OrdinalIgnoreCase);
                                    else if (excludeWord.EndsWith("ly", StringComparison.OrdinalIgnoreCase))
                                        suffixOk = word.EndsWith("ly", StringComparison.OrdinalIgnoreCase);
                                }
                                if (!suffixOk) continue;
                                if (isLatin && char.IsUpper(word[0]) != answerFirstCharIsUpper) continue;

                                var matchesFirstLast =
                                    char.ToLowerInvariant(word[0]) == char.ToLowerInvariant(excludeWord[0]) &&
                                    char.ToLowerInvariant(word[^1]) == char.ToLowerInvariant(excludeWord[^1]);

                                if (strictPhase)
                                {
                                    if (matchesFirstLast)
                                    {
                                        distractors.Add(word);
                                        usedWords.Add(wordLower);
                                    }
                                    else if (backupWordsSet.Add(wordLower))
                                    {
                                        backupDistractors.Add(word);
                                    }
                                }
                                else
                                {
                                    distractors.Add(word);
                                    usedWords.Add(wordLower);
                                }

                                if (distractors.Count >= count) break;
                            }
                        }
                    });

                if (!any) break;

                if (strictPhase)
                {
                    batchesScanned++;
                    if (batchesScanned >= maxStrictBatches)
                    {
                        foreach (var bw in backupDistractors)
                        {
                            if (distractors.Count >= count) break;
                            if (usedWords.Add(bw.ToLowerInvariant())) distractors.Add(bw);
                        }
                        strictPhase = false;
                    }
                }
                if (distractors.Count >= count) break;
            }
            return distractors;
        }

        private static bool IsAllAsciiLetters(string s)
        {
            if (s.Length == 0) return false;
            foreach (var c in s)
                if (!((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))) return false;
            return true;
        }

        private static string StripNonLetters(string word)
        {
            var hasNonLetter = false;
            foreach (var c in word)
            {
                if (!char.IsLetter(c)) { hasNonLetter = true; break; }
            }
            if (!hasNonLetter) return word;

            var buf = new char[word.Length];
            var len = 0;
            foreach (var c in word)
                if (char.IsLetter(c)) buf[len++] = c;
            return new string(buf, 0, len);
        }

        public List<Verse> GetRandomVerses() => GetRandomVersesInternal(0);

        private List<Verse> GetRandomVersesInternal(int depth)
        {
            var verses = new List<Verse>();
            if (depth > 5) return verses;
            try
            {
                if (_connection == null) return verses;
                var allBooks = BibleData.AllBooks;
                if (allBooks.Count == 0) return verses;

                var randomBook = allBooks[Random.Shared.Next(allBooks.Count)];
                var randomChapter = Random.Shared.Next(randomBook.Chapters) + 1;
                var verseCount = GetVerseCount(randomBook.CustomNumber, randomChapter);
                if (verseCount == 0) return GetRandomVersesInternal(depth + 1);

                var numberOfVerses = Math.Min(Random.Shared.Next(5) + 1, verseCount);
                var startVerse = Random.Shared.Next(verseCount - numberOfVerses + 1) + 1;

                ExecuteReader(
                    $"SELECT {ColumnVerse}, {ColumnText} FROM {VersesTable} " +
                    $"WHERE {ColumnBookNumber} = @p0 AND {ColumnChapter} = @p1 " +
                    $"AND {ColumnVerse} >= @p2 AND {ColumnVerse} < @p2 + @p3 " +
                    $"ORDER BY {ColumnVerse} ASC",
                    r =>
                    {
                        while (r.Read())
                        {
                            try
                            {
                                var v = r.GetInt32(r.GetOrdinal(ColumnVerse));
                                var t = r.GetString(r.GetOrdinal(ColumnText));
                                verses.Add(new Verse(v, t, randomBook.Name, randomChapter));
                            }
                            catch { }
                        }
                    },
                    randomBook.CustomNumber, randomChapter, startVerse, numberOfVerses);
            }
            catch { }
            return verses;
        }

        public List<SearchVerse> SearchVerses(
            string query, SearchOptions? options = null,
            bool inverse = false, bool exactPhrase = false)
        {
            var verses = new List<SearchVerse>();
            try
            {
                if (_connection == null) return verses;
                var trimmed = query.Trim();
                if (string.IsNullOrEmpty(trimmed)) return verses;

                var whereParts = new List<string>(4);
                var args = new List<object?>(4);
                var op = inverse ? "NOT LIKE" : "LIKE";

                if (exactPhrase)
                {
                    whereParts.Add($"text {op} @p{args.Count}");
                    args.Add($"%{query}%");
                }
                else
                {
                    foreach (var term in trimmed.Split(TermSplitters, StringSplitOptions.RemoveEmptyEntries))
                    {
                        whereParts.Add($"text {op} @p{args.Count}");
                        args.Add($"%{term}%");
                    }
                }

                int? minBook, maxBook;
                if (options?.BookRange is { } explicitRange)
                {
                    minBook = explicitRange.Item1;
                    maxBook = explicitRange.Item2;
                }
                else
                {
                    (minBook, maxBook) = ResolveScopeRange(options?.Scope);
                }

                if (minBook.HasValue && maxBook.HasValue)
                {
                    if (minBook.Value == maxBook.Value)
                    {
                        whereParts.Add($"book_number = @p{args.Count}");
                        args.Add(minBook.Value);
                    }
                    else
                    {
                        whereParts.Add($"book_number BETWEEN @p{args.Count} AND @p{args.Count + 1}");
                        args.Add(minBook.Value);
                        args.Add(maxBook.Value);
                    }
                }

                var sql = $"SELECT {ColumnBookNumber}, {ColumnChapter}, {ColumnVerse}, {ColumnText} " +
                          $"FROM {VersesTable}" +
                          (whereParts.Count > 0 ? " WHERE " + string.Join(" AND ", whereParts) : "");

                ExecuteReader(sql, r =>
                {
                    while (r.Read())
                    {
                        var bn = r.GetInt32(r.GetOrdinal(ColumnBookNumber));
                        var ch = r.GetInt32(r.GetOrdinal(ColumnChapter));
                        var vn = r.GetInt32(r.GetOrdinal(ColumnVerse));
                        var tx = r.GetString(r.GetOrdinal(ColumnText));
                        var bookName = GetBookInfo(bn)?.Name;
                        verses.Add(new SearchVerse(vn, tx, bn, ch, bookName));
                    }
                }, [.. args]);
            }
            catch { }
            return verses;
        }

        private static (int? min, int? max) ResolveScopeRange(string? scope)
        {
            if (string.IsNullOrEmpty(scope) || scope == SearchScopes.Whole)
                return (null, null);

            if (SearchScopes.IsBookScope(scope))
            {
                var bn = SearchScopes.GetBookNumberFromScope(scope);
                return bn.HasValue ? (bn.Value, bn.Value) : (null, null);
            }

            if (SearchScopes.ScopeRanges.TryGetValue(scope, out var range) && range is not null)
                return (range.Start, range.End);

            return (null, null);
        }

        public void AddBookmark(Verse verse)
        {
            try
            {
                InsertOrIgnore(BookmarksTable, new Dictionary<string, object?>
                {
                    [ColumnBookName] = verse.BookName ?? "",
                    [ColumnChapter] = verse.Chapter ?? 0,
                    [ColumnVerseNumber] = verse.VerseNumber,
                    [ColumnText] = verse.Text,
                });
            }
            catch { }
        }

        public void RemoveBookmark(Verse verse)
        {
            try
            {
                ExecuteNonQuery(
                    $"DELETE FROM {BookmarksTable} " +
                    $"WHERE {ColumnBookName} = @p0 AND {ColumnChapter} = @p1 AND {ColumnVerseNumber} = @p2",
                    verse.BookName ?? "", verse.Chapter ?? 0, verse.VerseNumber);
            }
            catch { }
        }

        public bool IsBookmarked(Verse verse)
        {
            var exists = false;
            try
            {
                ExecuteReader(
                    $"SELECT {ColumnVerseNumber} FROM {BookmarksTable} " +
                    $"WHERE {ColumnBookName} = @p0 AND {ColumnChapter} = @p1 AND {ColumnVerseNumber} = @p2",
                    r => exists = r.Read(),
                    verse.BookName ?? "", verse.Chapter ?? 0, verse.VerseNumber);
            }
            catch { }
            return exists;
        }

        public List<Verse> GetBookmarks()
        {
            var verses = new List<Verse>();
            try
            {
                ExecuteReader(
                    $"SELECT {ColumnBookName}, {ColumnChapter}, {ColumnVerseNumber}, {ColumnText} FROM {BookmarksTable}",
                    r =>
                    {
                        while (r.Read())
                        {
                            verses.Add(new Verse(
                                r.GetInt32(r.GetOrdinal(ColumnVerseNumber)),
                                r.GetString(r.GetOrdinal(ColumnText)),
                                r.GetString(r.GetOrdinal(ColumnBookName)),
                                r.GetInt32(r.GetOrdinal(ColumnChapter))));
                        }
                    });
            }
            catch { }
            return verses;
        }

        public void AddHighlight(Verse verse, int colorArgb)
        {
            try
            {
                InsertOrReplace(HighlightsTable, new Dictionary<string, object?>
                {
                    [ColumnBookName] = verse.BookName ?? "",
                    [ColumnChapter] = verse.Chapter ?? 0,
                    [ColumnVerseNumber] = verse.VerseNumber,
                    [ColumnText] = verse.Text,
                    [ColumnHighlightColor] = colorArgb,
                });
            }
            catch { }
        }

        public Color? GetHighlightColor(Verse verse)
        {
            int? colorInt = null;
            try
            {
                ExecuteReader(
                    $"SELECT {ColumnHighlightColor} FROM {HighlightsTable} " +
                    $"WHERE {ColumnBookName} = @p0 AND {ColumnChapter} = @p1 AND {ColumnVerseNumber} = @p2",
                    r => { if (r.Read() && !r.IsDBNull(0)) colorInt = r.GetInt32(0); },
                    verse.BookName ?? "", verse.Chapter ?? 0, verse.VerseNumber);
            }
            catch { }
            return colorInt.HasValue ? ArgbToColor(colorInt.Value) : null;
        }

        public void RemoveHighlight(Verse verse)
        {
            try
            {
                ExecuteNonQuery(
                    $"DELETE FROM {HighlightsTable} " +
                    $"WHERE {ColumnBookName} = @p0 AND {ColumnChapter} = @p1 AND {ColumnVerseNumber} = @p2",
                    verse.BookName ?? "", verse.Chapter ?? 0, verse.VerseNumber);
            }
            catch { }
        }

        public bool IsHighlighted(Verse verse)
        {
            var exists = false;
            try
            {
                ExecuteReader(
                    $"SELECT {ColumnVerseNumber} FROM {HighlightsTable} " +
                    $"WHERE {ColumnBookName} = @p0 AND {ColumnChapter} = @p1 AND {ColumnVerseNumber} = @p2",
                    r => exists = r.Read(),
                    verse.BookName ?? "", verse.Chapter ?? 0, verse.VerseNumber);
            }
            catch { }
            return exists;
        }

        public void AddOrUpdateNote(string book, int chapter, int startVerse, int endVerse, string element)
        {
            try
            {
                InsertOrReplace(ElementsTable, new Dictionary<string, object?>
                {
                    [ColumnBookName] = book,
                    [ColumnChapter] = chapter,
                    [ColumnStartVerse] = startVerse,
                    [ColumnEndVerse] = endVerse,
                    [ColumnElement] = element,
                    [ColumnTimestamp] = DateTimeOffset.UtcNow.ToUnixTimeSeconds(),
                });
            }
            catch { }
        }

        public string? GetElement(string book, int chapter, int startVerse, int endVerse)
        {
            string? element = null;
            try
            {
                ExecuteReader(
                    $"SELECT {ColumnElement} FROM {ElementsTable} " +
                    $"WHERE {ColumnBookName} = @p0 AND {ColumnChapter} = @p1 " +
                    $"AND {ColumnStartVerse} = @p2 AND {ColumnEndVerse} = @p3",
                    r => { if (r.Read()) element = r.GetString(r.GetOrdinal(ColumnElement)); },
                    book, chapter, startVerse, endVerse);
            }
            catch { }
            return element;
        }

        public void DeleteNote(string book, int chapter, int startVerse, int endVerse)
        {
            try
            {
                ExecuteNonQuery(
                    $"DELETE FROM {ElementsTable} " +
                    $"WHERE {ColumnBookName} = @p0 AND {ColumnChapter} = @p1 " +
                    $"AND {ColumnStartVerse} = @p2 AND {ColumnEndVerse} = @p3",
                    book, chapter, startVerse, endVerse);
            }
            catch { }
        }

        public List<Note> GetAllNotes()
        {
            var notes = new List<Note>();
            try
            {
                ExecuteReader(
                    $"SELECT {ColumnBookName}, {ColumnChapter}, {ColumnStartVerse}, " +
                    $"{ColumnEndVerse}, {ColumnElement}, {ColumnTimestamp} " +
                    $"FROM {ElementsTable} ORDER BY {ColumnTimestamp} DESC",
                    r =>
                    {
                        while (r.Read())
                        {
                            notes.Add(new Note(
                                r.GetString(r.GetOrdinal(ColumnBookName)),
                                r.GetInt32(r.GetOrdinal(ColumnChapter)),
                                r.GetInt32(r.GetOrdinal(ColumnStartVerse)),
                                r.GetInt32(r.GetOrdinal(ColumnEndVerse)),
                                r.GetString(r.GetOrdinal(ColumnElement)),
                                r.GetInt64(r.GetOrdinal(ColumnTimestamp))));
                        }
                    });
            }
            catch { }
            return notes;
        }

        public bool HasElement(Verse verse)
        {
            var exists = false;
            try
            {
                ExecuteReader(
                    $"SELECT {ColumnElement} FROM {ElementsTable} " +
                    $"WHERE {ColumnBookName} = @p0 AND {ColumnChapter} = @p1 " +
                    $"AND {ColumnStartVerse} <= @p2 AND {ColumnEndVerse} >= @p2",
                    r => exists = r.Read(),
                    verse.BookName ?? "", verse.Chapter ?? 0,
                    verse.VerseNumber);
            }
            catch { }
            return exists;
        }

        public string? GetWordDefinition(string word)
        {
            string? definition = null;
            try
            {
                ExecuteReader(
                    "SELECT definition FROM dictionary WHERE LOWER(topic) = @p0",
                    r => { if (r.Read()) definition = r.GetString(r.GetOrdinal("definition")); },
                    word.ToLowerInvariant());
            }
            catch { }
            return definition;
        }

        public (string topic, string definition)? GetWordDefinitionStartingWith(string prefix)
        {
            (string, string)? result = null;
            try
            {
                ExecuteReader(
                    "SELECT topic, definition FROM dictionary " +
                    "WHERE LOWER(topic) LIKE @p0 ORDER BY LENGTH(topic) ASC LIMIT 1",
                    r =>
                    {
                        if (r.Read())
                        {
                            var topic = r.IsDBNull(0) ? "" : r.GetString(0);
                            var def = r.IsDBNull(1) ? "" : r.GetString(1);
                            result = (topic, def);
                        }
                    },
                    prefix.ToLowerInvariant() + "%");
            }
            catch { }
            return result;
        }

        public List<(string topic, string definition)> GetDictionaryCandidatesContaining(
            string word, int limit = 50)
        {
            var results = new List<(string, string)>();
            try
            {
                ExecuteReader(
                    "SELECT topic, definition FROM dictionary " +
                    "WHERE LOWER(topic) LIKE @p0 " +
                    "ORDER BY LENGTH(topic) ASC LIMIT @p1",
                    r =>
                    {
                        while (r.Read())
                        {
                            var topic = r.IsDBNull(0) ? "" : r.GetString(0);
                            var def = r.IsDBNull(1) ? "" : r.GetString(1);
                            results.Add((topic, def));
                        }
                    },
                    "%" + word.ToLowerInvariant() + "%",
                    limit);
            }
            catch { }
            return results;
        }

        public List<(string topic, string definition)> GetDictionaryNeighbours(string word)
        {
            var results = new List<(string, string)>(2);
            var lowerWord = word.ToLowerInvariant();

            try
            {
                ExecuteReader(
                    "SELECT topic, definition FROM dictionary " +
                    "WHERE LOWER(topic) > @p0 " +
                    "ORDER BY LOWER(topic) ASC LIMIT 1",
                    r =>
                    {
                        if (r.Read())
                        {
                            var topic = r.IsDBNull(0) ? "" : r.GetString(0);
                            var def = r.IsDBNull(1) ? "" : r.GetString(1);
                            results.Add((topic, def));
                        }
                    },
                    lowerWord);
            }
            catch { }

            try
            {
                ExecuteReader(
                    "SELECT topic, definition FROM dictionary " +
                    "WHERE LOWER(topic) < @p0 " +
                    "ORDER BY LOWER(topic) DESC LIMIT 1",
                    r =>
                    {
                        if (r.Read())
                        {
                            var topic = r.IsDBNull(0) ? "" : r.GetString(0);
                            var def = r.IsDBNull(1) ? "" : r.GetString(1);
                            results.Add((topic, def));
                        }
                    },
                    lowerWord);
            }
            catch { }

            return results;
        }

        public string? GetStrongDefinition(string word)
        {
            string? result = null;
            try
            {
                ExecuteReader(
                    "SELECT definition FROM dictionary WHERE topic = @p0",
                    r => { if (r.Read()) result = r.GetString(r.GetOrdinal("definition")); },
                    word);
            }
            catch { }
            return result;
        }

        public void AddWordHighlight(Verse verse, int start, int end, int colorArgb)
        {
            try
            {
                InsertOrReplace(WordHighlightsTable, new Dictionary<string, object?>
                {
                    [ColumnBookName] = verse.BookName ?? "",
                    [ColumnChapter] = verse.Chapter ?? 0,
                    [ColumnVerseNumber] = verse.VerseNumber,
                    [ColumnStartPos] = start,
                    [ColumnEndPos] = end,
                    [ColumnHighlightColor] = colorArgb,
                });
            }
            catch { }
        }

        public void RemoveWordHighlight(Verse verse, int start, int end)
        {
            try
            {
                ExecuteNonQuery(
                    $"DELETE FROM {WordHighlightsTable} " +
                    $"WHERE {ColumnBookName} = @p0 AND {ColumnChapter} = @p1 " +
                    $"AND {ColumnVerseNumber} = @p2 AND {ColumnStartPos} = @p3 AND {ColumnEndPos} = @p4",
                    verse.BookName ?? "", verse.Chapter ?? 0, verse.VerseNumber, start, end);
            }
            catch { }
        }

        public List<SelectedWord> GetWordHighlightsForVerse(Verse verse)
        {
            var highlights = new List<SelectedWord>();
            try
            {
                ExecuteReader(
                    $"SELECT {ColumnStartPos}, {ColumnEndPos}, {ColumnHighlightColor} " +
                    $"FROM {WordHighlightsTable} " +
                    $"WHERE {ColumnBookName} = @p0 AND {ColumnChapter} = @p1 AND {ColumnVerseNumber} = @p2",
                    r =>
                    {
                        while (r.Read())
                        {
                            var start = r.GetInt32(r.GetOrdinal(ColumnStartPos));
                            var end = r.GetInt32(r.GetOrdinal(ColumnEndPos));
                            var c = r.GetInt32(r.GetOrdinal(ColumnHighlightColor));
                            highlights.Add(new SelectedWord(verse.VerseNumber, start, end, ArgbToColor(c)));
                        }
                    },
                    verse.BookName ?? "", verse.Chapter ?? 0, verse.VerseNumber);
            }
            catch { }
            return highlights;
        }

        private static Color ArgbToColor(int argb) =>
            Color.FromArgb(
                (byte)((argb >> 24) & 0xFF),
                (byte)((argb >> 16) & 0xFF),
                (byte)((argb >> 8) & 0xFF),
                (byte)(argb & 0xFF));

        public Task<List<Verse>> GetRandomVersesAsync(int count = 1) => Task.Run(() =>
        {
            var random = GetRandomVerses();
            if (random.Count == 0) random = new List<Verse>(GetBookmarks());
            return random.Take(count).ToList();
        });

        public Task<bool> IsBookmarkedAsync(Verse verse)
            => Task.Run(() => IsBookmarked(verse));

        public Task AddBookmarkAsync(Verse verse)
            => Task.Run(() => AddBookmark(verse));

        public Task RemoveBookmarkAsync(Verse verse)
            => Task.Run(() => RemoveBookmark(verse));

        private void ExecuteNonQuery(string sql, params object?[] args)
        {
            if (_connection == null) return;
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            for (int i = 0; i < args.Length; i++)
                cmd.Parameters.AddWithValue($"@p{i}", args[i] ?? DBNull.Value);
            cmd.ExecuteNonQuery();
        }

        private void InsertOrIgnore(string table, Dictionary<string, object?> values)
        {
            var cols = string.Join(", ", values.Keys);
            var pars = string.Join(", ", values.Keys.Select((_, i) => $"@p{i}"));
            ExecuteNonQuery($"INSERT OR IGNORE INTO {table} ({cols}) VALUES ({pars})",
                [.. values.Values]);
        }

        private void InsertOrReplace(string table, Dictionary<string, object?> values)
        {
            var cols = string.Join(", ", values.Keys);
            var pars = string.Join(", ", values.Keys.Select((_, i) => $"@p{i}"));
            ExecuteNonQuery($"INSERT OR REPLACE INTO {table} ({cols}) VALUES ({pars})",
                [.. values.Values]);
        }

        private void ExecuteReader(string sql, Action<SqliteDataReader> readerAction, params object?[] args)
        {
            if (_connection == null) return;
            using var cmd = _connection.CreateCommand();
            cmd.CommandText = sql;
            for (int i = 0; i < args.Length; i++)
                cmd.Parameters.AddWithValue($"@p{i}", args[i] ?? DBNull.Value);
            using var reader = cmd.ExecuteReader();
            readerAction(reader);
        }
    }

    public static class BibleVersionInfoRepository
    {
        public static BibleVersionInfo? GetVersionInfo(string dbName)
        {
            try
            {
                var dbFile = DatabaseHelper.GetDatabaseFilePath(dbName);
                if (!File.Exists(dbFile)) return null;

                using var conn = new SqliteConnection($"Data Source={dbFile};Mode=ReadOnly");
                conn.Open();

                string? description = null, detailedInfo = null;

                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT name, value FROM info WHERE name IN ('description', 'detailed_info')";
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var name = r.GetString(0);
                    var value = r.GetString(1);
                    if (name == "description") description = value;
                    else if (name == "detailed_info") detailedInfo = value;
                }
                return new BibleVersionInfo(description, detailedInfo);
            }
            catch { return null; }
        }
    }

    public abstract record ReferenceResult
    {
        public sealed record Single(Verse Verse, string BookName) : ReferenceResult;
        public sealed record Range(List<Verse> Verses, string BookName) : ReferenceResult;
        public sealed record Chapter(List<Verse> Verses, string BookName) : ReferenceResult;
        public sealed record Invalid : ReferenceResult;
    }

    public static partial class ReferenceFetcher
    {
        [GeneratedRegex(@"^([A-Za-z0-9 ]+?)\s+(\d+)(?::(\d+)(?:-(\d+))?)?$")]
        private static partial Regex ReferencePattern();

        public static List<VerseContent> GetVersesWithSubheadings(
            DatabaseHelper versesHelper, DatabaseHelper subheadingsHelper,
            int bookNumber, int chapter)
        {
            var verses = versesHelper.GetVerses(bookNumber, chapter);
            var subheadings = subheadingsHelper.GetSubheadings(bookNumber, chapter);
            var contentMap = new SortedDictionary<int, List<VerseContent>>();
            foreach (var v in verses)
            {
                if (!contentMap.TryGetValue(v.VerseNumber, out var list))
                    contentMap[v.VerseNumber] = list = new List<VerseContent>();
                list.Add(new VerseContent.VerseVal(v));
            }
            foreach (var s in subheadings)
            {
                if (!contentMap.TryGetValue(s.Verse, out var list))
                    contentMap[s.Verse] = list = [];
                list.Insert(0, new VerseContent.SubheadingVal(s));
            }
            return [.. contentMap.SelectMany(kv => kv.Value)];
        }

        public static ReferenceResult FetchByReference(string reference, DatabaseHelper versesHelper)
        {
            var m = ReferencePattern().Match(reference.Trim());
            if (!m.Success) return new ReferenceResult.Invalid();

            var bookNameInput = m.Groups[1].Value.Trim();
            if (!int.TryParse(m.Groups[2].Value, out var chapter))
                return new ReferenceResult.Invalid();
            int? startVerse = m.Groups[3].Success ? int.Parse(m.Groups[3].Value) : null;
            int? endVerse = m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : null;

            var book = FindClosestBook(bookNameInput);
            if (book is null) return new ReferenceResult.Invalid();

            var chapterVerses = versesHelper.GetVerses(book.CustomNumber, chapter);
            if (chapterVerses.Count == 0) return new ReferenceResult.Invalid();

            Verse Enrich(Verse v) => v with { BookName = book.Name, Chapter = chapter };

            if (startVerse == null)
                return new ReferenceResult.Chapter(chapterVerses.Select(Enrich).ToList(), book.Name);
            if (endVerse == null)
            {
                var found = chapterVerses.FirstOrDefault(v => v.VerseNumber == startVerse.Value);
                return found == null
                    ? new ReferenceResult.Invalid()
                    : new ReferenceResult.Single(Enrich(found), book.Name);
            }
            var range = chapterVerses
                .Where(v => v.VerseNumber >= startVerse.Value && v.VerseNumber <= endVerse.Value)
                .Select(Enrich).ToList();
            return range.Count > 0
                ? new ReferenceResult.Range(range, book.Name)
                : new ReferenceResult.Invalid();
        }

        private static BibleBook? FindClosestBook(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return null;
            var n = input.ToLowerInvariant().Trim();
            var books = BibleData.AllBooks;
            return books.FirstOrDefault(b => b.Name.Equals(n, StringComparison.InvariantCultureIgnoreCase))
                ?? books.FirstOrDefault(b => b.Name.Contains(n, StringComparison.InvariantCultureIgnoreCase))
                ?? books.FirstOrDefault(b => b.Name.StartsWith(n, StringComparison.InvariantCultureIgnoreCase))
                ?? FindFuzzy(n); 
        }

        private static readonly (BibleBook Book, string LowerName)[] LoweredBibleBooks =
        [.. BibleData.AllBooks.Select(b => (b, b.Name.ToLowerInvariant()))];

        private const int FuzzyMatchThreshold = 4;

        private static BibleBook? FindFuzzy(string n)
        {
            BibleBook? best = null;
            var bestScore = FuzzyMatchThreshold + 1;

            foreach (var (book, lowerName) in LoweredBibleBooks)
            {
                if (Math.Abs(lowerName.Length - n.Length) >= bestScore) continue;

                var d = ModalContentHelpers.LevenshteinDistance(n, lowerName);
                if (d < bestScore)
                {
                    bestScore = d;
                    best = book;
                    if (d == 0) break;
                }
            }

            return best;
        }
    }
}