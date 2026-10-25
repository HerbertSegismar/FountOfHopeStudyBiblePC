// Controls/ReaderAppBar.xaml.cs
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.ViewModels;
using MahApps.Metro.IconPacks;

namespace FountOfHopeStudyBiblePC.Controls
{
    public partial class ReaderAppBar : UserControl
    {
        private const double IconButtonSize = 35;
        private const double LandscapeHeight = 40;
        private const double PortraitHeight = 80;

        private readonly AppViewModel _viewModel;
        private PassageSelection? _currentPassage;
        private string _currentVersionAbbr = "Bible";

        public event Action? BibleIconClicked;
        public event Action? ThemeToggled;
        public event Action? ColorLensClicked;
        public event Action? BackRequested;
        public event Action<Screen>? ScreenRequested;

        private PackIconMaterial? _themeIcon;

        // Menu popup state
        private PackIconMaterial? _menuIcon;
        private RotateTransform? _menuRotation;
        private Popup? _menuPopup;
        private TextBox? _fontSizeBox;

        // Layout popup state
        private PackIconMaterial? _layoutIcon;
        private RotateTransform? _layoutRotation;
        private Popup? _layoutPopup;

        private bool? _lastIsLandscape;

        public ReaderAppBar(AppViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();

            BackButton.Template = AppBarShared.BuildCircleButtonTemplate(IconButtonSize / 2);
            BackIcon.Foreground = Brushes.White;

            ApplyOrientationLayout();
            ApplyGradientBackground();
            ApplyTitleButtonStyles();
            UpdateTitleVisibility();

            SizeChanged += (_, __) =>
            {
                if (ApplyOrientationLayout())
                    BuildActions();
            };

            Loaded += (_, __) =>
            {
                ThemeManager.Changed += OnThemeManagerChanged;
                _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            };
            Unloaded += (_, __) =>
            {
                ThemeManager.Changed -= OnThemeManagerChanged;
                _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
                if (_menuPopup is { IsOpen: true }) _menuPopup.IsOpen = false;
                if (_layoutPopup is { IsOpen: true }) _layoutPopup.IsOpen = false;
            };

            BuildActions();
            UpdateTitleForegrounds();
        }

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

        private void OnViewModelPropertyChanged(object? sender,
            System.ComponentModel.PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case nameof(AppViewModel.DarkTheme):
                    RefreshThemeIcon();
                    UpdateTitleForegrounds();
                    ApplyTitleButtonStyles();
                    break;
                case nameof(AppViewModel.SelectedColor):
                    ApplyGradientBackground();
                    break;
                case nameof(AppViewModel.HeaderButtonsColor):
                    UpdateTitleForegrounds();
                    ApplyTitleButtonStyles();
                    break;
                case nameof(AppViewModel.MultiVersion):
                case nameof(AppViewModel.MultiViewLayout):
                    BuildActions();
                    UpdateTitleVisibility();
                    ApplyTitleButtonStyles();
                    break;
            }
        }

        private bool IsLandscape => ActualWidth > ActualHeight;

        private bool ApplyOrientationLayout()
        {
            if (ActualWidth <= 0 && ActualHeight <= 0) return false;

            var isLandscape = IsLandscape;
            Height = isLandscape ? LandscapeHeight : PortraitHeight;

            var changed = _lastIsLandscape != isLandscape;
            _lastIsLandscape = isLandscape;
            return changed;
        }

        private void ApplyGradientBackground()
        {
            var primary = ThemeManager.PrimaryColor;
            var gradient = new LinearGradientBrush
            {
                StartPoint = new Point(0, 0),
                EndPoint = new Point(0, 1),
            };
            gradient.GradientStops.Add(new GradientStop(primary, 0.0));
            gradient.GradientStops.Add(new GradientStop(primary, 0.6));
            gradient.GradientStops.Add(new GradientStop(primary, 0.85));
            gradient.GradientStops.Add(new GradientStop(
                Color.FromArgb(0, primary.R, primary.G, primary.B), 1.0));
            gradient.Freeze();
            Background = gradient;
        }

        private void ApplyTitleButtonStyles()
        {
            var bg = Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF);
            BookChapterButton.Template = AppBarShared.BuildRoundedRectTemplate(4, bg);
            VersionButton.Template = AppBarShared.BuildRoundedRectTemplate(4, bg);
        }

        private void UpdateTitleForegrounds()
        {
            BookText.Foreground = Brushes.White;
            ChapterText.Foreground = Brushes.White;
            VersionText.Foreground = Brushes.White;
        }

        private void UpdateTitleVisibility()
        {
            var v = _viewModel.MultiVersion ? Visibility.Collapsed : Visibility.Visible;
            BookChapterButton.Visibility = v;
            VersionButton.Visibility = v;
            TitleRow.Visibility = v;
        }

        private void OnThemeManagerChanged()
        {
            if (!Dispatcher.CheckAccess())
            {
                Dispatcher.Invoke(OnThemeManagerChanged);
                return;
            }
            ApplyGradientBackground();
        }

        // ═══════════════════════════════════════════════════════════
        //  Actions panel
        // ═══════════════════════════════════════════════════════════
        private void BuildActions()
        {
            if (_menuPopup is { IsOpen: true }) _menuPopup.IsOpen = false;
            if (_layoutPopup is { IsOpen: true }) _layoutPopup.IsOpen = false;
            ActionsPanel.Children.Clear();

            var isLandscape = IsLandscape;

            bool showNavRow = (_viewModel.MultiVersion && !isLandscape)
                           || (!_viewModel.MultiVersion && isLandscape)
                           || (isLandscape && _viewModel.MultiVersion
                               && _viewModel.MultiViewLayout == "horizontal");

            if (showNavRow)
            {
                ActionsPanel.Children.Add(BuildIconButton(
                    PackIconMaterialKind.Home, "Home", 0.65,
                    () => ScreenRequested?.Invoke(new Screen.Home())));
                ActionsPanel.Children.Add(BuildIconButton(
                    PackIconMaterialKind.BookmarkMultiple, "Bookmarks", 0.55,
                    () => ScreenRequested?.Invoke(new Screen.Bookmarks())));

                var notesBtn = BuildIconButton(
                    PackIconMaterialKind.NoteText, "Notes", 0.55,
                    () => ScreenRequested?.Invoke(new Screen.Notes()));
                if (notesBtn.Content is PackIconMaterial n)
                {
                    n.RenderTransformOrigin = new Point(0.5, 0.5);
                    n.RenderTransform = new RotateTransform(90);
                }
                ActionsPanel.Children.Add(notesBtn);

                ActionsPanel.Children.Add(BuildIconButton(
                    PackIconMaterialKind.Magnify, "Search", 0.7,
                    () => ScreenRequested?.Invoke(new Screen.Search())));
                ActionsPanel.Children.Add(BuildIconButton(
                    PackIconMaterialKind.Cog, "Settings", 0.6,
                    () => ScreenRequested?.Invoke(new Screen.Settings())));
            }

            bool showThemeRow = !_viewModel.MultiVersion
                             || (!isLandscape && _viewModel.MultiViewLayout == "horizontal");

            if (showThemeRow)
            {
                var themeKind = _viewModel.DarkTheme
                    ? PackIconMaterialKind.Brightness6
                    : PackIconMaterialKind.Brightness2;
                var themeBtn = BuildIconButton(themeKind, "Toggle Theme", 0.6,
                    () => ThemeToggled?.Invoke());
                if (themeBtn.Content is PackIconMaterial ti)
                {
                    ti.RenderTransformOrigin = new Point(0.5, 0.5);
                    ti.RenderTransform = new RotateTransform(180);
                }
                _themeIcon = themeBtn.Content as PackIconMaterial;
                ActionsPanel.Children.Add(themeBtn);

                var colorBtn = BuildIconButton(PackIconMaterialKind.Palette, "Color Scheme", 0.6,
                    () => ColorLensClicked?.Invoke());
                if (colorBtn.Content is PackIconMaterial ci)
                {
                    ci.RenderTransformOrigin = new Point(0.5, 0.5);
                    ci.RenderTransform = new RotateTransform(180);
                }
                ActionsPanel.Children.Add(colorBtn);
            }
            else
            {
                _themeIcon = null;
            }

            ActionsPanel.Children.Add(BuildLayoutButton());
            ActionsPanel.Children.Add(BuildMenuButton());
        }

        private static Button BuildIconButton(
            PackIconMaterialKind kind, string tooltip, double iconScale, Action onClick)
            => AppBarShared.BuildIconButton(
                kind, tooltip, IconButtonSize, IconButtonSize * iconScale, onClick);

        // ═══════════════════════════════════════════════════════════
        //  Layout button (WindowsLayoutDropdown port)
        // ═══════════════════════════════════════════════════════════
        private Button BuildLayoutButton()
        {
            var btn = new Button
            {
                Width = IconButtonSize,
                Height = IconButtonSize,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = _viewModel.MultiVersion ? "Close MultiView" : "MultiView",
            };
            btn.Template = AppBarShared.BuildCircleButtonTemplate(IconButtonSize / 2);

            _layoutIcon = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.ViewGrid,
                Width = IconButtonSize * 0.55,
                Height = IconButtonSize * 0.55,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5),
                Foreground = Brushes.White,
            };
            _layoutRotation = new RotateTransform(0);
            _layoutIcon.RenderTransform = _layoutRotation;
            btn.Content = _layoutIcon;

            _layoutPopup = new Popup
            {
                StaysOpen = true,           // explicit close via window clicks
                AllowsTransparency = true,
                Placement = PlacementMode.Bottom,
                Child = BuildLayoutMenuContent(),
            };

            _layoutPopup.Opened += (_, _) =>
            {
                if (_layoutIcon is not null) _layoutIcon.Kind = PackIconMaterialKind.Close;
                AnimateLayoutRotation(180);

                var window = Window.GetWindow(this);
                if (window is not null)
                {
                    window.PreviewMouseLeftButtonDown += OnLayoutWindowMouseDown;
                    window.Deactivated += OnLayoutWindowDeactivated;
                }
            };
            _layoutPopup.Closed += (_, _) =>
            {
                if (_layoutIcon is not null) _layoutIcon.Kind = PackIconMaterialKind.ViewGrid;
                AnimateLayoutRotation(0);

                var window = Window.GetWindow(this);
                if (window is not null)
                {
                    window.PreviewMouseLeftButtonDown -= OnLayoutWindowMouseDown;
                    window.Deactivated -= OnLayoutWindowDeactivated;
                }
            };

            btn.Click += (_, _) =>
            {
                if (_layoutPopup is null) return;

                if (_layoutPopup.IsOpen)
                {
                    _layoutPopup.IsOpen = false;
                }
                else
                {
                    _layoutPopup.Child = BuildLayoutMenuContent();
                    _layoutPopup.PlacementTarget = btn;
                    _layoutPopup.Placement = PlacementMode.Bottom;
                    _layoutPopup.IsOpen = true;
                }
            };

            return btn;
        }

        private void OnLayoutWindowMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (_layoutPopup is null || !_layoutPopup.IsOpen) return;

            // Click on the layout button → let button.Click toggle it.
            if (_layoutPopup.PlacementTarget is UIElement pt &&
                (ReferenceEquals(e.OriginalSource, pt) ||
                 IsDescendantOf(e.OriginalSource as DependencyObject, pt)))
                return;

            _layoutPopup.IsOpen = false;
        }

        private void OnLayoutWindowDeactivated(object? sender, EventArgs e)
        {
            if (_layoutPopup?.IsOpen == true) _layoutPopup.IsOpen = false;
        }

        private void AnimateLayoutRotation(double angle)
        {
            if (_layoutRotation is null) return;
            var anim = new DoubleAnimation
            {
                To = angle,
                Duration = TimeSpan.FromMilliseconds(300),
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseInOut },
            };
            _layoutRotation.BeginAnimation(RotateTransform.AngleProperty, anim);
        }

        private FrameworkElement BuildLayoutMenuContent()
        {
            var bg = _viewModel.DarkTheme
                ? _viewModel.DarkModalBackgroundColor
                : _viewModel.LightModalBackgroundColor;
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);
            var primary = ThemeManager.PrimaryColor;

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

            // ── Header: "Parallel View" / "Single View" ──────────
            stack.Children.Add(new Border
            {
                Height = 25,
                Child = new TextBlock
                {
                    Text = _viewModel.MultiVersion ? "Parallel View" : "Single View",
                    FontWeight = FontWeights.Bold,
                    Foreground = new SolidColorBrush(primary),
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    TextAlignment = TextAlignment.Center,
                },
            });
            stack.Children.Add(AppBarShared.BuildSeparator());

            // ── Layout option rows ───────────────────────────────
            var layouts = new (string Title, PackIconMaterialKind Icon, double Rotate)[]
            {
                ("Single",     PackIconMaterialKind.Numeric1,   0),
                ("Horizontal", PackIconMaterialKind.ViewStream, 90),
                ("Vertical",   PackIconMaterialKind.ViewStream, 0),
            };

            foreach (var (title, iconKind, rotate) in layouts)
            {
                var isActive = title switch
                {
                    "Single" => !_viewModel.MultiVersion,
                    "Horizontal" => _viewModel.MultiVersion && _viewModel.MultiViewLayout == "horizontal",
                    "Vertical" => _viewModel.MultiVersion && _viewModel.MultiViewLayout == "vertical",
                    _ => false,
                };

                var icon = new PackIconMaterial
                {
                    Kind = iconKind,
                    Width = 16,
                    Height = 16,
                    Foreground = fg,
                };
                if (rotate != 0)
                {
                    icon.RenderTransformOrigin = new Point(0.5, 0.5);
                    icon.RenderTransform = new RotateTransform(rotate);
                }

                var capturedTitle = title;
                stack.Children.Add(AppBarShared.BuildMenuRow(
                    label: title,
                    icon: icon,
                    onClick: () =>
                    {
                        switch (capturedTitle)
                        {
                            case "Single":
                                _viewModel.MultiVersion = false;
                                break;
                            case "Horizontal":
                                _viewModel.SquareAspectViews = false;
                                _viewModel.MultiVersion = true;
                                _viewModel.MultiViewLayout = "horizontal";
                                break;
                            case "Vertical":
                                _viewModel.SquareAspectViews = false;
                                _viewModel.MultiVersion = true;
                                _viewModel.MultiViewLayout = "vertical";
                                break;
                        }
                        if (_layoutPopup is not null) _layoutPopup.IsOpen = false;
                    },
                    foreground: fg,
                    isActive: isActive,
                    isDarkTheme: _viewModel.DarkTheme));
            }

            // ── Square Aspect Ratio (only in parallel mode) ──────
            if (_viewModel.MultiVersion)
            {
                stack.Children.Add(AppBarShared.BuildSeparator());

                var rowGrid = new Grid();
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = new GridLength(1, GridUnitType.Star) });
                rowGrid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = GridLength.Auto });

                var label = new TextBlock
                {
                    Text = "Square Aspect Ratio",
                    Foreground = fg,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(8, 6, 8, 6),
                };
                Grid.SetColumn(label, 0);
                rowGrid.Children.Add(label);

                var check = new CheckBox
                {
                    IsChecked = _viewModel.SquareAspectViews,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(0, 0, 8, 0),
                    // Kotlin: onCheckedChange = null → the row handles the click.
                    IsHitTestVisible = false,
                };
                Grid.SetColumn(check, 1);
                rowGrid.Children.Add(check);

                var row = new Border
                {
                    CornerRadius = new CornerRadius(4),
                    Cursor = System.Windows.Input.Cursors.Hand,
                    Child = rowGrid,
                };

                var hoverBg = new SolidColorBrush(_viewModel.DarkTheme
                    ? Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF)
                    : Color.FromArgb(0x22, 0x00, 0x00, 0x00));
                row.MouseEnter += (_, _) => row.Background = hoverBg;
                row.MouseLeave += (_, _) => row.Background = Brushes.Transparent;
                row.MouseLeftButtonUp += (_, _) =>
                {
                    _viewModel.SquareAspectViews = !_viewModel.SquareAspectViews;
                    check.IsChecked = _viewModel.SquareAspectViews;
                };

                stack.Children.Add(row);
                // Note: RotatingPhoneGraphics preview omitted — separate
                // visual composable, not required for the layout selector
                // behavior. Add later if you want the preview.
            }

            container.Child = stack;
            return container;
        }

        // ═══════════════════════════════════════════════════════════
        //  Menu button (unchanged from prior fix)
        // ═══════════════════════════════════════════════════════════
        private Button BuildMenuButton()
        {
            var btn = new Button
            {
                Width = IconButtonSize,
                Height = IconButtonSize,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = "Menu",
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
                StaysOpen = true,
                AllowsTransparency = true,
                Placement = PlacementMode.Bottom,
                Child = BuildReaderMenuContent(),
            };

            _menuPopup.Opened += (_, __) =>
            {
                _menuIcon.Kind = PackIconMaterialKind.Close;
                AnimateMenuRotation(180);

                var window = Window.GetWindow(this);
                if (window is not null)
                {
                    window.PreviewMouseLeftButtonDown += OnWindowMouseDown;
                    window.Deactivated += OnWindowDeactivated;
                }
            };
            _menuPopup.Closed += (_, __) =>
            {
                CommitPendingFontSize();
                _fontSizeBox = null;

                _menuIcon.Kind = PackIconMaterialKind.Menu;
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
                    _menuPopup.Child = BuildReaderMenuContent();
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

        private void CommitPendingFontSize()
        {
            if (_fontSizeBox is null) return;
            if (int.TryParse(_fontSizeBox.Text, out var value))
                _viewModel.FontSize = Math.Clamp(value, 1, 100);
            _fontSizeBox.Text = _viewModel.FontSize.ToString();
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

        private void RefreshThemeIcon()
        {
            if (_themeIcon is null) return;
            _themeIcon.Kind = _viewModel.DarkTheme
                ? PackIconMaterialKind.Brightness6
                : PackIconMaterialKind.Brightness2;
        }

        // ═══════════════════════════════════════════════════════════
        //  Menu content
        // ═══════════════════════════════════════════════════════════
        private FrameworkElement BuildReaderMenuContent()
        {
            var isLandscape = IsLandscape;
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
                MaxWidth = isLandscape ? 900 : 340,
                Effect = new DropShadowEffect
                {
                    BlurRadius = 14,
                    ShadowDepth = 3,
                    Direction = 270,
                    Color = Colors.Black,
                    Opacity = 0.4,
                },
            };

            if (isLandscape)
            {
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = new GridLength(1, GridUnitType.Star) });
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                grid.ColumnDefinitions.Add(new ColumnDefinition
                { Width = new GridLength(1, GridUnitType.Star) });

                var leftColumn = new StackPanel { Margin = new Thickness(4) };
                BuildCommonScreenItems(leftColumn);
                if (_viewModel.BgImageIndex != 0)
                    leftColumn.Children.Add(BuildBackgroundTextureItem());
                Grid.SetColumn(leftColumn, 0);
                grid.Children.Add(leftColumn);

                var divider = new Border
                {
                    Width = 1,
                    Background = new SolidColorBrush(Color.FromArgb(0x33, 0x80, 0x80, 0x80)),
                    Margin = new Thickness(4, 6, 4, 6),
                };
                Grid.SetColumn(divider, 1);
                grid.Children.Add(divider);

                var rightColumn = new StackPanel { Margin = new Thickness(4) };
                BuildExtraReaderControls(rightColumn);
                Grid.SetColumn(rightColumn, 2);
                grid.Children.Add(rightColumn);

                container.Child = grid;
            }
            else
            {
                var stack = new StackPanel { Margin = new Thickness(4) };
                BuildCommonItems(stack);
                container.Child = stack;
            }

            return container;
        }

        private void BuildCommonScreenItems(Panel host)
        {
            var readerScreen = new Screen.Reader(_viewModel.PrimaryPassage);
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);

            foreach (var (title, screen, iconKind) in
                     AppBarShared.GetNavigationItems(_viewModel.PrimaryPassage))
            {
                var icon = new PackIconMaterial
                {
                    Kind = iconKind,
                    Width = 16,
                    Height = 16,
                    Foreground = fg,
                };
                var isActive = AppBarShared.ScreensMatch(readerScreen, screen);
                var target = screen;

                host.Children.Add(AppBarShared.BuildMenuRow(
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
        }

        private void BuildCommonItems(Panel host)
        {
            BuildCommonScreenItems(host);

            host.Children.Add(AppBarShared.BuildSeparator());
            host.Children.Add(BuildBackgroundTextureItem());
            host.Children.Add(AppBarShared.BuildSeparator());

            if (_viewModel.BgImageIndex != 0)
            {
                host.Children.Add(BuildOverlayOpacityItem());
                host.Children.Add(AppBarShared.BuildSeparator());
            }

            host.Children.Add(BuildDictionaryModeItem());
            host.Children.Add(AppBarShared.BuildSeparator());

            host.Children.Add(_viewModel.IsDictionaryMode
                ? BuildColorPickerItem("Verse Marker Color",
                    _viewModel.VerseMarkerColor,
                    () => _viewModel.ShowVerseMarkerColorWheelDialog = true)
                : BuildColorPickerItem("Word Marker Color",
                    _viewModel.WordMarkerColor,
                    () => _viewModel.ShowWordMarkerColorWheelDialog = true));

            host.Children.Add(AppBarShared.BuildSeparator());
            host.Children.Add(BuildColorPickerItem("Jesus' Words Color",
                _viewModel.WordsOfJesus,
                () => _viewModel.ShowJesusWordsColorWheelDialog = true));

            host.Children.Add(AppBarShared.BuildSeparator());

            var fontColor = _viewModel.DarkTheme
                ? _viewModel.DarkThemeReaderFontColor
                : _viewModel.LightThemeReaderFontColor;
            host.Children.Add(BuildColorPickerItem("Font Color", fontColor, () =>
            {
                if (_viewModel.DarkTheme) _viewModel.ShowDarkReaderFontColorWheelDialog = true;
                else _viewModel.ShowLightReaderFontColorWheelDialog = true;
            }));

            host.Children.Add(AppBarShared.BuildSeparator());
            host.Children.Add(BuildFontSizeItem());
        }

        private void BuildExtraReaderControls(Panel host)
        {
            if (_viewModel.BgImageIndex != 0)
            {
                host.Children.Add(BuildOverlayOpacityItem());
                host.Children.Add(AppBarShared.BuildSeparator());
            }

            host.Children.Add(BuildDictionaryModeItem());
            host.Children.Add(AppBarShared.BuildSeparator());

            host.Children.Add(_viewModel.IsDictionaryMode
                ? BuildColorPickerItem("Verse Marker Color",
                    _viewModel.VerseMarkerColor,
                    () => _viewModel.ShowVerseMarkerColorWheelDialog = true)
                : BuildColorPickerItem("Word Marker Color",
                    _viewModel.WordMarkerColor,
                    () => _viewModel.ShowWordMarkerColorWheelDialog = true));

            host.Children.Add(AppBarShared.BuildSeparator());
            host.Children.Add(BuildColorPickerItem("Jesus' Words Color",
                _viewModel.WordsOfJesus,
                () => _viewModel.ShowJesusWordsColorWheelDialog = true));

            host.Children.Add(AppBarShared.BuildSeparator());

            var fontColor = _viewModel.DarkTheme
                ? _viewModel.DarkThemeReaderFontColor
                : _viewModel.LightThemeReaderFontColor;
            host.Children.Add(BuildColorPickerItem("Font Color", fontColor, () =>
            {
                if (_viewModel.DarkTheme) _viewModel.ShowDarkReaderFontColorWheelDialog = true;
                else _viewModel.ShowLightReaderFontColorWheelDialog = true;
            }));

            host.Children.Add(AppBarShared.BuildSeparator());
            host.Children.Add(BuildFontSizeItem());
        }

        private Border BuildBackgroundTextureItem()
        {
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);
            var icon = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.TextureBox,
                Width = 16,
                Height = 16,
                Foreground = fg,
            };
            return AppBarShared.BuildMenuRow("Background Texture", icon, () =>
            {
                if (_menuPopup is not null) _menuPopup.IsOpen = false;
                _viewModel.ShowBgModal = true;
            }, fg, isDarkTheme: _viewModel.DarkTheme);
        }

        private Border BuildDictionaryModeItem()
        {
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);
            var icon = new PackIconMaterial
            {
                Kind = _viewModel.IsDictionaryMode
                    ? PackIconMaterialKind.LabelVariant
                    : PackIconMaterialKind.Fire,
                Width = 16,
                Height = 16,
                Foreground = new SolidColorBrush(ThemeManager.PrimaryColor),
            };
            var label = _viewModel.IsDictionaryMode
                ? "Dictionary Mode On"
                : "Word Marker On";

            return AppBarShared.BuildMenuRow(label, icon, () =>
            {
                _viewModel.IsDictionaryMode = !_viewModel.IsDictionaryMode;
                if (_menuPopup is not null)
                    _menuPopup.Child = BuildReaderMenuContent();
            }, fg, isDarkTheme: _viewModel.DarkTheme);
        }

        private Border BuildColorPickerItem(string label, Color color, Action onClick)
        {
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);
            var swatch = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(color),
                BorderBrush = AppBarShared.MenuBorderBrush(_viewModel.DarkTheme, 0x66),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
            };
            return AppBarShared.BuildMenuRow(label, swatch, () =>
            {
                if (_menuPopup is not null) _menuPopup.IsOpen = false;
                onClick();
            }, fg, isDarkTheme: _viewModel.DarkTheme);
        }

        private Border BuildOverlayOpacityItem()
        {
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);

            var slider = new Slider
            {
                Width = 140,
                Minimum = 0,
                Maximum = 1,
                Value = _viewModel.OverlayOpacity,
                VerticalAlignment = VerticalAlignment.Center,
            };
            slider.ValueChanged += (_, e) => _viewModel.OverlayOpacity = (float)e.NewValue;

            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(24, 4, 8, 4),
            };
            stack.Children.Add(new TextBlock
            {
                Text = "Overlay: ",
                Foreground = fg,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
            });
            stack.Children.Add(slider);

            return new Border
            {
                CornerRadius = new CornerRadius(4),
                Child = stack,
            };
        }

        // ═══════════════════════════════════════════════════════════
        //  Font-size row — editable TextBox + A- / A+ buttons
        // ═══════════════════════════════════════════════════════════
        private Border BuildFontSizeItem()
        {
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);
            var borderBrush = AppBarShared.MenuBorderBrush(_viewModel.DarkTheme, 0x33);

            var sizeBox = new TextBox
            {
                Text = _viewModel.FontSize.ToString(),
                Foreground = fg,
                CaretBrush = fg,
                Background = Brushes.Transparent,
                BorderBrush = borderBrush,
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Width = 44,
                Height = 26,
                Padding = new Thickness(0),
                Margin = new Thickness(4, 0, 4, 0),
            };
            _fontSizeBox = sizeBox;

            sizeBox.PreviewTextInput += (_, e) =>
            {
                e.Handled = e.Text.Any(c => !char.IsDigit(c));
            };

            DataObject.AddPastingHandler(sizeBox, (_, e) =>
            {
                if (e.DataObject.GetData(DataFormats.Text) is string pasted
                    && pasted.Any(c => !char.IsDigit(c)))
                {
                    e.CancelCommand();
                }
            });

            void CommitFontSize()
            {
                if (int.TryParse(sizeBox.Text, out var value))
                    _viewModel.FontSize = Math.Clamp(value, 1, 100);

                sizeBox.Text = _viewModel.FontSize.ToString();
                sizeBox.CaretIndex = sizeBox.Text.Length;
            }

            sizeBox.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    CommitFontSize();
                    e.Handled = true;
                }
            };
            sizeBox.LostFocus += (_, _) => CommitFontSize();

            var minusBtn = MakeSmallButton("A-", () =>
            {
                CommitFontSize();
                _viewModel.FontSize = Math.Max(1, _viewModel.FontSize - 1);
                sizeBox.Text = _viewModel.FontSize.ToString();
            });
            var plusBtn = MakeSmallButton("A+", () =>
            {
                CommitFontSize();
                _viewModel.FontSize = Math.Min(100, _viewModel.FontSize + 1);
                sizeBox.Text = _viewModel.FontSize.ToString();
            });

            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(24, 4, 8, 4),
            };
            stack.Children.Add(new TextBlock
            {
                Text = "Font Size: ",
                Foreground = fg,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
            });
            stack.Children.Add(minusBtn);
            stack.Children.Add(sizeBox);
            stack.Children.Add(plusBtn);

            return new Border
            {
                CornerRadius = new CornerRadius(4),
                Child = stack,
            };
        }

        private Button MakeSmallButton(string text, Action onClick)
        {
            var fg = AppBarShared.MenuForegroundBrush(_viewModel.DarkTheme);

            var btn = new Button
            {
                Content = text,
                Width = 32,
                Height = 26,
                Margin = new Thickness(4, 0, 4, 0),
                Background = Brushes.Transparent,
                BorderBrush = AppBarShared.MenuBorderBrush(_viewModel.DarkTheme, 0x33),
                BorderThickness = new Thickness(1),
                Foreground = fg,
                Cursor = System.Windows.Input.Cursors.Hand,
            };
            btn.Click += (_, _) => onClick();
            return btn;
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
        }
    }
}