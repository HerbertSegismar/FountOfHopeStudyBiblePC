// Controls/ReaderAppBar.xaml.cs
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
    public partial class ReaderAppBar : UserControl
    {
        private readonly AppViewModel _viewModel;
        private PassageSelection? _currentPassage;
        private string _currentVersionAbbr = "Bible";

        // ── Events subscribed by MainWindow ────────────────────────
        public event Action? BibleIconClicked;
        public event Action? ThemeToggled;
        public event Action? ColorLensClicked;
        public event Action? BackRequested;
        public event Action<Screen>? ScreenRequested;

        public ReaderAppBar(AppViewModel viewModel)
        {
            _viewModel = viewModel;

            InitializeComponent();

            // React to theme changes
            _viewModel.PropertyChanged += (_, e) =>
            {
                if (e.PropertyName == nameof(AppViewModel.DarkTheme))
                    UpdateThemeGlyph();
            };

            BuildActions();
        }

        // ═══════════════════════════════════════════════════════════
        //  Public API — called by MainWindow.UpdateAppBar()
        // ═══════════════════════════════════════════════════════════
        public void UpdatePassage(PassageSelection passage, string versionAbbr)
        {
            _currentPassage = passage;
            _currentVersionAbbr = versionAbbr;

            BookText.Text = passage.BookName;
            ChapterText.Text = $" {passage.Chapter}";
            VersionText.Text = versionAbbr;
        }

        public void SetBackVisible(bool visible)
            => BackButton.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;

        // ═══════════════════════════════════════════════════════════
        //  Actions panel — icon buttons + menu
        // ═══════════════════════════════════════════════════════════
        private void BuildActions()
        {
            ActionsPanel.Children.Clear();

            ActionsPanel.Children.Add(BuildIconButton("☾", "Toggle theme",
                () => ThemeToggled?.Invoke(), out var themeBtn));
            _themeButton = themeBtn;

            ActionsPanel.Children.Add(BuildIconButton("🎨", "Color scheme",
                () => ColorLensClicked?.Invoke(), out _));

            ActionsPanel.Children.Add(BuildMenuButton());

            UpdateThemeGlyph();
        }

        private Button? _themeButton;
        private TextBlock? _themeGlyph;

        private Button BuildIconButton(string glyph, string tooltip, Action onClick, out Button btn)
        {
            btn = new Button
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

            var icon = new TextBlock
            {
                Text = glyph,
                FontSize = 18,
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            _themeGlyph = icon;
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
                ToolTip = "Menu",
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

            var menu = BuildReaderMenu();
            btn.Click += (_, _) =>
            {
                menu.PlacementTarget = btn;
                menu.Placement = PlacementMode.Bottom;
                menu.IsOpen = true;
            };

            return btn;
        }

        private ContextMenu BuildReaderMenu()
        {
            var menu = new ContextMenu
            {
                Background = new SolidColorBrush(_viewModel.DarkTheme
                    ? _viewModel.DarkModalBackgroundColor
                    : _viewModel.LightModalBackgroundColor),
            };

            // Navigation items
            var navItems = new List<(string title, Screen screen)>
            {
                ("Home",      new Screen.Home()),
                ("Bookmarks", new Screen.Bookmarks()),
                ("Notes",     new Screen.Notes()),
                ("Search",    new Screen.Search()),
                ("Settings",  new Screen.Settings()),
            };

            foreach (var (title, screen) in navItems)
            {
                var mi = new MenuItem
                {
                    Header = title,
                    Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                };
                mi.Click += (_, _) =>
                {
                    menu.IsOpen = false;
                    ScreenRequested?.Invoke(screen);
                };
                menu.Items.Add(mi);
            }

            // Separator + background texture
            menu.Items.Add(new Separator());

            var bgItem = new MenuItem
            {
                Header = "Background Texture",
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
            };
            bgItem.Click += (_, _) =>
            {
                menu.IsOpen = false;
                _viewModel.ShowBgModal = true;
            };
            menu.Items.Add(bgItem);

            // Dictionary mode toggle
            var dictModeItem = new MenuItem
            {
                Header = _viewModel.IsDictionaryMode
                    ? "Dictionary Mode: On"
                    : "Dictionary Mode: Off",
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                IsCheckable = true,
                IsChecked = _viewModel.IsDictionaryMode,
            };
            dictModeItem.Click += (_, _) =>
            {
                _viewModel.IsDictionaryMode = !_viewModel.IsDictionaryMode;
                dictModeItem.IsChecked = _viewModel.IsDictionaryMode;
                dictModeItem.Header = _viewModel.IsDictionaryMode
                    ? "Dictionary Mode: On"
                    : "Dictionary Mode: Off";
            };
            menu.Items.Add(dictModeItem);

            // Font size
            menu.Items.Add(new Separator());
            menu.Items.Add(BuildFontSizeItem());

            return menu;
        }

        private MenuItem BuildFontSizeItem()
        {
            var container = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(4, 4, 4, 4),
            };

            var label = new TextBlock
            {
                Text = "Font Size: ",
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
            };
            container.Children.Add(label);

            var valueText = new TextBlock
            {
                Text = _viewModel.FontSize.ToString(),
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Width = 28,
                TextAlignment = TextAlignment.Center,
            };

            var minusBtn = new Button
            {
                Content = "A-",
                Width = 32,
                Height = 26,
                Margin = new Thickness(4, 0, 4, 0),
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                Cursor = System.Windows.Input.Cursors.Hand,
            };
            minusBtn.Click += (_, _) =>
            {
                _viewModel.FontSize = Math.Max(1, _viewModel.FontSize - 1);
                valueText.Text = _viewModel.FontSize.ToString();
            };

            var plusBtn = new Button
            {
                Content = "A+",
                Width = 32,
                Height = 26,
                Margin = new Thickness(4, 0, 0, 0),
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                Cursor = System.Windows.Input.Cursors.Hand,
            };
            plusBtn.Click += (_, _) =>
            {
                _viewModel.FontSize = Math.Min(100, _viewModel.FontSize + 1);
                valueText.Text = _viewModel.FontSize.ToString();
            };

            container.Children.Add(minusBtn);
            container.Children.Add(valueText);
            container.Children.Add(plusBtn);

            var mi = new MenuItem { Header = container };
            mi.IsEnabled = false;      // disable "click" hover — it's a container only
            // Re-enable pointer interactions inside the panel
            container.IsEnabled = true;
            return mi;
        }

        private void UpdateThemeGlyph()
        {
            if (_themeGlyph != null)
                _themeGlyph.Text = _viewModel.DarkTheme ? "☀" : "☾";

            if (_themeButton?.Content is TextBlock tb)
                tb.Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor);
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
        //  Header button handlers
        // ═══════════════════════════════════════════════════════════
        private void BackButton_Click(object sender, RoutedEventArgs e)
            => BackRequested?.Invoke();

        private void BookChapterButton_Click(object sender, RoutedEventArgs e)
            => BibleIconClicked?.Invoke();

        private void VersionButton_Click(object sender, RoutedEventArgs e)
        {
            App.Log("ReaderAppBar: VersionButton clicked");
            _viewModel.ShowPrimaryVersionDropdown = true;
            App.Log($"ReaderAppBar: ShowPrimaryVersionDropdown = {_viewModel.ShowPrimaryVersionDropdown}");
        }
    }
}