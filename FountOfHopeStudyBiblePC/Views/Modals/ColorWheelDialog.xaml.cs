// Views/Modals/ColorWheelDialog.xaml.cs
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
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

        // Preset palette (matches Kotlin colorPalette)
        private static readonly Color[] Palette =
        {
            Color.FromRgb(0xEF, 0x44, 0x44), // red
            Color.FromRgb(0xF9, 0x73, 0x16), // orange
            Color.FromRgb(0xF5, 0x9E, 0x0B), // amber
            Color.FromRgb(0xEA, 0xB3, 0x08), // yellow
            Color.FromRgb(0x84, 0xCC, 0x16), // lime
            Color.FromRgb(0x22, 0xC5, 0x5E), // green
            Color.FromRgb(0x10, 0xB9, 0x81), // emerald
            Color.FromRgb(0x14, 0xB8, 0xA6), // teal
            Color.FromRgb(0x06, 0xB6, 0xD4), // cyan
            Color.FromRgb(0x0E, 0xA5, 0xE9), // sky
            Color.FromRgb(0x3B, 0x82, 0xF6), // blue
            Color.FromRgb(0x63, 0x66, 0xF1), // indigo
            Color.FromRgb(0x8B, 0x5C, 0xF6), // violet
            Color.FromRgb(0xA8, 0x55, 0xF7), // purple
            Color.FromRgb(0xD9, 0x46, 0xEF), // fuchsia
            Color.FromRgb(0xEC, 0x48, 0x99), // pink
            Color.FromRgb(0x6B, 0x72, 0x80), // gray
            Colors.Black,
            Colors.White,
        };

        // ── Constructor ────────────────────────────────────────────
        public ColorWheelDialog(
            Color initialColor,
            Action<Color> onColorSelected,
            Action onDismiss)
        {
            // Assign fields BEFORE InitializeComponent so any slider event fired during parsing hits valid values
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
        }

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
            // Preserve current opacity from the slider
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

            // Don't fight the user while typing; only apply if valid and different
            if (ColorHelpers.TryParseHex(sanitized, out var parsed))
            {
                HexError.Visibility = Visibility.Collapsed;

                // Preserve alpha from the slider if hex has 6 chars
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
            // If the user typed an invalid hex, reset to the current valid color
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
            // If the hex box has invalid content, refuse
            if (!ColorHelpers.TryParseHex(HexBox.Text ?? "", out _))
            {
                HexError.Visibility = Visibility.Visible;
                return;
            }

            _onColorSelected(_selectedColor);
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            _onDismiss();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _onDismiss();
        }
    }
}