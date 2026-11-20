using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class SearchScopes
    {
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

        private const string BookScopePrefix = "book-";

        public static string CreateBookScope(int bookNumber) => $"{BookScopePrefix}{bookNumber}";

        public static bool IsBookScope(string scope)
            => scope.StartsWith(BookScopePrefix, System.StringComparison.Ordinal);

        public static int? GetBookNumberFromScope(string scope)
        {
            if (!IsBookScope(scope)) return null;
            return int.TryParse(scope.AsSpan(BookScopePrefix.Length), out var n) ? n : null;
        }

        public static IReadOnlyDictionary<string, ScopeRange?> ScopeRanges { get; } =
            new Dictionary<string, ScopeRange?>
            {
                [Whole] = null,
                [OldTestament] = new(10, 460),
                [NewTestament] = new(470, 730),
                [Law] = new(10, 50),
                [Historical] = new(60, 190),
                [Poetic] = new(220, 260),
                [MajorProphets] = new(290, 340),
                [MinorProphets] = new(350, 460),
                [Gospels] = new(470, 500),
                [HistoricalNt] = new(510, 510),
                [PaulineLetters] = new(520, 640),
                [Letters] = new(520, 720),
                [Vision] = new(730, 730),
            };

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

        public static IReadOnlyDictionary<string, ScopeConfig> ScopeConfigMap { get; } =
            new Dictionary<string, ScopeConfig>
            {
                [Whole] = new("Whole Bible", "Include all books (Genesis - Revelation)", "All"),
                [OldTestament] = new("Old Testament", "Genesis - Malachi", "Old Testament"),
                [NewTestament] = new("New Testament", "Matthew - Revelation", "New Testament"),
                [Law] = new("The Law", "Genesis, Exodus, Leviticus, Numbers, Deuteronomy", "Old Testament"),
                [Historical] = new("Historical Books", "Joshua, Judges, Ruth, Samuel, Kings, Chronicles, Ezra, Nehemiah, Esther", "Old Testament"),
                [Poetic] = new("Poetic Books", "Job, Psalms, Proverbs, Ecclesiastes, Song of Solomon", "Old Testament"),
                [MajorProphets] = new("Major Prophets", "Isaiah, Jeremiah, Lamentations, Ezekiel, Daniel", "Old Testament"),
                [MinorProphets] = new("Minor Prophets", "Hosea, Joel, Amos, Obadiah, Jonah, Micah, Nahum, Habakkuk, Zephaniah, Haggai, Zechariah, Malachi", "Old Testament"),
                [Gospels] = new("The Gospels", "Matthew, Mark, Luke, John", "New Testament"),
                [HistoricalNt] = new("Historical Book", "Acts", "New Testament"),
                [PaulineLetters] = new("Pauline Letters", "Romans, 1 & 2 Corinthians, Galatians, Ephesians, Philippians, Colossians, 1 & 2 Thessalonians, 1 & 2 Timothy, Titus, Philemon", "New Testament"),
                [Letters] = new("The Letters", "Romans to Jude", "New Testament"),
                [Vision] = new("The Book of Vision", "Revelation", "New Testament"),
            };

        private static readonly (List<string> Keys, Dictionary<string, ScopeConfig> Configs) BookScopeData =
            BuildBookScopeData();

        private static (List<string>, Dictionary<string, ScopeConfig>) BuildBookScopeData()
        {
            var count = BibleData.BibleBooksMap.Count;
            var keys = new List<string>(count);
            var configs = new Dictionary<string, ScopeConfig>(count);

            foreach (var bookNumber in BibleData.BibleBooksMap.Keys)
            {
                var key = CreateBookScope(bookNumber);
                keys.Add(key);

                var info = BibleData.GetBookInfo(bookNumber);
                configs[key] = info != null
                    ? new ScopeConfig(info.Name, $"Include only {info.Name}", "Individual Books")
                    : new ScopeConfig("Unknown Book", "Include this book", "Individual Books");
            }

            return (keys, configs);
        }

        public static IReadOnlyList<string> IndividualBookScopes => BookScopeData.Keys;

        public static IReadOnlyDictionary<string, IReadOnlyList<string>> ScopeCategories { get; } =
            new Dictionary<string, IReadOnlyList<string>>
            {
                ["All"] = [Whole],
                ["Old Testament"] = [OldTestament, Law, Historical, Poetic, MajorProphets, MinorProphets],
                ["New Testament"] = [NewTestament, Gospels, HistoricalNt, PaulineLetters, Letters, Vision],
                ["Individual Books"] = IndividualBookScopes,
            };

        public static ScopeConfig GetScopeConfig(string scope)
        {
            if (ScopeConfigMap.TryGetValue(scope, out var cfg))
                return cfg;

            if (BookScopeData.Configs.TryGetValue(scope, out var bookCfg))
                return bookCfg;

            if (IsBookScope(scope))
            {
                var bookNumber = GetBookNumberFromScope(scope);
                if (bookNumber.HasValue && BibleData.GetBookInfo(bookNumber.Value) is { } info)
                    return new ScopeConfig(info.Name, $"Include only {info.Name}", "Individual Books");

                return new ScopeConfig("Unknown Book", "Include this book", "Individual Books");
            }

            return new ScopeConfig("Unknown", "Unknown scope", "Unknown");
        }
    }
}