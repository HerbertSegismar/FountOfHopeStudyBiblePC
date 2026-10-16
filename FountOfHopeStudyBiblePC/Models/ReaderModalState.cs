using FountOfHopeStudyBiblePC.Services;

namespace FountOfHopeStudyBiblePC.Models
{
    public abstract record ReaderModalState
    {
        public sealed record None : ReaderModalState;

        public sealed record Word(
            string Term,                       // ← renamed from Word
            string Definition,
            DatabaseHelper? DbHelper,
            bool IsOldTestament,
            bool UseSecondaryDictionary) : ReaderModalState;

        public sealed record Strong(
            string StrongNumber,
            string Definition,
            DatabaseHelper? DbHelper,
            bool IsOldTestament) : ReaderModalState;

        public sealed record Commentary(
            string Title,
            string Content,
            DatabaseHelper? DbHelper,
            bool IsOldTestament) : ReaderModalState;

        public sealed record CrossRef(
            string Source,
            string Content,
            DatabaseHelper? DbHelper,
            int Book,
            int Chapter,
            int Verse,
            bool IsOldTestament) : ReaderModalState;

        public sealed record VerseCommentary(
            int Book,
            int Chapter,
            int Verse,
            DatabaseHelper? DbHelper,
            bool IsOldTestament) : ReaderModalState;
    }
}