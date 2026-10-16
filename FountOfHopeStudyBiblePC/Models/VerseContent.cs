namespace FountOfHopeStudyBiblePC.Models
{
    public abstract record VerseContent
    {
        public sealed record SubheadingVal(Subheading Subheading) : VerseContent;
        public sealed record VerseVal(Verse Verse) : VerseContent;
    }
}