// Controls/HomeAppBar.xaml.cs
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.ViewModels;
using MahApps.Metro.IconPacks;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;

namespace FountOfHopeStudyBiblePC.Controls
{
    public partial class HomeAppBar : UserControl
    {
        private const double IconButtonSize = 35;

        private readonly AppViewModel _viewModel;
        private Screen _currentScreen;

        private Popup? _menuPopup;
        private PackIconMaterial? _menuIcon;
        private RotateTransform? _menuRotation;

        public event Action? BibleIconClicked;
        public event Action? ThemeToggled;
        public event Action? ColorLensClicked;
        public event Action? BackRequested;
        public event Action<Screen>? ScreenRequested;

        public HomeAppBar(AppViewModel viewModel, Screen currentScreen)
        {
            _viewModel = viewModel;
            _currentScreen = currentScreen;

            InitializeComponent();

            BackButton.Template = AppBarShared.BuildCircleButtonTemplate(IconButtonSize / 2);
            BackIcon.Foreground = Brushes.White;

            PopulateContent();

            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppViewModel.DarkTheme) ||
                    e.PropertyName == nameof(AppViewModel.HeaderButtonsColor))
                    PopulateContent();
            };

            Unloaded += (_, _) =>
            {
                if (_menuPopup is { IsOpen: true }) _menuPopup.IsOpen = false;
            };
        }

        public void SetCurrentScreen(Screen screen)
        {
            _currentScreen = screen;
            PopulateContent();
        }

        public void SetBackVisible(bool visible)
            => BackButton.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        // ═══════════════════════════════════════════════════════════
        //  Content
        // ═══════════════════════════════════════════════════════════
        private void PopulateContent()
        {
            if (_menuPopup is { IsOpen: true }) _menuPopup.IsOpen = false;
            _menuPopup = null;
            _menuIcon = null;
            _menuRotation = null;

            TitleText.Text = AppBarShared.GetScreenTitle(_currentScreen);
            TitleText.Foreground = Brushes.White;

            ActionsPanel.Children.Clear();

            var isLandscape = SystemParameters.PrimaryScreenWidth > SystemParameters.PrimaryScreenHeight;

            if (isLandscape)
            {
                var navMargin = new Thickness(4, 0, 0, 0);

                ActionsPanel.Children.Add(BuildIconButton(
                    PackIconMaterialKind.Home, "Home", 0.65,
                    () => ScreenRequested?.Invoke(new Screen.Home()), navMargin));

                ActionsPanel.Children.Add(BuildIconButton(
                    PackIconMaterialKind.BookOpenPageVariant, "Bible Navigation", 0.65,
                    () => BibleIconClicked?.Invoke(), navMargin));

                ActionsPanel.Children.Add(BuildIconButton(
                    PackIconMaterialKind.BookmarkMultiple, "Bookmarks", 0.55,
                    () => ScreenRequested?.Invoke(new Screen.Bookmarks()), navMargin));

                ActionsPanel.Children.Add(BuildIconButton(
                    PackIconMaterialKind.NoteText, "Notes", 0.55,
                    () => ScreenRequested?.Invoke(new Screen.Notes()), navMargin,
                    rotateAngle: 90));

                ActionsPanel.Children.Add(BuildIconButton(
                    PackIconMaterialKind.Magnify, "Search", 0.7,
                    () => ScreenRequested?.Invoke(new Screen.Search()), navMargin));

                ActionsPanel.Children.Add(BuildIconButton(
                    PackIconMaterialKind.Cog, "Settings", 0.6,
                    () => ScreenRequested?.Invoke(new Screen.Settings()), navMargin));
            }
            else
            {
                ActionsPanel.Children.Add(BuildIconButton(
                    PackIconMaterialKind.BookOpenPageVariant, "Bible Navigation", 0.65,
                    () => BibleIconClicked?.Invoke(), new Thickness(4, 0, 0, 0)));
            }

            var themeKind = _viewModel.DarkTheme
                ? PackIconMaterialKind.Brightness6
                : PackIconMaterialKind.Brightness2;
            ActionsPanel.Children.Add(BuildIconButton(
                themeKind, "Toggle Theme", 0.6,
                () => ThemeToggled?.Invoke(), new Thickness(4, 0, 0, 0),
                rotateAngle: 180));

            ActionsPanel.Children.Add(BuildIconButton(
                PackIconMaterialKind.Palette, "Color Scheme", 0.6,
                () => ColorLensClicked?.Invoke(), new Thickness(4, 0, 0, 0),
                rotateAngle: 180));

            ActionsPanel.Children.Add(BuildMenuButton());
        }

        private static Button BuildIconButton(
            PackIconMaterialKind kind,
            string tooltip,
            double iconScale,
            Action onClick,
            Thickness? margin = null,
            double? rotateAngle = null)
        {
            var btn = AppBarShared.BuildIconButton(
                kind, tooltip, IconButtonSize, IconButtonSize * iconScale,
                onClick, margin);

            if (rotateAngle is double angle && btn.Content is PackIconMaterial icon)
            {
                icon.RenderTransformOrigin = new Point(0.5, 0.5);
                icon.RenderTransform = new RotateTransform(angle);
            }
            return btn;
        }

        // ═══════════════════════════════════════════════════════════
        //  Menu button
        // ═══════════════════════════════════════════════════════════
        private Button BuildMenuButton()
        {
            var btn = new Button
            {
                Width = IconButtonSize,
                Height = IconButtonSize,
                Margin = new Thickness(4, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Navigation",
            };
            btn.Template = AppBarShared.BuildCircleButtonTemplate(IconButtonSize / 2);

            _menuIcon = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.Menu,
                Width = IconButtonSize * 0.65,
                Height = IconButtonSize * 0.65,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5),
                Foreground = Brushes.White,
            };
            _menuRotation = new RotateTransform(0);
            _menuIcon.RenderTransform = _menuRotation;
            btn.Content = _menuIcon;

            _menuPopup = new Popup
            {
                // StaysOpen=true → we manage close manually. This is what
                // prevents WPF's light-dismiss from closing the popup
                // synchronously inside our own Click handler.
                StaysOpen = true,
                AllowsTransparency = true,
                Placement = PlacementMode.Bottom,
                Child = BuildHomeMenuContent(),
            };

            _menuPopup.Opened += (_, _) =>
            {
                if (_menuIcon is not null) _menuIcon.Kind = PackIconMaterialKind.Close;
                AnimateMenuRotation(180);

                var window = Window.GetWindow(this);
                if (window is not null)
                {
                    window.PreviewMouseLeftButtonDown += OnWindowMouseDown;
                    window.Deactivated += OnWindowDeactivated;
                }
            };
            _menuPopup.Closed += (_, _) =>
            {
                if (_menuIcon is not null) _menuIcon.Kind = PackIconMaterialKind.Menu;
                AnimateMenuRotation(0);

                var window = Window.GetWindow(this);
                if (window is not null)
                {
                    window.PreviewMouseLeftButtonDown -= OnWindowMouseDown;
                    window.Deactivated -= OnWindowDeactivated;
                }
            };

            btn.Click += (_, _) =>
            {
                if (_menuPopup is null) return;

                if (_menuPopup.IsOpen)
                {
                    _menuPopup.IsOpen = false;
                }
                else
                {
                    _menuPopup.Child = BuildHomeMenuContent();
                    _menuPopup.PlacementTarget = btn;
                    _menuPopup.Placement = PlacementMode.Bottom;
                    _menuPopup.IsOpen = true;
                }
            };

            return btn;
        }

        private void OnWindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_menuPopup is null || !_menuPopup.IsOpen) return;

            // Click on hamburger → let button.Click toggle it (avoids
            // double-handling; button.Click closes it cleanly).
            if (_menuPopup.PlacementTarget is UIElement pt &&
                (ReferenceEquals(e.OriginalSource, pt) ||
                 IsDescendantOf(e.OriginalSource as DependencyObject, pt)))
                return;

            _menuPopup.IsOpen = false;
        }

        private void OnWindowDeactivated(object? sender, EventArgs e)
        {
            if (_menuPopup?.IsOpen == true) _menuPopup.IsOpen = false;
        }

        private static bool IsDescendantOf(DependencyObject? element, DependencyObject? ancestor)
        {
            while (element is not null)
            {
                if (ReferenceEquals(element, ancestor)) return true;
                element = VisualTreeHelper.GetParent(element);
            }
            return false;
        }

        private void AnimateMenuRotation(double angle)
        {
            if (_menuRotation is null) return;
            var anim = new DoubleAnimation
            {
                To = angle,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            };
            _menuRotation.BeginAnimation(RotateTransform.AngleProperty, anim);
        }

        private FrameworkElement BuildHomeMenuContent()
        {
            var bg = _viewModel.DarkTheme
                ? _viewModel.DarkModalBackgroundColor
                : _viewModel.LightModalBackgroundColor;

            var container = new Border
            {
                Background = new SolidColorBrush(bg),
                BorderBrush = AppBarShared.MenuBorderBrush(_viewModel.DarkTheme, 0x33),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Padding = new Thickness(4),
                MinWidth = 220,
                MaxWidth = 340,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 14,
                    ShadowDepth = 3,
                    Direction = 270,
                    Color = Colors.Black,
                    Opacity = 0.4,
                },
            };

            var stack = new StackPanel { Margin = new Thickness(4) };
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);

            foreach (var (title, screen, iconKind) in
                     AppBarShared.GetNavigationItems(_viewModel.PrimaryPassage))
            {
                var isActive = AppBarShared.ScreensMatch(_currentScreen, screen);
                var target = screen;

                var icon = new PackIconMaterial
                {
                    Kind = iconKind,
                    Width = 16,
                    Height = 16,
                    Foreground = fg,
                };

                stack.Children.Add(AppBarShared.BuildMenuRow(
                    label: title,
                    icon: icon,
                    onClick: () =>
                    {
                        if (_menuPopup is not null) _menuPopup.IsOpen = false;
                        ScreenRequested?.Invoke(target);
                    },
                    foreground: fg,
                    isActive: isActive,
                    isDarkTheme: _viewModel.DarkTheme));
            }

            container.Child = stack;
            return container;
        }

        private void BackButton_Click(object sender, RoutedEventArgs e)
            => BackRequested?.Invoke();
    }
}