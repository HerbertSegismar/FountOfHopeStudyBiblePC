// Helpers/BibleVersionUtils.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class BibleVersionUtils
    {
        // ═══════════════════════════════════════════════════════════════
        //  Short abbreviations (versionMap)
        // ═══════════════════════════════════════════════════════════════
        public static Dictionary<string, string> VersionMap { get; } =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["afr.sqlite3"] = "AFR",
                ["ampc.sqlite3"] = "AMPC",
                ["avd.sqlite3"] = "AVD",
                ["bfc.sqlite3"] = "BFC",
                ["blp.sqlite3"] = "BLP",
                ["bm96.sqlite3"] = "BM96",
                ["bmdc.sqlite3"] = "BMDC",
                ["cebB.sqlite3"] = "CEBB",
                ["cjb.sqlite3"] = "CJB",
                ["csb17.sqlite3"] = "CSB17",
                ["cuv+.sqlite3"] = "CUV+",
                ["cuv23.sqlite3"] = "CUV23",
                ["esv.sqlite3"] = "ESV",
                ["esvgsb.sqlite3"] = "ESVGSB",
                ["eunsa.sqlite3"] = "EUNSA",
                ["fpb.sqlite3"] = "FPB",
                ["hbsi.sqlite3"] = "HBSI",
                ["hilab82.sqlite3"] = "HILAB",
                ["hind.sqlite3"] = "HIND",
                ["iesv+.sqlite3"] = "IESV+",
                ["ihot+.sqlite3"] = "IHOT+",
                ["jcb.sqlite3"] = "JCB",
                ["jnitb.sqlite3"] = "JNITB",
                ["kj2.sqlite3"] = "KJ2",
                ["kjv+.sqlite3"] = "KJV+",
                ["kjvru.sqlite3"] = "KJVRU",
                ["knrsv.sqlite3"] = "KNRSV",
                ["kor.sqlite3"] = "KOR",
                ["lccmn.sqlite3"] = "LCCMN",
                ["logos.sqlite3"] = "Logos",
                ["mbb05.sqlite3"] = "MBB",
                ["mhb.sqlite3"] = "MHB",
                ["mhbc.sqlite3"] = "MHBC",
                ["nasb+.sqlite3"] = "NASB+",
                ["nfc19.sqlite3"] = "NFC19",
                ["niv11.sqlite3"] = "NIV11",
                ["nkjv.sqlite3"] = "NKJV",
                ["nlt15.sqlite3"] = "NLT15",
                ["nlv11.sqlite3"] = "NLV11",
                ["nvb.sqlite3"] = "NVB",
                ["ojb.sqlite3"] = "OJB",
                ["rnksv.sqlite3"] = "RNKSV",
                ["sab.sqlite3"] = "SAB",
                ["sfilos.sqlite3"] = "SFILOS",
                ["tagab01.sqlite3"] = "TAGAB",
                ["tjb.sqlite3"] = "TJB",
                ["vmd.sqlite3"] = "VMD",
                ["wmb.sqlite3"] = "WMB",
                ["woy.sqlite3"] = "WOY",
                ["ylt.sqlite3"] = "YLT",
                ["УБД96.sqlite3"] = "УБД96",
                ["РБО2.sqlite3"] = "РБО2",
                ["tncv.sqlite3"] = "TNCV",
                ["tsv11.sqlite3"] = "TSV",
                ["ripv.sqlite3"] = "RIPV",
                ["bdk.sqlite3"] = "BDK",
                ["pmpv94.sqlite3"] = "PMPV94",
                ["cebfb11.sqlite3"] = "CEBFB11",
                ["asnd.sqlite3"] = "ASND",
                ["hlgn.sqlite3"] = "HLGN",
                ["bpv.sqlite3"] = "BPV",
                ["cuv2s+.sqlite3"] = "CUV2S+",
                ["jf+.sqlite3"] = "JF+",
            };

        // ═══════════════════════════════════════════════════════════════
        //  Full descriptions (descriptionMap)
        // ═══════════════════════════════════════════════════════════════
        public static Dictionary<string, string> DescriptionMap { get; } =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["afr.sqlite3"] = "Bible Afrikaans",
                ["ampc.sqlite3"] = "Amplified Bible Classic Edition",
                ["avd.sqlite3"] = "New Van Dyck Bible",
                ["bfc.sqlite3"] = "La Bible en Français Courant",
                ["blp.sqlite3"] = "Biblia La Palabra",
                ["bm96.sqlite3"] = "Alkitab Berita Baik",
                ["bmdc.sqlite3"] = "Alkitab Berita Baik + Deuterokanonika",
                ["cebB.sqlite3"] = "Cebuano Bible",
                ["cjb.sqlite3"] = "Complete Jewish Bible",
                ["csb17.sqlite3"] = "Christian Standard Bible",
                ["cuv+.sqlite3"] = "Chinese Union Version",
                ["jf+.sqlite3"] = "Chinese Union Version",
                ["cuv2s+.sqlite3"] = "Chinese Union Version Cross References",
                ["cuv23.sqlite3"] = "Ukrainian Contemporary Version",
                ["esv.sqlite3"] = "English Standard Version",
                ["esvgsb.sqlite3"] = "ESV Global Study Bible",
                ["eunsa.sqlite3"] = "Sagrada Biblia",
                ["fpb.sqlite3"] = "Filos Pergamos Bible",
                ["hbsi.sqlite3"] = "पवित्र बाइबिल",
                ["hilab82.sqlite3"] = "Hiligaynon Ang Biblia 1982",
                ["hind.sqlite3"] = "पवित्र बाइबिल",
                ["iesv+.sqlite3"] = "Greek-English Interlinear NT",
                ["ihot+.sqlite3"] = "Hebrew-English Interlinear OT",
                ["jcb.sqlite3"] = "リビングバイブル",
                ["jnitb.sqlite3"] = "新共同訳聖書",
                ["kj2.sqlite3"] = "King James 2000",
                ["kjv+.sqlite3"] = "King James Version 1769",
                ["kjvru.sqlite3"] = "King James Bible in Russian",
                ["knrsv.sqlite3"] = "평양말 성경",
                ["kor.sqlite3"] = "성경 - Korean Bible 2010",
                ["lccmn.sqlite3"] = "Lời Chúa Cho Mọi Người",
                ["logos.sqlite3"] = "Logos Bible",
                ["mbb05.sqlite3"] = "Magandang Balita Biblia 2005",
                ["mhb.sqlite3"] = "Modern Hebrew Bible with Vowels",
                ["mhbc.sqlite3"] = "Modern Hebrew Bible (Consonants)",
                ["nasb+.sqlite3"] = "New American Standard Bible",
                ["nfc19.sqlite3"] = "Nouvelle Français Courant",
                ["niv11.sqlite3"] = "New International Version",
                ["nkjv.sqlite3"] = "New King James Version",
                ["nlt15.sqlite3"] = "New Living Translation",
                ["nlv11.sqlite3"] = "Nuwe Lewende Vertaling",
                ["nvb.sqlite3"] = "New Vietnamese Bible",
                ["ojb.sqlite3"] = "Orthodox Jewish Bible",
                ["rnksv.sqlite3"] = "Revised New Korean Standard Version",
                ["sab.sqlite3"] = "Sharif Arabic Bible",
                ["sfilos.sqlite3"] = "Σπύρος Φίλος",
                ["tagab01.sqlite3"] = "Tagalog Biblia 2001",
                ["tjb.sqlite3"] = "Terjemahan Baru",
                ["vmd.sqlite3"] = "Versi Mudah Dibaca",
                ["wmb.sqlite3"] = "World Messianic Bible",
                ["woy.sqlite3"] = "Word of Yahweh",
                ["ylt.sqlite3"] = "Young's Literal Translation",
                ["УБД96.sqlite3"] = "The Holy Bible in Ukrainian",
                ["РБО2.sqlite3"] = "Библия. Современный русский перевод",
                ["tncv.sqlite3"] = "Thai New Contemporary Version",
                ["tsv11.sqlite3"] = "Thai Standard Version Revision",
                ["ripv.sqlite3"] = "Ti Baro a Naimbag a Damag Biblia",
                ["bdk.sqlite3"] = "Buuk Do Kinorohingan",
                ["pmpv94.sqlite3"] = "Ing Mayap a Balita Biblia",
                ["cebfb11.sqlite3"] = "Cebuano Form-Based Bible",
                ["asnd.sqlite3"] = "Ang Salita ng Dios",
                ["hlgn.sqlite3"] = "Ang Pulong Sang Dios",
                ["bpv.sqlite3"] = "An Marahay Na Bareta Biblia",
            };

        // ═══════════════════════════════════════════════════════════════
        //  Language map (versionLanguage)
        // ═══════════════════════════════════════════════════════════════
        private static Dictionary<string, string> VersionLanguage { get; } =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["afr.sqlite3"] = "Afrikaans",
                ["ampc.sqlite3"] = "English",
                ["avd.sqlite3"] = "Arabic",
                ["bfc.sqlite3"] = "French",
                ["blp.sqlite3"] = "Spanish",
                ["bm96.sqlite3"] = "Malay",
                ["bmdc.sqlite3"] = "Malay",
                ["cebB.sqlite3"] = "Cebuano",
                ["cebfb11.sqlite3"] = "Cebuano",
                ["cjb.sqlite3"] = "English Messianic",
                ["csb17.sqlite3"] = "English",
                ["cuv+.sqlite3"] = "Chinese",
                ["jf+.sqlite3"] = "Chinese",
                ["cuv2s+.sqlite3"] = "Chinese",
                ["cuv23.sqlite3"] = "Ukrainian",
                ["esv.sqlite3"] = "English",
                ["esvgsb.sqlite3"] = "English",
                ["eunsa.sqlite3"] = "Spanish",
                ["fpb.sqlite3"] = "Greek",
                ["hbsi.sqlite3"] = "Hindi",
                ["hilab82.sqlite3"] = "Hiligaynon",
                ["hlgn.sqlite3"] = "Hiligaynon",
                ["hind.sqlite3"] = "Hindi",
                ["iesv+.sqlite3"] = "Greek/Hebrew Interlinear",
                ["ihot+.sqlite3"] = "Greek/Hebrew Interlinear",
                ["jcb.sqlite3"] = "Japanese",
                ["jnitb.sqlite3"] = "Japanese",
                ["kj2.sqlite3"] = "English",
                ["kjv+.sqlite3"] = "English",
                ["kjvru.sqlite3"] = "Russian",
                ["knrsv.sqlite3"] = "Korean",
                ["kor.sqlite3"] = "Korean",
                ["lccmn.sqlite3"] = "Vietnamese",
                ["logos.sqlite3"] = "English",
                ["mhb.sqlite3"] = "Hebrew",
                ["mhbc.sqlite3"] = "Hebrew",
                ["nasb+.sqlite3"] = "English",
                ["nfc19.sqlite3"] = "French",
                ["niv11.sqlite3"] = "English",
                ["nkjv.sqlite3"] = "English",
                ["nlt15.sqlite3"] = "English",
                ["nlv11.sqlite3"] = "Afrikaans",
                ["nvb.sqlite3"] = "Vietnamese",
                ["ojb.sqlite3"] = "English Messianic",
                ["rnksv.sqlite3"] = "Korean",
                ["sab.sqlite3"] = "Arabic",
                ["sfilos.sqlite3"] = "Greek",
                ["tagab01.sqlite3"] = "Tagalog",
                ["asnd.sqlite3"] = "Tagalog",
                ["mbb05.sqlite3"] = "Tagalog",
                ["tjb.sqlite3"] = "Indonesian",
                ["vmd.sqlite3"] = "Indonesian",
                ["wmb.sqlite3"] = "English Messianic",
                ["woy.sqlite3"] = "English Messianic",
                ["ylt.sqlite3"] = "English",
                ["УБД96.sqlite3"] = "Ukrainian",
                ["РБО2.sqlite3"] = "Russian",
                ["tncv.sqlite3"] = "Thai",
                ["tsv11.sqlite3"] = "Thai",
                ["ripv.sqlite3"] = "Ilocano",
                ["bdk.sqlite3"] = "Ilocano",
                ["pmpv94.sqlite3"] = "Kapampangan",
                ["bpv.sqlite3"] = "Bikolano",
            };

        // ═══════════════════════════════════════════════════════════════
        //  Lookups
        // ═══════════════════════════════════════════════════════════════
        public static string GetAbbreviation(string dbFile)
            => VersionMap.TryGetValue(dbFile, out var abbr) ? abbr : "Bible";

        public static string GetDescriptionForVersion(string dbFile)
            => DescriptionMap.TryGetValue(dbFile, out var desc) ? desc : "Bible translation";

        public static string GetLanguageForVersion(string key)
            => VersionLanguage.TryGetValue(key, out var lang) ? lang : "English";

        // ═══════════════════════════════════════════════════════════════
        //  Grouped-by-language (for the version picker UI)
        // ═══════════════════════════════════════════════════════════════
        /// <summary>
        /// Returns each language with its enabled versions, in the order:
        ///   1. English
        ///   2. English Messianic
        ///   3. Remaining languages in alphabetical order
        /// Within each language, versions are sorted by their short name.
        /// </summary>
        public static SortedDictionary<string, List<(string dbFile, string shortName)>>
            GetFilteredVersionsGroupedByLanguage(HashSet<string> disabledVersions)
        {
            var enabledMap = VersionMap
                .Where(kv => !disabledVersions.Contains(kv.Key))
                .ToList();

            var groups = new Dictionary<string, List<(string dbFile, string shortName)>>();

            foreach (var kv in enabledMap)
            {
                var lang = VersionLanguage.TryGetValue(kv.Key, out var l) ? l : "Other";
                if (!groups.TryGetValue(lang, out var list))
                    groups[lang] = list = new List<(string, string)>();
                list.Add((kv.Key, kv.Value));
            }

            // Sort each language's versions by short name
            foreach (var list in groups.Values)
                list.Sort((a, b) => string.CompareOrdinal(a.shortName, b.shortName));

            // Build the preferred language order
            var preferredOrder = new List<string> { "English", "English Messianic" };
            preferredOrder.AddRange(
                groups.Keys
                    .Where(k => k != "English" && k != "English Messianic")
                    .OrderBy(k => k, StringComparer.Ordinal));

            var result = new SortedDictionary<string, List<(string, string)>>(
                Comparer<string>.Create((a, b) =>
                {
                    var ia = preferredOrder.IndexOf(a);
                    var ib = preferredOrder.IndexOf(b);
                    if (ia < 0) ia = int.MaxValue;
                    if (ib < 0) ib = int.MaxValue;
                    return ia.CompareTo(ib);
                }));

            foreach (var kv in groups)
                result[kv.Key] = kv.Value;

            return result;
        }
    }
}