using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static partial class ModalContentHelpers
    {
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

        public static string SanitizeHtmlContent(string? html)
        {
            if (string.IsNullOrEmpty(html)) return "";

            var sanitized = html;

            var ppEnd = sanitized.IndexOf("</pp>", System.StringComparison.Ordinal);
            if (ppEnd != -1)
                sanitized = sanitized[..(ppEnd + "</pp>".Length)];

            sanitized = ScriptRegex().Replace(sanitized, "");
            sanitized = OnAttrDoubleQuoteRegex().Replace(sanitized, "");
            sanitized = OnAttrSingleQuoteRegex().Replace(sanitized, "");
            sanitized = OnAttrBareRegex().Replace(sanitized, "");
            sanitized = JavascriptRegex().Replace(sanitized, "#");
            sanitized = HtmlCommentRegex().Replace(sanitized, "");
            sanitized = StyleRegex().Replace(sanitized, "");
            sanitized = MetaRefreshRegex().Replace(sanitized, "");
            sanitized = GluedInlineTagRegex().Replace(sanitized, "</$1> $2");
            sanitized = NewlineRegex().Replace(sanitized, "<br>");

            return sanitized.Trim();
        }

        public static string PrepareStrongContent(string? rawDefinition)
        {
            var sanitized = SanitizeHtmlContent(rawDefinition);
            var index = sanitized.IndexOf("Derivation", StringComparison.OrdinalIgnoreCase);
            if (index != -1)
                return sanitized[..index] + "<br><br>" + sanitized[index..];
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
                if (cleaned.StartsWith(start, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = LeadingPunctRegex()
                        .Replace(cleaned[start.Length..], "").Trim();
                    break;
                }
            }

            if (cleaned.StartsWith("<", StringComparison.Ordinal))
            {
                var m = WrapperTagRegex().Match(cleaned);
                if (m.Success)
                {
                    var content = m.Groups[2].Value.Trim();
                    if (possibleStarts.Any(s => content.Equals(s, StringComparison.OrdinalIgnoreCase)))
                        cleaned = LeadingPunctRegex()
                            .Replace(cleaned[m.Value.Length..], "").Trim();
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

            if (isExact)
                return SanitizeHtmlContent(pairs[0].definition);

            // Single closest match — title says "Closest match for <searched>",
            // so the body must reveal which word was actually matched.
            if (pairs.Count == 1)
                return $"<b>{Capitalize(pairs[0].topic)}</b><br>" +
                       CleanDefinition(pairs[0].topic, pairs[0].definition);

            // Multiple fuzzy matches — label each block with its own topic.
            return string.Join("<br><hr><br>", pairs.Select(p =>
                $"<b>{Capitalize(p.topic)}</b><br>{CleanDefinition(p.topic, p.definition)}"));
        }

        // ──────────────────────────────────────────────────────────────────
        //  Ported from the Kotlin implementation:
        //   1. LIKE %word% containment search
        //   2. customDistance × 1000 + levenshtein ranking, top 5
        //   3. neighbour fallback for words not contained in any topic
        // ──────────────────────────────────────────────────────────────────
        public static List<(string topic, string definition)>? GetDefinitionOrClosest(
            DatabaseHelper? db, string word)
        {
            if (db == null || string.IsNullOrWhiteSpace(word)) return null;

            var lowerWord = word.Trim().ToLowerInvariant();

            // 1. Exact key — cheapest and most precise.
            var exact = db.GetWordDefinition(lowerWord)
                     ?? db.GetWordDefinition(word);
            if (!string.IsNullOrWhiteSpace(exact))
                return [(word, exact)];

            // 2. Topics that CONTAIN the search term anywhere.
            var candidates = db.GetDictionaryCandidatesContaining(lowerWord, 50);
            if (candidates.Count > 0)
            {
                return candidates
                    .OrderBy(c => CustomDistance(lowerWord, c.topic.ToLowerInvariant()) * 1000
                                + LevenshteinDistance(lowerWord, c.topic.ToLowerInvariant()))
                    .Take(5)
                    .ToList();
            }

            // 3. Nothing contains the word — fall back to alphabetical
            //    neighbours, ranked by edit distance.
            var neighbours = db.GetDictionaryNeighbours(lowerWord);
            if (neighbours.Count == 0) return null;

            return neighbours
                .OrderBy(c => LevenshteinDistance(lowerWord, c.topic.ToLowerInvariant()))
                .ToList();
        }

        public static int LevenshteinDistance(string s1, string s2)
        {
            var len1 = s1.Length;
            var len2 = s2.Length;
            var cost = new int[len1 + 1, len2 + 1];

            for (var i = 0; i <= len1; i++) cost[i, 0] = i;
            for (var j = 0; j <= len2; j++) cost[0, j] = j;

            for (var i = 1; i <= len1; i++)
            {
                for (var j = 1; j <= len2; j++)
                {
                    var match = s1[i - 1] == s2[j - 1] ? 0 : 1;
                    cost[i, j] = Math.Min(
                        Math.Min(cost[i - 1, j] + 1, cost[i, j - 1] + 1),
                        cost[i - 1, j - 1] + match);
                }
            }
            return cost[len1, len2];
        }

        public static int CustomDistance(string word, string topic)
        {
            if (topic.Equals(word, StringComparison.OrdinalIgnoreCase)) return 0;

            var w = word.ToLowerInvariant();
            var t = topic.ToLowerInvariant();

            // Whole-word hit inside a multi-word topic ("propitiation offering")
            var words = t.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            if (words.Contains(w)) return 10 + words.Length;

            var commonPrefixLen = CommonPrefixLength(w, t);
            if (commonPrefixLen >= w.Length - 1)
                return 12 + Math.Max(0, t.Length - w.Length);

            var prefixLen = t.StartsWith(w) ? t.Length - w.Length : int.MaxValue / 2;
            var suffixLen = t.EndsWith(w) ? t.Length - w.Length : int.MaxValue / 2;
            var minAffix = Math.Min(prefixLen, suffixLen);
            if (minAffix < int.MaxValue / 2) return 20 + minAffix;

            if (t.Contains(w)) return 30 + (t.Length - w.Length);

            return 100 + LevenshteinDistance(w, t);
        }

        private static int CommonPrefixLength(string a, string b)
        {
            var len = Math.Min(a.Length, b.Length);
            for (var i = 0; i < len; i++)
                if (a[i] != b[i]) return i;
            return len;
        }

        public static List<VerseCommentary>? GetVerseCommentaries(
            DatabaseHelper? dbHelper, int bookNumber, int chapter, int verse)
        {
            if (dbHelper == null) return null;
            try { return dbHelper.GetCommentariesForVerse(bookNumber, chapter, verse); }
            catch { return null; }
        }

        public static List<Verse> FetchVerses(PassageSelection passage, DatabaseHelper? db)
        {
            if (db == null) return [];

            var result = new List<Verse>();

            var startCh = passage.Chapter;
            var endCh = passage.ChapterEnd ?? passage.Chapter;
            var maxChapters = BibleData.GetBookByCustomNumber(passage.BookNumber)?.Chapters ?? endCh;
            endCh = Math.Min(endCh, maxChapters);

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

        public static PassageSelection? ParseVerseLink(string href, string linkText)
        {
            if (!href.StartsWith("B:", StringComparison.OrdinalIgnoreCase)) return null;
            var body = href[2..].Trim();

            var m = VerseLinkRegex().Match(body);
            if (!m.Success) return null;

            var bookNum = int.Parse(m.Groups[1].Value);
            var chap = int.Parse(m.Groups[2].Value);
            var vStart = int.Parse(m.Groups[3].Value);
            int? vEnd = m.Groups[4].Success ? int.Parse(m.Groups[4].Value) : null;

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
            => string.IsNullOrEmpty(s) ? s : char.ToUpperInvariant(s[0]) + s[1..];
    }
}