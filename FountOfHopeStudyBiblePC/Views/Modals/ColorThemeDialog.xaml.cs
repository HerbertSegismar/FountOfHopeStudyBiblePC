// Views/Modals/ColorThemeDialog.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class ColorThemeDialog : Window
    {
        private readonly Action<Color> _onColorSelected;
        private readonly Action _onCustomColorClick;
        private readonly Action _onDismiss;
        private readonly Color _currentColor;
        private readonly ModalOverlayController _overlay;

        public ColorThemeDialog(
            Color currentColor,
            Action<Color> onColorSelected,
            Action onCustomColorClick,
            Action onDismiss)
        {
            _currentColor = currentColor;
            _onColorSelected = onColorSelected ?? (_ => { });
            _onCustomColorClick = onCustomColorClick ?? (() => { });
            _onDismiss = onDismiss ?? (() => { });

            InitializeComponent();

            _overlay = new ModalOverlayController(this);

            BuildPresets();
        }

        // ── Thin XAML stubs → ModalOverlayController ─────────────
        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Window_KeyDown(object sender, KeyEventArgs e)
            => _overlay.HandleKeyDown(sender, e);

        // ═══════════════════════════════════════════════════════════
        //  Presets
        // ═══════════════════════════════════════════════════════════
        private void BuildPresets()
        {
            PresetGrid.Children.Clear();

            foreach (var theme in PredefinedColorThemes.All)
            {
                var isSelected = ColorsEqual(theme.PrimaryColor, _currentColor);

                var card = new Border
                {
                    Background = new SolidColorBrush(Color.FromRgb(0x23, 0x2A, 0x33)),
                    CornerRadius = new CornerRadius(10),
                    Margin = new Thickness(4),
                    Padding = new Thickness(8),
                    Cursor = Cursors.Hand,
                    BorderBrush = new SolidColorBrush(isSelected
                        ? Color.FromRgb(0x7F, 0xB2, 0xFF)
                        : Colors.Transparent),
                    BorderThickness = new Thickness(isSelected ? 2 : 0),
                    Tag = theme,
                };

                var stack = new StackPanel();
                stack.Children.Add(new Border
                {
                    Height = 44,
                    CornerRadius = new CornerRadius(8),
                    Background = new SolidColorBrush(theme.PrimaryColor),
                    Margin = new Thickness(0, 0, 0, 6),
                });
                stack.Children.Add(new TextBlock
                {
                    Text = theme.Name,
                    FontSize = 11,
                    FontWeight = FontWeights.Medium,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xEA, 0xF0)),
                    TextAlignment = TextAlignment.Center,
                    TextTrimming = TextTrimming.CharacterEllipsis,
                });

                card.Child = stack;
                card.MouseLeftButtonUp += (_, _) =>
                {
                    if (card.Tag is ColorTheme t)
                    {
                        _onColorSelected(t.PrimaryColor);
                        Close();   // confirm action → immediate
                    }
                };

                PresetGrid.Children.Add(card);
            }
        }

        private static bool ColorsEqual(Color a, Color b)
            => a.R == b.R && a.G == b.G && a.B == b.B;

        // ═══════════════════════════════════════════════════════════
        //  Handlers
        // ═══════════════════════════════════════════════════════════
        private void CustomCard_Click(object sender, MouseButtonEventArgs e)
        {
            _onCustomColorClick();
            Close();   // navigate to color wheel → immediate
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
            => _overlay.DismissWithFade();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);   // raises Closed → controller detaches owner handlers
            _onDismiss();
        }
    }
}