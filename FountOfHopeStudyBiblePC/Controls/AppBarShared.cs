using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using MahApps.Metro.IconPacks;

namespace FountOfHopeStudyBiblePC.Controls
{
    internal static class AppBarShared
    {
        public static Brush MenuForegroundBrush(bool darkTheme)
            => new SolidColorBrush(darkTheme ? Colors.White : Colors.Black);

        public static Brush MenuBorderBrush(bool darkTheme, byte alpha = 0x33)
            => new SolidColorBrush(darkTheme
                ? Color.FromArgb(alpha, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(alpha, 0x00, 0x00, 0x00));

        public static ControlTemplate BuildCircleButtonTemplate(double radius)
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border))
            {
                Name = "B"
            };
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty,
                HorizontalAlignment.Center);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty,
                VerticalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;

            var trigger = new Trigger { Property = UIElement.IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(
                Border.BackgroundProperty,
                new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                "B"));
            template.Triggers.Add(trigger);
            return template;
        }

        public static ControlTemplate BuildRoundedRectTemplate(double radius, Color bg)
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border))
            {
                Name = "B"
            };
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(bg));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(FrameworkElement.HorizontalAlignmentProperty,
                HorizontalAlignment.Center);
            presenter.SetValue(FrameworkElement.VerticalAlignmentProperty,
                VerticalAlignment.Center);
            presenter.SetValue(ContentPresenter.MarginProperty, new Thickness(8, 0, 8, 0));
            border.AppendChild(presenter);
            template.VisualTree = border;
            return template;
        }

        public static Button BuildIconButton(
            PackIconMaterialKind kind,
            string tooltip,
            double buttonSize,
            double iconSize,
            Action onClick,
            Thickness? margin = null,
            Brush? foreground = null)
                {
            var btn = new Button
            {
                Width = buttonSize,
                Height = buttonSize,
                Margin = margin ?? new Thickness(0),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = tooltip,
                Template = BuildCircleButtonTemplate(buttonSize / 2)
            };

            var icon = new PackIconMaterial
                    {
                        Kind = kind,
                        Width = iconSize,
                        Height = iconSize,
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Foreground = foreground ?? Brushes.White,
                    };
                    btn.Content = icon;
                    btn.Click += (_, _) => onClick();
                    return btn;
        }

        public static Border BuildMenuRow(
            string label,
            FrameworkElement? icon,
            Action? onClick,
            Brush foreground,
            bool isActive = false,
            bool isDarkTheme = true)
        {
            var hoverBg = new SolidColorBrush(isDarkTheme
                ? Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)
                : Color.FromArgb(0x22, 0x00, 0x00, 0x00));
            var activeBg = new SolidColorBrush(Color.FromArgb(0x33,
                ThemeManager.PrimaryColor.R,
                ThemeManager.PrimaryColor.G,
                ThemeManager.PrimaryColor.B));

            var row = new Border
            {
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(8, 6, 8, 6),
                Background = isActive ? activeBg : Brushes.Transparent,
                Cursor = onClick != null
                    ? System.Windows.Input.Cursors.Hand
                    : System.Windows.Input.Cursors.Arrow,
            };

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition
            {
                Width = new GridLength(1, GridUnitType.Star),
            });

            if (icon != null)
            {
                icon.Margin = new Thickness(0, 0, 10, 0);
                icon.VerticalAlignment = VerticalAlignment.Center;
                Grid.SetColumn(icon, 0);
                grid.Children.Add(icon);
            }

            var text = new TextBlock
            {
                Text = label,
                Foreground = foreground,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = isActive ? FontWeights.Bold : FontWeights.Normal,
            };
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            row.Child = grid;

            if (onClick != null)
            {
                row.MouseLeftButtonUp += (_, _) => onClick();
                row.MouseEnter += (_, _) =>
                {
                    if (!isActive) row.Background = hoverBg;
                };
                row.MouseLeave += (_, _) =>
                {
                    if (!isActive) row.Background = Brushes.Transparent;
                };
            }

            return row;
        }

        public static Border BuildSeparator(double margin = 4) => new()
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromArgb(0x33, 0x80, 0x80, 0x80)),
            Margin = new Thickness(margin),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

        public static (string title, Screen screen, PackIconMaterialKind icon)[]
            GetNavigationItems(PassageSelection primaryPassage)
            =>
        [
            ("Home",      new Screen.Home(),                    PackIconMaterialKind.Home),
            ("Reader",    new Screen.Reader(primaryPassage),    PackIconMaterialKind.BookOpenPageVariant),
            ("Bookmarks", new Screen.Bookmarks(),               PackIconMaterialKind.Bookmark),
            ("Notes",     new Screen.Notes(),                   PackIconMaterialKind.NoteText),
            ("Search",    new Screen.Search(),                  PackIconMaterialKind.Magnify),
            ("Settings",  new Screen.Settings(),                PackIconMaterialKind.Cog),
        ];

        public static bool ScreensMatch(Screen a, Screen b) => (a, b) switch
        {
            (Screen.Home, Screen.Home) => true,
            (Screen.Reader, Screen.Reader) => true,
            (Screen.Bookmarks, Screen.Bookmarks) => true,
            (Screen.Notes, Screen.Notes) => true,
            (Screen.Search, Screen.Search) => true,
            (Screen.Settings, Screen.Settings) => true,
            _ => false
        };

        public static string GetScreenTitle(Screen screen) => screen switch
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
    }
}