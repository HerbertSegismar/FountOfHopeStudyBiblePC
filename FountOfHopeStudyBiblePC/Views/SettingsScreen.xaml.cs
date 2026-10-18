// Views/SettingsScreen.xaml.cs
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.ViewModels;
using FountOfHopeStudyBiblePC.Views.Modals;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Views
{
    public partial class SettingsScreen : UserControl
    {
        private readonly AppViewModel _viewModel;

        public SettingsScreen()
        {
            InitializeComponent();

            _viewModel = ViewModels.AppViewModelAccessor.Current ?? new AppViewModel();
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            Loaded += (_, _) => RebuildAll();
        }

        private void OnViewModelPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppViewModel.DarkTheme):
                case nameof(AppViewModel.MultiVersion):
                case nameof(AppViewModel.RenderOrbs):
                case nameof(AppViewModel.SelectedFontFamily):
                case nameof(AppViewModel.IsDictionaryMode):
                case nameof(AppViewModel.BgImageIndex):
                    Dispatcher.BeginInvoke(new Action(RebuildAll),
                        System.Windows.Threading.DispatcherPriority.Background);
                    break;
            }
        }

        // -----------------------------------------------------------
        //  Rebuild everything (simple + correct; runs fast)
        // -----------------------------------------------------------
        private void RebuildAll()
        {
            SettingsRootPanel.Children.Clear();

            SettingsRootPanel.Children.Add(BuildAppSettingsSection());
            SettingsRootPanel.Children.Add(BuildReaderSettingsSection());
            SettingsRootPanel.Children.Add(BuildHighlightPaletteSection());
            SettingsRootPanel.Children.Add(BuildVersionManagementSection());
            SettingsRootPanel.Children.Add(BuildDatabaseSection());
            SettingsRootPanel.Children.Add(BuildMoreSection());
            SettingsRootPanel.Children.Add(BuildQuickActionsSection());
        }

        // -----------------------------------------------------------
        //  SECTION BUILDERS
        // -----------------------------------------------------------
        private Border BuildAppSettingsSection()
        {
            var content = new StackPanel();

            content.Children.Add(BuildSwitchRow(
                "Dark Mode",
                "Toggle between light and dark themes",
                _viewModel.DarkTheme,
                v => _viewModel.DarkTheme = v));

            content.Children.Add(BuildLabel("Color Scheme"));

            // Preset color buttons + custom
            var colorStrip = new WrapPanel { Margin = new Thickness(0, 8, 0, 0) };
            foreach (var theme in PredefinedColorThemes.All)
            {
                var isSelected = ColorsEqual(_viewModel.SelectedColor ?? Colors.Transparent, theme.PrimaryColor)
                                 && !_viewModel.IsCustomColor;
                var btn = BuildColorButton(theme.Name, theme.PrimaryColor, isSelected,
                    () =>
                    {
                        _viewModel.SelectedColor = theme.PrimaryColor;
                        _viewModel.IsCustomColor = false;
                        _viewModel.CustomColor = null;
                    });
                colorStrip.Children.Add(btn);
            }

            var customSelected = _viewModel.IsCustomColor;
            var customColor = _viewModel.CustomColor
                              ?? _viewModel.SelectedColor
                              ?? Color.FromRgb(0x67, 0x50, 0xA4);
            var customBtn = BuildColorButton("Custom", customColor, customSelected,
                () => _viewModel.ShowColorWheelDialog = true);
            colorStrip.Children.Add(customBtn);

            content.Children.Add(colorStrip);

            return BuildSectionBorder("App Settings", "Customize app appearance", content);
        }

        private Border BuildReaderSettingsSection()
        {
            var content = new StackPanel();

            // -- Primary version card --
            content.Children.Add(BuildVersionCard(
                "Primary Bible Version",
                _viewModel.CurrentVersionAbbr,
                BibleVersionUtils.GetDescriptionForVersion(_viewModel.CurrentDbName),
                onClick: () => _viewModel.ShowPrimaryVersionDropdown = true,
                onInfoClick: () =>
                {
                    _viewModel.VersionInfoForDialog = _viewModel.CurrentDbName;
                    _viewModel.ShowVersionInfoDialog = true;
                }));

            // -- Secondary version (multi only) --
            if (_viewModel.MultiVersion)
            {
                content.Children.Add(BuildVersionCard(
                    "Secondary Bible Version",
                    _viewModel.SecondaryVersionAbbr,
                    BibleVersionUtils.GetDescriptionForVersion(_viewModel.SecondaryDbName),
                    onClick: () => _viewModel.ShowSecondaryVersionDropdown = true,
                    onInfoClick: () =>
                    {
                        _viewModel.VersionInfoForDialog = _viewModel.SecondaryDbName;
                        _viewModel.ShowVersionInfoDialog = true;
                    }));
            }

            // -- Multi-version toggle --
            content.Children.Add(BuildSwitchRow(
                "Multi-Version Display",
                "Show two Bible versions side by side",
                _viewModel.MultiVersion,
                v => _viewModel.MultiVersion = v));

            if (_viewModel.MultiVersion)
            {
                content.Children.Add(BuildLayoutToggleRow(
                    _viewModel.MultiViewLayout,
                    v => _viewModel.MultiViewLayout = v));

                content.Children.Add(BuildSwitchRow(
                    "Scroll Sync",
                    "Synchronize scrolling between versions",
                    _viewModel.ScrollSync,
                    v => _viewModel.ScrollSync = v));

                content.Children.Add(BuildSwitchRow(
                    "Square Aspect Ratio",
                    "Auto square aspect when device rotates (multi-version only)",
                    _viewModel.SquareAspectViews,
                    v => _viewModel.SquareAspectViews = v));
            }

            // -- Orbs count (if orbs enabled) --
            if (_viewModel.RenderOrbs)
            {
                content.Children.Add(BuildStepperRow(
                    "Orbs Count (1–20)",
                    "Adjust the number of rendered orbs",
                    _viewModel.OrbsCount,
                    () => _viewModel.OrbsCount = Math.Max(1, _viewModel.OrbsCount - 1),
                    () => _viewModel.OrbsCount = Math.Min(20, _viewModel.OrbsCount + 1)));
            }

            // -- Font family --
            content.Children.Add(BuildLabel("Font Family"));
            var fontStrip = new WrapPanel { Margin = new Thickness(0, 8, 0, 8) };
            foreach (var family in FontsHelper.AvailableFontFamilies)
            {
                var isSelected = _viewModel.SelectedFontFamily == family;
                var btn = BuildFontButton(family, isSelected,
                    () => _viewModel.SelectedFontFamily = family);
                fontStrip.Children.Add(btn);
            }
            content.Children.Add(fontStrip);

            // -- Font size --
            content.Children.Add(BuildStepperRow(
                "Font Size",
                "Adjust text size",
                _viewModel.FontSize,
                () => _viewModel.FontSize = Math.Max(1, _viewModel.FontSize - 1),
                () => _viewModel.FontSize = Math.Min(100, _viewModel.FontSize + 1)));

            // -- Study / Dictionary / Strong's toggles --
            content.Children.Add(BuildSwitchRow(
                "Study Mode",
                "Show cross-refs and commentary buttons after each verse",
                _viewModel.IsStudyMode,
                v => _viewModel.IsStudyMode = v));

            content.Children.Add(BuildSwitchRow(
                "Dictionary Mode",
                "Tap a word to look up its definition (off = highlight mode)",
                _viewModel.IsDictionaryMode,
                v => _viewModel.IsDictionaryMode = v));

            content.Children.Add(BuildSwitchRow(
                "Strong's Concordance",
                "Show Strong's numbers (only applies to versions with + e.g. KJV+)",
                _viewModel.ShowStrongs,
                v => _viewModel.ShowStrongs = v));

            // -- Color swatch rows --
            if (!_viewModel.IsDictionaryMode)
            {
                content.Children.Add(BuildColorRow(
                    "Word Marker Color",
                    "Color for highlighting individual words",
                    _viewModel.WordMarkerColor,
                    () => _viewModel.ShowWordMarkerColorWheelDialog = true));
            }

            content.Children.Add(BuildColorRow(
                "Words of Jesus Color",
                "Color for the words of the Lord Jesus",
                _viewModel.WordsOfJesus,
                () => _viewModel.ShowJesusWordsColorWheelDialog = true));

            content.Children.Add(BuildColorRow(
                "Header Contents Color",
                "App bar text and buttons color",
                _viewModel.HeaderButtonsColor,
                () => _viewModel.ShowHeaderButtonsColorWheel = true));

            content.Children.Add(BuildColorRow(
                "Verse Marker Color",
                "Color for highlighting entire verses",
                _viewModel.VerseMarkerColor,
                () => _viewModel.ShowVerseMarkerColorWheelDialog = true));

            if (_viewModel.DarkTheme)
            {
                content.Children.Add(BuildColorRow(
                    "Modal Background Color",
                    "Modal background for dark theme",
                    _viewModel.DarkModalBackgroundColor,
                    () => _viewModel.ShowDarkOverlayColorWheel = true));

                content.Children.Add(BuildColorRow(
                    "Reader Font Color",
                    "Reader text color for dark theme",
                    _viewModel.DarkThemeReaderFontColor,
                    () => _viewModel.ShowDarkReaderFontColorWheelDialog = true));
            }
            else
            {
                content.Children.Add(BuildColorRow(
                    "Modal Background Color",
                    "Modal background for light theme",
                    _viewModel.LightModalBackgroundColor,
                    () => _viewModel.ShowLightOverlayColorWheel = true));

                content.Children.Add(BuildColorRow(
                    "Reader Font Color",
                    "Reader text color for light theme",
                    _viewModel.LightThemeReaderFontColor,
                    () => _viewModel.ShowLightReaderFontColorWheelDialog = true));
            }

            // -- Background texture --
            content.Children.Add(BuildClickRow(
                "Reader BG Texture",
                "Choose from built-in textures or upload a custom one",
                onClick: () => App.Log("SettingsScreen: BgModal not yet implemented — TODO"),
                trailing: new TextBlock { Text = "›", FontSize = 18, Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xB4, 0xC0)) }));

            if (_viewModel.BgImageIndex > 0)
            {
                content.Children.Add(BuildOverlaySliderRow());
            }

            return BuildSectionBorder("Reader Settings",
                "Customize your Bible reading experience", content);
        }

        private Border BuildHighlightPaletteSection()
        {
            var content = new StackPanel();

            // Row of 6 highlight color squares
            var row = new UniformGrid
            {
                Columns = 6,
                Margin = new Thickness(0, 4, 0, 8)
            };

            for (int i = 0; i < 6; i++)
            {
                int idx = i;
                var color = i < _viewModel.PredefinedHighlightColors.Count
                    ? _viewModel.PredefinedHighlightColors[i]
                    : Colors.White;
                var square = new Border
                {
                    Height = 40,
                    Margin = new Thickness(4),
                    CornerRadius = new CornerRadius(10),
                    Background = new SolidColorBrush(color),
                    BorderBrush = ColorsEqual(color, Colors.White)
                        ? new SolidColorBrush(Color.FromRgb(0xAA, 0xB4, 0xC0))
                        : new SolidColorBrush(color),
                    BorderThickness = new Thickness(2),
                    Cursor = Cursors.Hand,
                };
                square.MouseLeftButtonUp += (_, _) =>
                {
                    _viewModel.EditingHighlightColorIndex = idx;
                    _viewModel.ShowHighlightColorEditor = true;
                };
                row.Children.Add(square);
            }

            content.Children.Add(row);

            var resetBtn = BuildTextButton("Reset to Default Colors",
                () => ConfirmResetHighlightColors());
            content.Children.Add(resetBtn);

            return BuildSectionBorder("Word Marker Palette Colors",
                "Tap to edit colors", content);
        }

        private Border BuildVersionManagementSection()
        {
            var content = new StackPanel();
            var btn = BuildPrimaryButton("Enable / Disable Versions", () =>
            {
                var dlg = new VersionManagementDialog(_viewModel)
                {
                    Owner = Window.GetWindow(this)
                };
                dlg.ShowDialog();
            });
            content.Children.Add(btn);
            return BuildSectionBorder("Version Management",
                "Choose which Bible versions appear in the version selector", content);
        }

        private Border BuildDatabaseSection()
        {
            var content = new StackPanel();

            content.Children.Add(BuildClickRow(
                "Refresh All Databases",
                "Force recopy all database files from assets",
                onClick: () => ConfirmRefreshDatabases(),
                trailing: new TextBlock { Text = "?", FontSize = 18, Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xB4, 0xC0)) }));

            content.Children.Add(new TextBlock
            {
                Text = "Recopies all Bible versions and dictionaries. Useful if databases become corrupted.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xB4, 0xC0)),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 4, 0, 0),
            });

            return BuildSectionBorder("Database Management",
                "Refresh and manage Bible databases", content);
        }

        private Border BuildMoreSection()
        {
            var content = new StackPanel();

            content.Children.Add(BuildClickRow(
                "Data & Storage",
                "Manage app data and cache",
                onClick: () => MessageBox.Show(
                    "App data is stored at:\n" +
                    Path.Combine(Environment.GetFolderPath(
                        Environment.SpecialFolder.LocalApplicationData),
                        "FountOfHopeStudyBiblePC"),
                    "Data & Storage", MessageBoxButton.OK, MessageBoxImage.Information),
                trailing: new TextBlock { Text = "›", FontSize = 18, Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xB4, 0xC0)) }));

            content.Children.Add(BuildDivider());

            content.Children.Add(BuildClickRow(
                "About",
                "App version and information",
                onClick: () =>
                {
                    var dlg = new AboutDialog { Owner = Window.GetWindow(this) };
                    dlg.ShowDialog();
                },
                trailing: new TextBlock { Text = "›", FontSize = 18, Foreground = new SolidColorBrush(Color.FromRgb(0xAA, 0xB4, 0xC0)) }));

            return BuildSectionBorder("More Options",
                "Additional preferences", content);
        }

        private Border BuildQuickActionsSection()
        {
            var content = new Grid();
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(8) });
            content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var resetBtn = new Button
            {
                Content = "?  Reset All",
                Height = 44,
                Background = new SolidColorBrush(Color.FromRgb(0x3A, 0x1F, 0x1F)),
                BorderThickness = new Thickness(0),
                Foreground = new SolidColorBrush(Color.FromRgb(0xEB, 0x4E, 0x4E)),
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
            };
            resetBtn.Click += (_, _) => ConfirmResetAll();
            Grid.SetColumn(resetBtn, 0);
            content.Children.Add(resetBtn);

            var feedbackBtn = new Button
            {
                Content = "?  Feedback",
                Height = 44,
                Background = new SolidColorBrush(
                    (Color)ColorConverter.ConvertFromString("#FF7FB2FF")),
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand,
            };
            feedbackBtn.Click += (_, _) =>
            {
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = "mailto:fountofhopedevotionals@gmail.com?subject=FoH Bible App Feedback",
                        UseShellExecute = true,
                    });
                }
                catch { }
            };
            Grid.SetColumn(feedbackBtn, 2);
            content.Children.Add(feedbackBtn);

            return BuildSectionBorder("Quick Actions", "Common tasks", content);
        }

        // -----------------------------------------------------------
        //  UI BUILDING BLOCKS
        // -----------------------------------------------------------
        private Border BuildSectionBorder(string title, string? subtitle, UIElement content)
        {
            var stack = new StackPanel();

            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("OnSurface"),
            });

            if (!string.IsNullOrEmpty(subtitle))
            {
                stack.Children.Add(new TextBlock
                {
                    Text = subtitle,
                    FontSize = 12,
                    Foreground = (Brush)FindResource("OnSurfaceVariant"),
                    Margin = new Thickness(0, 2, 0, 0),
                });
            }

            stack.Children.Add(new Border { Height = 12 });
            stack.Children.Add(content);

            return new Border
            {
                Background = (Brush)FindResource("CardBackgroundAlt"),
                CornerRadius = new CornerRadius(14),
                Padding = new Thickness(16),
                Margin = new Thickness(0, 0, 0, 16),
                Child = stack,
            };
        }

        private FrameworkElement BuildLabel(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("OnSurface"),
                Margin = new Thickness(0, 12, 0, 0),
            };
        }

        private FrameworkElement BuildDivider()
        {
            return new Border
            {
                Height = 1,
                Background = (Brush)FindResource("DividerBrush"),
                Margin = new Thickness(0, 4, 0, 4),
            };
        }

        private FrameworkElement BuildSwitchRow(string title, string subtitle, bool value, Action<bool> onChange)
        {
            var grid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = (Brush)FindResource("OnSurface"),
            });
            left.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 12,
                Foreground = (Brush)FindResource("OnSurfaceVariant"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 8, 0),
            });
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var cb = new CheckBox
            {
                IsChecked = value,
                VerticalAlignment = VerticalAlignment.Center,
                MinWidth = 24,
            };
            cb.Checked += (_, _) => onChange(true);
            cb.Unchecked += (_, _) => onChange(false);
            Grid.SetColumn(cb, 1);
            grid.Children.Add(cb);

            return grid;
        }

        private FrameworkElement BuildClickRow(string title, string subtitle, Action onClick, FrameworkElement trailing)
        {
            var grid = new Grid { Margin = new Thickness(0, 8, 0, 8), Cursor = Cursors.Hand };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = (Brush)FindResource("OnSurface"),
            });
            left.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 12,
                Foreground = (Brush)FindResource("OnSurfaceVariant"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 2, 8, 0),
            });
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            Grid.SetColumn(trailing, 1);
            grid.Children.Add(trailing);

            grid.MouseLeftButtonUp += (_, _) => onClick();
            return grid;
        }

        private FrameworkElement BuildStepperRow(string title, string subtitle, int value, Action onMinus, Action onPlus)
        {
            var grid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = (Brush)FindResource("OnSurface"),
            });
            left.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 12,
                Foreground = (Brush)FindResource("OnSurfaceVariant"),
                Margin = new Thickness(0, 2, 8, 0),
            });
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var stepper = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var minus = new Button { Content = "-", Width = 32, Height = 32, FontSize = 16, Cursor = Cursors.Hand };
            minus.Click += (_, _) => onMinus();
            stepper.Children.Add(minus);

            stepper.Children.Add(new TextBlock
            {
                Text = value.ToString(),
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("AppPrimaryBrush"),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(8, 0, 8, 0),
                MinWidth = 32,
                TextAlignment = TextAlignment.Center,
            });

            var plus = new Button { Content = "+", Width = 32, Height = 32, FontSize = 16, Cursor = Cursors.Hand };
            plus.Click += (_, _) => onPlus();
            stepper.Children.Add(plus);

            Grid.SetColumn(stepper, 1);
            grid.Children.Add(stepper);
            return grid;
        }

        private FrameworkElement BuildColorRow(string title, string subtitle, Color color, Action onClick)
        {
            var grid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = (Brush)FindResource("OnSurface"),
            });
            left.Children.Add(new TextBlock
            {
                Text = subtitle,
                FontSize = 12,
                Foreground = (Brush)FindResource("OnSurfaceVariant"),
                Margin = new Thickness(0, 2, 8, 0),
            });
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var swatch = new Border
            {
                Width = 30,
                Height = 30,
                CornerRadius = new CornerRadius(15),
                Background = new SolidColorBrush(color),
                BorderBrush = (Brush)FindResource("AppPrimaryBrush"),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
            };
            swatch.MouseLeftButtonUp += (_, _) => onClick();
            Grid.SetColumn(swatch, 1);
            grid.Children.Add(swatch);
            return grid;
        }

        private FrameworkElement BuildVersionCard(string title, string abbr, string description, Action onClick, Action onInfoClick)
        {
            var grid = new Grid { Margin = new Thickness(0, 6, 0, 6) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel { Cursor = Cursors.Hand };
            left.Children.Add(new TextBlock
            {
                Text = title,
                FontSize = 13,
                FontWeight = FontWeights.SemiBold,
                Foreground = (Brush)FindResource("AppPrimaryBrush"),
            });
            left.Children.Add(new TextBlock
            {
                Text = string.IsNullOrEmpty(abbr) ? "Select version" : abbr,
                FontSize = 15,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("OnSurface"),
                Margin = new Thickness(0, 4, 0, 0),
            });
            left.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 11,
                Foreground = (Brush)FindResource("OnSurfaceVariant"),
                Margin = new Thickness(0, 2, 0, 0),
            });
            left.MouseLeftButtonUp += (_, _) => onClick();
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var info = new Button
            {
                Content = "?",
                Width = 32,
                Height = 32,
                FontSize = 16,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = (Brush)FindResource("OnSurfaceVariant"),
                Cursor = Cursors.Hand,
            };
            info.Click += (_, _) => onInfoClick();
            Grid.SetColumn(info, 1);
            grid.Children.Add(info);

            return new Border
            {
                Background = (Brush)FindResource("CardBackground"),
                CornerRadius = new CornerRadius(8),
                Padding = new Thickness(12, 8, 12, 8),
                Margin = new Thickness(0, 4, 0, 4),
                Child = grid,
            };
        }

        private FrameworkElement BuildLayoutToggleRow(string current, Action<string> onChange)
        {
            var grid = new Grid { Margin = new Thickness(0, 8, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = "Multi-View Layout",
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = (Brush)FindResource("OnSurface"),
            });
            left.Children.Add(new TextBlock
            {
                Text = "Horizontal or vertical arrangement",
                FontSize = 12,
                Foreground = (Brush)FindResource("OnSurfaceVariant"),
                Margin = new Thickness(0, 2, 0, 0),
            });
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var pill = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            foreach (var opt in new[] { "horizontal", "vertical" })
            {
                var isSelected = current == opt;
                var btn = new Button
                {
                    Content = char.ToUpper(opt[0]) + opt.Substring(1),
                    Padding = new Thickness(12, 6, 12, 6),
                    Margin = new Thickness(4, 0, 0, 0),
                    Background = isSelected
                        ? (Brush)FindResource("AppPrimaryBrush")
                        : Brushes.Transparent,
                    BorderBrush = (Brush)FindResource("AppPrimaryBrush"),
                    BorderThickness = new Thickness(1),
                    Foreground = isSelected ? Brushes.White : (Brush)FindResource("OnSurface"),
                    Cursor = Cursors.Hand,
                };
                var captured = opt;
                btn.Click += (_, _) => onChange(captured);
                pill.Children.Add(btn);
            }
            Grid.SetColumn(pill, 1);
            grid.Children.Add(pill);
            return grid;
        }

        private FrameworkElement BuildOverlaySliderRow()
        {
            var stack = new StackPanel { Margin = new Thickness(0, 8, 0, 8) };
            var header = new Grid();
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            header.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            header.Children.Add(new TextBlock
            {
                Text = "Reader BG Overlay",
                FontSize = 14,
                FontWeight = FontWeights.Medium,
                Foreground = (Brush)FindResource("OnSurface"),
            });

            var pct = new TextBlock
            {
                Text = $"{(int)(_viewModel.OverlayOpacity * 100)}%",
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = (Brush)FindResource("AppPrimaryBrush"),
            };
            Grid.SetColumn(pct, 1);
            header.Children.Add(pct);
            stack.Children.Add(header);

            var row = new Grid { Margin = new Thickness(0, 8, 0, 0) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var slider = new Slider
            {
                Minimum = 0,
                Maximum = 1,
                Value = _viewModel.OverlayOpacity,
                VerticalAlignment = VerticalAlignment.Center,
            };
            slider.ValueChanged += (_, e) =>
            {
                _viewModel.OverlayOpacity = (float)e.NewValue;
                pct.Text = $"{(int)(e.NewValue * 100)}%";
            };
            Grid.SetColumn(slider, 0);
            row.Children.Add(slider);

            var color = _viewModel.DarkTheme ? _viewModel.DarkOverlayColor : _viewModel.LightOverlayColor;
            var swatch = new Border
            {
                Width = 30,
                Height = 30,
                CornerRadius = new CornerRadius(15),
                Background = new SolidColorBrush(color),
                BorderBrush = (Brush)FindResource("AppPrimaryBrush"),
                BorderThickness = new Thickness(1),
                Cursor = Cursors.Hand,
                Margin = new Thickness(8, 0, 0, 0),
            };
            swatch.MouseLeftButtonUp += (_, _) =>
            {
                if (_viewModel.DarkTheme) _viewModel.ShowDarkOverlayColorWheel = true;
                else _viewModel.ShowLightOverlayColorWheel = true;
            };
            Grid.SetColumn(swatch, 1);
            row.Children.Add(swatch);

            stack.Children.Add(row);
            return stack;
        }

        private FrameworkElement BuildColorButton(string name, Color color, bool isSelected, Action onClick)
        {
            var border = new Border
            {
                BorderBrush = isSelected
                    ? new SolidColorBrush(color)
                    : new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(isSelected ? 2 : 1),
                CornerRadius = new CornerRadius(10),
                Padding = new Thickness(8),
                Margin = new Thickness(0, 0, 6, 6),
                Cursor = Cursors.Hand,
            };

            var stack = new StackPanel();
            stack.Children.Add(new Border
            {
                Width = 40,
                Height = 40,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush(color),
            });
            stack.Children.Add(new TextBlock
            {
                Text = name,
                FontSize = 10,
                FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal,
                Foreground = (Brush)FindResource("OnSurface"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 4, 0, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                MaxWidth = 60,
            });
            border.Child = stack;
            border.MouseLeftButtonUp += (_, _) => onClick();
            return border;
        }

        private FrameworkElement BuildFontButton(string family, bool isSelected, Action onClick)
        {
            var btn = new Button
            {
                Content = char.ToUpper(family[0]) + family.Substring(1),
                Padding = new Thickness(16, 8, 16, 8),
                Margin = new Thickness(0, 0, 8, 8),
                Background = isSelected
                    ? (Brush)FindResource("AppPrimaryBrush")
                    : (Brush)FindResource("CardBackground"),
                BorderBrush = (Brush)FindResource("AppPrimaryBrush"),
                BorderThickness = new Thickness(1),
                Foreground = isSelected ? Brushes.White : (Brush)FindResource("OnSurface"),
                FontFamily = FontsHelper.GetFontFamily(family),
                FontWeight = isSelected ? FontWeights.Bold : FontWeights.Normal,
                FontSize = 13,
                Cursor = Cursors.Hand,
            };
            btn.Click += (_, _) => onClick();
            return btn;
        }

        private FrameworkElement BuildPrimaryButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Content = text,
                Height = 44,
                Background = (Brush)FindResource("AppPrimaryBrush"),
                BorderThickness = new Thickness(0),
                Foreground = Brushes.White,
                FontWeight = FontWeights.SemiBold,
                FontSize = 14,
                Cursor = Cursors.Hand,
            };
            btn.Click += (_, _) => onClick();
            return btn;
        }

        private FrameworkElement BuildTextButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Content = text,
                Padding = new Thickness(12, 8, 12, 8),
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = (Brush)FindResource("AppPrimaryBrush"),
                FontSize = 13,
                Cursor = Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            btn.Click += (_, _) => onClick();
            return btn;
        }

        // -----------------------------------------------------------
        //  Dialog triggers
        // -----------------------------------------------------------
        private void ConfirmResetHighlightColors()
        {
            var result = MessageBox.Show(
                "This will reset all marker colors to their default values. Continue?",
                "Reset Highlight Colors", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _viewModel.ResetHighlightColorsToDefault();
                RebuildAll();
            }
        }

        private void ConfirmRefreshDatabases()
        {
            var result = MessageBox.Show(
                "This will recopy all Bible versions and dictionaries from assets.\n\n" +
                "Bookmarks and settings will not be affected.\n\nContinue?",
                "Refresh All Databases", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            var assetsRoot = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets");
            _ = _viewModel.RefreshDatabasesAsync(assetsRoot);

            MessageBox.Show(
                "Database refresh started in the background.\nCheck the log for progress.",
                "Refresh Started", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ConfirmResetAll()
        {
            var result = MessageBox.Show(
                "This will restore EVERY setting to its default value:\n\n" +
                "• Font size, theme, colors\n" +
                "• Bible versions, layout, sync\n" +
                "• Background, overlay, palette colors\n" +
                "• Study mode, etc.\n\n" +
                "This action cannot be undone. Continue?",
                "Reset All Settings", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            ResetAllToDefaults();
            RebuildAll();
        }

        private void ResetAllToDefaults()
        {
            _viewModel.DisabledVersions = new HashSet<string>();
            _viewModel.HeaderButtonsColor = Colors.White;
            _viewModel.RenderOrbs = false;
            _viewModel.OrbsCount = 3;
            _viewModel.FontSize = 18;
            _viewModel.DarkTheme = false;
            _viewModel.SelectedColor = Color.FromRgb(0x67, 0x50, 0xA4);
            _viewModel.IsCustomColor = false;
            _viewModel.CustomColor = null;
            _viewModel.SelectedFontFamily = "system";
            _viewModel.CurrentDbName = "kj2.sqlite3";
            _viewModel.CurrentVersionAbbr = "KJ2";
            _viewModel.MultiVersion = false;
            _viewModel.SecondaryDbName = "kjv+.sqlite3";
            _viewModel.SecondaryVersionAbbr = "KJV+";
            _viewModel.MultiViewLayout = "horizontal";
            _viewModel.ScrollSync = true;
            _viewModel.CustomTextureUri = null;
            _viewModel.BgImageIndex = 0;
            _viewModel.OverlayOpacity = 0.8f;
            _viewModel.LightOverlayColor = Colors.White;
            _viewModel.DarkOverlayColor = Color.FromRgb(0x10, 0x0F, 0x21);
            _viewModel.DarkThemeReaderFontColor = Colors.White;
            _viewModel.LightThemeReaderFontColor = Color.FromRgb(0x10, 0x10, 0x15);
            _viewModel.LightModalBackgroundColor = Color.FromRgb(0xEA, 0xE7, 0xE3);
            _viewModel.DarkModalBackgroundColor = Color.FromRgb(0x12, 0x15, 0x23);
            _viewModel.WordMarkerColor = Color.FromArgb(0xDD, 0xAC, 0x95, 0xE1);
            _viewModel.VerseMarkerColor = Color.FromRgb(0x95, 0xF1, 0x98);
            _viewModel.IsDictionaryMode = true;
            _viewModel.IsStudyMode = false;
            _viewModel.ShowStrongs = false;
            _viewModel.WordsOfJesus = Color.FromRgb(0xDA, 0x42, 0x27);
            _viewModel.ResetHighlightColorsToDefault();
            _viewModel.SquareAspectViews = true;
            _viewModel.IsReaderFullScreen = false;
            _viewModel.PrimaryPassage = new PassageSelection(10, "Genesis", 1, 1);
            _viewModel.SecondaryPassage = new PassageSelection(500, "John", 1, 1);
            _viewModel.SelectedPrimaryDictLanguage = "English";
            _viewModel.SelectedSecondaryDictLanguage = "English";
            _viewModel.SelectedPrimaryDictionary = "atsbd";
            _viewModel.SelectedSecondaryDictionary = "cbtel";
            _viewModel.SelectedVerseCommentary = "cbsc";
            _viewModel.SelectedCrossReferenceDatabase = "obx";
        }

        // -----------------------------------------------------------
        //  Helpers
        // -----------------------------------------------------------
        private static bool ColorsEqual(Color a, Color b)
            => a.A == b.A && a.R == b.R && a.G == b.G && a.B == b.B;
    }
}