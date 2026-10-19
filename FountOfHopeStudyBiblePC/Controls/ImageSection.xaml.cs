using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;

// No `using System.IO;` — `Path` collides with System.Windows.Shapes.Path.
// All IO is fully qualified where used.

namespace FountOfHopeStudyBiblePC.Controls
{
    public partial class ImageSection : UserControl
    {
        // ── Public surface ──────────────────────────────────────────────
        public AppViewModel? ViewModel { get; set; }
        public DatabaseHelper? DatabaseHelper { get; set; }

        public event EventHandler<PassageSelection>? PassageSelected;
        public event Action? ContinueReadingRequested;

        public bool IsDarkTheme
        {
            get => (bool)GetValue(IsDarkThemeProperty);
            set => SetValue(IsDarkThemeProperty, value);
        }
        public static readonly DependencyProperty IsDarkThemeProperty =
            DependencyProperty.Register(nameof(IsDarkTheme), typeof(bool),
                typeof(ImageSection),
                new PropertyMetadata(false, (d, _) =>
                    ((ImageSection)d).RefreshFallbackTheme()));

        // ── Data ────────────────────────────────────────────────────────
        private static readonly InspirationalVerseRef[] InspirationalRefs =
        {
            new InspirationalVerseRef(230, 46, 10),
            new InspirationalVerseRef(570, 4, 13),
            new InspirationalVerseRef(230, 23, 1),
            new InspirationalVerseRef(300, 29, 11),
            new InspirationalVerseRef(240, 3, 5),
            new InspirationalVerseRef(160, 8, 10),
            new InspirationalVerseRef(670, 5, 7),
            new InspirationalVerseRef(60, 1, 9),
            new InspirationalVerseRef(520, 8, 28),
            new InspirationalVerseRef(570, 4, 7),
            new InspirationalVerseRef(470, 5, 16),
            new InspirationalVerseRef(470, 19, 26),
            new InspirationalVerseRef(230, 46, 1),
            new InspirationalVerseRef(230, 27, 1),
            new InspirationalVerseRef(570, 4, 6),
            new InspirationalVerseRef(290, 40, 31),
            new InspirationalVerseRef(230, 34, 18),
            new InspirationalVerseRef(500, 3, 16),
            new InspirationalVerseRef(540, 5, 17),
            new InspirationalVerseRef(520, 8, 28),
            new InspirationalVerseRef(550, 5, 22, 23),
            new InspirationalVerseRef(230, 119, 105),
            new InspirationalVerseRef(20, 14, 14),
            new InspirationalVerseRef(470, 11, 28),
            new InspirationalVerseRef(470, 6, 33),
            new InspirationalVerseRef(500, 16, 33),
            new InspirationalVerseRef(530, 13, 13),
            new InspirationalVerseRef(470, 18, 20),
            new InspirationalVerseRef(230, 28, 7),
            new InspirationalVerseRef(670, 2, 9),
            new InspirationalVerseRef(590, 5, 16, 18),
            new InspirationalVerseRef(290, 41, 10),
            new InspirationalVerseRef(470, 19, 26),
            new InspirationalVerseRef(240, 18, 10),
            new InspirationalVerseRef(290, 40, 29),
            new InspirationalVerseRef(600, 3, 3),
            new InspirationalVerseRef(230, 34, 8),
            new InspirationalVerseRef(520, 5, 1),
            new InspirationalVerseRef(570, 4, 19),
            new InspirationalVerseRef(620, 1, 7),
        };

        private static readonly string[] InspirationalTexts =
        {
            "Be still and know that I am God. - Psalm 46:10",
            "I can do all things through Christ who strengthens me. - Philippians 4:13",
            "The Lord is my shepherd; I shall not want. - Psalm 23:1",
            "For I know the plans I have for you, declares the Lord. - Jeremiah 29:11",
            "Trust in the Lord with all your heart. - Proverbs 3:5",
            "The joy of the Lord is your strength. - Nehemiah 8:10",
            "Cast all your anxiety on him because he cares for you. - 1 Peter 5:7",
            "Be strong and courageous. Do not be afraid. - Joshua 1:9",
            "In all things God works for the good of those who love him. - Romans 8:28",
            "The peace of God, which transcends all understanding, will guard your hearts and your minds. - Philippians 4:7",
            "Let your light shine before others. - Matthew 5:16",
            "With God all things are possible. - Matthew 19:26",
            "God is our refuge and strength, an ever-present help in trouble. - Psalm 46:1",
            "The Lord is my light and my salvation—whom shall I fear? - Psalm 27:1",
            "Do not be anxious about anything, but in every situation, by prayer and petition, with thanksgiving, present your requests to God. - Philippians 4:6",
            "But those who hope in the Lord will renew their strength. They will soar on wings like eagles. - Isaiah 40:31",
            "The Lord is close to the brokenhearted and saves those who are crushed in spirit. - Psalm 34:18",
            "For God so loved the world that he gave his one and only Son, that whoever believes in him shall not perish but have eternal life. - John 3:16",
            "Therefore, if anyone is in Christ, the new creation has come: The old has gone, the new is here! - 2 Corinthians 5:17",
            "And we know that in all things God works for the good of those who love him, who have been called according to his purpose. - Romans 8:28",
            "But the fruit of the Spirit is love, joy, peace, forbearance, kindness, goodness, faithfulness, gentleness and self-control. - Galatians 5:22-23",
            "Your word is a lamp for my feet, a light on my path. - Psalm 119:105",
            "The Lord will fight for you; you need only to be still. - Exodus 14:14",
            "Come to me, all you who are weary and burdened, and I will give you rest. - Matthew 11:28",
            "But seek first his kingdom and his righteousness, and all these things will be given to you as well. - Matthew 6:33",
            "I have told you these things, so that in me you may have peace. In this world you will have trouble. But take heart! I have overcome the world. - John 16:33",
            "And now these three remain: faith, hope and love. But the greatest of these is love. - 1 Corinthians 13:13",
            "For where two or three gather in my name, there am I with them. - Matthew 18:20",
            "The Lord is my strength and my shield; my heart trusts in him, and he helps me. - Psalm 28:7",
            "But you are a chosen people, a royal priesthood, a holy nation, God's special possession, that you may declare the praises of him who called you out of darkness into his wonderful light. - 1 Peter 2:9",
            "Rejoice always, pray continually, give thanks in all circumstances; for this is God's will for you in Christ Jesus. - 1 Thessalonians 5:16-18",
            "So do not fear, for I am with you; do not be dismayed, for I am your God. I will strengthen you and help you; I will uphold you with my righteous right hand. - Isaiah 41:10",
            "Jesus looked at them and said, 'With man this is impossible, but with God all things are possible.' - Matthew 19:26",
            "The name of the Lord is a fortified tower; the righteous run to it and are safe. - Proverbs 18:10",
            "He gives strength to the weary and increases the power of the weak. - Isaiah 40:29",
            "But the Lord is faithful, and he will strengthen you and protect you from the evil one. - 2 Thessalonians 3:3",
            "Taste and see that the Lord is good; blessed is the one who takes refuge in him. - Psalm 34:8",
            "Therefore, since we have been justified through faith, we have peace with God through our Lord Jesus Christ. - Romans 5:1",
            "And my God will meet all your needs according to the riches of his glory in Christ Jesus. - Philippians 4:19",
            "For the Spirit God gave us does not make us timid, but gives us power, love and self-discipline. - 2 Timothy 1:7",
        };

        // ── State ────────────────────────────────────────────────────────
        private readonly Random _rng = new();
        private InspirationalVerseRef? _selectedRef;
        private string _quoteText = string.Empty;
        private bool _loaded;
        private int _loadGeneration;

        public ImageSection()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            SizeChanged += OnSizeChanged;
            Unloaded += OnUnloaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e) => TryStartLoad();
        private void OnSizeChanged(object sender, SizeChangedEventArgs e) => TryStartLoad();

        private void TryStartLoad()
        {
            if (_loaded) return;
            if (ActualWidth <= 0 || ActualHeight <= 0) return;
            _loaded = true;
            _ = LoadRandomAsync();
        }

        private void OnUnloaded(object sender, RoutedEventArgs e) => StopEffects();

        // ── Loading ─────────────────────────────────────────────────────
        private async Task LoadRandomAsync()
        {
            int gen = ++_loadGeneration;
            try
            {
                string? path = null;
                try { path = ImageAssets.PickHeroImage(); }
                catch (Exception ex) { App.Log($"ImageSection: PickHeroImage threw {ex.Message}"); }

                if (string.IsNullOrEmpty(path)) { ShowFallback("No image available."); return; }

                BitmapImage? bmp = null;
                try
                {
                    bmp = new BitmapImage();
                    bmp.BeginInit();
                    bmp.UriSource = new Uri(path, UriKind.RelativeOrAbsolute);
                    bmp.CacheOption = BitmapCacheOption.OnLoad;
                    bmp.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    bmp.EndInit();
                    bmp.Freeze();
                }
                catch (Exception ex) { App.Log($"ImageSection: BitmapImage load threw {ex.Message}"); }

                if (gen != _loadGeneration) return;
                if (bmp is null) { ShowFallback("Image failed to load."); return; }

                LoadingPlaceholder.Visibility = Visibility.Collapsed;
                FallbackPanel.Visibility = Visibility.Collapsed;
                OverlayPanel.Visibility = Visibility.Visible;
                HeroImage.Visibility = Visibility.Visible;
                ShareButton.Visibility = Visibility.Visible;
                DownloadButton.Visibility = Visibility.Visible;
                HeroImage.Source = bmp;

                string fileName = System.IO.Path.GetFileName(path) ?? "";
                EffectType effect =
                    fileName.Contains("w", StringComparison.OrdinalIgnoreCase) ? EffectType.Snow :
                    fileName.Contains("o", StringComparison.OrdinalIgnoreCase) ? EffectType.Orbs :
                                                                                 EffectType.Meteors;
                ApplyEffect(effect);

                await LoadVerseAsync();
                if (gen != _loadGeneration) return;
                UpdateOverlay();
            }
            catch (Exception ex)
            {
                App.Log($"ImageSection.LoadRandomAsync EXCEPTION: {ex.Message}");
                if (gen == _loadGeneration) ShowFallback("Something went wrong.");
            }
        }

        private async Task LoadVerseAsync()
        {
            _selectedRef = null;
            _quoteText = string.Empty;

            if (DatabaseHelper is not null)
            {
                var refItem = InspirationalRefs[_rng.Next(InspirationalRefs.Length)];
                var verses = await TryGetVersesAsync(DatabaseHelper, refItem.BookNumber, refItem.Chapter);

                if (verses is { Count: > 0 })
                {
                    List<Verse> selected = refItem.EndVerse is int end
                        ? verses.Where(v => v.VerseNumber >= refItem.Verse &&
                                            v.VerseNumber <= end).ToList()
                        : verses.Where(v => v.VerseNumber == refItem.Verse).ToList();

                    if (selected.Count > 0)
                    {
                        string rawText = string.Join(" ", selected
                            .Select(v => v.Text ?? "")
                            .Where(t => !string.IsNullOrWhiteSpace(t)))
                            .Trim();

                        string cleanText = SimpleVerseProcessor.StripXmlTags(rawText);
                        if (string.IsNullOrWhiteSpace(cleanText) && !string.IsNullOrWhiteSpace(rawText))
                            cleanText = rawText;

                        if (!string.IsNullOrWhiteSpace(cleanText))
                        {
                            string bookName = BibleData.GetBookByCustomNumber(refItem.BookNumber)?.Name
                                              ?? "Unknown";
                            string reference = refItem.EndVerse is int e2
                                ? $"{bookName} {refItem.Chapter}:{refItem.Verse}-{e2}"
                                : $"{bookName} {refItem.Chapter}:{refItem.Verse}";

                            _quoteText = $"{cleanText} - {reference}";
                            _selectedRef = refItem;
                            return;
                        }
                    }
                }
            }

            _quoteText = InspirationalTexts[_rng.Next(InspirationalTexts.Length)];
        }

        private static async Task<List<Verse>?> TryGetVersesAsync(
            DatabaseHelper db, int bookNumber, int chapter)
        {
            try
            {
                var type = db.GetType();
                foreach (var name in new[] { "GetVersesAsync", "GetChapterVersesAsync",
                                             "GetVerses",      "GetChapterVerses" })
                {
                    var m = type.GetMethod(name, new[] { typeof(int), typeof(int) });
                    if (m is null) continue;

                    var ret = m.Invoke(db, new object[] { bookNumber, chapter });
                    if (ret is null) return null;

                    if (ret is Task task)
                    {
                        await task.ConfigureAwait(false);
                        var resultProp = task.GetType().GetProperty("Result");
                        var value = resultProp?.GetValue(task);
                        if (value is IEnumerable<Verse> async) return async.ToList();
                        return null;
                    }
                    if (ret is IEnumerable<Verse> sync) return sync.ToList();
                }
            }
            catch (Exception ex) { App.Log($"ImageSection.TryGetVersesAsync: {ex.Message}"); }
            return null;
        }

        // ── Overlay / fallback ──────────────────────────────────────────
        private void UpdateOverlay()
        {
            if (string.IsNullOrEmpty(_quoteText)) { OverlayPanel.Visibility = Visibility.Collapsed; return; }

            int split = _quoteText.LastIndexOf(" - ", StringComparison.Ordinal);
            string verse = (split > 0 ? _quoteText[..split] : _quoteText).Trim();
            string @ref = (split > 0 ? _quoteText[(split + 3)..] : string.Empty).Trim();

            OverlayVerse.Text = string.IsNullOrEmpty(verse) ? string.Empty : $"“{verse}”";
            OverlayRef.Text = string.IsNullOrEmpty(@ref) ? string.Empty : $"- {@ref}";
            OverlayVerse.Visibility = string.IsNullOrEmpty(verse) ? Visibility.Collapsed : Visibility.Visible;
            OverlayRef.Visibility = string.IsNullOrEmpty(@ref) ? Visibility.Collapsed : Visibility.Visible;
            OverlayPanel.Visibility = Visibility.Visible;
        }

        private void ShowFallback(string? reason = null)
        {
            StopEffects();
            HeroImage.Visibility = Visibility.Collapsed;
            LoadingPlaceholder.Visibility = Visibility.Collapsed;
            OverlayPanel.Visibility = Visibility.Collapsed;
            ShareButton.Visibility = Visibility.Collapsed;
            DownloadButton.Visibility = Visibility.Collapsed;
            FallbackPanel.Visibility = Visibility.Visible;

            if (string.IsNullOrEmpty(_quoteText))
                _quoteText = InspirationalTexts[_rng.Next(InspirationalTexts.Length)];

            int split = _quoteText.LastIndexOf(" - ", StringComparison.Ordinal);
            string verse = (split > 0 ? _quoteText[..split] : _quoteText).Trim();
            string @ref = (split > 0 ? _quoteText[(split + 3)..] : string.Empty).Trim();

            FallbackVerse.Text = string.IsNullOrEmpty(verse) ? (reason ?? string.Empty) : $"“{verse}”";
            FallbackRef.Text = string.IsNullOrEmpty(@ref)
                ? (string.IsNullOrEmpty(verse) ? string.Empty : (reason ?? string.Empty))
                : $"- {@ref}";

            RefreshFallbackTheme();
        }

        private void RefreshFallbackTheme()
        {
            bool isDark = IsDarkTheme;
            FallbackHeader.Foreground = isDark ? Brushes.White : Brushes.Black;
            FallbackVerse.Foreground = isDark
                ? new SolidColorBrush(Color.FromArgb(0xD9, 255, 255, 255))
                : new SolidColorBrush(Color.FromArgb(0xBF, 0, 0, 0));
            FallbackRef.Foreground = isDark
                ? new SolidColorBrush(Color.FromArgb(0xA6, 255, 255, 255))
                : new SolidColorBrush(Color.FromArgb(0x8C, 0, 0, 0));
        }

        // ── Click ───────────────────────────────────────────────────────
        private void OnCardClick(object sender, MouseButtonEventArgs e)
        {
            if (_selectedRef is not null)
            {
                var bookName = BibleData.GetBookByCustomNumber(_selectedRef.BookNumber)?.Name;
                if (!string.IsNullOrEmpty(bookName))
                {
                    PassageSelected?.Invoke(this, new PassageSelection(
                        _selectedRef.BookNumber, bookName,
                        _selectedRef.Chapter, _selectedRef.Verse));
                    return;
                }
            }
            ContinueReadingRequested?.Invoke();
        }

        // ── Share / download ────────────────────────────────────────────
        private async void OnShareClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ShareButton.IsEnabled = DownloadButton.IsEnabled = false;
            try
            {
                var bitmap = await CaptureCompositeAsync();
                if (bitmap is null) return;

                var data = new DataObject();
                data.SetImage(bitmap);
                if (!string.IsNullOrEmpty(_quoteText)) data.SetText(_quoteText);
                Clipboard.SetDataObject(data, true);

                MessageBox.Show(Window.GetWindow(this),
                    "Inspiration copied to clipboard.",
                    "Share", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex) { App.Log($"ImageSection.OnShareClick: {ex.Message}"); }
            finally { ShareButton.IsEnabled = DownloadButton.IsEnabled = true; }
        }

        private async void OnDownloadClick(object sender, RoutedEventArgs e)
        {
            e.Handled = true;
            ShareButton.IsEnabled = DownloadButton.IsEnabled = false;
            try
            {
                var bitmap = await CaptureCompositeAsync();
                if (bitmap is null) return;

                var downloads = System.IO.Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "Downloads");
                System.IO.Directory.CreateDirectory(downloads);

                var fileName = $"inspiration_{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}.png";
                var fullPath = System.IO.Path.Combine(downloads, fileName);

                using (var fs = System.IO.File.Create(fullPath))
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(bitmap));
                    encoder.Save(fs);
                }

                MessageBox.Show(Window.GetWindow(this),
                    $"Image saved to Downloads:\n{fileName}",
                    "Download", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                App.Log($"ImageSection.OnDownloadClick: {ex.Message}");
                MessageBox.Show(Window.GetWindow(this),
                    $"Failed to save image:\n{ex.Message}",
                    "Download", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally { ShareButton.IsEnabled = DownloadButton.IsEnabled = true; }
        }

        private async Task<BitmapSource?> CaptureCompositeAsync()
        {
            await Task.Delay(80);
            await Dispatcher.InvokeAsync(() => { },
                System.Windows.Threading.DispatcherPriority.Render);

            var dpi = VisualTreeHelper.GetDpi(RootCard);
            int width = (int)(RootCard.ActualWidth * dpi.DpiScaleX);
            int height = (int)(RootCard.ActualHeight * dpi.DpiScaleY);
            if (width <= 0 || height <= 0) return null;

            var rtb = new RenderTargetBitmap(width, height,
                dpi.PixelsPerInchX, dpi.PixelsPerInchY, PixelFormats.Pbgra32);
            rtb.Render(RootCard);
            rtb.Freeze();
            return rtb;
        }

        // ── Effects scaffolding ─────────────────────────────────────────
        private enum EffectType { None, Snow, Meteors, Orbs }

        private EffectType _currentEffect = EffectType.None;
        private DateTime _lastFrameTime = DateTime.MinValue;

        private readonly List<MeteorState> _meteors = new();
        private readonly List<SnowflakeState> _snowflakes = new();
        private readonly List<OrbState> _orbs = new();

        private void ApplyEffect(EffectType type)
        {
            StopEffects();
            _currentEffect = type;
            _lastFrameTime = DateTime.MinValue;

            switch (type)
            {
                case EffectType.Meteors: StartMeteors(3); break;
                case EffectType.Snow: StartSnow(30); break;
                case EffectType.Orbs: StartOrbs(3); break;
            }

            if (type != EffectType.None)
                CompositionTarget.Rendering += OnRendering;
        }

        private void StopEffects()
        {
            CompositionTarget.Rendering -= OnRendering;
            EffectsLayer.Children.Clear();
            _meteors.Clear();
            _snowflakes.Clear();
            _orbs.Clear();
            _currentEffect = EffectType.None;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            if (_lastFrameTime == DateTime.MinValue) _lastFrameTime = now;
            double dt = (now - _lastFrameTime).TotalSeconds;
            _lastFrameTime = now;
            if (dt <= 0 || dt > 0.05) dt = 0.016;

            switch (_currentEffect)
            {
                case EffectType.Meteors: UpdateMeteors(dt); break;
                case EffectType.Snow: UpdateSnow(dt); break;
                case EffectType.Orbs: UpdateOrbs(dt); break;
            }
        }

        private sealed class MeteorState
        {
            public FrameworkElement Element = null!;
            public double StartX, StartY, EndX, EndY;
            public double Progress;
            public double Duration;
            public double Delay;
            public double BaseOpacity;
            public double HeadOffsetX;   // ← new
            public double HeadOffsetY;   // ← new
        }

        private void StartMeteors(int count)
        {
            double w = Math.Max(1, ActualWidth);
            double h = Math.Max(1, ActualHeight);

            for (int i = 0; i < count; i++)
            {
                var s = new MeteorState();
                RegenerateMeteor(s, w, h);
                EffectsLayer.Children.Add(s.Element);
                _meteors.Add(s);
            }
        }

        private void RegenerateMeteor(MeteorState s, double w, double h)
        {
            double startPct = _rng.NextDouble() * -100 - 20;
            const double shiftPct = -300;

            double coreRadius = _rng.NextDouble() * 1 + 0.5;
            double glowRadius = coreRadius * (_rng.NextDouble() * 3 + 4);
            double trailLength = _rng.NextDouble() * 150 + 80;

            double baseOpacity = _rng.NextDouble() * 0.4 + 0.6;
            double duration = _rng.NextDouble() * 10 + 5;
            double delay = _rng.NextDouble() * 6;

            var colors = new[]
            {
        Color.FromRgb(255, 255, 255),
        Color.FromRgb(224, 255, 255),
        Color.FromRgb(176, 224, 230),
        Color.FromRgb(255, 250, 205),
        Color.FromRgb(255, 228, 181),
    };
            var color = colors[_rng.Next(colors.Length)];

            s.StartX = w * startPct / 100.0;
            s.StartY = -trailLength;
            s.EndX = s.StartX - (w * shiftPct / 100.0);
            s.EndY = h + trailLength;
            s.Progress = 0;
            s.Duration = duration;
            s.Delay = delay;
            s.BaseOpacity = baseOpacity;

            double dY = s.EndY - s.StartY;
            double dX = s.EndX - s.StartX;
            double rotationDeg = Math.Atan2(dY, dX) * 180 / Math.PI;

            double containerW = trailLength + glowRadius;
            double containerH = glowRadius * 2;
            double headX = containerW - glowRadius;   // == trailLength
            double centerY = containerH / 2;            // == glowRadius

            s.HeadOffsetX = headX;
            s.HeadOffsetY = centerY;

            // ── Container: Canvas (children positioned explicitly) ──────────────
            var container = new Canvas
            {
                Width = containerW,
                Height = containerH,
                IsHitTestVisible = false,
                Opacity = 0,
                Background = Brushes.Transparent,
                RenderTransformOrigin = new Point(headX / containerW, 0.5),
                RenderTransform = new RotateTransform(rotationDeg),
            };

            // ── Trail: Polygon with Stretch=None and explicit Canvas position ───
            var trail = new Polygon
            {
                Points = new PointCollection
        {
            new Point(headX, centerY - coreRadius * 0.8),
            new Point(0,     centerY),
            new Point(headX, centerY + coreRadius * 0.8),
        },
                Fill = new LinearGradientBrush
                {
                    StartPoint = new Point(1, 0.5),  // head side
                    EndPoint = new Point(0, 0.5),  // tail side
                    GradientStops =
            {
                new GradientStop(Color.FromArgb(204, color.R, color.G, color.B), 0),
                new GradientStop(Colors.Transparent, 1),
            }
                },
                Stretch = Stretch.None,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Top,
            };
            Canvas.SetLeft(trail, 0);
            Canvas.SetTop(trail, 0);
            container.Children.Add(trail);

            // ── Glow (centered on head) ─────────────────────────────────────────
            var glow = new Ellipse
            {
                Width = glowRadius * 2,
                Height = glowRadius * 2,
                Fill = new RadialGradientBrush
                {
                    GradientStops =
            {
                new GradientStop(Color.FromArgb(153, color.R, color.G, color.B), 0),
                new GradientStop(Colors.Transparent, 1),
            }
                }
            };
            Canvas.SetLeft(glow, headX - glowRadius);
            Canvas.SetTop(glow, centerY - glowRadius);
            container.Children.Add(glow);

            // ── Core (small white dot at head) ──────────────────────────────────
            var core = new Ellipse
            {
                Width = coreRadius * 2,
                Height = coreRadius * 2,
                Fill = Brushes.White,
            };
            Canvas.SetLeft(core, headX - coreRadius);
            Canvas.SetTop(core, centerY - coreRadius);
            container.Children.Add(core);

            s.Element = container;
        }

        private void UpdateMeteors(double dt)
        {
            double w = Math.Max(1, ActualWidth);
            double h = Math.Max(1, ActualHeight);

            foreach (var s in _meteors)
            {
                if (s.Delay > 0) { s.Delay -= dt; continue; }

                s.Progress += dt / s.Duration;
                if (s.Progress >= 1)
                {
                    EffectsLayer.Children.Remove(s.Element);
                    RegenerateMeteor(s, w, h);
                    EffectsLayer.Children.Add(s.Element);
                    continue;
                }

                double currentX = s.StartX + (s.EndX - s.StartX) * s.Progress;
                double currentY = s.StartY + (s.EndY - s.StartY) * s.Progress;
                if (double.IsNaN(currentX) || double.IsNaN(currentY)) continue;

                // Head lands exactly at (currentX, currentY) — offset removed.
                Canvas.SetLeft(s.Element, currentX - s.HeadOffsetX);
                Canvas.SetTop(s.Element, currentY - s.HeadOffsetY);

                double opacityCurve = s.Progress < 0.15
                    ? s.Progress / 0.15
                    : 1 - ((s.Progress - 0.15) / 0.85);
                s.Element.Opacity = s.BaseOpacity * opacityCurve;
            }
        }

        private sealed class SnowflakeState
        {
            public FrameworkElement Element = null!;
            public double SizeDp, DurationMs, Opacity, LeftPercent;
            public double WobbleMaxPx, WobbleDurationMs, RotationSpeed, BlurDp;
            public double Progress;
            public double WobbleProgress;
            public double WobbleDir = 1;
            public RotateTransform? Rotate;
        }

        private void StartSnow(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var s = GenerateSnowflake();
                var brush = new RadialGradientBrush
                {
                    GradientStops =
                    {
                        new GradientStop(Colors.White, 0),
                        new GradientStop(Color.FromArgb(77, 255, 255, 255), 0.7),
                        new GradientStop(Colors.Transparent, 1),
                    }
                };
                var flake = new Ellipse
                {
                    Width = s.SizeDp,
                    Height = s.SizeDp,
                    Fill = brush,
                    Opacity = s.Opacity,
                    IsHitTestVisible = false,
                };
                if (s.BlurDp > 0)
                    flake.Effect = new BlurEffect { Radius = s.BlurDp };

                s.Rotate = new RotateTransform(0);
                flake.RenderTransform = s.Rotate;
                flake.RenderTransformOrigin = new Point(0.5, 0.5);

                s.Element = flake;
                EffectsLayer.Children.Add(flake);
                _snowflakes.Add(s);
            }
        }

        private SnowflakeState GenerateSnowflake()
        {
            int layer = _rng.Next(3);
            var s = new SnowflakeState();
            switch (layer)
            {
                case 0:
                    s.SizeDp = _rng.Next(6, 11);
                    s.DurationMs = _rng.Next(3000, 5001);
                    s.Opacity = _rng.NextDouble() * 0.9 + 0.4;
                    s.BlurDp = _rng.Next(2, 5);
                    s.LeftPercent = _rng.NextDouble();
                    s.WobbleMaxPx = _rng.Next(40, 81);
                    s.WobbleDurationMs = _rng.Next(2000, 4001);
                    s.RotationSpeed = _rng.Next(90, 181);
                    break;
                case 1:
                    s.SizeDp = _rng.Next(3, 6);
                    s.DurationMs = _rng.Next(6000, 9001);
                    s.Opacity = _rng.NextDouble() * 0.9 + 0.6;
                    s.BlurDp = 1;
                    s.LeftPercent = _rng.NextDouble();
                    s.WobbleMaxPx = _rng.Next(20, 41);
                    s.WobbleDurationMs = _rng.Next(3000, 5001);
                    s.RotationSpeed = _rng.Next(45, 91);
                    break;
                default:
                    s.SizeDp = _rng.Next(1, 3);
                    s.DurationMs = _rng.Next(10000, 15001);
                    s.Opacity = _rng.NextDouble() + 0.3;
                    s.BlurDp = 0;
                    s.LeftPercent = _rng.NextDouble();
                    s.WobbleMaxPx = _rng.Next(10, 21);
                    s.WobbleDurationMs = _rng.Next(5000, 7001);
                    s.RotationSpeed = 30;
                    break;
            }
            return s;
        }

        private void UpdateSnow(double dt)
        {
            double w = Math.Max(1, ActualWidth);
            double h = Math.Max(1, ActualHeight);

            foreach (var s in _snowflakes)
            {
                s.Progress += dt * 1000 / s.DurationMs;
                if (s.Progress >= 1) s.Progress -= 1;

                // Wobble oscillates -1 → 1 with mirror (like Kotlin AutoReverse)
                s.WobbleProgress += dt * 1000 / s.WobbleDurationMs * s.WobbleDir;
                if (s.WobbleProgress >= 1) { s.WobbleProgress = 1; s.WobbleDir = -1; }
                if (s.WobbleProgress <= -1) { s.WobbleProgress = -1; s.WobbleDir = 1; }

                // Sine-wave easing to match Kotlin SineWaveEasing
                double wobble = Math.Sin((s.WobbleProgress + 1) * 0.5 * Math.PI * 0.5);
                // Remap: Kotlin wobble is -1..1 linear eased by sine(frac*PI/2), where frac goes 0→1→0
                // Simpler: use raw wobble -1..1 through sine shaping
                double wobbleSine = Math.Sin((s.WobbleProgress * 0.5 + 0.5) * Math.PI * 0.5) * 2 - 1;

                double x = s.LeftPercent * w + wobbleSine * s.WobbleMaxPx;
                double y = s.Progress * (h + 100) - 50;

                Canvas.SetLeft(s.Element, x);
                Canvas.SetTop(s.Element, y);
                if (s.Rotate is not null) s.Rotate.Angle = s.Progress * s.RotationSpeed;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  ORBS  — faithful port of Kotlin FloatingOrbsBackground()
        // ═══════════════════════════════════════════════════════════════
        private sealed class OrbState
        {
            public Ellipse Element = null!;
            public RadialGradientBrush Brush = null!;
            public double X, Y, DX, DY;
            public double BaseRadius;
            public double Hue, HueSpeed;
            public double PulsePhase, PulseSpeed;
            public double ShapePhase, ShapeSpeed;
            public double Alpha, TargetMaxAlpha, AlphaSpeed;
            public int State;   // 0 idle, 1 fade-in, 2 hold, 3 fade-out
            public double HoldTimer;
        }

        private void StartOrbs(int count)
        {
            for (int i = 0; i < count; i++)
            {
                var brush = new RadialGradientBrush
                {
                    GradientStops =
                    {
                        new GradientStop(Colors.White, 0),
                        new GradientStop(Colors.White, 0.42),
                        new GradientStop(Colors.Transparent, 1),
                    }
                };
                var ellipse = new Ellipse
                {
                    Fill = brush,
                    IsHitTestVisible = false,
                    Visibility = Visibility.Collapsed,
                };
                var s = new OrbState { Element = ellipse, Brush = brush };
                EffectsLayer.Children.Add(ellipse);
                _orbs.Add(s);
            }
        }

        private void SpawnOrb(OrbState o, double w, double h)
        {
            o.BaseRadius = _rng.NextDouble() * 100 + 80;
            o.X = _rng.NextDouble() * w;
            o.Y = _rng.NextDouble() * h;
            o.DX = (_rng.NextDouble() - 0.5) * 60;
            o.DY = (_rng.NextDouble() - 0.5) * 60;
            o.Hue = _rng.NextDouble() * 360;
            o.HueSpeed = (_rng.NextDouble() - 0.5) * 10;
            o.PulsePhase = _rng.NextDouble() * 2 * Math.PI;
            o.PulseSpeed = _rng.NextDouble() * 1.5 + 0.5;
            o.ShapePhase = _rng.NextDouble() * 2 * Math.PI;
            o.ShapeSpeed = _rng.NextDouble() * 1.5 + 0.5;
            o.Alpha = 0;
            o.TargetMaxAlpha = _rng.NextDouble() * 0.25 + 0.15;
            o.AlphaSpeed = _rng.NextDouble() * 0.05 + 0.02;
            o.HoldTimer = _rng.NextDouble() * 6 + 4;
            o.State = 1;
        }

        private void UpdateOrbs(double dt)
        {
            double w = Math.Max(1, ActualWidth);
            double h = Math.Max(1, ActualHeight);
            if (dt > 0.05) dt = 0.016;
            float fdt = (float)dt;

            foreach (var o in _orbs)
            {
                if (o.State == 0)
                {
                    if (_rng.NextDouble() < 0.01) SpawnOrb(o, w, h);
                    continue;
                }

                o.X += o.DX * fdt;
                o.Y += o.DY * fdt;
                if (o.X < 0 && o.DX < 0) o.DX *= -0.95f;
                if (o.X > w && o.DX > 0) o.DX *= -0.95f;
                if (o.Y < 0 && o.DY < 0) o.DY *= -0.95f;
                if (o.Y > h && o.DY > 0) o.DY *= -0.95f;

                o.Hue = (o.Hue + o.HueSpeed * fdt) % 360;
                if (o.Hue < 0) o.Hue += 360;

                o.PulsePhase += o.PulseSpeed * fdt;
                o.ShapePhase += o.ShapeSpeed * fdt;

                switch (o.State)
                {
                    case 1:
                        o.Alpha += o.AlphaSpeed * fdt;
                        if (o.Alpha >= o.TargetMaxAlpha) { o.Alpha = o.TargetMaxAlpha; o.State = 2; }
                        break;
                    case 2:
                        o.HoldTimer -= fdt;
                        if (o.HoldTimer <= 0) o.State = 3;
                        break;
                    case 3:
                        o.Alpha -= o.AlphaSpeed * fdt;
                        if (o.Alpha <= 0) { o.Alpha = 0; o.State = 0; o.Element.Visibility = Visibility.Collapsed; }
                        break;
                }
            }

            ApplyOrbInteractions(fdt);

            foreach (var o in _orbs)
            {
                if (o.State == 0) continue;

                double pulseFactor = 1 + 0.15 * Math.Sin(o.PulsePhase);
                double coreRadius = o.BaseRadius * pulseFactor;
                double gradientRadius = coreRadius * 2.0;
                double diameter = gradientRadius * 2;

                o.Element.Width = diameter;
                o.Element.Height = diameter;
                o.Element.Visibility = Visibility.Visible;
                Canvas.SetLeft(o.Element, o.X - gradientRadius);
                Canvas.SetTop(o.Element, o.Y - gradientRadius);

                var (r, g, b) = HsvToRgb(o.Hue, 0.85, 1.0);
                byte aOuter = (byte)Math.Min(255, o.Alpha * 1.25 * 255);
                byte aInner = (byte)Math.Min(255, o.Alpha * 255);
                o.Brush.GradientStops[0].Color = Color.FromArgb(aOuter, r, g, b);
                o.Brush.GradientStops[1].Color = Color.FromArgb(aInner, r, g, b);
                o.Brush.GradientStops[2].Color = Colors.Transparent;
            }
        }

        private void ApplyOrbInteractions(float dt)
        {
            const float interactionRadius = 350;
            const float repulsionStrength = 80;
            const float hueBlendFactor = 0.03f;

            for (int i = 0; i < _orbs.Count; i++)
            {
                if (_orbs[i].State == 0) continue;
                for (int j = i + 1; j < _orbs.Count; j++)
                {
                    if (_orbs[j].State == 0) continue;
                    var a = _orbs[i];
                    var b = _orbs[j];
                    double dxPos = b.X - a.X;
                    double dyPos = b.Y - a.Y;
                    double dist = Math.Sqrt(dxPos * dxPos + dyPos * dyPos);
                    if (dist < interactionRadius && dist > 0.1)
                    {
                        double force = (1 - dist / interactionRadius) * repulsionStrength * dt;
                        double fx = (dxPos / dist) * force;
                        double fy = (dyPos / dist) * force;
                        a.DX -= fx; a.DY -= fy;
                        b.DX += fx; b.DY += fy;

                        double hueDiff = b.Hue - a.Hue;
                        double normalized =
                            hueDiff > 180 ? hueDiff - 360 :
                            hueDiff < -180 ? hueDiff + 360 : hueDiff;

                        double shift = normalized * hueBlendFactor * dt;
                        a.Hue = (a.Hue + shift) % 360;
                        b.Hue = (b.Hue - shift) % 360;
                        if (a.Hue < 0) a.Hue += 360;
                        if (b.Hue < 0) b.Hue += 360;
                    }
                }
            }
        }

        private static (byte r, byte g, byte b) HsvToRgb(double h, double s, double v)
        {
            h %= 360; if (h < 0) h += 360;
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c;
            double r1, g1, b1;
            if (h < 60) { r1 = c; g1 = x; b1 = 0; }
            else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
            else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
            else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
            else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }
            return ((byte)Math.Round((r1 + m) * 255),
                    (byte)Math.Round((g1 + m) * 255),
                    (byte)Math.Round((b1 + m) * 255));
        }
    }

    public sealed class InspirationalVerseRef
    {
        public int BookNumber { get; }
        public int Chapter { get; }
        public int Verse { get; }
        public int? EndVerse { get; }
        public InspirationalVerseRef(int b, int c, int v, int? end = null)
        { BookNumber = b; Chapter = c; Verse = v; EndVerse = end; }
    }
}