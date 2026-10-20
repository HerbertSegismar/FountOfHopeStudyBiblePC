// Views/HomeScreen.xaml.cs
using FountOfHopeStudyBiblePC.Controls;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FountOfHopeStudyBiblePC.Views
{
    public partial class HomeScreen : UserControl
    {
        public AppViewModel ViewModel => _viewModel;
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

        /// <summary>Set once we're subscribed, so we can un-subscribe cleanly.</summary>
        private bool _themeHooked;

        /// <summary>Guards against re-entry while ApplyTheme is running.</summary>
        private bool _applyingTheme;

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
            var sw = Stopwatch.StartNew();
            App.Log("HomeScreen ctor - enter");

            InitializeComponent();
            App.Log($"HomeScreen ctor - InitializeComponent took {sw.ElapsedMilliseconds}ms");

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
            App.Log($"HomeScreen ctor - BuildQuickActions took {sw.ElapsedMilliseconds}ms");

            LoadPopularDevotionals();
            HookImageSection();

            // React to theme changes from BOTH sources of truth.
            ThemeManager.Changed += OnThemeChanged;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            _themeHooked = true;

            Loaded += OnLoaded;
            Unloaded += OnUnloaded;

            // Apply the current palette once so items created before
            // subscription still pick up the right colors.
            Loaded += (_, __) => ApplyTheme();

            sw.Stop();
            App.Log($"HomeScreen ctor - COMPLETE, total {sw.ElapsedMilliseconds}ms");
        }

        private void SetRandomBackgroundImage()
        {
            var textures = _viewModel.Textures;
            if (textures is null || textures.Count == 0)
                return;

            var path = ImageAssets.GetAssetImagePath(textures[_rng.Next(textures.Count)]);

            Task.Run(() =>
            {
                var img = LoadThumbnail(path, decodeWidth: 900);
                if (img is not null)
                    Dispatcher.Invoke(() => DailyVerseBackgroundBrush.ImageSource = img);
            });
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            SetRandomBackgroundImage();

            Dispatcher.BeginInvoke(new Action(async () =>
            {
                var sw = Stopwatch.StartNew();
                App.Log("HomeScreen refresh - start");
                await RefreshDailyVerseAsync();
                App.Log($"HomeScreen refresh - took {sw.ElapsedMilliseconds}ms");
            }), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            if (!_themeHooked) return;
            ThemeManager.Changed -= OnThemeChanged;
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
            _themeHooked = false;
        }

        // ── Theme reactivity ────────────────────────────────────────────────
        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppViewModel.DarkTheme) ||
                e.PropertyName == nameof(AppViewModel.HeaderButtonsColor))
            {
                OnThemeChanged();
            }
        }

        private void OnThemeChanged()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(OnThemeChanged);
                return;
            }
            ApplyTheme();
        }

        private void ApplyTheme()
        {
            if (_applyingTheme) return;
            _applyingTheme = true;
            try
            {
                // (1) Swap DynamicResource brushes.
                ThemeResources.Refresh();

                var isDark = ThemeManager.DarkTheme;
                var primary = ThemeManager.PrimaryColor;

                var textBrush = GetBrush(ThemeResources.Text);
                var secondaryBrush = GetBrush(ThemeResources.SecondaryText);
                var primaryBrush = GetBrush(ThemeResources.Primary);

                // (3) Item sweep — bold tile labels and icon glyphs use primary.
                foreach (var ic in FindVisualChildren<ItemsControl>(this))
                {
                    if (ic.ItemsSource is null) continue;

                    if (ReferenceEquals(ic.ItemsSource, QuickActions) ||
                        ReferenceEquals(ic.ItemsSource, PopularDevotionals))
                    {
                        RefreshItemsControl(ic, primaryBrush, secondaryBrush, primaryBrush);
                    }
                }

                // (4) Custom controls with their own theme DPs.
                if (FindDescendant<FountRain>(this) is { } rain)
                {
                    rain.MatrixColor = primary;
                    rain.OverlayColor = primary;
                    rain.IsDarkTheme = isDark;
                    rain.RefreshFromTheme();
                }

                TrySetIsDarkTheme(ImageSectionControl, isDark);

                // (5) Recolor any originally non-white, non-theme-surface
                //     element (background / border) to the primary color.
                RecolorAccents(this, primary);

                // (6) Root background.
                if (Background is SolidColorBrush || Background == null)
                    Background = GetBrush(ThemeResources.PageBackground);
            }
            finally
            {
                _applyingTheme = false;
            }
        }

        // ── Theme helpers ───────────────────────────────────────────────────
        private static Brush GetBrush(string key)
        {
            if (Application.Current?.Resources[key] is Brush b)
                return b;
            return Brushes.Transparent;
        }

        /// <summary>
        /// Walk every realised container of an ItemsControl and repaint the
        /// TextBlocks and Controls inside it. Bold text → primary; regular
        /// text → secondary. Opt-out: set <c>Tag="NoTheme"</c>.
        /// </summary>
        private static void RefreshItemsControl(
            ItemsControl? control, Brush primaryText, Brush secondaryText, Brush icon)
        {
            if (control == null) return;

            foreach (var container in EnumerateContainers(control))
            {
                foreach (var tb in FindVisualChildren<TextBlock>(container))
                {
                    if (Equals(tb.Tag, "NoTheme")) continue;

                    tb.Foreground = tb.FontWeight >= FontWeights.SemiBold
                        ? primaryText
                        : secondaryText;
                }

                foreach (var ctl in FindVisualChildren<Control>(container))
                {
                    if (Equals(ctl.Tag, "NoTheme")) continue;
                    if (ctl.Foreground is SolidColorBrush) ctl.Foreground = icon;
                }
            }
        }

        private static void RecolorAccents(DependencyObject root, Color primary)
        {
            foreach (var fe in FindVisualChildren<FrameworkElement>(root))
            {
                if (Equals(fe.Tag, "NoTheme")) continue;

                switch (fe)
                {
                    case Control ctl when ctl.Background is SolidColorBrush cbg
                                      && !IsThemeNeutral(cbg.Color):
                        ctl.Background = FrozenBrush(
                            Color.FromArgb(cbg.Color.A, primary.R, primary.G, primary.B));
                        break;

                    case Border b when b.Background is SolidColorBrush bbg
                                    && !IsThemeNeutral(bbg.Color):
                        b.Background = FrozenBrush(
                            Color.FromArgb(bbg.Color.A, primary.R, primary.G, primary.B));
                        break;
                }

                switch (fe)
                {
                    case Control ctl when ctl.BorderBrush is SolidColorBrush cbb
                                      && !IsThemeNeutral(cbb.Color):
                        ctl.BorderBrush = FrozenBrush(
                            Color.FromArgb(cbb.Color.A, primary.R, primary.G, primary.B));
                        break;

                    case Border b when b.BorderBrush is SolidColorBrush bbb
                                    && !IsThemeNeutral(bbb.Color):
                        b.BorderBrush = FrozenBrush(
                            Color.FromArgb(bbb.Color.A, primary.R, primary.G, primary.B));
                        break;
                }
            }
        }

        private static bool IsThemeNeutral(Color c)
        {
            // Opaque grayscale → white, black, or any gray (theme surfaces).
            if (c.A == 255 && c.R == c.G && c.G == c.B) return true;

            // The three opaque dark-theme surfaces MainWindow installs.
            if (c.A == 255)
            {
                if (c == Color.FromRgb(0x10, 0x14, 0x18)) return true;
                if (c == Color.FromRgb(0x1C, 0x21, 0x28)) return true;
                if (c == Color.FromRgb(0x23, 0x2A, 0x33)) return true;
            }

            // Low-alpha grayscale → subtle overlays (dividers, hover tints).
            if (c.A < 0x80 && c.R == c.G && c.G == c.B) return true;

            return false;
        }

        private static Brush FrozenBrush(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        private static IEnumerable<DependencyObject> EnumerateContainers(ItemsControl control)
        {
            for (int i = 0; i < control.Items.Count; i++)
            {
                if (control.ItemContainerGenerator.ContainerFromIndex(i) is DependencyObject c)
                    yield return c;
            }
        }

        private static T? FindDescendant<T>(DependencyObject root) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match) return match;
                if (FindDescendant<T>(child) is { } deeper) return deeper;
            }
            return null;
        }

        private static IEnumerable<T> FindVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            int n = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < n; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T t) yield return t;
                foreach (var d in FindVisualChildren<T>(child)) yield return d;
            }
        }

        /// <summary>Reflection-free DP setter — uses a virtual call, not reflection.</summary>
        private static void TrySetIsDarkTheme(DependencyObject? target, bool isDark)
        {
            if (target == null) return;
            var prop = target.GetType().GetProperty("IsDarkTheme");
            if (prop != null && prop.CanWrite && prop.PropertyType == typeof(bool))
                prop.SetValue(target, isDark);
        }

        // ── Image section ───────────────────────────────────────────────────
        private void HookImageSection()
        {
            ImageSectionControl.ViewModel = _viewModel;
            ImageSectionControl.DatabaseHelper = _databaseHelper;
            ImageSectionControl.IsDarkTheme = ThemeManager.DarkTheme;

            ImageSectionControl.PassageSelected += (_, selection) =>
                _onNavigateToReader(selection);
        }

        // ── Quick actions ───────────────────────────────────────────────────
        private void BuildQuickActions()
        {
            App.Log($"BuildQuickActions: Textures.Count={_viewModel.Textures.Count}");

            var totalImages = _viewModel.Textures;
            var uniqueQuickActionImages = totalImages
                .OrderBy(_ => _rng.Next())
                .Take(5)
                .ToList();

            while (uniqueQuickActionImages.Count < 5)
                uniqueQuickActionImages.Add(string.Empty);

            var tiles = new[]
            {
                new QuickAction { Title = "Reader",    Icon = "📖", OnClick = () => _onNavigateToScreen(new Screen.Reader())    },
                new QuickAction { Title = "Bookmarks", Icon = "🔖", OnClick = () => _onNavigateToScreen(new Screen.Bookmarks()) },
                new QuickAction { Title = "Notes",     Icon = "📝", OnClick = () => _onNavigateToScreen(new Screen.Notes())     },
                new QuickAction { Title = "Search",    Icon = "🔍", OnClick = () => _onNavigateToScreen(new Screen.Search())    },
                new QuickAction { Title = "Settings",  Icon = "⚙",  OnClick = () => _onNavigateToScreen(new Screen.Settings())  },
            };

            for (int i = 0; i < 5; i++)
            {
                tiles[i].BackgroundImage = ImageAssets.GetAssetImagePath(uniqueQuickActionImages[i]);
                QuickActions.Add(tiles[i]);
            }

            foreach (var tile in tiles)
            {
                var path = tile.BackgroundImage;
                var action = tile;

                Task.Run(() =>
                {
                    var thumb = LoadThumbnail(path);
                    if (thumb == null) return;
                    Dispatcher.Invoke(() => action.BackgroundImageSource = thumb);
                });
            }
        }

        private static ImageSource? LoadThumbnail(string? pathOrUri, int decodeWidth = 240)
        {
            if (string.IsNullOrEmpty(pathOrUri))
            {
                App.Log("LoadThumbnail: → null (empty input)");
                return null;
            }

            try
            {
                Uri? uri = null;

                if (pathOrUri.StartsWith("pack://", StringComparison.OrdinalIgnoreCase))
                {
                    if (Uri.TryCreate(pathOrUri, UriKind.Absolute, out uri))
                        App.Log($"LoadThumbnail: pack URI '{uri}'");
                    else
                    {
                        App.Log($"LoadThumbnail: → null (invalid pack URI '{pathOrUri}')");
                        return null;
                    }
                }
                else if (File.Exists(pathOrUri))
                {
                    uri = new Uri(pathOrUri, UriKind.Absolute);
                    App.Log($"LoadThumbnail: file path '{uri}'");
                }
                else
                {
                    var candidate = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, pathOrUri);
                    if (File.Exists(candidate))
                    {
                        uri = new Uri(candidate, UriKind.Absolute);
                        App.Log($"LoadThumbnail: resolved relative '{uri}'");
                    }
                }

                if (uri == null)
                {
                    App.Log($"LoadThumbnail: → null (could not resolve '{pathOrUri}')");
                    return null;
                }

                var bmp = new BitmapImage();
                bmp.BeginInit();
                bmp.UriSource = uri;
                bmp.DecodePixelWidth = decodeWidth;
                bmp.CacheOption = BitmapCacheOption.OnLoad;
                bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bmp.EndInit();
                bmp.Freeze();
                App.Log($"LoadThumbnail: → loaded {bmp.PixelWidth}x{bmp.PixelHeight}");
                return bmp;
            }
            catch (Exception ex)
            {
                App.Log($"LoadThumbnail: → EXCEPTION {ex.Message}");
                return null;
            }
        }

        private void QuickAction_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button { Tag: QuickAction action })
                action.OnClick?.Invoke();
        }

        private async Task RefreshDailyVerseAsync()
        {
            if (_databaseHelper is null)
            {
                _dailyVerses = null;
                RenderDailyVerse();
                return;
            }

            // Show skeleton while loading.
            _dailyVerses = null;
            RenderDailyVerse();

            _dailyVerses = await _databaseHelper.GetRandomVersesAsync(5);

            if (_dailyVerses is { Count: 0 })
                _dailyVerses = null;

            RenderDailyVerse();
            await UpdateBookmarkStateAsync();
        }

        private void RenderDailyVerse()
        {
            if (_dailyVerses is null || _dailyVerses.Count == 0)
            {
                VerseContentPanel.Visibility = Visibility.Collapsed;
                VerseSkeletonPanel.Visibility = Visibility.Visible;
                return;
            }

            VerseContentPanel.Visibility = Visibility.Visible;
            VerseSkeletonPanel.Visibility = Visibility.Collapsed;

            var first = _dailyVerses[0];
            DailyVerseReference.Text = $"{first.BookName} {first.Chapter}:{first.VerseNumber}";

            DailyVerseText.Inlines.Clear();
            for (int i = 0; i < _dailyVerses.Count; i++)
            {
                var verse = _dailyVerses[i];

                // Bold verse number (15pt) – matches Kotlin SpanStyle
                DailyVerseText.Inlines.Add(new Run($"{verse.VerseNumber} ")
                {
                    FontSize = 15,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.White
                });

                // Body text
                DailyVerseText.Inlines.Add(new Run(SimpleVerseProcessor.StripXmlTags(verse.Text)));

                if (i < _dailyVerses.Count - 1)
                    DailyVerseText.Inlines.Add(new LineBreak());
            }
        }

        private async void RefreshVerseButton_Click(object sender, RoutedEventArgs e)
        {
            e.Handled = true;

            RefreshIconText.Visibility = Visibility.Collapsed;
            RefreshSpinner.Visibility = Visibility.Visible;
            try
            {
                await RefreshDailyVerseAsync();
                // Kotlin uses a 500 ms minimum so the spinner is visible.
                await Task.Delay(500);
            }
            finally
            {
                RefreshIconText.Visibility = Visibility.Visible;
                RefreshSpinner.Visibility = Visibility.Collapsed;
            }
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

        private async Task UpdateBookmarkStateAsync()
        {
            if (_databaseHelper is null || _dailyVerses is null || _dailyVerses.Count == 0)
            {
                _isBookmarked = false;
                BookmarkIconText.Opacity = 0.6;
                return;
            }

            _isBookmarked = await BookmarkHelper
                .IsBookmarkedAsync(_dailyVerses[0], _databaseHelper)
                .ConfigureAwait(true);

            BookmarkIconText.Opacity = _isBookmarked ? 1.0 : 0.6;
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

        private void Devotional_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement { Tag: PopularDevotional d })
            {
                var bookNumber = BibleData.GetBookByName(d.BookName)?.CustomNumber ?? 1;
                _onNavigateToReader(new PassageSelection(bookNumber, d.BookName, d.Chapter, d.Verse));
            }
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