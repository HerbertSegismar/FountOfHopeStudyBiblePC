namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class InteractiveModalUtils
    {
        public static readonly Dictionary<string, List<string>> DictionariesByLanguage = new()
        {
            ["English"] = ["atsbd", "cbtel", "isbe", "noah", "oxford", "topical", "tcr"],
            ["Arabic"] = ["ardbt"],
            ["Spanish"] = ["eunsa"],
            ["Hebrew"] = ["gesenius"],
            ["Russian"] = ["rbo", "Брокгауз", "ББС"],
            ["Chinese"] = ["白云晓"]
        };

        public static readonly Dictionary<string, string> DictionaryDisplayNames = new()
        {
            ["atsbd"] = "ATSBD",
            ["cbtel"] = "CBTEL",
            ["eunsa"] = "Sagrada Biblia Spanish Dictionary",
            ["isbe"] = "Int'l Standard Bible Encyclopedia",
            ["noah"] = "Noah Webster's Dictionary",
            ["oxford"] = "Oxford Dictionary",
            ["rbo"] = "Modern Russian Translation Dictionary",
            ["topical"] = "Topical Bible Dictionary",
            ["tcr"] = "Thompson Chain Reference",
            ["gesenius"] = "Hebrew and Chaldee Lexicon",
            ["ardbt"] = "Arabic Dictionary of Biblical Theology",
            ["Брокгауз"] = "Библейская Энциклопедия Брокгауза",
            ["ББС"] = "Большой Библейский Словарь",
            ["白云晓"] = "白云晓圣经词典"
        };

        public static readonly List<string> VerseCommentaries = new()
        {
            "cbsc", "spurgeon", "ebc", "fairbairn",
            "hawker", "mhwbc", "scofield", "tsk"
        };

        public static readonly Dictionary<string, string> VerseCommentaryDisplayNames = new()
        {
            ["cbsc"] = "Cambridge Bible Commentary",
            ["spurgeon"] = "Charles Spurgeon's Commentary",
            ["ebc"] = "Expositor's Bible Commentary",
            ["fairbairn"] = "Fairbairn's Typology of Scripture",
            ["hawker"] = "Hawker's Poor Man's Commentary",
            ["mhwbc"] = "Matthew Henry's Commentary",
            ["scofield"] = "Scofield Reference Bible",
            ["tsk"] = "Treasury of Scripture Knowledge"
        };

        public static readonly List<string> CrossReferenceDatabases = ["esv", "niv11", "obx"];

        public static readonly Dictionary<string, string> CrossReferenceDatabaseDisplayNames = new()
        {
            ["esv"] = "References from ESV",
            ["niv11"] = "References from NIV",
            ["obx"] = "References from OpenBible Project"
        };
    }
}