// Helpers/SimpleVerseProcessor.cs
using System.Collections.Generic;
using System.Net;
using System.Text.RegularExpressions;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class SimpleVerseProcessor
    {
        // <f>...</f> — footnotes/commentary, always strip content entirely
        private static readonly Regex FootnoteRegex = new(
            "<f[^>]*>.*?</f>", RegexOptions.Singleline | RegexOptions.Compiled);

        // <S>bare_number</S> — Strong's markers wrapping just a number → strip content
        private static readonly Regex BareStrongsRegex = new(
            @"<S[^>]*>[HG]?\d+</S>", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        // <S .../> — self-closing → strip entirely
        private static readonly Regex SelfClosingStrongsRegex = new(
            @"<S[^>]*/>", RegexOptions.Compiled);

        // remaining <S>/</S> tags — some modules wrap words inside → strip tags only
        private static readonly Regex StrongsTagOnlyRegex = new(
            @"</?S[^>]*>", RegexOptions.Compiled);

        // catch-all for any other tags
        private static readonly Regex AnyTagRegex = new(
            "<[^>]+>", RegexOptions.Compiled);

        private static readonly Regex WhitespaceRegex = new(
            "\\s+", RegexOptions.Compiled);

        public static string StripXmlTags(string text)
        {
            if (string.IsNullOrEmpty(text)) return "";

            var t = FootnoteRegex.Replace(text, "");
            t = BareStrongsRegex.Replace(t, "");
            t = SelfClosingStrongsRegex.Replace(t, "");
            t = StrongsTagOnlyRegex.Replace(t, "");
            t = AnyTagRegex.Replace(t, "");
            t = WebUtility.HtmlDecode(t);
            t = WhitespaceRegex.Replace(t, " ").Trim();
            return t;
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