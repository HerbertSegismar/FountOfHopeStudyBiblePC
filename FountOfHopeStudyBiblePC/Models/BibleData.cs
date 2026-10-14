// Models/BibleData.cs
using System.Collections.Generic;
using System.Linq;

namespace FountOfHopeStudyBiblePC.Models
{
    /// <summary>Minimal book-metadata lookup, mirroring the Kotlin `BibleData` helper.</summary>
    public static class BibleData
    {
        public record BookInfo(string Name, int CustomNumber);

        private static readonly List<BookInfo> Books = new()
        {
            new("Genesis", 1), new("Exodus", 2), new("Leviticus", 3), new("Numbers", 4),
            new("Deuteronomy", 5), new("Joshua", 6), new("Judges", 7), new("Ruth", 8),
            new("1 Samuel", 9), new("2 Samuel", 10), new("1 Kings", 11), new("2 Kings", 12),
            new("Psalms", 19), new("Proverbs", 20), new("Isaiah", 23), new("Jeremiah", 24),
            new("Daniel", 27), new("Micah", 33), new("Zechariah", 38), new("Malachi", 39),
            new("Matthew", 40), new("Mark", 41), new("Luke", 42), new("John", 43),
            new("Acts", 44), new("Romans", 45), new("1 Corinthians", 46), new("Galatians", 48),
            new("Ephesians", 49), new("Philippians", 50), new("1 Thessalonians", 52),
            new("Hebrews", 58), new("Revelation", 66)
        };

        public static BookInfo? GetBookByName(string name) =>
            Books.FirstOrDefault(b => string.Equals(b.Name, name, System.StringComparison.OrdinalIgnoreCase));
    }
}