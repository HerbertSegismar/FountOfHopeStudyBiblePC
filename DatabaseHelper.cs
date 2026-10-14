// Services/DatabaseHelper.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Services
{
    /// <summary>
    /// Stub replacement for the Kotlin DatabaseHelper.
    /// Replace the bodies with real SQLite calls — the async surface is identical.
    /// </summary>
    public class DatabaseHelper
    {
        private readonly Random _rng = new();
        private readonly List<Verse> _bookmarks = new();

        private static readonly List<Verse> SeedVerses = new()
        {
            new Verse { BookName = "Psalms", Chapter = 23, VerseNumber = 1, Text = "The Lord is my shepherd; I shall not want." },
            new Verse { BookName = "John", Chapter = 3, VerseNumber = 16, Text = "For God so loved the world, that he gave his only Son..." },
            new Verse { BookName = "Proverbs", Chapter = 3, VerseNumber = 5, Text = "Trust in the Lord with all your heart..." },
            new Verse { BookName = "Romans", Chapter = 8, VerseNumber = 28, Text = "And we know that for those who love God all things work together for good..." },
            new Verse { BookName = "Philippians", Chapter = 4, VerseNumber = 13, Text = "I can do all things through him who strengthens me." },
            new Verse { BookName = "Isaiah", Chapter = 40, VerseNumber = 31, Text = "But they who wait for the Lord shall renew their strength..." },
            new Verse { BookName = "Joshua", Chapter = 1, VerseNumber = 9, Text = "Have I not commanded you? Be strong and courageous..." },
            new Verse { BookName = "Jeremiah", Chapter = 29, VerseNumber = 11, Text = "For I know the plans I have for you, declares the Lord..." }
        };

        public Task<List<Verse>> GetRandomVersesAsync(int count = 1) =>
            Task.Run(() => SeedVerses.OrderBy(_ => _rng.Next()).Take(count).ToList());

        public Task<bool> IsBookmarkedAsync(Verse verse) =>
            Task.Run(() => _bookmarks.Any(b => b.Equals(verse)));

        public Task AddBookmarkAsync(Verse verse) =>
            Task.Run(() =>
            {
                if (!_bookmarks.Any(b => b.Equals(verse))) _bookmarks.Add(verse);
            });

        public Task RemoveBookmarkAsync(Verse verse) =>
            Task.Run(() => _bookmarks.RemoveAll(b => b.Equals(verse)));
    }
}