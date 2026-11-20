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

        private const int StackAllocMaxWidth = 256;

        public static string SanitizeHtmlContent(string? html)
        {
            if (string.IsNullOrEmpty(html)) return "";

            var sanitized = html;

            var ppEnd = sanitized.IndexOf("</pp>", StringComparison.Ordinal);
            if (ppEnd != -1)
                sanitized = sanitized[..(ppEnd + 5)];

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

            var cleaned = sanitized;
            foreach (var start in new[] { upperTopic, capTopic })
            {
                if (cleaned.StartsWith(start, StringComparison.OrdinalIgnoreCase))
                {
                    cleaned = LeadingPunctRegex()
                        .Replace(cleaned[start.Length..], "").Trim();
                    break;
                }
            }

            if (cleaned.StartsWith('<'))
            {
                var m = WrapperTagRegex().Match(cleaned);
                if (m.Success)
                {
                    var content = m.Groups[2].Value.Trim();
                    if (content.Equals(upperTopic, StringComparison.OrdinalIgnoreCase) ||
                        content.Equals(capTopic, StringComparison.OrdinalIgnoreCase))
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
                && pairs[0].topic.Equals(originalWord, StringComparison.OrdinalIgnoreCase);

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

            if (pairs.Count == 1)
                return $"<b>{Capitalize(pairs[0].topic)}</b><br>" +
                       CleanDefinition(pairs[0].topic, pairs[0].definition);

            return string.Join("<br><hr><br>", pairs.Select(p =>
                $"<b>{Capitalize(p.topic)}</b><br>{CleanDefinition(p.topic, p.definition)}"));
        }

        public static List<(string topic, string definition)>? GetDefinitionOrClosest(
            DatabaseHelper? db, string word)
        {
            if (db == null || string.IsNullOrWhiteSpace(word)) return null;

            var lowerWord = word.Trim().ToLowerInvariant();

            var exact = db.GetWordDefinition(lowerWord);
            if (exact is null && !word.Equals(lowerWord, StringComparison.Ordinal))
                exact = db.GetWordDefinition(word);
            if (!string.IsNullOrWhiteSpace(exact))
                return [(word, exact)];

            var candidates = db.GetDictionaryCandidatesContaining(lowerWord, 50);
            if (candidates.Count > 0)
            {
                var scored = new List<(string topic, string definition, long score)>(candidates.Count);
                foreach (var c in candidates)
                {
                    var lowered = c.topic.ToLowerInvariant();
                    var lev = LevenshteinDistance(lowerWord, lowered);
                    var cd = CustomDistanceLower(lowerWord, lowered, lev);
                    scored.Add((c.topic, c.definition, (long)cd * 1000 + lev));
                }
                scored.Sort(static (a, b) => a.score.CompareTo(b.score));

                var count = Math.Min(5, scored.Count);
                var result = new List<(string topic, string definition)>(count);
                for (int i = 0; i < count; i++)
                    result.Add((scored[i].topic, scored[i].definition));
                return result;
            }

            var neighbours = db.GetDictionaryNeighbours(lowerWord);
            if (neighbours.Count == 0) return null;
            if (neighbours.Count == 1) return neighbours;

            var lev0 = LevenshteinDistance(lowerWord, neighbours[0].topic.ToLowerInvariant());
            var lev1 = LevenshteinDistance(lowerWord, neighbours[1].topic.ToLowerInvariant());
            if (lev1 < lev0) (neighbours[0], neighbours[1]) = (neighbours[1], neighbours[0]);
            return neighbours;
        }

        public static int LevenshteinDistance(string s1, string s2)
        {
            if (s1.Length == 0) return s2.Length;
            if (s2.Length == 0) return s1.Length;

            if (s1.Length > s2.Length)
                (s1, s2) = (s2, s1);

            int len1 = s1.Length, len2 = s2.Length;
            int width = len2 + 1;

            Span<int> prev = width <= StackAllocMaxWidth ? stackalloc int[width] : new int[width];
            Span<int> curr = width <= StackAllocMaxWidth ? stackalloc int[width] : new int[width];

            for (int j = 0; j < width; j++) prev[j] = j;

            for (int i = 1; i <= len1; i++)
            {
                curr[0] = i;
                char c1 = s1[i - 1];
                for (int j = 1; j <= len2; j++)
                {
                    int match = c1 == s2[j - 1] ? 0 : 1;
                    int a = prev[j] + 1;
                    int b = curr[j - 1] + 1;
                    int c = prev[j - 1] + match;
                    int min = a < b ? a : b;
                    if (c < min) min = c;
                    curr[j] = min;
                }

                var temp = prev;
                prev = curr;
                curr = temp;
            }

            return prev[len2];
        }

        public static int CustomDistance(string word, string topic)
        {
            var w = word.ToLowerInvariant();
            var t = topic.ToLowerInvariant();
            return CustomDistanceLower(w, t, -1);
        }

        private static int CustomDistanceLower(string w, string t, int precomputedLev)
        {
            if (t.Equals(w, StringComparison.Ordinal)) return 0;

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

            var lev = precomputedLev >= 0 ? precomputedLev : LevenshteinDistance(w, t);
            return 100 + lev;
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
                        .Replace('–', '-').Replace('—', '-');
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