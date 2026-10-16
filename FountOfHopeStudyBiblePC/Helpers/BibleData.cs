// Helpers/BibleData.cs
using System.Collections.Generic;
using System.Linq;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class BibleData
    {
        private static readonly List<BibleBook> AllBooksList = new()
        {
            new() { CustomNumber =  10, Name = "Genesis",         Chapters = 50,  Testament = Testament.Old, Abbreviation = "Gen"   },
            new() { CustomNumber =  20, Name = "Exodus",          Chapters = 40,  Testament = Testament.Old, Abbreviation = "Exo"   },
            new() { CustomNumber =  30, Name = "Leviticus",       Chapters = 27,  Testament = Testament.Old, Abbreviation = "Lev"   },
            new() { CustomNumber =  40, Name = "Numbers",         Chapters = 36,  Testament = Testament.Old, Abbreviation = "Num"   },
            new() { CustomNumber =  50, Name = "Deuteronomy",     Chapters = 34,  Testament = Testament.Old, Abbreviation = "Deu"   },
            new() { CustomNumber =  60, Name = "Joshua",          Chapters = 24,  Testament = Testament.Old, Abbreviation = "Josh"  },
            new() { CustomNumber =  70, Name = "Judges",          Chapters = 21,  Testament = Testament.Old, Abbreviation = "Judg"  },
            new() { CustomNumber =  80, Name = "Ruth",            Chapters = 4,   Testament = Testament.Old, Abbreviation = "Ruth"  },
            new() { CustomNumber =  90, Name = "1 Samuel",        Chapters = 31,  Testament = Testament.Old, Abbreviation = "1Sam"  },
            new() { CustomNumber = 100, Name = "2 Samuel",        Chapters = 24,  Testament = Testament.Old, Abbreviation = "2Sam"  },
            new() { CustomNumber = 110, Name = "1 Kings",         Chapters = 22,  Testament = Testament.Old, Abbreviation = "1King" },
            new() { CustomNumber = 120, Name = "2 Kings",         Chapters = 25,  Testament = Testament.Old, Abbreviation = "2King" },
            new() { CustomNumber = 130, Name = "1 Chronicles",    Chapters = 29,  Testament = Testament.Old, Abbreviation = "1Chr"  },
            new() { CustomNumber = 140, Name = "2 Chronicles",    Chapters = 36,  Testament = Testament.Old, Abbreviation = "2Chr"  },
            new() { CustomNumber = 150, Name = "Ezra",            Chapters = 10,  Testament = Testament.Old, Abbreviation = "Ezra"  },
            new() { CustomNumber = 160, Name = "Nehemiah",        Chapters = 13,  Testament = Testament.Old, Abbreviation = "Neh"   },
            new() { CustomNumber = 190, Name = "Esther",          Chapters = 10,  Testament = Testament.Old, Abbreviation = "Esth"  },
            new() { CustomNumber = 220, Name = "Job",             Chapters = 42,  Testament = Testament.Old, Abbreviation = "Job"   },
            new() { CustomNumber = 230, Name = "Psalms",          Chapters = 150, Testament = Testament.Old, Abbreviation = "Psalm" },
            new() { CustomNumber = 240, Name = "Proverbs",        Chapters = 31,  Testament = Testament.Old, Abbreviation = "Prov"  },
            new() { CustomNumber = 250, Name = "Ecclesiastes",    Chapters = 12,  Testament = Testament.Old, Abbreviation = "Eccl"  },
            new() { CustomNumber = 260, Name = "Song of Solomon", Chapters = 8,   Testament = Testament.Old, Abbreviation = "Song"  },
            new() { CustomNumber = 290, Name = "Isaiah",          Chapters = 66,  Testament = Testament.Old, Abbreviation = "Isa"   },
            new() { CustomNumber = 300, Name = "Jeremiah",        Chapters = 52,  Testament = Testament.Old, Abbreviation = "Jer"   },
            new() { CustomNumber = 310, Name = "Lamentations",    Chapters = 5,   Testament = Testament.Old, Abbreviation = "Lam"   },
            new() { CustomNumber = 330, Name = "Ezekiel",         Chapters = 48,  Testament = Testament.Old, Abbreviation = "Ezek"  },
            new() { CustomNumber = 340, Name = "Daniel",          Chapters = 12,  Testament = Testament.Old, Abbreviation = "Dan"   },
            new() { CustomNumber = 350, Name = "Hosea",           Chapters = 14,  Testament = Testament.Old, Abbreviation = "Hos"   },
            new() { CustomNumber = 360, Name = "Joel",            Chapters = 3,   Testament = Testament.Old, Abbreviation = "Joel"  },
            new() { CustomNumber = 370, Name = "Amos",            Chapters = 9,   Testament = Testament.Old, Abbreviation = "Amos"  },
            new() { CustomNumber = 380, Name = "Obadiah",         Chapters = 1,   Testament = Testament.Old, Abbreviation = "Obad"  },
            new() { CustomNumber = 390, Name = "Jonah",           Chapters = 4,   Testament = Testament.Old, Abbreviation = "Jonah" },
            new() { CustomNumber = 400, Name = "Micah",           Chapters = 7,   Testament = Testament.Old, Abbreviation = "Mic"   },
            new() { CustomNumber = 410, Name = "Nahum",           Chapters = 3,   Testament = Testament.Old, Abbreviation = "Nah"   },
            new() { CustomNumber = 420, Name = "Habakkuk",        Chapters = 3,   Testament = Testament.Old, Abbreviation = "Hab"   },
            new() { CustomNumber = 430, Name = "Zephaniah",       Chapters = 3,   Testament = Testament.Old, Abbreviation = "Zeph"  },
            new() { CustomNumber = 440, Name = "Haggai",          Chapters = 2,   Testament = Testament.Old, Abbreviation = "Hag"   },
            new() { CustomNumber = 450, Name = "Zechariah",       Chapters = 14,  Testament = Testament.Old, Abbreviation = "Zech"  },
            new() { CustomNumber = 460, Name = "Malachi",         Chapters = 4,   Testament = Testament.Old, Abbreviation = "Mal"   },
            new() { CustomNumber = 470, Name = "Matthew",         Chapters = 28,  Testament = Testament.New, Abbreviation = "Matt"  },
            new() { CustomNumber = 480, Name = "Mark",            Chapters = 16,  Testament = Testament.New, Abbreviation = "Mark"  },
            new() { CustomNumber = 490, Name = "Luke",            Chapters = 24,  Testament = Testament.New, Abbreviation = "Luke"  },
            new() { CustomNumber = 500, Name = "John",            Chapters = 21,  Testament = Testament.New, Abbreviation = "John"  },
            new() { CustomNumber = 510, Name = "Acts",            Chapters = 28,  Testament = Testament.New, Abbreviation = "Acts"  },
            new() { CustomNumber = 520, Name = "Romans",          Chapters = 16,  Testament = Testament.New, Abbreviation = "Rom"   },
            new() { CustomNumber = 530, Name = "1 Corinthians",   Chapters = 16,  Testament = Testament.New, Abbreviation = "1Cor"  },
            new() { CustomNumber = 540, Name = "2 Corinthians",   Chapters = 13,  Testament = Testament.New, Abbreviation = "2Cor"  },
            new() { CustomNumber = 550, Name = "Galatians",       Chapters = 6,   Testament = Testament.New, Abbreviation = "Gal"   },
            new() { CustomNumber = 560, Name = "Ephesians",       Chapters = 6,   Testament = Testament.New, Abbreviation = "Eph"   },
            new() { CustomNumber = 570, Name = "Philippians",     Chapters = 4,   Testament = Testament.New, Abbreviation = "Phil"  },
            new() { CustomNumber = 580, Name = "Colossians",      Chapters = 4,   Testament = Testament.New, Abbreviation = "Col"   },
            new() { CustomNumber = 590, Name = "1 Thessalonians", Chapters = 5,   Testament = Testament.New, Abbreviation = "1Thes" },
            new() { CustomNumber = 600, Name = "2 Thessalonians", Chapters = 3,   Testament = Testament.New, Abbreviation = "2Thes" },
            new() { CustomNumber = 610, Name = "1 Timothy",       Chapters = 6,   Testament = Testament.New, Abbreviation = "1Tim"  },
            new() { CustomNumber = 620, Name = "2 Timothy",       Chapters = 4,   Testament = Testament.New, Abbreviation = "2Tim"  },
            new() { CustomNumber = 630, Name = "Titus",           Chapters = 3,   Testament = Testament.New, Abbreviation = "Titus" },
            new() { CustomNumber = 640, Name = "Philemon",        Chapters = 1,   Testament = Testament.New, Abbreviation = "Phlm"  },
            new() { CustomNumber = 650, Name = "Hebrews",         Chapters = 13,  Testament = Testament.New, Abbreviation = "Heb"   },
            new() { CustomNumber = 660, Name = "James",           Chapters = 5,   Testament = Testament.New, Abbreviation = "James" },
            new() { CustomNumber = 670, Name = "1 Peter",         Chapters = 5,   Testament = Testament.New, Abbreviation = "1Pet"  },
            new() { CustomNumber = 680, Name = "2 Peter",         Chapters = 3,   Testament = Testament.New, Abbreviation = "2Pet"  },
            new() { CustomNumber = 690, Name = "1 John",          Chapters = 5,   Testament = Testament.New, Abbreviation = "1John" },
            new() { CustomNumber = 700, Name = "2 John",          Chapters = 1,   Testament = Testament.New, Abbreviation = "2John" },
            new() { CustomNumber = 710, Name = "3 John",          Chapters = 1,   Testament = Testament.New, Abbreviation = "3John" },
            new() { CustomNumber = 720, Name = "Jude",            Chapters = 1,   Testament = Testament.New, Abbreviation = "Jude"  },
            new() { CustomNumber = 730, Name = "Revelation",      Chapters = 22,  Testament = Testament.New, Abbreviation = "Rev"   },
        };

        // Fill in the 1..66 standard number from list position.
        private static readonly List<BibleBook> BooksWithStandardNumbers =
            AllBooksList
                .Select((b, i) => b with { StandardNumber = i + 1 })
                .ToList();

        public static IReadOnlyDictionary<int, BibleBook> BibleBooksMap { get; } =
            BooksWithStandardNumbers.ToDictionary(b => b.CustomNumber);

        public static IReadOnlyList<BibleBook> AllBooks { get; } = BooksWithStandardNumbers;
        public static IReadOnlyList<BibleBook> OldTestamentBooks { get; } = BooksWithStandardNumbers.Where(b => b.Testament == Testament.Old).ToList();
        public static IReadOnlyList<BibleBook> NewTestamentBooks { get; } = BooksWithStandardNumbers.Where(b => b.Testament == Testament.New).ToList();

        public static BibleBook? GetBookByCustomNumber(int customNumber)
            => BibleBooksMap.TryGetValue(customNumber, out var book) ? book : null;

        public static BibleBook? GetBookByName(string name)
            => BooksWithStandardNumbers.FirstOrDefault(
                b => string.Equals(b.Name, name, System.StringComparison.OrdinalIgnoreCase));

        /// <summary>Same as the Kotlin top-level `getBookInfo(bookNumber)`.</summary>
        public static BibleBook? GetBookInfo(int bookNumber) => GetBookByCustomNumber(bookNumber);
    }
}