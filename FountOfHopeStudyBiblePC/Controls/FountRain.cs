// Controls/FountRain.cs
// WPF port of the Compose `FountRain` composable.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace FountOfHopeStudyBiblePC.Controls
{
    /// <summary>
    /// Matrix-style "digital rain" of characters with randomly fading
    /// Jesus-attribute overlays, shown inside a rounded Card.
    /// </summary>
    public class FountRain : UserControl
    {
        // ---------- Constants (mirror the Kotlin version) ----------
        private const int TRAIL_LENGTH = 15;
        private const double MATRIX_HEIGHT = 612;
        private const double FONT_SIZE = 12;

        private const string MatrixChars =
            "0123456789" +
            "abcdefghijklmnopqrstuvwxyz" +
            "ABCDEFGHIJKLMNOPQRSTUVWXYZ" +
            "~!@#$%^&*()_-+=" +
            "ᜀᜁᜂᜃᜄᜅᜆᜇᜈᜉᜊᜋᜌᜎᜏᜐᜑ" +
            "αβγδεζηθικλμνξοπρστυφχψω" +
            "ΑΒΓΔΕΖΗΘΙΚΛΜΝΞΟΠΡΣΤΥΦΧΨΩ" +
            "אבגדהוזחטיכלמנסעפצקרשתךםןףץ";

        private static readonly string[] JesusAttributes =
        {
            "Jesus Christ", "Messiah", "Savior", "Redeemer", "Son of God",
            "Lamb of God", "King of Kings", "Lord of Lords", "Prince of Peace",
            "Alpha and Omega", "The Way", "The Truth", "The Life", "Good Shepherd",
            "Light of the World", "Bread of Life", "The Resurrection", "Emmanuel",
            "Wonderful Counselor", "Mighty God", "Everlasting Father", "The Word",
            "Son of Man", "The Door", "The Vinedresser", "True Vine", "The Amen",
            "Author and Finisher of Our Faith", "Chief Cornerstone",
            "Bright Morning Star", "Lion of the Tribe of Judah", "Root of David",
            "Holy One of Israel", "Bridegroom", "Head of the Church", "Mediator",
            "Great High Priest", "The Prophet", "The Rock", "Stone of Stumbling",
            "Captain of Our Salvation", "Chosen One",
            "Image of the Invisible God", "Firstborn Over All Creation",
            "Firstborn from the Dead", "The Righteous One", "I AM",
            "The Great I Am", "Lord of All", "Judge of the Living and the Dead",
            "Shiloh", "Sun of Righteousness", "The Branch", "Man of Sorrows",
            "Faithful and True Witness", "Lord of Glory", "The Power of God",
            "The Wisdom of God", "Our Passover Lamb", "Shepherd of Souls",
            "The Resurrection and the Life", "The Holy One", "The Just One",
            "The Advocate", "The Deliverer", "The Hope of Nations",
            "The Consolation of Israel", "The Desire of All Nations",
            "The Fountain of Living Waters", "The Rod from the Stem of Jesse",
            "The Governor Among the Nations", "The Word of Life",
            "The Spirit of Life", "The Beloved Son", "The Light of Men",
            "The True Light", "The Horn of Salvation",
            "The Dayspring from on High", "The Upholder of All Things",
            "The Apostle of Our Confession", "The Bishop of Souls",
            "The Christ of God", "The Holy Servant",
            "The Pioneer of Salvation", "The Author of Eternal Salvation",
            "The Forerunner", "The Lawgiver", "The Lord of the Harvest",
            "The Lord of the Sabbath", "The Truth of God", "The Vine",
            "The Living Stone", "The Chosen Stone",
            "The Precious Cornerstone", "The Foundation", "The Temple",
            "The Light of Heaven", "The King of the Jews", "The King of Israel",
            "The King of Righteousness", "The King of Peace", "The King of Glory",
            "The Lord Strong and Mighty", "The Lord Mighty in Battle",
            "The Lord of Hosts", "The Lord Our Righteousness",
            "The Lord Who Heals", "The Lord Who Provides",
            "The Lord Who Sanctifies", "The Lord Who Sees",
            "The Angel of God", "The Angel of the Lord",
            "Yahweh", "Jehovah", "Elohim", "El Shaddai", "Adonai",
            "Jehovah Jireh", "Jehovah Rapha", "Jehovah Nissi", "Jehovah Shalom",
            "Jehovah Raah", "Jehovah Tsidkenu", "Jehovah Shammah",
            "El Elyon", "El Roi", "El Olam",
            "Yahweh Yireh", "Yahweh Rapha", "Yahweh Nissi", "Yahweh Shalom",
            "Yahweh Raah", "Yahweh Tsidkenu", "Yahweh Shammah", "Yahweh Sabaoth",
            "God With Us", "The Great High Priest", "Prayer-answering God"
        };

        // ---------- Nested types ----------
        private sealed class RainDrop
        {
            public string[] Trail = Array.Empty<string>();
            public double X;
            public double HeadY;
            public double ElapsedMs;       // time spent in current fall
            public double FallDurationMs;  // 2000..5000
            public bool Waiting;
            public double GapRemainingMs;  // pause before next fall
        }

        private sealed class AttributeOverlay
        {
            public string Text = "";
            public double Left;
            public double BaseTop;
            public double FontSize;
            public double ElapsedMs;
            public const double FadeInMs = 500;
            public const double HoldMs = 2000;
            public const double FadeOutMs = 1500;
            public double TotalMs => FadeInMs + HoldMs + FadeOutMs;
        }

        // ---------- Dependency properties ----------
        public static readonly DependencyProperty MatrixColorProperty =
            DependencyProperty.Register(nameof(MatrixColor), typeof(Color),
                typeof(FountRain),
                new PropertyMetadata(Color.FromRgb(0x7F, 0xB2, 0xFF),
                    (d, _) => ((FountRain)d).RefreshTitleAndRain()));

        public Color MatrixColor
        {
            get => (Color)GetValue(MatrixColorProperty);
            set => SetValue(MatrixColorProperty, value);
        }

        public static readonly DependencyProperty OverlayColorProperty =
            DependencyProperty.Register(nameof(OverlayColor), typeof(Color),
                typeof(FountRain),
                new PropertyMetadata(Color.FromRgb(0x7F, 0xB2, 0xFF),
                    (d, _) => ((FountRain)d)._rain.InvalidateVisual()));

        public Color OverlayColor
        {
            get => (Color)GetValue(OverlayColorProperty);
            set => SetValue(OverlayColorProperty, value);
        }

        public static readonly DependencyProperty MatrixFontFamilyProperty =
            DependencyProperty.Register(nameof(MatrixFontFamily), typeof(FontFamily),
                typeof(FountRain), new PropertyMetadata(new FontFamily("Consolas")));

        public FontFamily MatrixFontFamily
        {
            get => (FontFamily)GetValue(MatrixFontFamilyProperty);
            set => SetValue(MatrixFontFamilyProperty, value);
        }

        public static readonly DependencyProperty OverlayFontFamilyProperty =
            DependencyProperty.Register(nameof(OverlayFontFamily), typeof(FontFamily),
                typeof(FountRain), new PropertyMetadata(new FontFamily("Segoe UI")));

        public FontFamily OverlayFontFamily
        {
            get => (FontFamily)GetValue(OverlayFontFamilyProperty);
            set => SetValue(OverlayFontFamilyProperty, value);
        }

        public static readonly DependencyProperty IsDarkThemeProperty =
            DependencyProperty.Register(nameof(IsDarkTheme), typeof(bool),
                typeof(FountRain), new PropertyMetadata(true,
                    (d, _) => ((FountRain)d).UpdateCardBackground()));

        public bool IsDarkTheme
        {
            get => (bool)GetValue(IsDarkThemeProperty);
            set => SetValue(IsDarkThemeProperty, value);
        }

        // ---------- State ----------
        private readonly List<RainDrop> _drops = new();
        private readonly Random _rng = new();
        private readonly DispatcherTimer _timer;
        private DateTime _lastTickUtc = DateTime.UtcNow;
        private double _fontSizeDip = FONT_SIZE;

        private AttributeOverlay? _overlay;
        private double _overlayWaitMs;

        private readonly Border _cardBorder;
        private readonly TextBlock _title;
        private readonly RainElement _rain;

        // Cache of FormattedText per character so we don't rebuild them every frame.
        private readonly Dictionary<(char c, double size, FontFamily fam), FormattedText> _glyphCache
            = new();

        public FountRain()
        {
            // ----- Card container -----
            _cardBorder = new Border
            {
                CornerRadius = new CornerRadius(16),
                Padding = new Thickness(16),
                Margin = new Thickness(16),
                Background = Brushes.Transparent,
                SnapsToDevicePixels = true
            };

            var stack = new StackPanel { Orientation = Orientation.Vertical };

            _title = new TextBlock
            {
                Text = "Here is the Fount of Hope",
                TextAlignment = TextAlignment.Center,
                FontSize = FONT_SIZE * 1.5,
                Margin = new Thickness(0, 0, 0, 26),
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Foreground = new SolidColorBrush(MatrixColor)
            };
            stack.Children.Add(_title);

            _rain = new RainElement(this)
            {
                Height = MATRIX_HEIGHT,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Top,
                ClipToBounds = true
            };
            stack.Children.Add(_rain);

            _cardBorder.Child = stack;
            Content = _cardBorder;

            UpdateCardBackground();

            // 60 fps tick
            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _timer.Tick += (_, __) => OnTick();

            Loaded += (_, __) => { RebuildDrops(); _lastTickUtc = DateTime.UtcNow; _timer.Start(); };
            Unloaded += (_, __) => _timer.Stop();
        }

        private void RefreshTitleAndRain()
        {
            _title.Foreground = new SolidColorBrush(MatrixColor);
            _rain.InvalidateVisual();
        }

        private void UpdateCardBackground()
        {
            // Mirrors:  if (darkTheme) Color.Black.copy(0.5f)
            //           else            colorScheme.primary.copy(0.1f)
            _cardBorder.Background = IsDarkTheme
                ? new SolidColorBrush(Color.FromArgb(128, 0, 0, 0))
                : new SolidColorBrush(Color.FromArgb(26, 0x7F, 0xB2, 0xFF));
        }

        // ---------- RainDrop build / update ----------

        private void RebuildDrops()
        {
            _drops.Clear();
            var w = _rain.ActualWidth;
            if (w <= 0 || _fontSizeDip <= 0) return;

            var numColumns = (int)(w / _fontSizeDip);
            if (numColumns <= 0) return;
            var numDrops = (int)Math.Round(numColumns * 0.7);

            // Fisher–Yates shuffle of columns, take first numDrops.
            var cols = new int[numColumns];
            for (int i = 0; i < numColumns; i++) cols[i] = i;
            for (int i = numColumns - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                (cols[i], cols[j]) = (cols[j], cols[i]);
            }

            var totalHeight = MATRIX_HEIGHT + TRAIL_LENGTH * _fontSizeDip;
            for (int k = 0; k < numDrops && k < numColumns; k++)
            {
                var trail = new string[TRAIL_LENGTH];
                for (int t = 0; t < TRAIL_LENGTH; t++)
                    trail[t] = MatrixChars[_rng.Next(MatrixChars.Length)].ToString();

                // Stagger so drops don't all start at the top at once.
                var initialHeadY = _rng.NextDouble() * totalHeight;

                _drops.Add(new RainDrop
                {
                    Trail = trail,
                    X = cols[k] * _fontSizeDip,
                    HeadY = initialHeadY,
                    ElapsedMs = _rng.Next(2000, 5000),
                    FallDurationMs = 2000 + _rng.Next(3000),
                    Waiting = false
                });
            }
        }

        private void OnTick()
        {
            var now = DateTime.UtcNow;
            var dtMs = (now - _lastTickUtc).TotalMilliseconds;
            _lastTickUtc = now;
            if (dtMs <= 0) return;
            if (dtMs > 100) dtMs = 100; // clamp after a stall

            var totalHeight = MATRIX_HEIGHT + TRAIL_LENGTH * _fontSizeDip;

            foreach (var drop in _drops)
            {
                if (drop.Waiting)
                {
                    drop.GapRemainingMs -= dtMs;
                    if (drop.GapRemainingMs <= 0)
                    {
                        drop.Waiting = false;
                        drop.HeadY = 0;
                        drop.ElapsedMs = 0;
                        drop.FallDurationMs = 2000 + _rng.Next(3000);
                    }
                    continue;
                }

                drop.ElapsedMs += dtMs;
                var t = drop.ElapsedMs / drop.FallDurationMs;
                if (t >= 1.0)
                {
                    drop.HeadY = totalHeight;
                    drop.Waiting = true;
                    drop.GapRemainingMs = 1000 + _rng.Next(3000);
                }
                else
                {
                    drop.HeadY = t * totalHeight;
                }
            }

            UpdateOverlay(dtMs);
            _rain.InvalidateVisual();
        }

        // ---------- Overlay (Jesus attributes) ----------

        private void UpdateOverlay(double dtMs)
        {
            if (_overlay == null)
            {
                _overlayWaitMs -= dtMs;
                if (_overlayWaitMs <= 0) SpawnOverlay();
                return;
            }

            _overlay.ElapsedMs += dtMs;
            if (_overlay.ElapsedMs >= _overlay.TotalMs)
            {
                _overlay = null;
                _overlayWaitMs = 0; // pick a new one immediately (matches Kotlin)
            }
        }

        private void SpawnOverlay()
        {
            var w = _rain.ActualWidth;
            var h = _rain.ActualHeight;
            if (w <= 0 || h <= 0) { _overlayWaitMs = 200; return; }

            var text = JesusAttributes[_rng.Next(JesusAttributes.Length)];
            var fs = _rng.NextDouble() * 12 + 10; // 10..22
            var estW = text.Length * (fs / 2.5);
            var left = _rng.NextDouble() * 0.8 * w;
            left = Math.Min(left, Math.Max(0, w - estW));
            var top = _rng.NextDouble() * 0.8 * h;

            _overlay = new AttributeOverlay
            {
                Text = text,
                Left = left,
                BaseTop = top,
                FontSize = fs,
                ElapsedMs = 0
            };
        }

        // ---------- Rendering ----------

        private sealed class RainElement : FrameworkElement
        {
            private readonly FountRain _owner;
            public RainElement(FountRain owner) { _owner = owner; Focusable = false; }

            protected override void OnRenderSizeChanged(SizeChangedInfo info)
            {
                base.OnRenderSizeChanged(info);
                _owner.RebuildDrops();
                _owner._lastTickUtc = DateTime.UtcNow;
            }

            protected override void OnRender(DrawingContext dc)
            {
                base.OnRender(dc);
                _owner.RenderRain(dc, ActualWidth, ActualHeight);
            }
        }

        private void RenderRain(DrawingContext dc, double width, double height)
        {
            // --- Matrix drops ---
            foreach (var drop in _drops)
            {
                for (int j = 0; j < TRAIL_LENGTH; j++)
                {
                    var y = drop.HeadY - j * _fontSizeDip;
                    if (y <= 0 || y >= height) continue;

                    double opacity = j == 0
                        ? 1.0
                        : Math.Max(0.0, 1.0 - (j / (double)TRAIL_LENGTH) * 1.2);

                    var brush = new SolidColorBrush(MatrixColor) { Opacity = opacity };
                    brush.Freeze();

                    var c = drop.Trail[j][0];
                    var ft = GetGlyph(c, _fontSizeDip, MatrixFontFamily);
                    ft.SetForegroundBrush(brush);
                    dc.DrawText(ft, new Point(drop.X, y - ft.Baseline));
                }
            }

            // --- Jesus attribute overlay ---
            if (_overlay != null)
            {
                var e = _overlay.ElapsedMs;
                double alpha, yOffset;

                if (e < AttributeOverlay.FadeInMs)
                {
                    var t = e / AttributeOverlay.FadeInMs;
                    alpha = t;
                    yOffset = 20 * (1 - t);
                }
                else if (e < AttributeOverlay.FadeInMs + AttributeOverlay.HoldMs)
                {
                    alpha = 1;
                    yOffset = 0;
                }
                else
                {
                    var t = (e - AttributeOverlay.FadeInMs - AttributeOverlay.HoldMs)
                            / AttributeOverlay.FadeOutMs;
                    alpha = 1 - t;
                    yOffset = -20 * t;
                }

                var brush = new SolidColorBrush(OverlayColor)
                {
                    Opacity = Math.Max(0, Math.Min(1, alpha))
                };
                brush.Freeze();

                var typeface = new Typeface(
                    OverlayFontFamily, FontStyles.Normal,
                    FontWeights.Normal, FontStretches.Normal);

                var ft = new FormattedText(
                    _overlay.Text,
                    CultureInfo.InvariantCulture,
                    FlowDirection.LeftToRight,
                    typeface,
                    _overlay.FontSize,
                    brush,
                    VisualTreeHelper.GetDpi(this).PixelsPerDip);

                dc.DrawText(ft,
                    new Point(_overlay.Left, _overlay.BaseTop + yOffset));
            }
        }

        private FormattedText GetGlyph(char c, double size, FontFamily family)
        {
            var key = (c, size, family);
            if (_glyphCache.TryGetValue(key, out var ft)) return ft;

            var typeface = new Typeface(
                family, FontStyles.Normal,
                FontWeights.Normal, FontStretches.Normal);

            ft = new FormattedText(
                c.ToString(),
                CultureInfo.InvariantCulture,
                FlowDirection.LeftToRight,
                typeface,
                size,
                Brushes.White, // placeholder, replaced per draw
                VisualTreeHelper.GetDpi(this).PixelsPerDip);

            _glyphCache[key] = ft;
            return ft;
        }
    }
}