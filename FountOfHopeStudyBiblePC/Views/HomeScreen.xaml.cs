// Views/HomeScreen.xaml.cs
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FountOfHopeStudyBiblePC.Controls;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Views
{
    public partial class HomeScreen : UserControl
    {
        // ── Dependencies ────────────────────────────────────────────────────
        private readonly AppViewModel _viewModel;
        private readonly Action<PassageSelection> _onNavigateToReader;
        private readonly Action<Screen> _onNavigateToScreen;
        private readonly DatabaseHelper? _databaseHelper;

        private readonly Action _onTakeBibleQuizClick;
        private readonly Action _onLearnHebrewClick;
        private readonly Action _onLearnGreekClick;
        private readonly Action _onOpenDictionaryClick;
        private readonly Action _onOpenAnimatorClick;
        private readonly Action _onOpenGenerationsClick;

        private readonly Random _rng = new();

        // ── State ───────────────────────────────────────────────────────────
        private List<Verse>? _dailyVerses;
        private bool _isBookmarked;

        /// <summary>Bound from XAML via ElementName=HomeRoot.</summary>
        public ObservableCollection<QuickAction> QuickActions { get; } = new();

        public ObservableCollection<PopularDevotional> PopularDevotionals { get; } = new();

        // ── Constructor ─────────────────────────────────────────────────────
        public HomeScreen(
            AppViewModel viewModel,
            Action<PassageSelection> onNavigateToReader,
            Action<Screen> onNavigateToScreen,
            DatabaseHelper? databaseHelper = null,
            Action? onTakeBibleQuizClick = null,
            Action? onLearnHebrewClick = null,
            Action? onLearnGreekClick = null,
            Action? onOpenDictionaryClick = null,
            Action? onOpenAnimatorClick = null,
            Action? onOpenGenerationsClick = null)
        {
            InitializeComponent();

            _viewModel = viewModel;
            _onNavigateToReader = onNavigateToReader;
            _onNavigateToScreen = onNavigateToScreen;
            _databaseHelper = databaseHelper;

            _onTakeBibleQuizClick = onTakeBibleQuizClick ?? (() => { });
            _onLearnHebrewClick = onLearnHebrewClick ?? (() => { });
            _onLearnGreekClick = onLearnGreekClick ?? (() => { });
            _onOpenDictionaryClick = onOpenDictionaryClick ?? (() => { });
            _onOpenAnimatorClick = onOpenAnimatorClick ?? (() => { });
            _onOpenGenerationsClick = onOpenGenerationsClick ?? (() => { });

            BuildQuickActions();
            LoadPopularDevotionals();
            HookImageSection();

            Loaded += OnLoaded;
        }

        // ── Lifecycle ───────────────────────────────────────────────────────

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            // LaunchedEffect(databaseHelper) { dailyVerses = loadRandomVerses(it) }
            await RefreshDailyVerseAsync();
        }

        private void HookImageSection()
        {
            ImageSectionControl.DatabaseHelper = _databaseHelper;
            ImageSectionControl.ContinueReadingRequested += () =>
            {
                // Continue Reading → first daily verse, else Genesis 1
                var verse = _dailyVerses?.FirstOrDefault();
                _onNavigateToReader(BuildSelection(verse));
            };
        }

        // ── Quick actions ───────────────────────────────────────────────────

        private void BuildQuickActions()
        {
            var totalImages = _viewModel.Textures;
            var uniqueQuickActionImages = totalImages
                .OrderBy(_ => _rng.Next())
                .Take(5)
                .ToList();

            while (uniqueQuickActionImages.Count < 5)
                uniqueQuickActionImages.Add(string.Empty);

            QuickActions.Add(new QuickAction
            {
                Title = "Reader",
                Icon = "📖",
                BackgroundImage = ImageAssets.GetAssetImagePath(uniqueQuickActionImages[0]),
                OnClick = () => _onNavigateToScreen(new Screen.Reader())
            });
            QuickActions.Add(new QuickAction
            {
                Title = "Bookmarks",
                Icon = "🔖",
                BackgroundImage = ImageAssets.GetAssetImagePath(uniqueQuickActionImages[1]),
                OnClick = () => _onNavigateToScreen(new Screen.Bookmarks())
            });
            QuickActions.Add(new QuickAction
            {
                Title = "Notes",
                Icon = "📝",
                BackgroundImage = ImageAssets.GetAssetImagePath(uniqueQuickActionImages[2]),
                OnClick = () => _onNavigateToScreen(new Screen.Notes())
            });
            QuickActions.Add(new QuickAction
            {
                Title = "Search",
                Icon = "🔍",
                BackgroundImage = ImageAssets.GetAssetImagePath(uniqueQuickActionImages[3]),
                OnClick = () => _onNavigateToScreen(new Screen.Search())
            });
            QuickActions.Add(new QuickAction
            {
                Title = "Settings",
                Icon = "⚙",
                BackgroundImage = ImageAssets.GetAssetImagePath(uniqueQuickActionImages[4]),
                OnClick = () => _onNavigateToScreen(new Screen.Settings())
            });
        }

        private void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: QuickAction action })
                action.OnClick?.Invoke();
        }

        // ── Daily verse ─────────────────────────────────────────────────────

        private async Task RefreshDailyVerseAsync()
        {
            if (_databaseHelper is null)
            {
                DailyVerseText.Text = "No database available.";
                DailyVerseReference.Text = string.Empty;
                return;
            }

            DailyVerseText.Text = "Loading…";
            _dailyVerses = await _databaseHelper.GetRandomVersesAsync();

            RenderDailyVerse();
            await UpdateBookmarkStateAsync();
        }

        private void RenderDailyVerse()
        {
            if (_dailyVerses is null || _dailyVerses.Count == 0)
            {
                DailyVerseText.Text = "Tap ⟳ for a verse.";
                DailyVerseReference.Text = string.Empty;
                return;
            }

            var first = _dailyVerses[0];
            DailyVerseText.Text = SimpleVerseProcessor.StripXmlTags(first.Text);
            DailyVerseReference.Text = $"{first.BookName} {first.Chapter}:{first.VerseNumber}";
        }

        private async void RefreshVerseButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;   // don't bubble to the card click
            await RefreshDailyVerseAsync();
        }

        private void DailyVerseCard_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            var verse = _dailyVerses?.FirstOrDefault();
            if (verse is null) return;

            _onNavigateToReader(BuildSelection(verse));
        }

        private static PassageSelection BuildSelection(Verse? verse)
        {
            if (verse is null)
                return new PassageSelection(1, "Genesis", 1, 1);

            var bookNumber = BibleData.GetBookByName(verse.BookName ?? string.Empty)?.CustomNumber ?? 1;

            return new PassageSelection(
                bookNumber,
                verse.BookName ?? "Genesis",
                verse.Chapter ?? 1,
                verse.VerseNumber);
        }

        // ── Bookmark / share ────────────────────────────────────────────────

        private async Task UpdateBookmarkStateAsync()
        {
            if (_databaseHelper is null || _dailyVerses is null || _dailyVerses.Count == 0)
            {
                _isBookmarked = false;
                BookmarkVerseButton.Content = "🔖";
                return;
            }

            _isBookmarked = await BookmarkHelper.IsBookmarkedAsync(_dailyVerses[0], _databaseHelper)
                                                .ConfigureAwait(true);
            BookmarkVerseButton.Content = _isBookmarked ? "🔖" : "🔖";
            BookmarkVerseButton.Opacity = _isBookmarked ? 1.0 : 0.6;
        }

        private async void BookmarkVerseButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (_databaseHelper is null || _dailyVerses is null || _dailyVerses.Count == 0) return;

            if (_isBookmarked)
                await BookmarkHelper.RemoveBookmarksAsync(_dailyVerses, _databaseHelper).ConfigureAwait(true);
            else
                await BookmarkHelper.AddBookmarksAsync(_dailyVerses, _databaseHelper).ConfigureAwait(true);

            await UpdateBookmarkStateAsync();
        }

        private void ShareVerseButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            if (_dailyVerses is null || _dailyVerses.Count == 0) return;

            var owner = Window.GetWindow(this);
            if (owner != null)
                VerseShareHelper.ShareVerses(owner, _dailyVerses);
        }

        // ── Popular devotionals ─────────────────────────────────────────────

        private void LoadPopularDevotionals()
        {
            foreach (var devotional in GetRandomDevotionals())
                PopularDevotionals.Add(devotional);
        }

        private void Devotional_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { Tag: PopularDevotional d }) return;

            var bookNumber = BibleData.GetBookByName(d.BookName)?.CustomNumber ?? 1;
            _onNavigateToReader(new PassageSelection(bookNumber, d.BookName, d.Chapter, d.Verse));
        }

        /// <summary>Direct port of getRandomDevotionals().</summary>
        private List<PopularDevotional> GetRandomDevotionals()
        {
            var allDevotionals = new List<PopularDevotional>
            {
                new("Psalm 23", "The Lord is my shepherd...", "Psalms", 23, 1),
                new("The Lord's Prayer", "Our Father in heaven...", "Matthew", 6, 9),
                new("The Love Chapter", "Love is patient, love is kind...", "1 Corinthians", 13, 1),
                new("John 3:16", "For God so loved the world...", "John", 3, 16),
                new("The Beatitudes", "Blessed are the poor in spirit...", "Matthew", 5, 3),
                new("The Good Samaritan", "A man was going down from Jerusalem...", "Luke", 10, 30),
                new("The Prodigal Son", "There was a man who had two sons...", "Luke", 15, 11),
                new("The Sermon on the Mount", "Seeing the crowds, he went up on the mountain...", "Matthew", 5, 1),
                new("The Great Commandment", "You shall love the Lord your God...", "Matthew", 22, 37),
                new("The Golden Rule", "So whatever you wish that others would do to you...", "Matthew", 7, 12),
                new("I Can Do All Things", "I can do all things through him who strengthens me...", "Philippians", 4, 13),
                new("God's Plans for You", "For I know the plans I have for you...", "Jeremiah", 29, 11),
                new("Trust in the Lord", "Trust in the Lord with all your heart...", "Proverbs", 3, 5),
                new("All Things Work Together for Good", "And we know that for those who love God all things work together for good...", "Romans", 8, 28),
                new("Wait for the Lord", "But they who wait for the Lord shall renew their strength...", "Isaiah", 40, 31),
                new("Saved by Grace", "For by grace you have been saved through faith...", "Ephesians", 2, 8),
                new("Lamp to My Feet", "Your word is a lamp to my feet...", "Psalms", 119, 105),
                new("Be Strong and Courageous", "Have I not commanded you? Be strong and courageous...", "Joshua", 1, 9),
                new("Come to Me", "Come to me, all who labor and are heavy laden...", "Matthew", 11, 28),
                new("Rejoice Always", "Rejoice always, pray without ceasing...", "1 Thessalonians", 5, 16),
                new("The Alpha & Omega", "Behold, He comes with clouds...", "Revelation", 1, 7),
                new("The Armor of God", "Put on the whole armor of God...", "Ephesians", 6, 11),
                new("The Fruit of the Spirit", "But the fruit of the Spirit is love, joy, peace...", "Galatians", 5, 22),
                new("The Great Commission", "Go therefore and make disciples of all nations...", "Matthew", 28, 19),
                new("The New Commandment", "A new commandment I give to you, that you love one another...", "John", 13, 34),
                new("The Resurrection", "But in fact Christ has been raised from the dead...", "1 Corinthians", 15, 20),
                new("The Shema", "Hear, O Israel: The Lord our God, the Lord is one...", "Deuteronomy", 6, 4),
                new("The Ten Commandments", "And God spoke all these words, saying...", "Exodus", 20, 1),
                new("The Birth of Jesus", "And she gave birth to her firstborn son...", "Luke", 2, 7),
                new("The Crucifixion", "And when they had crucified him...", "Matthew", 27, 35),
                new("The Ascension", "And when he had said these things...", "Acts", 1, 9),
                new("The Holy Spirit Comes", "And suddenly there came from heaven a sound...", "Acts", 2, 2),
                new("The New Heaven and Earth", "Then I saw a new heaven and a new earth...", "Revelation", 21, 1),
                new("Faith Hall of Fame", "Now faith is the assurance of things hoped for...", "Hebrews", 11, 1),
                new("Nothing Can Separate Us", "For I am sure that neither death nor life...", "Romans", 8, 38),
                new("The Word Became Flesh", "In the beginning was the Word...", "John", 1, 1),
                new("The Parable of the Sower", "A sower went out to sow...", "Matthew", 13, 3),
                new("The Feeding of the 5,000", "Then Jesus took the loaves...", "John", 6, 11),
                new("The Good Shepherd", "I am the good shepherd...", "John", 10, 11),
                new("The Way and the Truth", "Jesus said to him, 'I am the way...'", "John", 14, 6),
                new("The Vine and Branches", "I am the vine; you are the branches...", "John", 15, 5),
                new("The Light of the World", "Again Jesus spoke to them, saying...", "John", 8, 12),
                new("The Resurrection and Life", "Jesus said to her, 'I am the resurrection...'", "John", 11, 25),
                new("The Transfiguration", "And he was transfigured before them...", "Matthew", 17, 2),
                new("The Widow's Offering", "And he called his disciples to him...", "Mark", 12, 43),
                new("The Suffering Servant", "He was despised and rejected by men, a man of sorrows...", "Isaiah", 53, 3),
                new("The Crucifixion Psalm", "My God, my God, why have you forsaken me?", "Psalms", 22, 1),
                new("The First Gospel", "He will crush your head, and you will strike his heel.", "Genesis", 3, 15),
                new("Prophecy of Bethlehem", "But you, Bethlehem Ephrathah... out of you will come for me one who will be ruler over Israel.", "Micah", 5, 2),
                new("The Triumphal Entry", "See, your king comes to you, righteous and victorious, lowly and riding on a donkey.", "Zechariah", 9, 9),
                new("The Virgin Birth", "The virgin will conceive and give birth to a son, and will call him Immanuel.", "Isaiah", 7, 14),
                new("The Messenger of the Covenant", "The Lord you are seeking will suddenly come to his temple.", "Malachi", 3, 1),
                new("The Son of Man", "He was given authority, glory and sovereign power; all nations worshiped him.", "Daniel", 7, 14),
                new("The Righteous Branch", "I will raise up for David a righteous Branch, a King who will reign wisely.", "Jeremiah", 23, 5),
                new("The Priest Forever", "You are a priest forever, in the order of Melchizedek.", "Psalms", 110, 4),
                new("The Passover Lamb", "The animals you choose must be year-old males without defect...", "Exodus", 12, 5),
                new("The Bronze Serpent", "So Moses made a bronze serpent and put it up on a pole...", "Numbers", 21, 9)
            };

            return allDevotionals
                .OrderBy(_ => _rng.Next())
                .Take(5)
                .ToList();
        }

        // ── Useful Space grid handlers ──────────────────────────────────────

        private void TakeBibleQuiz_Click(object sender, RoutedEventArgs e) => _onTakeBibleQuizClick();
        private void LearnHebrew_Click(object sender, RoutedEventArgs e) => _onLearnHebrewClick();
        private void LearnGreek_Click(object sender, RoutedEventArgs e) => _onLearnGreekClick();
        private void OpenDictionary_Click(object sender, RoutedEventArgs e) => _onOpenDictionaryClick();
        private void OpenAnimator_Click(object sender, RoutedEventArgs e) => _onOpenAnimatorClick();
        private void OpenGenerations_Click(object sender, RoutedEventArgs e) => _onOpenGenerationsClick();
    }
}