// Controls/HomeAppBar.xaml.cs
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.ViewModels;
using MahApps.Metro.IconPacks;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Controls
{
    public partial class HomeAppBar : UserControl
    {
        private readonly AppViewModel _viewModel;
        private Screen _currentScreen;

        // Events the host (MainWindow) subscribes to
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

            PopulateContent();

            // Re-render when theme changes.
            // ── DarkTheme:       the theme toggle icon (sun ↔ moon) swaps.
            // ── HeaderButtonsColor: the dropdown menu items pick up the
            //    new color. (The app bar itself stays white — see below.)
            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppViewModel.DarkTheme) ||
                    e.PropertyName == nameof(AppViewModel.HeaderButtonsColor))
                    PopulateContent();
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
            TitleText.Text = GetScreenTitle(_currentScreen);

            // App bar content sits on the primary-color gradient → always white.
            TitleText.Foreground = Brushes.White;

            ActionsPanel.Children.Clear();

            var isLandscape = SystemParameters.PrimaryScreenWidth > SystemParameters.PrimaryScreenHeight;

            // Landscape: full icon row
            if (isLandscape)
            {
                ActionsPanel.Children.Add(BuildIconButton(PackIconMaterialKind.Home, "Home",
                    () => ScreenRequested?.Invoke(new Screen.Home())));

                ActionsPanel.Children.Add(BuildIconButton(PackIconMaterialKind.BookOpenPageVariant, "Bible Navigation",
                    () => BibleIconClicked?.Invoke()));

                ActionsPanel.Children.Add(BuildIconButton(PackIconMaterialKind.Bookmark, "Bookmarks",
                    () => ScreenRequested?.Invoke(new Screen.Bookmarks())));

                ActionsPanel.Children.Add(BuildIconButton(PackIconMaterialKind.NoteText, "Notes",
                    () => ScreenRequested?.Invoke(new Screen.Notes())));

                ActionsPanel.Children.Add(BuildIconButton(PackIconMaterialKind.Magnify, "Search",
                    () => ScreenRequested?.Invoke(new Screen.Search())));

                ActionsPanel.Children.Add(BuildIconButton(PackIconMaterialKind.Cog, "Settings",
                    () => ScreenRequested?.Invoke(new Screen.Settings())));
            }
            else
            {
                // Portrait: only the Bible icon
                ActionsPanel.Children.Add(BuildIconButton(PackIconMaterialKind.BookOpenPageVariant, "Bible Navigation",
                    () => BibleIconClicked?.Invoke()));
            }

            // Always: theme, color lens, menu
            var themeKind = _viewModel.DarkTheme
                ? PackIconMaterialKind.WhiteBalanceSunny
                : PackIconMaterialKind.WeatherNight;
            ActionsPanel.Children.Add(BuildIconButton(themeKind, "Toggle Theme",
                () => ThemeToggled?.Invoke()));

            ActionsPanel.Children.Add(BuildIconButton(PackIconMaterialKind.Palette, "Color Scheme",
                () => ColorLensClicked?.Invoke()));

            ActionsPanel.Children.Add(BuildMenuButton());
        }

        // ═══════════════════════════════════════════════════════════
        //  Button factories
        // ═══════════════════════════════════════════════════════════
        private Button BuildIconButton(PackIconMaterialKind kind, string tooltip, Action onClick)
        {
            var btn = new Button
            {
                Width = 36,
                Height = 36,
                Margin = new Thickness(4, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = tooltip,
            };
            btn.Template = BuildTransparentCircleTemplate();

            var icon = new PackIconMaterial
            {
                Kind = kind,
                Width = 18,
                Height = 18,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                // App bar overlays the primary-color gradient — always white.
                Foreground = Brushes.White,
            };

            btn.Content = icon;
            btn.Click += (_, _) => onClick();
            return btn;
        }

        private Button BuildMenuButton()
        {
            var btn = new Button
            {
                Width = 36,
                Height = 36,
                Margin = new Thickness(4, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Navigation",
            };
            btn.Template = BuildTransparentCircleTemplate();

            var icon = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.Menu,
                Width = 20,
                Height = 20,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                // App bar overlays the primary-color gradient — always white.
                Foreground = Brushes.White,
            };
            btn.Content = icon;

            var menu = BuildNavigationMenu();
            btn.Click += (_, _) =>
            {
                menu.PlacementTarget = btn;
                menu.Placement = PlacementMode.Bottom;
                menu.IsOpen = true;
            };

            return btn;
        }

        // ═══════════════════════════════════════════════════════════
        //  Dropdown menu — sits on the modal background, NOT the app
        //  bar gradient, so its icons/text remain theme-aware.
        // ═══════════════════════════════════════════════════════════
        private ContextMenu BuildNavigationMenu()
        {
            var menu = new ContextMenu
            {
                Background = new SolidColorBrush(_viewModel.DarkTheme
                    ? _viewModel.DarkModalBackgroundColor
                    : _viewModel.LightModalBackgroundColor),
            };

            var items = new List<(string title, Screen screen, PackIconMaterialKind icon)>
            {
                ("Home",      new Screen.Home(),                         PackIconMaterialKind.Home),
                ("Reader",    new Screen.Reader(_viewModel.PrimaryPassage), PackIconMaterialKind.BookOpenPageVariant),
                ("Bookmarks", new Screen.Bookmarks(),                    PackIconMaterialKind.Bookmark),
                ("Notes",     new Screen.Notes(),                        PackIconMaterialKind.NoteText),
                ("Search",    new Screen.Search(),                       PackIconMaterialKind.Magnify),
                ("Settings",  new Screen.Settings(),                     PackIconMaterialKind.Cog),
            };

            foreach (var (title, screen, iconKind) in items)
            {
                var isActive = ScreensMatch(_currentScreen, screen);

                var menuIcon = new PackIconMaterial
                {
                    Kind = iconKind,
                    Width = 16,
                    Height = 16,
                    VerticalAlignment = VerticalAlignment.Center,
                };
                // Menu icon is drawn on the modal background, so keep the
                // theme-derived color here (not white).
                menuIcon.SetResourceReference(Control.ForegroundProperty, ThemeResources.Icon);

                var mi = new MenuItem
                {
                    Header = title,
                    Icon = menuIcon,
                    FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                    Background = isActive
                                ? new SolidColorBrush(Color.FromArgb(0x33,
                                      ThemeManager.PrimaryColor.R,
                                      ThemeManager.PrimaryColor.G,
                                      ThemeManager.PrimaryColor.B))
                                : Brushes.Transparent,
                };

                mi.Click += (_, _) =>
                {
                    menu.IsOpen = false;
                    ScreenRequested?.Invoke(screen);
                };

                menu.Items.Add(mi);
            }

            return menu;
        }

        private static bool ScreensMatch(Screen a, Screen b)
        {
            return (a, b) switch
            {
                (Screen.Home, Screen.Home) => true,
                (Screen.Reader, Screen.Reader) => true,
                (Screen.Bookmarks, Screen.Bookmarks) => true,
                (Screen.Notes, Screen.Notes) => true,
                (Screen.Search, Screen.Search) => true,
                (Screen.Settings, Screen.Settings) => true,
                _ => false
            };
        }

        private static ControlTemplate BuildTransparentCircleTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "B";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(18));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);

            template.VisualTree = border;

            var trigger = new Trigger
            {
                Property = IsMouseOverProperty,
                Value = true,
            };
            trigger.Setters.Add(new Setter(
                Border.BackgroundProperty,
                new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                "B"));
            template.Triggers.Add(trigger);

            return template;
        }

        // ═══════════════════════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════════════════════
        private static string GetScreenTitle(Screen screen) => screen switch
        {
            Screen.Home => "Home",
            Screen.Reader => "Reader",
            Screen.Bookmarks => "Bookmarks",
            Screen.Notes => "Notes",
            Screen.Search => "Search",
            Screen.Settings => "Settings",
            Screen.Quiz => "Quiz",
            Screen.LearnHebrew => "Learn Hebrew",
            Screen.LearnGreek => "Learn Greek",
            Screen.Dictionary => "Dictionary",
            Screen.Animator => "Animator",
            Screen.Generations => "Generations",
            _ => "Fount of Hope",
        };

        private void BackButton_Click(object sender, RoutedEventArgs e)
            => BackRequested?.Invoke();
    }
}