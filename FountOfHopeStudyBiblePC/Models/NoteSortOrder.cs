namespace FountOfHopeStudyBiblePC.Models
{
    public enum NoteSortOrder
    {
        DateNewest,
        DateOldest,
        Book,
        Chapter
    }

    public static class NoteSortOrderExtensions
    {
        public static string DisplayName(this NoteSortOrder order) => order switch
        {
            NoteSortOrder.DateNewest => "Newest First",
            NoteSortOrder.DateOldest => "Oldest First",
            NoteSortOrder.Book => "By Book",
            NoteSortOrder.Chapter => "By Chapter",
            _ => "Newest First"
        };
    }
}