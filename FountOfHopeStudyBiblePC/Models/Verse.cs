// Models/Verse.cs
namespace FountOfHopeStudyBiblePC.Models
{
    public class Verse
    {
        public string? BookName { get; set; }
        public int? Chapter { get; set; }
        public int VerseNumber { get; set; }
        public string Text { get; set; } = string.Empty;

        /// <summary>Matched on book/chapter/verse, mirroring the Kotlin data class equality.</summary>
        public override bool Equals(object? obj) =>
            obj is Verse v &&
            v.BookName == BookName &&
            v.Chapter == Chapter &&
            v.VerseNumber == VerseNumber &&
            v.Text == Text;

        public override int GetHashCode() =>
            System.HashCode.Combine(BookName, Chapter, VerseNumber, Text);
    }
}