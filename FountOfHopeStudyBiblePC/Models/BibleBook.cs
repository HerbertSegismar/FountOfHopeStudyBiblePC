namespace FountOfHopeStudyBiblePC.Models
{
    public record BibleBook
    {
        public int CustomNumber { get; init; }
        public string Name { get; init; } = "";
        public int Chapters { get; init; }
        public Testament Testament { get; init; } = Testament.Old;
        public string Abbreviation { get; init; } = "";
        public int StandardNumber { get; init; }
    }
}