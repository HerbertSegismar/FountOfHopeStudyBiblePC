using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class ModalContentHelpers
    {
        // ── HTML sanitization ───────────────────────────────────────
        public static string SanitizeHtmlContent(string? html)
        {
            if (string.IsNullOrEmpty(html)) return "";
            // Remove <script> blocks
            html = Regex.Replace(html, "<script[\\s\\S]*?</script>", "",
                RegexOptions.IgnoreCase);
            // Convert <br> and <p> to newlines
            html = Regex.Replace(html, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
            html = Regex.Replace(html, "</p>", "\n\n", RegexOptions.IgnoreCase);
            // Strip remaining tags but keep <a href="...">...</a>
            html = Regex.Replace(html, @"<(?!\/?a\b)[^>]+>", "");
            return html.Trim();
        }

        public static string PrepareStrongContent(string? def)
        {
            if (string.IsNullOrEmpty(def)) return "Definition not found.";
            return SanitizeHtmlContent(def);
        }

        public static string CleanDefinition(string word, string definition)
        {
            var clean = SanitizeHtmlContent(definition);
            var idx = clean.IndexOf(word, StringComparison.OrdinalIgnoreCase);
            if (idx > 0) clean = clean.Substring(idx);
            return clean;
        }

        // ── Dictionary lookup ───────────────────────────────────────
        /// <summary>
        /// Returns (topic, definition) pairs. Tries exact match first,
        /// then prefix match, then a LIKE fallback.
        /// </summary>
        public static List<(string topic, string definition)>? GetDefinitionOrClosest(
            DatabaseHelper? db, string word)
        {
            if (db == null || string.IsNullOrWhiteSpace(word)) return null;

            // Exact match (case-insensitive)
            var exact = db.GetWordDefinition(word);
            if (!string.IsNullOrWhiteSpace(exact))
                return new List<(string, string)> { (word, exact) };

            // Prefix match — first 4 letters
            if (word.Length >= 4)
            {
                var prefix = word.Substring(0, 4);
                var hit = db.GetWordDefinitionStartingWith(prefix);
                if (hit.HasValue)
                    return new List<(string, string)> { hit.Value };
            }

            return null;
        }

        // ── Definition content builder ──────────────────────────────
        public static string BuildDefinitionContent(
            string originalWord,
            List<(string topic, string definition)> pairs,
            bool isOxford,
            bool isTopical)
        {
            if (pairs.Count == 0) return "No definition found.";
            if (pairs.Count == 1)
                return SanitizeHtmlContent(pairs[0].definition);

            return string.Join("\n\n──────────\n\n",
                pairs.Select(p =>
                    $"<b>{p.topic}</b>\n" + SanitizeHtmlContent(p.definition)));
        }

        public static List<VerseCommentary>? GetVerseCommentaries(
    DatabaseHelper? dbHelper, int bookNumber, int chapter, int verse)
        {
            if (dbHelper == null)
            {
                System.Diagnostics.Debug.WriteLine("[Commentary] dbHelper is null");
                return null;
            }

            var result = new List<VerseCommentary>();
            try
            {
                // ── Open a fresh connection to the same file ─────────────
                // (Assumes DatabaseHelper's constructor path is AppDomain.CurrentDomain.BaseDirectory
                //  + databaseName, matching the Kotlin asset-copy behaviour.)
                // If DatabaseHelper exposes Database/Connection, use that instead:
                //     var conn = dbHelper.Database;   // reuse
                var dbPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, dbHelper.DatabaseName);

                if (!System.IO.File.Exists(dbPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[Commentary] DB file not found: {dbPath}");
                    return result;
                }

                using var conn = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dbPath}");
                conn.Open();

                using var cmd = conn.CreateCommand();

                // ── CRITICAL: chapter_number_to = 0 must be treated like NULL ──
                cmd.CommandText = @"
            SELECT text,
                   chapter_number_from, verse_number_from,
                   chapter_number_to,   verse_number_to
            FROM commentaries
            WHERE book_number = @book
              AND (chapter_number_from IS NULL OR chapter_number_from = 0
                   OR chapter_number_from <= @chapter)
              AND (chapter_number_to   IS NULL OR chapter_number_to   = 0
                   OR chapter_number_to   >= @chapter)";
                cmd.Parameters.AddWithValue("@book", bookNumber);
                cmd.Parameters.AddWithValue("@chapter", chapter);

                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var text = reader.IsDBNull(0) ? "" : reader.GetString(0);

                    var chFrom = reader.IsDBNull(1) ? 1 : reader.GetInt32(1);
                    if (chFrom <= 0) chFrom = 1;

                    var vFrom = reader.IsDBNull(2) || reader.GetInt32(2) <= 0
                        ? 1 : reader.GetInt32(2);

                    var chTo = reader.IsDBNull(3) ? (int?)null : reader.GetInt32(3);
                    if (chTo is <= 0) chTo = null;

                    var vTo = reader.IsDBNull(4) ? (int?)null : reader.GetInt32(4);
                    if (vTo is <= 0) vTo = null;

                    // ── Single-verse entry: both To fields are null/0 ─────
                    if (chTo == null && vTo == null)
                    {
                        if (chapter == chFrom && verse == vFrom)
                        {
                            result.Add(new VerseCommentary(
                                SanitizeCommentaryText(text), chFrom, vFrom, null, null));
                        }
                        continue;
                    }

                    // ── Chapter range check ──────────────────────────────
                    if (chapter < chFrom) continue;
                    if (chTo is int ct && chapter > ct) continue;

                    // ── Verse range check (only when on the boundary chapter) ──
                    var lowerMet = chapter != chFrom || verse >= vFrom;
                    var upperMet = chTo == null
                                || chapter != chTo
                                || vTo == null
                                || verse <= vTo.Value;

                    if (lowerMet && upperMet)
                    {
                        result.Add(new VerseCommentary(
                            SanitizeCommentaryText(text), chFrom, vFrom, chTo, vTo));
                    }
                }

                System.Diagnostics.Debug.WriteLine(
                    $"[Commentary] book={bookNumber} chapter={chapter} verse={verse} → {result.Count} row(s)");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Commentary] Query failed: {ex}");
            }

            return result;
        }

        private static string SanitizeCommentaryText(string text)
        {
            text = Regex.Replace(text, "<script[\\s\\S]*?</script>", "");
            text = Regex.Replace(text, "</i>(\\w)", "</i> $1", RegexOptions.IgnoreCase);
            text = Regex.Replace(text, "</em>(\\w)", "</em> $1", RegexOptions.IgnoreCase);
            return text;
        }

        // ── Fetch verses for cross-reference targets ────────────────
        public static List<Verse> FetchVerses(PassageSelection passage, DatabaseHelper? db)
        {
            if (db == null) return new List<Verse>();

            var result = new List<Verse>();
            int startCh = passage.Chapter;
            int endCh = passage.ChapterEnd ?? passage.Chapter;
            int startV = passage.Verse ?? 1;
            int endV = passage.VerseEnd ?? startV;

            for (int ch = startCh; ch <= endCh; ch++)
            {
                var all = db.GetVerses(passage.BookNumber, ch);
                foreach (var v in all)
                {
                    if (ch == startCh && v.VerseNumber < startV) continue;
                    if (ch == endCh && v.VerseNumber > endV) continue;
                    result.Add(v with { BookName = passage.BookName, Chapter = ch });
                }
            }
            return result;
        }

        // ── Parse a "B:Book Ch:V" hyperlink href into a PassageSelection ─
        public static PassageSelection? ParseVerseLink(string href, string fallbackText)
        {
            if (!href.StartsWith("B:")) return null;
            var body = href.Substring(2).Trim();

            // Format: "<bookNumber> <chapter>:<verse>[-<verseEnd>]"
            var m = Regex.Match(body, @"^(\d+)\s+(\d+):(\d+)(?:-(\d+))?$");
            if (!m.Success) return null;

            var bookNum = int.Parse(m.Groups[1].Value);
            var chap = int.Parse(m.Groups[2].Value);
            var vStart = int.Parse(m.Groups[3].Value);
            int? vEnd = m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : null;

            var book = BibleData.GetBookByCustomNumber(bookNum);
            return new PassageSelection(
                bookNum,
                book?.Name ?? fallbackText,
                chap,
                vStart)
            {
                VerseEnd = vEnd
            };
        }
    }
}