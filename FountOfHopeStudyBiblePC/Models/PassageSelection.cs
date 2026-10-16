// Models/PassageSelection.cs
namespace FountOfHopeStudyBiblePC.Models
{
    public class PassageSelection
    {
        public int BookNumber { get; set; }
        public string BookName { get; set; } = "";
        public int Chapter { get; set; }
        public int? Verse { get; set; }
        public int? VerseEnd { get; set; }
        public int? ChapterEnd { get; set; }

        public PassageSelection() { }

        public PassageSelection(int bookNumber, string bookName, int chapter, int? verse = null)
        {
            BookNumber = bookNumber;
            BookName = bookName;
            Chapter = chapter;
            Verse = verse;
        }

        /// <summary>
        /// Kotlin `data class`-style copy helper. Use:
        ///   var next = current.With(verse: 1);
        ///   var ranged = current.With(verseEnd: 13, chapterEnd: 5);
        /// </summary>
        public PassageSelection With(
            int? verse = null,
            int? verseEnd = null,
            int? chapterEnd = null,
            int? chapter = null,
            int? bookNumber = null,
            string? bookName = null)
        {
            return new PassageSelection(
                bookNumber ?? BookNumber,
                bookName ?? BookName,
                chapter ?? Chapter,
                verse ?? Verse)
            {
                VerseEnd = verseEnd ?? VerseEnd,
                ChapterEnd = chapterEnd ?? ChapterEnd,
            };
        }
    }
}