// Models/BookUi.cs
namespace FountOfHopeStudyBiblePC.Models
{
    public record BookUi(
        int BookNumber,
        string LongName,
        string ShortName,
        Testament Testament,
        int TotalChapters);
}