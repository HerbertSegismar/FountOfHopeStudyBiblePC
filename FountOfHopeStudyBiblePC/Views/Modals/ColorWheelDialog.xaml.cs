using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Helpers;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class ColorWheelDialog : Window
    {
        private static readonly Action NoOp = () => { };
        private static readonly Action<Color> NoOpColor = _ => { };
        private static readonly SolidColorBrush PaletteSwatchBorderBrush =
            Frozen(Color.FromArgb(0x44, 0xFF, 0xFF, 0xFF));

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

        private static SolidColorBrush Frozen(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        private readonly Action<Color> _onColorSelected;
        private readonly Action _onDismiss;
        private readonly ModalOverlayController _overlay;

        private SolidColorBrush _currentBrush = Frozen(Colors.Black);
        private Color _selectedColor;
        private bool _suppressHexEvent;
        private bool _suppressSliderEvent;

        public ColorWheelDialog(
            Color initialColor,
            Action<Color> onColorSelected,
            Action onDismiss)
        {
            _onColorSelected = onColorSelected ?? NoOpColor;
            _onDismiss = onDismiss ?? NoOp;
            _selectedColor = initialColor;
            _suppressSliderEvent = true;
            _suppressHexEvent = true;

            InitializeComponent();

            _overlay = new ModalOverlayController(this);

            ColorWheel.Brightness = ColorHelpers.GetBrightness(initialColor);
            ColorWheel.SelectedColor = initialColor;

            BuildPalette();
            ApplyColor(initialColor, updateWheel: true, updateOpacity: true);

            _suppressSliderEvent = false;
            _suppressHexEvent = false;
        }

        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Window_KeyDown(object sender, KeyEventArgs e)
            => _overlay.HandleKeyDown(sender, e);

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
                    Background = Frozen(color),
                    BorderBrush = PaletteSwatchBorderBrush,
                    BorderThickness = new Thickness(1),
                    Cursor = Cursors.Hand,
                    Tag = color,
                };
                swatch.MouseLeftButtonUp += (_, _) =>
                {
                    if (swatch.Tag is Color c)
                        ApplyColor(c, updateWheel: true, updateOpacity: true);
                };
                PalettePanel.Children.Add(swatch);
            }
        }

        private void ApplyColor(Color color, bool updateWheel, bool updateOpacity)
        {
            _selectedColor = color;

            if (!_currentBrush.IsFrozen || _currentBrush.Color != color)
                _currentBrush = Frozen(color);

            ColorSwatch.Background = _currentBrush;
            LightPreview.Background = _currentBrush;
            DarkPreview.Background = _currentBrush;

            _suppressHexEvent = true;
            HexBox.Text = ColorHelpers.ToHexString(color);
            HexBox.CaretIndex = HexBox.Text.Length;
            _suppressHexEvent = false;
            HexError.Visibility = Visibility.Collapsed;

            var (_, sat, val) = ColorHelpers.ToHsv(color);

            _suppressSliderEvent = true;
            BrightnessSlider.Value = val;
            SaturationSlider.Value = sat;
            if (updateOpacity) OpacitySlider.Value = color.A / 255.0;
            _suppressSliderEvent = false;

            BrightnessLabel.Text = $"{(int)(val * 100)}%";
            SaturationLabel.Text = $"{(int)(sat * 100)}%";
            if (updateOpacity) OpacityLabel.Text = $"{(int)(color.A / 255.0 * 100)}%";

            if (updateWheel)
            {
                ColorWheel.Brightness = (float)val;
                ColorWheel.SelectedColor = color;
            }
        }

        private void ColorWheel_ColorSelected(object? sender, Color color)
        {
            byte alpha = (byte)Math.Round(OpacitySlider.Value * 255);
            var withAlpha = Color.FromArgb(alpha, color.R, color.G, color.B);
            ApplyColor(withAlpha, updateWheel: false, updateOpacity: false);
        }

        private void BrightnessSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvent) return;
            BrightnessLabel.Text = $"{(int)(e.NewValue * 100)}%";
            ApplyColor(ColorHelpers.AdjustBrightness(_selectedColor, (float)e.NewValue),
                updateWheel: true, updateOpacity: true);
        }

        private void SaturationSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvent) return;
            SaturationLabel.Text = $"{(int)(e.NewValue * 100)}%";
            ApplyColor(ColorHelpers.AdjustSaturation(_selectedColor, (float)e.NewValue),
                updateWheel: true, updateOpacity: true);
        }

        private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (_suppressSliderEvent) return;
            OpacityLabel.Text = $"{(int)(e.NewValue * 100)}%";
            ApplyColor(ColorHelpers.AdjustOpacity(_selectedColor, (float)e.NewValue),
                updateWheel: false, updateOpacity: true);
        }

        private void HexBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_suppressHexEvent) return;

            var sanitized = ColorHelpers.SanitizeHexInput(HexBox.Text ?? "");

            if (!ColorHelpers.TryParseHex(sanitized, out var parsed))
            {
                HexError.Visibility = sanitized.Length > 1
                    ? Visibility.Visible
                    : Visibility.Collapsed;
                return;
            }

            HexError.Visibility = Visibility.Collapsed;

            if (sanitized.Length == 7)
                parsed = Color.FromArgb((byte)Math.Round(OpacitySlider.Value * 255),
                                        parsed.R, parsed.G, parsed.B);

            ApplyColor(parsed, updateWheel: true, updateOpacity: true);
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
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => _overlay.DismissWithFade();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _onDismiss();
        }
    }
}