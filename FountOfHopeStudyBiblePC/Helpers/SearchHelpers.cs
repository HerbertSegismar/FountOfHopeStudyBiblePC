// Helpers/SearchHelpers.cs
using System;
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

        public static string GenerateColorFromString(string s)
        {
            if (string.IsNullOrEmpty(s)) return Palette[0];
            int hash = 0;
            foreach (var c in s)
                hash = c + ((hash << 5) - hash);
            return Palette[Math.Abs(hash) % Palette.Length];
        }

        public static string? GetScopeForBookNumber(int bookNumber)
        {
            foreach (var kv in SearchScopes.ScopeRanges)
            {
                var scope = kv.Key;
                var range = kv.Value;
                if (range == null) continue;
                if (bookNumber < range.Start || bookNumber > range.End) continue;
                if (scope == SearchScopes.Whole ||
                    scope == SearchScopes.OldTestament ||
                    scope == SearchScopes.NewTestament) continue;
                return scope;
            }
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
            return book?.Testament == Testament.Old ? "#DC2626" : "#059669";
        }
    }
}