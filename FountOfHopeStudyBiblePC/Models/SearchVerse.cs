namespace FountOfHopeStudyBiblePC.Models
{
    public record SearchVerse(
        int VerseNumber,
        string? Text,
        int BookNumber = 0,
        int Chapter = 0,
        string? BookName = null,
        string? BookColor = null);
}