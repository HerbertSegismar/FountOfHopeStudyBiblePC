// Controls/ReaderAppBar.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
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
        private Popup? _menuPopup;    // ← was ContextMenu

        private bool? _lastIsLandscape;

        public ReaderAppBar(AppViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();

            // Back arrow — always white (overlays the primary gradient)
            BackButton.Template = BuildTransparentCircleTemplate();
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
            };

            BuildActions();
            UpdateTitleForegrounds();
        }

        // ═══════════════════════════════════════════════════════════
        //  Public API
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
        //  Layout
        // ═══════════════════════════════════════════════════════════
        private bool IsLandscape => ActualWidth > ActualHeight;

        private bool ApplyOrientationLayout()
        {
            if (ActualWidth <= 0 && ActualHeight <= 0) return false;

            var isLandscape = IsLandscape;
            Height = isLandscape ? LandscapeHeight : PortraitHeight;

            // Title columns are now Auto-sized (content-driven) and capped by
            // MaxWidth in XAML. This keeps the verse-reference buttons hugging
            // their text on wide desktop layouts while still allowing the Grid
            // to squeeze them naturally in narrow portrait windows.

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
            BookChapterButton.Template = BuildRoundedRectTemplate(4, bg);
            VersionButton.Template = BuildRoundedRectTemplate(4, bg);
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

            ActionsPanel.Children.Add(BuildMenuButton());
        }

        // ═══════════════════════════════════════════════════════════
        //  App bar icon buttons — ALWAYS WHITE (overlay primary)
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
                Foreground = Brushes.White,
            };
            _menuRotation = new RotateTransform(0);
            _menuIcon.RenderTransform = _menuRotation;
            btn.Content = _menuIcon;

            _menuPopup = BuildReaderMenu();

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
                // Rebuild on each open so theme/orientation changes are picked up.
                _menuPopup.Child = BuildReaderMenuContent();
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
        //  ReaderDropdownContent — port of the Kotlin composable
        // ═══════════════════════════════════════════════════════════
        private Popup BuildReaderMenu()
        {
            return new Popup
            {
                StaysOpen = false,
                AllowsTransparency = true,
                Placement = PlacementMode.Bottom,
                Child = BuildReaderMenuContent(),
            };
        }

        /// <summary>
        /// Kotlin:
        ///   isLandscape ? Row{ Column{ commonItems + BgTexture } ; VDiv ; Column{ ExtraReaderControls } }
        ///               : Column{ commonItems }
        /// where commonItems = screens + (if !isLandscape: everything else).
        /// </summary>
        private FrameworkElement BuildReaderMenuContent()
        {
            var isLandscape = IsLandscape;
            var bg = _viewModel.DarkTheme
                ? _viewModel.DarkModalBackgroundColor
                : _viewModel.LightModalBackgroundColor;

            var container = new Border
            {
                Background = new SolidColorBrush(bg),
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
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
                // ── Row(weight(1f) + VerticalDivider + weight(1f)) ──
                var grid = new Grid();
                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star),
                });
                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = GridLength.Auto,
                });
                grid.ColumnDefinitions.Add(new ColumnDefinition
                {
                    Width = new GridLength(1, GridUnitType.Star),
                });

                // Left column: commonItems (screens only in landscape)
                //               + Background Texture (only if bgImageIndex != 0)
                var leftColumn = new StackPanel { Margin = new Thickness(4) };
                BuildCommonScreenItems(leftColumn);
                if (_viewModel.BgImageIndex != 0)
                {
                    leftColumn.Children.Add(BuildBackgroundTextureItem());
                }
                Grid.SetColumn(leftColumn, 0);
                grid.Children.Add(leftColumn);

                // VerticalDivider
                var divider = new Border
                {
                    Width = 1,
                    Background = new SolidColorBrush(Color.FromArgb(0x33, 0x80, 0x80, 0x80)),
                    Margin = new Thickness(4, 6, 4, 6),
                };
                Grid.SetColumn(divider, 1);
                grid.Children.Add(divider);

                // Right column: ExtraReaderControls
                var rightColumn = new StackPanel { Margin = new Thickness(4) };
                BuildExtraReaderControls(rightColumn);
                Grid.SetColumn(rightColumn, 2);
                grid.Children.Add(rightColumn);

                container.Child = grid;
            }
            else
            {
                // ── Column { commonItems } — commonItems includes everything ──
                var stack = new StackPanel { Margin = new Thickness(4) };
                BuildCommonItems(stack);
                container.Child = stack;
            }

            return container;
        }

        // ── commonItems — screens (always shown) ────────────────────
        private void BuildCommonScreenItems(Panel host)
        {
            var navItems = new (string title, Screen screen, PackIconMaterialKind icon, bool isActive)[]
            {
                ("Home",      new Screen.Home(),      PackIconMaterialKind.Home,                  false),
                ("Reader",    new Screen.Reader(_viewModel.PrimaryPassage),
                                                      PackIconMaterialKind.BookOpenPageVariant,  true),
                ("Bookmarks", new Screen.Bookmarks(), PackIconMaterialKind.Bookmark,              false),
                ("Notes",     new Screen.Notes(),     PackIconMaterialKind.NoteText,              false),
                ("Search",    new Screen.Search(),    PackIconMaterialKind.Magnify,               false),
                ("Settings",  new Screen.Settings(),  PackIconMaterialKind.Cog,                   false),
            };

            foreach (var (title, screen, iconKind, isActive) in navItems)
            {
                var icon = new PackIconMaterial
                {
                    Kind = iconKind,
                    Width = 16,
                    Height = 16,
                    Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                };
                host.Children.Add(BuildMenuRow(
                    label: title,
                    icon: icon,
                    onClick: () =>
                    {
                        if (_menuPopup is not null) _menuPopup.IsOpen = false;
                        ScreenRequested?.Invoke(screen);
                    },
                    isActive: isActive));
            }
        }

        // ── commonItems — portrait only (screens + everything else) ─
        private void BuildCommonItems(Panel host)
        {
            BuildCommonScreenItems(host);

            host.Children.Add(BuildSeparator());
            host.Children.Add(BuildBackgroundTextureItem());
            host.Children.Add(BuildSeparator());

            if (_viewModel.BgImageIndex != 0)
            {
                host.Children.Add(BuildOverlayOpacityItem());
                host.Children.Add(BuildSeparator());
            }

            host.Children.Add(BuildDictionaryModeItem());
            host.Children.Add(BuildSeparator());

            host.Children.Add(_viewModel.IsDictionaryMode
                ? BuildColorPickerItem("Verse Marker Color",
                    _viewModel.VerseMarkerColor,
                    () => _viewModel.ShowVerseMarkerColorWheelDialog = true)
                : BuildColorPickerItem("Word Marker Color",
                    _viewModel.WordMarkerColor,
                    () => _viewModel.ShowWordMarkerColorWheelDialog = true));

            host.Children.Add(BuildSeparator());
            host.Children.Add(BuildColorPickerItem("Jesus' Words Color",
                _viewModel.WordsOfJesus,
                () => _viewModel.ShowJesusWordsColorWheelDialog = true));

            host.Children.Add(BuildSeparator());

            var fontColor = _viewModel.DarkTheme
                ? _viewModel.DarkThemeReaderFontColor
                : _viewModel.LightThemeReaderFontColor;
            host.Children.Add(BuildColorPickerItem("Font Color", fontColor, () =>
            {
                if (_viewModel.DarkTheme) _viewModel.ShowDarkReaderFontColorWheelDialog = true;
                else _viewModel.ShowLightReaderFontColorWheelDialog = true;
            }));

            host.Children.Add(BuildSeparator());
            host.Children.Add(BuildFontSizeItem());
        }

        // ── ExtraReaderControls — landscape second column ───────────
        private void BuildExtraReaderControls(Panel host)
        {
            if (_viewModel.BgImageIndex != 0)
            {
                host.Children.Add(BuildOverlayOpacityItem());
                host.Children.Add(BuildSeparator());
            }

            host.Children.Add(BuildDictionaryModeItem());
            host.Children.Add(BuildSeparator());

            host.Children.Add(_viewModel.IsDictionaryMode
                ? BuildColorPickerItem("Verse Marker Color",
                    _viewModel.VerseMarkerColor,
                    () => _viewModel.ShowVerseMarkerColorWheelDialog = true)
                : BuildColorPickerItem("Word Marker Color",
                    _viewModel.WordMarkerColor,
                    () => _viewModel.ShowWordMarkerColorWheelDialog = true));

            host.Children.Add(BuildSeparator());
            host.Children.Add(BuildColorPickerItem("Jesus' Words Color",
                _viewModel.WordsOfJesus,
                () => _viewModel.ShowJesusWordsColorWheelDialog = true));

            host.Children.Add(BuildSeparator());

            var fontColor = _viewModel.DarkTheme
                ? _viewModel.DarkThemeReaderFontColor
                : _viewModel.LightThemeReaderFontColor;
            host.Children.Add(BuildColorPickerItem("Font Color", fontColor, () =>
            {
                if (_viewModel.DarkTheme) _viewModel.ShowDarkReaderFontColorWheelDialog = true;
                else _viewModel.ShowLightReaderFontColorWheelDialog = true;
            }));

            host.Children.Add(BuildSeparator());
            host.Children.Add(BuildFontSizeItem());
        }

        // ═══════════════════════════════════════════════════════════
        //  Individual menu-row builders
        //  NOTE: these rows sit on the modal background, so they stay
        //        theme-aware (NOT white like the app bar icons).
        // ═══════════════════════════════════════════════════════════
        private Border BuildBackgroundTextureItem()
        {
            var icon = new PackIconMaterial
            {
                Kind = PackIconMaterialKind.TextureBox,
                Width = 16,
                Height = 16,
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
            };
            return BuildMenuRow("Background Texture", icon, () =>
            {
                if (_menuPopup is not null) _menuPopup.IsOpen = false;
                _viewModel.ShowBgModal = true;
            });
        }

        private Border BuildDictionaryModeItem()
        {
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

            return BuildMenuRow(label, icon, () =>
            {
                _viewModel.IsDictionaryMode = !_viewModel.IsDictionaryMode;
                // Toggle in place (menu stays open, matching Kotlin).
                if (_menuPopup is not null)
                    _menuPopup.Child = BuildReaderMenuContent();
            });
        }

        private Border BuildColorPickerItem(string label, Color color, Action onClick)
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
            return BuildMenuRow(label, swatch, () =>
            {
                if (_menuPopup is not null) _menuPopup.IsOpen = false;
                onClick();
            });
        }

        private Border BuildOverlayOpacityItem()
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

            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(24, 4, 8, 4),
            };
            stack.Children.Add(new TextBlock
            {
                Text = "Overlay: ",
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
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

        private Border BuildFontSizeItem()
        {
            var valueText = new TextBlock
            {
                Text = _viewModel.FontSize.ToString(),
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold,
                Width = 28,
                TextAlignment = TextAlignment.Center,
            };

            var minusBtn = MakeSmallButton("A-", () =>
            {
                _viewModel.FontSize = Math.Max(1, _viewModel.FontSize - 1);
                valueText.Text = _viewModel.FontSize.ToString();
            });
            var plusBtn = MakeSmallButton("A+", () =>
            {
                _viewModel.FontSize = Math.Min(100, _viewModel.FontSize + 1);
                valueText.Text = _viewModel.FontSize.ToString();
            });

            var stack = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(24, 4, 8, 4),
            };
            stack.Children.Add(new TextBlock
            {
                Text = "Font Size: ",
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 8, 0),
            });
            stack.Children.Add(minusBtn);
            stack.Children.Add(valueText);
            stack.Children.Add(plusBtn);

            return new Border
            {
                CornerRadius = new CornerRadius(4),
                Child = stack,
            };
        }

        private Button MakeSmallButton(string text, Action onClick)
        {
            var btn = new Button
            {
                Content = text,
                Width = 32,
                Height = 26,
                Margin = new Thickness(4, 0, 4, 0),
                Background = Brushes.Transparent,
                BorderBrush = new SolidColorBrush(Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF)),
                BorderThickness = new Thickness(1),
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
                Cursor = System.Windows.Input.Cursors.Hand,
            };
            btn.Click += (_, _) => onClick();
            return btn;
        }

        // ═══════════════════════════════════════════════════════════
        //  Reusable row template — mimics Material3 DropdownMenuItem
        // ═══════════════════════════════════════════════════════════
        private Border BuildMenuRow(
            string label,
            FrameworkElement? icon,
            Action? onClick,
            bool isActive = false)
        {
            var hoverBg = new SolidColorBrush(Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF));
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
                Foreground = new SolidColorBrush(_viewModel.HeaderButtonsColor),
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

        private static Border BuildSeparator() => new()
        {
            Height = 1,
            Background = new SolidColorBrush(Color.FromArgb(0x33, 0x80, 0x80, 0x80)),
            Margin = new Thickness(4, 4, 4, 4),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

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