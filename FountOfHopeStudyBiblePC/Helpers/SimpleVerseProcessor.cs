using System.Net;
using System.Text.RegularExpressions;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static partial class SimpleVerseProcessor
    {
        [GeneratedRegex("<f[^>]*>.*?</f>", RegexOptions.Singleline)]
        private static partial Regex FootnoteRegex();

        [GeneratedRegex(@"<S[^>]*>[HG]?\d+</S>", RegexOptions.IgnoreCase)]
        private static partial Regex BareStrongsRegex();

        [GeneratedRegex("<[^>]+>")]
        private static partial Regex AnyTagRegex();

        [GeneratedRegex("\\s+")]
        private static partial Regex WhitespaceRegex();

        public static string StripXmlTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var t = text;

            if (t.IndexOf('<') >= 0)
            {
                t = FootnoteRegex().Replace(t, "");
                t = BareStrongsRegex().Replace(t, "");
                t = AnyTagRegex().Replace(t, "");
            }

            if (t.IndexOf('&') >= 0)
                t = WebUtility.HtmlDecode(t);

            return WhitespaceRegex().Replace(t, " ").Trim();
        }

        public static string ExtractVerseReference(List<Verse> verses)
        {
            if (verses == null || verses.Count == 0) return "";
            var first = verses[0];
            return verses.Count == 1
                ? $"{first.BookName} {first.Chapter}:{first.VerseNumber}"
                : $"{first.BookName} {first.Chapter}:{first.VerseNumber}-{verses[^1].VerseNumber}";
        }
    }
}