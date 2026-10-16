// Helpers/SearchScopes.cs
using System.Collections.Generic;
using System.Linq;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class SearchScopes
    {
        // ── Scope keys (Kotlin `SCOPE_*` consts) ─────────────────────
        public const string Whole = "whole";
        public const string OldTestament = "old-testament";
        public const string NewTestament = "new-testament";
        public const string Law = "law";
        public const string Historical = "historical";
        public const string Poetic = "poetic";
        public const string MajorProphets = "major-prophets";
        public const string MinorProphets = "minor-prophets";
        public const string Gospels = "gospels";
        public const string HistoricalNt = "historical-nt";
        public const string PaulineLetters = "pauline-letters";
        public const string Letters = "letters";
        public const string Vision = "vision";

        // ── Individual book scopes ────────────────────────────────────
        public static string CreateBookScope(int bookNumber) => $"book-{bookNumber}";

        public static bool IsBookScope(string scope)
            => scope.StartsWith("book-", System.StringComparison.Ordinal);

        public static int? GetBookNumberFromScope(string scope)
        {
            if (!IsBookScope(scope)) return null;
            var tail = scope.Substring("book-".Length);
            return int.TryParse(tail, out var n) ? n : (int?)null;
        }

        // ── Scope ranges ──────────────────────────────────────────────
        public static IReadOnlyDictionary<string, ScopeRange?> ScopeRanges { get; } =
            new Dictionary<string, ScopeRange?>
            {
                [Whole] = null,
                [OldTestament] = new ScopeRange(10, 460),
                [NewTestament] = new ScopeRange(470, 730),
                [Law] = new ScopeRange(10, 50),
                [Historical] = new ScopeRange(60, 190),
                [Poetic] = new ScopeRange(220, 260),
                [MajorProphets] = new ScopeRange(290, 340),
                [MinorProphets] = new ScopeRange(350, 460),
                [Gospels] = new ScopeRange(470, 500),
                [HistoricalNt] = new ScopeRange(510, 510),
                [PaulineLetters] = new ScopeRange(520, 640),
                [Letters] = new ScopeRange(520, 720),
                [Vision] = new ScopeRange(730, 730),
            };

        // ── Scope colors (used for badges / chips in Search UI) ──────
        public static IReadOnlyDictionary<string, string> ScopeColors { get; } =
            new Dictionary<string, string>
            {
                [Law] = "#e88054",
                [Historical] = "#548fe8",
                [Poetic] = "#E3DA57",
                [MajorProphets] = "#6DF3CE",
                [MinorProphets] = "#fa6e6e",
                [Gospels] = "#45F34A",
                [HistoricalNt] = "#b17df5",
                [PaulineLetters] = "#f5ab7d",
                [Letters] = "#46E0F3",
                [Vision] = "#F3EA92",
            };

        // ── Scope configs (label / description / category) ───────────
        public static IReadOnlyDictionary<string, ScopeConfig> ScopeConfigMap { get; } =
            new Dictionary<string, ScopeConfig>
            {
                [Whole] = new(
                    "Whole Bible",
                    "Include all books (Genesis - Revelation)",
                    "All"),

                [OldTestament] = new(
                    "Old Testament",
                    "Genesis - Malachi",
                    "Old Testament"),

                [NewTestament] = new(
                    "New Testament",
                    "Matthew - Revelation",
                    "New Testament"),

                [Law] = new(
                    "The Law",
                    "Genesis, Exodus, Leviticus, Numbers, Deuteronomy",
                    "Old Testament"),

                [Historical] = new(
                    "Historical Books",
                    "Joshua, Judges, Ruth, Samuel, Kings, Chronicles, Ezra, Nehemiah, Esther",
                    "Old Testament"),

                [Poetic] = new(
                    "Poetic Books",
                    "Job, Psalms, Proverbs, Ecclesiastes, Song of Solomon",
                    "Old Testament"),

                [MajorProphets] = new(
                    "Major Prophets",
                    "Isaiah, Jeremiah, Lamentations, Ezekiel, Daniel",
                    "Old Testament"),

                [MinorProphets] = new(
                    "Minor Prophets",
                    "Hosea, Joel, Amos, Obadiah, Jonah, Micah, Nahum, Habakkuk, Zephaniah, Haggai, Zechariah, Malachi",
                    "Old Testament"),

                [Gospels] = new(
                    "The Gospels",
                    "Matthew, Mark, Luke, John",
                    "New Testament"),

                [HistoricalNt] = new(
                    "Historical Book",
                    "Acts",
                    "New Testament"),

                [PaulineLetters] = new(
                    "Pauline Letters",
                    "Romans, 1 & 2 Corinthians, Galatians, Ephesians, Philippians, Colossians, 1 & 2 Thessalonians, 1 & 2 Timothy, Titus, Philemon",
                    "New Testament"),

                [Letters] = new(
                    "The Letters",
                    "Romans to Jude",
                    "New Testament"),

                [Vision] = new(
                    "The Book of Vision",
                    "Revelation",
                    "New Testament"),
            };

        // ── Individual book scopes (one per book) ─────────────────────
        public static IReadOnlyList<string> IndividualBookScopes { get; } =
            BibleData.BibleBooksMap.Keys
                .Select(CreateBookScope)
                .ToList();

        // ── Scope categories (used by the scope picker UI) ───────────
        public static IReadOnlyDictionary<string, List<string>> ScopeCategories { get; } =
            new Dictionary<string, List<string>>
            {
                ["All"] = new() { Whole },

                ["Old Testament"] = new()
                {
                    OldTestament, Law, Historical, Poetic, MajorProphets, MinorProphets,
                },

                ["New Testament"] = new()
                {
                    NewTestament, Gospels, HistoricalNt, PaulineLetters, Letters, Vision,
                },

                ["Individual Books"] = IndividualBookScopes.ToList(),
            };

        // ── Config lookup (with book fallback) ────────────────────────
        public static ScopeConfig GetScopeConfig(string scope)
        {
            if (IsBookScope(scope))
            {
                var bookNumber = GetBookNumberFromScope(scope);
                if (bookNumber.HasValue)
                {
                    var info = BibleData.GetBookInfo(bookNumber.Value);
                    if (info != null)
                    {
                        return new ScopeConfig(
                            info.Name,
                            $"Include only {info.Name}",
                            "Individual Books");
                    }
                }
                return new ScopeConfig(
                    "Unknown Book",
                    "Include this book",
                    "Individual Books");
            }

            return ScopeConfigMap.TryGetValue(scope, out var cfg)
                ? cfg
                : new ScopeConfig("Unknown", "Unknown scope", "Unknown");
        }
    }
}