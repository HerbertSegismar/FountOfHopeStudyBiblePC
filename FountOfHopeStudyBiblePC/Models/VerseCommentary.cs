namespace FountOfHopeStudyBiblePC.Models
{
    public record VerseCommentary(
        string Text,
        int ChapterFrom,
        int VerseFrom,
        int? ChapterTo,
        int? VerseTo);
}