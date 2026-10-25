// Views/Modals/ColorThemeDialog.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class ColorThemeDialog : Window
    {
        private readonly Action<Color> _onColorSelected;
        private readonly Action _onCustomColorClick;
        private readonly Action _onDismiss;
        private readonly Color _currentColor;

        private bool _isDismissing;
        private bool _dismissArmed;

        /// <summary>Close the dialog if the owner window is resized.</summary>
        public bool CloseOnOwnerResize { get; set; } = true;

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
            BuildPresets();

            Loaded += OnLoaded;
        }

        // ═══════════════════════════════════════════════════════════
        //  Scrim dismiss infrastructure
        // ═══════════════════════════════════════════════════════════
        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Card content (preset cards, custom card, buttons) keeps its own
            // events — we only arm dismissal when the press starts on the scrim.
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