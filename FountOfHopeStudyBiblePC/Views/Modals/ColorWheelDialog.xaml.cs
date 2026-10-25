// Views/Modals/ColorWheelDialog.xaml.cs
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FountOfHopeStudyBiblePC.Helpers;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class ColorWheelDialog : Window
    {
        private readonly Action<Color> _onColorSelected;
        private readonly Action _onDismiss;

        private Color _selectedColor;
        private bool _suppressHexEvent;
        private bool _suppressSliderEvent;

        private bool _isDismissing;
        private bool _dismissArmed;

        /// <summary>Close the dialog if the owner window is resized.</summary>
        public bool CloseOnOwnerResize { get; set; } = true;

        private static readonly Color[] Palette =
        {
            Color.FromRgb(0xEF, 0x44, 0x44),
            Color.FromRgb(0xF9, 0x73, 0x16),
            Color.FromRgb(0xF5, 0x9E, 0x0B),
            Color.FromRgb(0xEA, 0xB3, 0x08),
            Color.FromRgb(0x84, 0xCC, 0x16),
            Color.FromRgb(0x22, 0xC5, 0x5E),
            Color.FromRgb(0x10, 0xB9, 0x81),
            Color.FromRgb(0x14, 0xB8, 0xA6),
            Color.FromRgb(0x06, 0xB6, 0xD4),
            Color.FromRgb(0x0E, 0xA5, 0xE9),
            Color.FromRgb(0x3B, 0x82, 0xF6),
            Color.FromRgb(0x63, 0x66, 0xF1),
            Color.FromRgb(0x8B, 0x5C, 0xF6),
            Color.FromRgb(0xA8, 0x55, 0xF7),
            Color.FromRgb(0xD9, 0x46, 0xEF),
            Color.FromRgb(0xEC, 0x48, 0x99),
            Color.FromRgb(0x6B, 0x72, 0x80),
            Colors.Black,
            Colors.White,
        };

        // ── Constructor ────────────────────────────────────────────
        public ColorWheelDialog(
            Color initialColor,
            Action<Color> onColorSelected,
            Action onDismiss)
        {
            _onColorSelected = onColorSelected ?? (_ => { });
            _onDismiss = onDismiss ?? (() => { });
            _selectedColor = initialColor;
            _suppressSliderEvent = true;
            _suppressHexEvent = true;

            InitializeComponent();

            ColorWheel.Brightness = ColorHelpers.GetBrightness(initialColor);
            ColorWheel.SelectedColor = initialColor;

            BuildPalette();
            SyncUiFromColor(initialColor);

            _suppressSliderEvent = false;
            _suppressHexEvent = false;

            Loaded += OnLoaded;
        }

        // ═══════════════════════════════════════════════════════════
        //  Scrim dismiss infrastructure
        // ═══════════════════════════════════════════════════════════
        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only arm dismissal when the press starts on the scrim.
            // Sliders, TextBox, ColorWheel, palette swatches, buttons all
            // keep their own mouse lifecycle untouched.
            if (!ReferenceEquals(e.OriginalSource, Scrim))
            {
                _dismissArmed = false;
                return;
            }

            _dismissArmed = true;
            Scrim.CaptureMouse();
            e.Handled = true;
        }

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            bool shouldDismiss = _dismissArmed;
            _dismissArmed = false;

            if (Scrim.IsMouseCaptured)
                Scrim.ReleaseMouseCapture();

            if (shouldDismiss)
                DismissWithFade();

            e.Handled = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DismissWithFade();
                e.Handled = true;
            }
        }

        private void DismissWithFade()
        {
            if (_isDismissing) return;
            _isDismissing = true;

            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120));
            fade.Completed += (_, _) => Close();
            BeginAnimation(OpacityProperty, fade);
        }

        // ═══════════════════════════════════════════════════════════
        //  Owner bounds tracking
        // ═══════════════════════════════════════════════════════════
        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (Owner is null) return;
            Owner.LocationChanged += Owner_LocationChanged;
            Owner.SizeChanged += Owner_SizeChanged;
            Owner.StateChanged += Owner_StateChanged;
            Owner.Closed += Owner_Closed;
            SyncBoundsToOwner();
        }

        private void SyncBoundsToOwner()
        {
            if (Owner is null) return;
            WindowStartupLocation = WindowStartupLocation.Manual;

            if (Owner.WindowState == WindowState.Maximized)
            {
                WindowState = WindowState.Maximized;
                return;
            }
            WindowState = WindowState.Normal;
            Left = Owner.Left;
            Top = Owner.Top;
            Width = Owner.ActualWidth;
            Height = Owner.ActualHeight;
        }

        private void Owner_LocationChanged(object? sender, EventArgs e)
        {
            if (Owner is null) return;
            Left = Owner.Left;
            Top = Owner.Top;
        }

        private void Owner_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (CloseOnOwnerResize) { DismissWithFade(); return; }
            if (Owner is null) return;
            Left = Owner.Left;
            Top = Owner.Top;
            Width = Owner.ActualWidth;
            Height = Owner.ActualHeight;
        }

        private void Owner_StateChanged(object? sender, EventArgs e)
        {
            if (Owner?.WindowState == WindowState.Minimized) DismissWithFade();
        }

        private void Owner_Closed(object? sender, EventArgs e) => DismissWithFade();

        // ═══════════════════════════════════════════════════════════
        //  Palette
        // ═══════════════════════════════════════════════════════════
        private void BuildPalette()
        {
            PalettePanel.Children.Clear();
            foreach (var color in Palette)
            {
                var swatch = new Border
                {
                    Width = 28,
                    Height = 28,
                    CornerRadius = new CornerRadius(14),
                    Margin = new Thickness(3),
                    Background = new SolidColorBrush(color),
                    BorderBrush = new SolidColorBrush(Color.FromArgb(0x44, 0xFF, 0xFF, 0xFF)),
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    Tag = color,
                };
                swatch.MouseLeftButtonUp += (_, _) =>
                {
                    if (swatch.Tag is Color c)
                        SetColor(c, updateWheel: true);
                };
                PalettePanel.Children.Add(swatch);
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Sync
        // ═══════════════════════════════════════════════════════════
        private void SyncUiFromColor(Color color)
        {
            _selectedColor = color;

            var brush = new SolidColorBrush(color);
            ColorSwatch.Background = brush;
            LightPreview.Background = brush;
            DarkPreview.Background = brush;

            _suppressHexEvent = true;
            HexBox.Text = ColorHelpers.ToHexString(color);
            HexBox.CaretIndex = HexBox.Text.Length;
            _suppressHexEvent = false;
            HexError.Visibility = Visibility.Collapsed;

            var (_, sat, val) = ColorHelpers.ToHsv(color);

            _suppressSliderEvent = true;
            BrightnessSlider.Value = val;
            SaturationSlider.Value = sat;
            OpacitySlider.Value = color.A / 255.0;
            _suppressSliderEvent = false;

            BrightnessLabel.Text = $"{(int)(val * 100)}%";
            SaturationLabel.Text = $"{(int)(sat * 100)}%";
            OpacityLabel.Text = $"{(int)(color.A / 255.0 * 100)}%";

            ColorWheel.Brightness = (float)val;
            ColorWheel.SelectedColor = color;
        }

        private void SetColor(Color color, bool updateWheel)
        {
            _selectedColor = color;

            var brush = new SolidColorBrush(color);
            ColorSwatch.Background = brush;
            LightPreview.Background = brush;
            DarkPreview.Background = brush;

            _suppressHexEvent = true;
            HexBox.Text = ColorHelpers.ToHexString(color);
            HexBox.CaretIndex = HexBox.Text.Length;
            _suppressHexEvent = false;
            HexError.Visibility = Visibility.Collapsed;

            var (_, sat, val) = ColorHelpers.ToHsv(color);

            _suppressSliderEvent = true;
            BrightnessSlider.Value = val;
            SaturationSlider.Value = sat;
            OpacitySlider.Value = color.A / 255.0;
            _suppressSliderEvent = false;

            BrightnessLabel.Text = $"{(int)(val * 100)}%";
            SaturationLabel.Text = $"{(int)(sat * 100)}%";
            OpacityLabel.Text = $"{(int)(color.A / 255.0 * 100)}%";

            if (updateWheel)
            {
                ColorWheel.Brightness = (float)val;
                ColorWheel.SelectedColor = color;
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Event handlers
        // ═══════════════════════════════════════════════════════════
        private void ColorWheel_ColorSelected(object? sender, Color color)
        {
            byte alpha = (byte)Math.Round(OpacitySlider.Value * 255);
            var withAlpha = Color.FromArgb(alpha, color.R, color.G, color.B);

            _selectedColor = withAlpha;

            var brush = new SolidColorBrush(withAlpha);
            ColorSwatch.Background = brush;
            LightPreview.Background = brush;
            DarkPreview.Background = brush;

            _suppressHexEvent = true;
            HexBox.Text = ColorHelpers.ToHexString(withAlpha);
            HexBox.CaretIndex = HexBox.Text.Length;
            _suppressHexEvent = false;
            HexError.Visibility = Visibility.Collapsed;

            var (_, sat, val) = ColorHelpers.ToHsv(withAlpha);

            _suppressSliderEvent = true;
            BrightnessSlider.Value = val;
            SaturationSlider.Value = sat;
            _suppressSliderEvent = false;

            BrightnessLabel.Text = $"{(int)(val * 100)}%";
            SaturationLabel.Text = $"{(int)(sat * 100)}%";
        }

        private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvent) return;
            BrightnessLabel.Text = $"{(int)(e.NewValue * 100)}%";
            var newColor = ColorHelpers.AdjustBrightness(_selectedColor, (float)e.NewValue);
            SetColor(newColor, updateWheel: true);
        }

        private void SaturationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvent) return;
            SaturationLabel.Text = $"{(int)(e.NewValue * 100)}%";
            var newColor = ColorHelpers.AdjustSaturation(_selectedColor, (float)e.NewValue);
            SetColor(newColor, updateWheel: true);
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvent) return;
            OpacityLabel.Text = $"{(int)(e.NewValue * 100)}%";
            var newColor = ColorHelpers.AdjustOpacity(_selectedColor, (float)e.NewValue);
            SetColor(newColor, updateWheel: false);
        }

        private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressHexEvent) return;

            var sanitized = ColorHelpers.SanitizeHexInput(HexBox.Text ?? "");

            if (ColorHelpers.TryParseHex(sanitized, out var parsed))
            {
                HexError.Visibility = Visibility.Collapsed;

                if (sanitized.Length == 7)
                    parsed = Color.FromArgb((byte)Math.Round(OpacitySlider.Value * 255),
                                            parsed.R, parsed.G, parsed.B);

                _selectedColor = parsed;

                var brush = new SolidColorBrush(parsed);
                ColorSwatch.Background = brush;
                LightPreview.Background = brush;
                DarkPreview.Background = brush;

                var (_, sat, val) = ColorHelpers.ToHsv(parsed);

                _suppressSliderEvent = true;
                BrightnessSlider.Value = val;
                SaturationSlider.Value = sat;
                OpacitySlider.Value = parsed.A / 255.0;
                _suppressSliderEvent = false;

                BrightnessLabel.Text = $"{(int)(val * 100)}%";
                SaturationLabel.Text = $"{(int)(sat * 100)}%";
                OpacityLabel.Text = $"{(int)(parsed.A / 255.0 * 100)}%";

                ColorWheel.Brightness = (float)val;
                ColorWheel.SelectedColor = parsed;
            }
            else
            {
                HexError.Visibility = sanitized.Length > 1
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void HexBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (!ColorHelpers.TryParseHex(HexBox.Text ?? "", out _))
            {
                _suppressHexEvent = true;
                HexBox.Text = ColorHelpers.ToHexString(_selectedColor);
                HexBox.CaretIndex = HexBox.Text.Length;
                _suppressHexEvent = false;
                HexError.Visibility = Visibility.Collapsed;
            }
        }

        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            if (!ColorHelpers.TryParseHex(HexBox.Text ?? "", out _))
            {
                HexError.Visibility = Visibility.Visible;
                return;
            }

            _onColorSelected(_selectedColor);
            Close();   // confirm action → immediate
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => DismissWithFade();

        protected override void OnClosed(EventArgs e)
        {
            if (Owner is not null)
            {
                Owner.LocationChanged -= Owner_LocationChanged;
                Owner.SizeChanged -= Owner_SizeChanged;
                Owner.StateChanged -= Owner_StateChanged;
                Owner.Closed -= Owner_Closed;
            }
            base.OnClosed(e);
            _onDismiss();
        }
    }
}