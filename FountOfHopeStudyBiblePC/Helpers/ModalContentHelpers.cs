using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static partial class ModalContentHelpers
    {
        // ═══════════════════════════════════════════════════════════
        //  Regexes
        // ═══════════════════════════════════════════════════════════
        [GeneratedRegex(@"<script[\s\S]*?</script>", RegexOptions.IgnoreCase)]
        private static partial Regex ScriptRegex();

        [GeneratedRegex(@"\s+on\w+\s*=\s*""[^""]*""")]
        private static partial Regex OnAttrDoubleQuoteRegex();

        [GeneratedRegex(@"\s+on\w+\s*=\s*'[^']*'")]
        private static partial Regex OnAttrSingleQuoteRegex();

        [GeneratedRegex(@"\s+on\w+\s*=[^\s>]+")]
        private static partial Regex OnAttrBareRegex();

        [GeneratedRegex(@"javascript:[^"">]+")]
        private static partial Regex JavascriptRegex();

        [GeneratedRegex(@"<!--[\s\S]*?-->")]
        private static partial Regex HtmlCommentRegex();

        [GeneratedRegex(@"<style[^>]*>[\s\S]*?</style>", RegexOptions.IgnoreCase)]
        private static partial Regex StyleRegex();

        [GeneratedRegex(@"<meta[^>]*http-equiv\s*=\s*['""]?refresh['""]?[^>]*>", RegexOptions.IgnoreCase)]
        private static partial Regex MetaRefreshRegex();

        [GeneratedRegex(@"^[,.:;-]+")]
        private static partial Regex LeadingPunctRegex();

        [GeneratedRegex(@"^<(\w+)>(\s*([\w ]+)\s*)</\1>", RegexOptions.IgnoreCase)]
        private static partial Regex WrapperTagRegex();

        [GeneratedRegex(@"^(\d+)\s+(\d+):(\d+)(?:-(\d+))?$")]
        private static partial Regex VerseLinkRegex();

        [GeneratedRegex(@"</(i|em|b|strong|u|sup|sub)>(\w)", RegexOptions.IgnoreCase)]
        private static partial Regex GluedInlineTagRegex();

        [GeneratedRegex(@"\r\n|\r|\n")]
        private static partial Regex NewlineRegex();

        // ═══════════════════════════════════════════════════════════
        //  HTML sanitization
        // ═══════════════════════════════════════════════════════════
        public static string SanitizeHtmlContent(string? html)
        {
            if (string.IsNullOrEmpty(html)) return "";

            var sanitized = html;

            // 1. Truncate at first </pp> — drops DB-specific tail content
            var ppEnd = sanitized.IndexOf("</pp>", System.StringComparison.Ordinal);
            if (ppEnd != -1)
                sanitized = sanitized.Substring(0, ppEnd + "</pp>".Length);

            // 2. Remove dangerous constructs
            sanitized = ScriptRegex().Replace(sanitized, "");
            sanitized = OnAttrDoubleQuoteRegex().Replace(sanitized, "");
            sanitized = OnAttrSingleQuoteRegex().Replace(sanitized, "");
            sanitized = OnAttrBareRegex().Replace(sanitized, "");
            sanitized = JavascriptRegex().Replace(sanitized, "#");
            sanitized = HtmlCommentRegex().Replace(sanitized, "");
            sanitized = StyleRegex().Replace(sanitized, "");
            sanitized = MetaRefreshRegex().Replace(sanitized, "");

            // 3. Separate inline formatting glued to following text
            //    <b>Header</b>Body  →  <b>Header</b> Body
            sanitized = GluedInlineTagRegex().Replace(sanitized, "</$1> $2");

            // 4. Normalise hard newlines to <br> so WPF renders them like Android
            sanitized = NewlineRegex().Replace(sanitized, "<br>");

            return sanitized.Trim();
        }

        public static string PrepareStrongContent(string? rawDefinition)
        {
            var sanitized = SanitizeHtmlContent(rawDefinition);
            var index = sanitized.IndexOf("Derivation", System.StringComparison.OrdinalIgnoreCase);
            if (index != -1)
                return sanitized.Substring(0, index) + "<br><br>" + sanitized.Substring(index);
            return sanitized;
        }

        public static string CleanDefinition(string topic, string rawDef)
        {
            var sanitized = SanitizeHtmlContent(rawDef);
            var upperTopic = topic.ToUpperInvariant();
            var capTopic = Capitalize(topic);
            var possibleStarts = new[] { upperTopic, capTopic };

            var cleaned = sanitized;
            foreach (var start in possibleStarts)
            {
                if (cleaned.StartsWith(start, System.StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = LeadingPunctRegex()
                        .Replace(cleaned.Substring(start.Length), "").Trim();
                    break;
                }
            }

            if (cleaned.StartsWith("<", System.StringComparison.Ordinal))
            {
                var m = WrapperTagRegex().Match(cleaned);
                if (m.Success)
                {
                    var content = m.Groups[2].Value.Trim();
                    if (possibleStarts.Any(s => content.Equals(s, System.StringComparison.OrdinalIgnoreCase)))
                        cleaned = LeadingPunctRegex()
                            .Replace(cleaned.Substring(m.Value.Length), "").Trim();
                }
            }

            return cleaned;
        }

        public static string FormatDefinitionContent(string word, string content, bool isOxford)
            => isOxford ? $"<b>{word.ToUpperInvariant()}</b><br>{content}" : content;

        public static string BuildDefinitionContent(
            string originalWord,
            List<(string topic, string definition)> pairs,
            bool isOxford,
            bool isTopical)
        {
            if (pairs.Count == 0) return "No definition found.";

            var isExact = pairs.Count == 1
                && pairs[0].topic.Equals(originalWord, System.StringComparison.OrdinalIgnoreCase);

            if (isOxford && !isTopical)
            {
                if (pairs.Count == 1)
                {
                    var topic = pairs[0].topic;
                    var raw = isExact
                        ? SanitizeHtmlContent(pairs[0].definition)
                        : CleanDefinition(topic, pairs[0].definition);
                    return $"<b>{topic.ToUpperInvariant()}</b><br>{raw}";
                }
                return string.Join("<br><hr><br>", pairs.Select(p =>
                    $"<b>{p.topic.ToUpperInvariant()}</b><br>{SanitizeHtmlContent(p.definition)}"));
            }

            string rawContent;
            if (isExact)
                rawContent = SanitizeHtmlContent(pairs[0].definition);
            else if (pairs.Count == 1)
                rawContent = CleanDefinition(pairs[0].topic, pairs[0].definition);
            else
                rawContent = string.Join("<br><hr><br>",
                    pairs.Select(p => SanitizeHtmlContent(p.definition)));

            return rawContent;
        }

        // ═══════════════════════════════════════════════════════════
        //  Dictionary lookup
        // ═══════════════════════════════════════════════════════════
        public static List<(string topic, string definition)>? GetDefinitionOrClosest(
            DatabaseHelper? db, string word)
        {
            if (db == null || string.IsNullOrWhiteSpace(word)) return null;

            var exact = db.GetWordDefinition(word);
            if (!string.IsNullOrWhiteSpace(exact))
                return new List<(string, string)> { (word, exact) };

            if (word.Length >= 4)
            {
                var prefix = word.Substring(0, 4);
                var hit = db.GetWordDefinitionStartingWith(prefix);
                if (hit.HasValue)
                    return new List<(string, string)> { hit.Value };
            }

            return null;
        }

        // ═══════════════════════════════════════════════════════════
        //  Verse commentary
        // ═══════════════════════════════════════════════════════════
        public static List<VerseCommentary>? GetVerseCommentaries(
            DatabaseHelper? dbHelper, int bookNumber, int chapter, int verse)
        {
            if (dbHelper == null) return null;
            try
            {
                return dbHelper.GetCommentariesForVerse(bookNumber, chapter, verse);
            }
            catch (System.Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Commentary] {ex.Message}");
                return null;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Fetch verses for cross-reference targets
        // ═══════════════════════════════════════════════════════════
        public static List<Verse> FetchVerses(PassageSelection passage, DatabaseHelper? db)
        {
            if (db == null) return new List<Verse>();

            var result = new List<Verse>();

            var startCh = passage.Chapter;
            var endCh = passage.ChapterEnd ?? passage.Chapter;
            var maxChapters = BibleData.GetBookByCustomNumber(passage.BookNumber)?.Chapters ?? endCh;
            endCh = System.Math.Min(endCh, maxChapters);

            var startV = passage.Verse ?? 1;
            var spanningChapters = passage.ChapterEnd.HasValue;

            for (int ch = startCh; ch <= endCh; ch++)
            {
                var chapterStartVerse = ch == startCh ? startV : 1;
                int chapterEndVerse;
                if (ch == endCh)
                {
                    if (passage.VerseEnd.HasValue)
                        chapterEndVerse = passage.VerseEnd.Value;
                    else if (spanningChapters)
                        chapterEndVerse = db.GetVerseCount(passage.BookNumber, ch);
                    else
                        chapterEndVerse = chapterStartVerse;
                }
                else
                {
                    chapterEndVerse = db.GetVerseCount(passage.BookNumber, ch);
                }

                foreach (var v in db.GetVerses(passage.BookNumber, ch))
                {
                    if (v.VerseNumber < chapterStartVerse) continue;
                    if (v.VerseNumber > chapterEndVerse) continue;
                    result.Add(v with { BookName = passage.BookName, Chapter = ch });
                }
            }
            return result;
        }

        // ═══════════════════════════════════════════════════════════
        //  Parse "B:<book> <chapter>:<verse>[-<verseEnd>]" href
        // ═══════════════════════════════════════════════════════════
        public static PassageSelection? ParseVerseLink(string href, string linkText)
        {
            if (!href.StartsWith("B:", System.StringComparison.OrdinalIgnoreCase)) return null;
            var body = href.Substring(2).Trim();

            var m = VerseLinkRegex().Match(body);
            if (!m.Success) return null;

            var bookNum = int.Parse(m.Groups[1].Value);
            var chap = int.Parse(m.Groups[2].Value);
            var vStart = int.Parse(m.Groups[3].Value);
            int? vEnd = m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : null;

            // If href has no range, try to extract it from the visible link text
            if (vEnd == null)
            {
                var textParts = linkText.Split(':');
                if (textParts.Length >= 2)
                {
                    var textVersePart = textParts[^1].Trim()
                        .Replace("–", "-").Replace("—", "-");
                    if (textVersePart.Contains('-'))
                    {
                        var rangeParts = textVersePart.Split('-');
                        if (rangeParts.Length == 2)
                        {
                            var startFromText = int.TryParse(rangeParts[0].Trim(), out var sf) ? sf : (int?)null;
                            var endFromText = int.TryParse(rangeParts[1].Trim(), out var ef) ? ef : (int?)null;
                            if (startFromText == vStart && endFromText != null)
                                vEnd = endFromText;
                        }
                    }
                }
            }

            var book = BibleData.GetBookByCustomNumber(bookNum);
            return new PassageSelection(bookNum, book?.Name ?? "", chap, vStart)
            {
                VerseEnd = vEnd
            };
        }

        private static string Capitalize(string s)
            => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s.Substring(1);
    }
}