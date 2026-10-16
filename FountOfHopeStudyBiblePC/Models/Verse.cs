namespace FountOfHopeStudyBiblePC.Models
{
    public record Verse
    {
        public int VerseNumber { get; init; }
        public string Text { get; init; } = "";
        public string? BookName { get; init; }
        public int? Chapter { get; init; }
        public int? BookNumber { get; init; }

        public Verse() { }

        public Verse(int verseNumber, string text)
        {
            VerseNumber = verseNumber;
            Text = text;
        }

        public Verse(int verseNumber, string text, string? bookName, int? chapter, int? bookNumber = null)
        {
            VerseNumber = verseNumber;
            Text = text;
            BookName = bookName;
            Chapter = chapter;
            BookNumber = bookNumber;
        }
    }
}