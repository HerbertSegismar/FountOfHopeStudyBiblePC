using System.Collections.Generic;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class InteractiveModalUtils
    {
        /// <summary>Language → list of dictionary keys.</summary>
        public static Dictionary<string, List<string>> DictionariesByLanguage { get; } = new()
        {
            ["English"] = new() { "atsbd", "cbtel", "vines", "oxford", "topical", "webster" },
            ["Greek"] = new() { "greekdict" },
            ["Hebrew"] = new() { "hebrewdict" },
            ["Latin"] = new() { "latin" },
        };

        public static Dictionary<string, string> DictionaryDisplayNames { get; } = new()
        {
            ["atsbd"] = "ATS Bible Dictionary",
            ["cbtel"] = "CBTEL Dictionary",
            ["vines"] = "Vine's Expository",
            ["oxford"] = "Oxford Bible Dictionary",
            ["topical"] = "Nave's Topical Bible",
            ["webster"] = "Webster's 1828",
            ["greekdict"] = "Greek Lexicon",
            ["hebrewdict"] = "Hebrew Lexicon",
            ["latin"] = "Latin Dictionary",
        };

        public static List<string> VerseCommentaries { get; } = new()
        {
            "cbsc", "mh", "jfb", "barnes", "clarke", "pnt", "wesley", "calvin"
        };

        public static Dictionary<string, string> VerseCommentaryDisplayNames { get; } = new()
        {
            ["cbsc"] = "Cambridge Bible for Schools and Colleges",
            ["mh"] = "Matthew Henry Commentary",
            ["jfb"] = "Jamieson-Fausset-Brown",
            ["barnes"] = "Barnes' Notes",
            ["clarke"] = "Adam Clarke Commentary",
            ["pnt"] = "People's New Testament",
            ["wesley"] = "John Wesley's Notes",
            ["calvin"] = "Calvin's Commentaries",
        };

        public static List<string> CrossReferenceDatabases { get; } = new()
        {
            "obx", "tsk", "nave", "torrey"
        };

        public static Dictionary<string, string> CrossReferenceDatabaseDisplayNames { get; } = new()
        {
            ["obx"] = "OpenBible Cross References",
            ["tsk"] = "Treasury of Scripture Knowledge",
            ["nave"] = "Nave's Cross References",
            ["torrey"] = "Torrey's Cross References",
        };

        /// <summary>Return the language for a dictionary key, or null.</summary>
        public static string? GetLanguageForDictionary(string dictKey)
        {
            foreach (var kv in DictionariesByLanguage)
                if (kv.Value.Contains(dictKey)) return kv.Key;
            return null;
        }
    }
}