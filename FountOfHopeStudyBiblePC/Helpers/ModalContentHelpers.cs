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

        // ── Verse commentary lookup ─────────────────────────────────
        public static List<VerseCommentary>? GetVerseCommentaries(
            DatabaseHelper? db, int book, int chapter, int verse)
        {
            if (db == null) return null;
            return db.GetCommentariesForVerse(book, chapter, verse);
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