// Controls/ReaderAppBar.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
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

        // Events subscribed by MainWindow
        public event Action? BibleIconClicked;
        public event Action? ThemeToggled;
        public event Action? ColorLensClicked;
        public event Action? BackRequested;
        public event Action<Screen>? ScreenRequested;

        // Icon / animation state
        private PackIconMaterial? _themeIcon;
        private PackIconMaterial? _menuIcon;
        private RotateTransform? _menuRotation;
        private ContextMenu? _menuPopup;

        // Tracks the last applied orientation so we only rebuild on actual flips
        private bool? _lastIsLandscape;

        public ReaderAppBar(AppViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();

            ApplyOrientationLayout();
            ApplyGradientBackground();
            ApplyTitleButtonStyles();
            UpdateTitleVisibility();

            // Rebuild actions when the window flips orientation
            SizeChanged += (_, __) =>
            {
                if (ApplyOrientationLayout())
                    BuildActions();
            };

            // Subscription lifetime follows the visual lifetime of this control
            // so orphaned handlers don't pile up on the static ThemeManager event.
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
            };

            BuildActions();
            UpdateTitleForegrounds();
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
        {
            BackButton.Visibility = visible ? Visibility.Visible : Visibility.Collapsed;
        }

        // ═══════════════════════════════════════════════════════════
        //  ViewModel subscription
        // ═══════════════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════════════
        //  Layout: orientation + gradient + title styling
        // ═══════════════════════════════════════════════════════════
        private bool IsLandscape => ActualWidth > ActualHeight;

        /// <summary>
        /// Applies landscape/portrait layout. Returns <c>true</c> if the
        /// orientation actually changed since the last call, so the caller
        /// can rebuild the action row only when needed.
        /// </summary>
        private bool ApplyOrientationLayout()
        {
            if (ActualWidth <= 0 && ActualHeight <= 0) return false;

            var isLandscape = IsLandscape;
            Height = isLandscape ? LandscapeHeight : PortraitHeight;

            // Weight the title columns per Compose:
            //   landscape → 2:1, portrait → 1.2:1
            TitleBookCol.Width = new GridLength(isLandscape ? 2 : 1.2, GridUnitType.Star);
            TitleVersionCol.Width = new GridLength(1, GridUnitType.Star);

            var changed = _lastIsLandscape != isLandscape;
            _lastIsLandscape = isLandscape;
            return changed;
        }

        /// <summary>
        /// Port of Brush.verticalGradient(0.6 primary, 0.85 primary, 1.0 transparent).
        /// </summary>
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

        /// <summary>
        /// Port of ButtonDefaults.buttonColors(containerColor = onBackground.copy(0.2f))
        /// + shape = RoundedCornerShape(4.dp).
        /// </summary>
        private void ApplyTitleButtonStyles()
        {
            var fg = _viewModel.HeaderButtonsColor;
            var bg = Color.FromArgb(0x33, fg.R, fg.G, fg.B);

            BookChapterButton.Template = BuildRoundedRectTemplate(4, bg);
            VersionButton.Template = BuildRoundedRectTemplate(4, bg);
        }

        private void UpdateTitleForegrounds()
        {
            var fg = new SolidColorBrush(_viewModel.HeaderButtonsColor);
            BookText.Foreground = fg;
            ChapterText.Foreground = fg;
            VersionText.Foreground = fg;
        }

        /// <summary>Port of `if (!viewModel.multiVersion) { title row }`.</summary>
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
        //  Actions — port of the Compose `actions = { … }` slot
        // ═══════════════════════════════════════════════════════════
        private void BuildActions()
        {
            // Close any open menu before tearing down the button that owns it,
            // otherwise the popup orphans and can never be dismissed.
            if (_menuPopup is { IsOpen: true }) _menuPopup.IsOpen = false;

            ActionsPanel.Children.Clear();

            var isLandscape = IsLandscape;

            // Nav icons — mirrors the big `if (…)` in the Compose:
            //   multiVersion && !landscape
            //   !multiVersion && landscape
            //   landscape && multiVersion && horizontal
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

            // Theme + Color — mirrors the second `if (…)` in the Compose:
            //   !multiVersion
            //   OR (!landscape && multiViewLayout == "horizontal")
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
                _themeIcon = null;   // no theme button in this layout
            }

            // Menu is always present.
            ActionsPanel.Children.Add(BuildMenuButton());
        }

        // ═══════════════════════════════════════════════════════════
        //  Button factories
        // ═══════════════════════════════════════════════════════════
        private Button BuildIconButton(
            PackIconMaterialKind kind, string tooltip, double iconScale, Action onClick)
        {
            var btn = new Button
            {
                Width = IconButtonSize,
                Height = IconButtonSize,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                ToolTip = tooltip,
            };
            btn.Template = BuildTransparentCircleTemplate();

            var icon = new PackIconMaterial
            {
                Kind = kind,
                Width = IconButtonSize * iconScale,
                Height = IconButtonSize * iconScale,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            icon.SetResourceReference(Control.ForegroundProperty, ThemeResources.Icon);

            btn.Content = icon;
            btn.Click += (_, _) => onClick();
            return btn;
        }

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
            btn.Template = BuildTransparentCircleTemplate();

            _menuIcon = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.Menu,
                Width = IconButtonSize * 0.65,
                Height = IconButtonSize * 0.65,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                RenderTransformOrigin = new Point(0.5, 0.5),
            };
            _menuIcon.SetResourceReference(Control.ForegroundProperty, ThemeResources.Icon);
            _menuRotation = new RotateTransform(0);
            _menuIcon.RenderTransform = _menuRotation;
            btn.Content = _menuIcon;

            _menuPopup = BuildReaderMenu();

            // Crossfade + 180° rotation on open (matches Compose).
            _menuPopup.Opened += (_, __) =>
            {
                _menuIcon.Kind = PackIconMaterialKind.Close;
                AnimateMenuRotation(180);
            };
            _menuPopup.Closed += (_, __) =>
            {
                _menuIcon.Kind = PackIconMaterialKind.Menu;
                AnimateMenuRotation(0);
            };

            btn.Click += (_, _) =>
            {
                if (_menuPopup is null) return;
                _menuPopup.PlacementTarget = btn;
                _menuPopup.Placement = PlacementMode.Bottom;
                _menuPopup.IsOpen = !_menuPopup.IsOpen;
            };

            return btn;
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
        //  Dropdown menu — port of ReaderDropdownContent
        // ═══════════════════════════════════════════════════════════
        private ContextMenu BuildReaderMenu()
        {
            var menu = new ContextMenu
            {
                Background = new SolidColorBrush(_viewModel.DarkTheme
                    ? _viewModel.DarkModalBackgroundColor
                    : _viewModel.LightModalBackgroundColor),
            };

            var navItems = new (string title, Screen screen, PackIconMaterialKind icon)[]
            {
                ("Home",      new Screen.Home(),      PackIconMaterialKind.Home),
                ("Reader",    new Screen.Reader(_viewModel.PrimaryPassage), PackIconMaterialKind.BookOpenPageVariant),
                ("Bookmarks", new Screen.Bookmarks(), PackIconMaterialKind.Bookmark),
                ("Notes",     new Screen.Notes(),     PackIconMaterialKind.NoteText),
                ("Search",    new Screen.Search(),    PackIconMaterialKind.Magnify),
                ("Settings",  new Screen.Settings(),  PackIconMaterialKind.Cog),
            };

            foreach (var (title, screen, icon) in navItems)
            {
                var mi = new MenuItem
                {
                    Header = title,
                    Icon = BuildMenuIcon(icon),
                    Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                };
                mi.Click += (_, _) =>
                {
                    menu.IsOpen = false;
                    ScreenRequested?.Invoke(screen);
                };
                menu.Items.Add(mi);
            }

            menu.Items.Add(new Separator());

            var bgItem = new MenuItem
            {
                Header = "Background Texture",
                Icon = BuildMenuIcon(PackIconMaterialKind.TextureBox),
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
            };
            bgItem.Click += (_, _) =>
            {
                menu.IsOpen = false;
                _viewModel.ShowBgModal = true;
            };
            menu.Items.Add(bgItem);

            if (_viewModel.BgImageIndex != 0)
            {
                menu.Items.Add(BuildOverlayOpacityItem(menu));
            }

            menu.Items.Add(new Separator());

            var dictModeItem = new MenuItem
            {
                Header = _viewModel.IsDictionaryMode ? "Dictionary Mode On" : "Word Marker On",
                Icon = BuildMenuIcon(_viewModel.IsDictionaryMode
                    ? PackIconMaterialKind.LabelVariant
                    : PackIconMaterialKind.Fire),
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                IsCheckable = true,
                IsChecked = _viewModel.IsDictionaryMode,
            };
            dictModeItem.Click += (_, _) =>
            {
                _viewModel.IsDictionaryMode = !_viewModel.IsDictionaryMode;
                dictModeItem.IsChecked = _viewModel.IsDictionaryMode;
                dictModeItem.Header = _viewModel.IsDictionaryMode ? "Dictionary Mode On" : "Word Marker On";
            };
            menu.Items.Add(dictModeItem);

            menu.Items.Add(new Separator());

            if (_viewModel.IsDictionaryMode)
                menu.Items.Add(BuildColorPickerItem("Verse Marker Color",
                    _viewModel.VerseMarkerColor, () => _viewModel.ShowVerseMarkerColorWheelDialog = true));
            else
                menu.Items.Add(BuildColorPickerItem("Word Marker Color",
                    _viewModel.WordMarkerColor, () => _viewModel.ShowWordMarkerColorWheelDialog = true));

            menu.Items.Add(BuildColorPickerItem("Jesus' Words Color",
                _viewModel.WordsOfJesus, () => _viewModel.ShowJesusWordsColorWheelDialog = true));

            var fontColor = _viewModel.DarkTheme
                ? _viewModel.DarkThemeReaderFontColor
                : _viewModel.LightThemeReaderFontColor;
            menu.Items.Add(BuildColorPickerItem("Font Color", fontColor, () =>
            {
                if (_viewModel.DarkTheme) _viewModel.ShowDarkReaderFontColorWheelDialog = true;
                else _viewModel.ShowLightReaderFontColorWheelDialog = true;
            }));

            menu.Items.Add(new Separator());

            menu.Items.Add(BuildFontSizeItem());

            return menu;
        }

        private static PackIconMaterial BuildMenuIcon(PackIconMaterialKind kind)
        {
            var icon = new PackIconMaterial
            {
                Kind = kind,
                Width = 16,
                Height = 16,
                VerticalAlignment = VerticalAlignment.Center,
            };
            icon.SetResourceReference(Control.ForegroundProperty, ThemeResources.Icon);
            return icon;
        }

        private MenuItem BuildColorPickerItem(string label, Color color, Action onClick)
        {
            var swatch = new Border
            {
                Width = 16,
                Height = 16,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(color),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                VerticalAlignment = VerticalAlignment.Center,
            };

            var mi = new MenuItem
            {
                Header = label,
                Icon = swatch,
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
            };
            mi.Click += (_, _) =>
            {
                if (_menuPopup is not null) _menuPopup.IsOpen = false;
                onClick();
            };
            return mi;
        }

        private MenuItem BuildOverlayOpacityItem(ContextMenu menu)
        {
            var slider = new Slider
            {
                Width = 140,
                Minimum = 0,
                Maximum = 1,
                Value = _viewModel.OverlayOpacity,
                VerticalAlignment = VerticalAlignment.Center,
            };
            slider.ValueChanged += (_, e) => _viewModel.OverlayOpacity = (float)e.NewValue;

            var container = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(4),
            };
            container.Children.Add(new TextBlock
            {
                Text = "Overlay: ",
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
            });
            container.Children.Add(slider);

            return new MenuItem { Header = container, IsEnabled = false };
        }

        private MenuItem BuildFontSizeItem()
        {
            var container = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(4),
            };

            container.Children.Add(new TextBlock
            {
                Text = "Font Size: ",
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
            });

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

            return new MenuItem { Header = container, IsEnabled = false };
        }

        // ═══════════════════════════════════════════════════════════
        //  Templates
        // ═══════════════════════════════════════════════════════════
        private static ControlTemplate BuildTransparentCircleTemplate()
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "B";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(IconButtonSize / 2));
            border.SetValue(Border.BackgroundProperty, Brushes.Transparent);

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            border.AppendChild(presenter);
            template.VisualTree = border;

            var trigger = new Trigger { Property = IsMouseOverProperty, Value = true };
            trigger.Setters.Add(new Setter(
                Border.BackgroundProperty,
                new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                "B"));
            template.Triggers.Add(trigger);
            return template;
        }

        private static ControlTemplate BuildRoundedRectTemplate(double radius, Color bg)
        {
            var template = new ControlTemplate(typeof(Button));
            var border = new FrameworkElementFactory(typeof(Border));
            border.Name = "B";
            border.SetValue(Border.CornerRadiusProperty, new CornerRadius(radius));
            border.SetValue(Border.BackgroundProperty, new SolidColorBrush(bg));

            var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
            presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
            presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
            presenter.SetValue(ContentPresenter.MarginProperty, new Thickness(8, 0, 8, 0));
            border.AppendChild(presenter);
            template.VisualTree = border;
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
        }
    }
}