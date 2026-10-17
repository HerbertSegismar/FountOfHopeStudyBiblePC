// Controls/HomeAppBar.xaml.cs
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.ViewModels;

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

            // Re-render when theme or header color changes
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
            TitleText.Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor);

            ActionsPanel.Children.Clear();

            var isLandscape = SystemParameters.PrimaryScreenWidth > SystemParameters.PrimaryScreenHeight;

            // Landscape: full icon row
            if (isLandscape)
            {
                ActionsPanel.Children.Add(BuildIconButton("🏠", "Home",
                    () => ScreenRequested?.Invoke(new Screen.Home())));

                ActionsPanel.Children.Add(BuildAnimatedIconButton("📖", "Bible Navigation",
                    () => BibleIconClicked?.Invoke()));

                ActionsPanel.Children.Add(BuildIconButton("🔖", "Bookmarks",
                    () => ScreenRequested?.Invoke(new Screen.Bookmarks())));
                ActionsPanel.Children.Add(BuildIconButton("📝", "Notes",
                    () => ScreenRequested?.Invoke(new Screen.Notes())));
                ActionsPanel.Children.Add(BuildIconButton("🔍", "Search",
                    () => ScreenRequested?.Invoke(new Screen.Search())));
                ActionsPanel.Children.Add(BuildIconButton("⚙", "Settings",
                    () => ScreenRequested?.Invoke(new Screen.Settings())));
            }
            else
            {
                // Portrait: only the Bible icon
                ActionsPanel.Children.Add(BuildAnimatedIconButton("📖", "Bible Navigation",
                    () => BibleIconClicked?.Invoke()));
            }

            // Always: theme, color lens, menu
            var themeGlyph = _viewModel.DarkTheme ? "☀" : "☾";
            ActionsPanel.Children.Add(BuildAnimatedIconButton(themeGlyph, "Toggle Theme",
                () => ThemeToggled?.Invoke()));

            ActionsPanel.Children.Add(BuildAnimatedIconButton("🎨", "Color Scheme",
                () => ColorLensClicked?.Invoke()));

            ActionsPanel.Children.Add(BuildMenuButton());
        }

        // ═══════════════════════════════════════════════════════════
        //  Button factories
        // ═══════════════════════════════════════════════════════════
        private Button BuildIconButton(string glyph, string tooltip, Action onClick)
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

            btn.Content = new TextBlock
            {
                Text = glyph,
                FontSize = 18,
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            btn.Click += (_, _) => onClick();
            return btn;
        }

        private Button BuildAnimatedIconButton(string glyph, string tooltip, Action onClick)
        {
            return BuildIconButton(glyph, tooltip, onClick);
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

            btn.Content = new TextBlock
            {
                Text = "☰",
                FontSize = 20,
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            var menu = BuildNavigationMenu();
            btn.Click += (_, _) =>
            {
                menu.PlacementTarget = btn;
                menu.Placement = PlacementMode.Bottom;
                menu.IsOpen = true;
            };

            return btn;
        }

        private ContextMenu BuildNavigationMenu()
        {
            var menu = new ContextMenu
            {
                Background = new SolidColorBrush(_viewModel.DarkTheme
                    ? _viewModel.DarkModalBackgroundColor
                    : _viewModel.LightModalBackgroundColor),
            };

            var items = new List<(string title, Screen screen)>
            {
                ("Home",      new Screen.Home()),
                ("Reader",    new Screen.Reader(_viewModel.PrimaryPassage)),
                ("Bookmarks", new Screen.Bookmarks()),
                ("Notes",     new Screen.Notes()),
                ("Search",    new Screen.Search()),
                ("Settings",  new Screen.Settings()),
            };

            foreach (var (title, screen) in items)
            {
                var isActive = ScreensMatch(_currentScreen, screen);

                var mi = new MenuItem
                {
                    Header = title,
                    FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
                    Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                    Background = isActive
                        ? new SolidColorBrush(Color.FromArgb(0x33, 0x7F, 0xB2, 0xFF))
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