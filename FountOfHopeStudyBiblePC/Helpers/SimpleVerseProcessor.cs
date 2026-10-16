// Helpers/SimpleVerseProcessor.cs
using System.Collections.Generic;
using System.Text.RegularExpressions;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class SimpleVerseProcessor
    {
        private static readonly Regex FootnoteRegex = new(
            "<f[^>]*>.*?</f>", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex StrongRegex = new(
            "<S[^>]*>.*?</S>", RegexOptions.Singleline | RegexOptions.Compiled);
        private static readonly Regex AnyTagRegex = new(
            "<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex WhitespaceRegex = new(
            "\\s+", RegexOptions.Compiled);

        public static string StripXmlTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var t = FootnoteRegex.Replace(text, "");
            t = StrongRegex.Replace(t, "");
            t = AnyTagRegex.Replace(t, "");
            t = WhitespaceRegex.Replace(t, " ");
            return t.Trim();
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