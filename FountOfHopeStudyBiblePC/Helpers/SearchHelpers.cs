using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class SearchHelpers
    {
        public static readonly string[] PopularSearchTerms =
        {
            "faith", "love", "hope", "grace", "peace", "joy", "forgiveness",
            "salvation", "redemption", "righteousness", "sanctification", "holiness",
            "sin", "creation", "God", "Jesus", "Christ", "Savior", "Holy Spirit", "sacrifice"
        };

        private static readonly string[] Palette =
        {
            "#3B82F6", "#EF4444", "#10B981", "#F59E0B", "#8B5CF6",
            "#EC4899", "#06B6D4", "#84CC16", "#F97316", "#6366F1"
        };

        private const string OldTestamentFallbackColor = "#DC2626";
        private const string NewTestamentFallbackColor = "#059669";

        public static string GenerateColorFromString(string s)
        {
            if (string.IsNullOrEmpty(s)) return Palette[0];
            var hash = 0;
            foreach (var c in s)
                hash = c + ((hash << 5) - hash);
            return Palette[(hash & 0x7FFFFFFF) % Palette.Length];
        }

        public static string? GetScopeForBookNumber(int n)
        {
            if (n >= 10 && n <= 50) return SearchScopes.Law;
            if (n >= 60 && n <= 190) return SearchScopes.Historical;
            if (n >= 220 && n <= 260) return SearchScopes.Poetic;
            if (n >= 290 && n <= 340) return SearchScopes.MajorProphets;
            if (n >= 350 && n <= 460) return SearchScopes.MinorProphets;
            if (n >= 470 && n <= 500) return SearchScopes.Gospels;
            if (n == 510) return SearchScopes.HistoricalNt;
            if (n >= 520 && n <= 640) return SearchScopes.PaulineLetters;
            if (n >= 641 && n <= 720) return SearchScopes.Letters;
            if (n == 730) return SearchScopes.Vision;
            return null;
        }

        public static string GetBookColor(string bookName, SearchVerse? verse = null)
        {
            if (verse?.BookColor is { Length: > 0 } explicitColor) return explicitColor;
            if (verse == null) return GenerateColorFromString(bookName);

            var scope = GetScopeForBookNumber(verse.BookNumber);
            if (scope != null && SearchScopes.ScopeColors.TryGetValue(scope, out var scopeColor))
                return scopeColor;

            var book = BibleData.GetBookByCustomNumber(verse.BookNumber);
            return book?.Testament == Testament.Old
                ? OldTestamentFallbackColor
                : NewTestamentFallbackColor;
        }
    }
}