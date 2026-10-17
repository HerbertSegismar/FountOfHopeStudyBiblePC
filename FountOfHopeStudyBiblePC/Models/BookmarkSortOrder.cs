// Models/BookmarkSortOrder.cs
namespace FountOfHopeStudyBiblePC.Models
{
    public enum BookmarkSortOrder
    {
        ByBook,
        ByChapter,
        RecentlyAdded
    }

    public static class BookmarkSortOrderExtensions
    {
        public static string DisplayName(this BookmarkSortOrder order) => order switch
        {
            BookmarkSortOrder.ByBook => "By Book",
            BookmarkSortOrder.ByChapter => "By Chapter",
            BookmarkSortOrder.RecentlyAdded => "Recently Added",
            _ => "Recently Added"
        };
    }
}