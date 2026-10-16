namespace FountOfHopeStudyBiblePC.Models
{
    public record Note(
        string BookName,
        int Chapter,
        int StartVerse,
        int EndVerse,
        string NoteText,
        long Timestamp);
}