// Helpers/BookmarkHelper.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class BookmarkHelper
    {
        public static Task<bool> IsBookmarkedAsync(Verse verse, DatabaseHelper dbHelper) =>
            dbHelper.IsBookmarkedAsync(verse);

        public static async Task AddBookmarksAsync(IEnumerable<Verse> verses, DatabaseHelper dbHelper)
        {
            foreach (var verse in verses)
                await dbHelper.AddBookmarkAsync(verse).ConfigureAwait(false);
        }

        public static async Task RemoveBookmarksAsync(IEnumerable<Verse> verses, DatabaseHelper dbHelper)
        {
            foreach (var verse in verses)
                await dbHelper.RemoveBookmarkAsync(verse).ConfigureAwait(false);
        }
    }
}