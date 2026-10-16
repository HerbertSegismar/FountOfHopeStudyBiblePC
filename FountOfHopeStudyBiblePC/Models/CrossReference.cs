namespace FountOfHopeStudyBiblePC.Models
{
    public record CrossReference
    {
        public int BookTo { get; init; }
        public int ChapterTo { get; init; }
        public int VerseToStart { get; init; }
        public int VerseToEnd { get; init; }
    }
}