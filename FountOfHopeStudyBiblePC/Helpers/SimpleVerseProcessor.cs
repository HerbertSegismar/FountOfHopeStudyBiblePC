// Helpers/SimpleVerseProcessor.cs
using System.Text.RegularExpressions;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class SimpleVerseProcessor
    {
        private static readonly Regex XmlTagRegex = new(@"<[^>]+>", RegexOptions.Compiled);

        public static string StripXmlTags(string? input) =>
            string.IsNullOrEmpty(input) ? string.Empty : XmlTagRegex.Replace(input, string.Empty).Trim();
    }
}
