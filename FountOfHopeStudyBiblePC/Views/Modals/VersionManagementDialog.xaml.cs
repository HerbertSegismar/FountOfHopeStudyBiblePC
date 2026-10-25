// Views/Modals/VersionManagementDialog.xaml.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class VersionManagementDialog : Window
    {
        private readonly AppViewModel _viewModel;
        private bool _suppressSelectAll;

        // ── Scrim dismiss state ─────────────────────────────────────
        private bool _isDismissing;
        private bool _dismissArmed;

        /// <summary>
        /// If true, the dialog dismisses when the owner window is resized.
        /// Default is true — the card is centred and fixed-size.
        /// </summary>
        public bool CloseOnOwnerResize { get; set; } = true;

        public VersionManagementDialog(AppViewModel viewModel)
        {
            _viewModel = viewModel;
            InitializeComponent();
            Loaded += (_, _) => BuildList();
            Loaded += OnLoaded;
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        // ═══════════════════════════════════════════════════════════
        //  ViewModel subscription (detached in OnClosed)
        // ═══════════════════════════════════════════════════════════
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppViewModel.DarkTheme))
                Dispatcher.BeginInvoke(new Action(BuildList));
        }

        // ═══════════════════════════════════════════════════════════
        //  Scrim dismiss infrastructure
        // ═══════════════════════════════════════════════════════════
        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only arm dismissal when the press starts on the scrim.
            // CheckBoxes (ButtonBase) capture the mouse themselves on down —
            // if we captured on the card they'd stop toggling.
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
            if (Owner is not null)
            {
                Owner.LocationChanged += Owner_LocationChanged;
                Owner.SizeChanged += Owner_SizeChanged;
                Owner.StateChanged += Owner_StateChanged;
                Owner.Closed += Owner_Closed;
            }

            SyncBoundsToOwner();

            // Reveal after bounds are correct (avoids pre-layout flash).
            Opacity = 1;
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
        //  List building
        // ═══════════════════════════════════════════════════════════
        private void BuildList()
        {
            VersionsList.Children.Clear();

            var activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                _viewModel.CurrentDbName
            };
            if (_viewModel.MultiVersion) activeKeys.Add(_viewModel.SecondaryDbName);

            var nonActive = BibleVersionUtils.VersionMap.Keys
                .Where(k => !activeKeys.Contains(k))
                .ToList();

            // Update Select All
            _suppressSelectAll = true;
            SelectAllCheck.IsChecked = nonActive.All(k => !_viewModel.DisabledVersions.Contains(k));
            _suppressSelectAll = false;

            // Group by language
            var groups = new Dictionary<string, List<(string key, string shortName)>>();
            foreach (var kv in BibleVersionUtils.VersionMap)
            {
                var lang = BibleVersionUtils.GetLanguageForVersion(kv.Key);
                if (!groups.TryGetValue(lang, out var list))
                    groups[lang] = list = new List<(string, string)>();
                list.Add((kv.Key, kv.Value));
            }
            foreach (var list in groups.Values)
                list.Sort((a, b) => string.CompareOrdinal(a.shortName, b.shortName));

            var order = new List<string> { "English", "English Messianic" };
            order.AddRange(groups.Keys
                .Where(k => k != "English" && k != "English Messianic")
                .OrderBy(k => k, StringComparer.Ordinal));

            foreach (var lang in order)
            {
                if (!groups.ContainsKey(lang)) continue;
                VersionsList.Children.Add(BuildLanguageHeader(lang));

                foreach (var (key, shortName) in groups[lang])
                {
                    var isActive = activeKeys.Contains(key);
                    var enabled = !_viewModel.DisabledVersions.Contains(key) || isActive;
                    var desc = BibleVersionUtils.GetDescriptionForVersion(key);

                    VersionsList.Children.Add(BuildVersionRow(key, shortName, desc, isActive, enabled));
                }
            }
        }

        private FrameworkElement BuildLanguageHeader(string language)
        {
            return new Border
            {
                Background = (Brush)FindResource("CardBackground"),
                Padding = new Thickness(20, 10, 20, 6),
                Margin = new Thickness(0, 8, 0, 0),
                Child = new TextBlock
                {
                    Text = language,
                    FontSize = 13,
                    FontWeight = FontWeights.Bold,
                    Foreground = (Brush)FindResource("AppPrimaryBrush"),
                }
            };
        }

        private FrameworkElement BuildVersionRow(string key, string shortName, string description, bool isActive, bool enabled)
        {
            var grid = new Grid { Margin = new Thickness(20, 8, 20, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            left.Children.Add(new TextBlock
            {
                Text = shortName,
                FontSize = 15,
                FontWeight = FontWeights.SemiBold,
                Foreground = isActive
                    ? (Brush)FindResource("OnSurfaceVariant")
                    : (Brush)FindResource("OnSurface"),
            });
            left.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 11,
                Foreground = (Brush)FindResource("OnSurfaceVariant"),
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            if (isActive)
            {
                left.Children.Add(new TextBlock
                {
                    Text = "Currently active",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = (Brush)FindResource("AppPrimaryBrush"),
                });
            }
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var cb = new CheckBox
            {
                IsChecked = isActive || enabled,
                IsEnabled = !isActive,
                VerticalAlignment = VerticalAlignment.Center,
            };
            var captured = key;
            cb.Checked += (_, _) =>
            {
                if (!isActive)
                {
                    var set = new HashSet<string>(_viewModel.DisabledVersions);
                    set.Remove(captured);
                    _viewModel.DisabledVersions = set;
                    UpdateSelectAllState();
                }
            };
            cb.Unchecked += (_, _) =>
            {
                if (!isActive)
                {
                    var set = new HashSet<string>(_viewModel.DisabledVersions);
                    set.Add(captured);
                    _viewModel.DisabledVersions = set;
                    UpdateSelectAllState();
                }
            };
            Grid.SetColumn(cb, 1);
            grid.Children.Add(cb);

            return new Border
            {
                Padding = new Thickness(0),
                Margin = new Thickness(0),
                BorderBrush = (Brush)FindResource("DividerBrush"),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = grid,
            };
        }

        private void SelectAll_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressSelectAll) return;

            var activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                _viewModel.CurrentDbName
            };
            if (_viewModel.MultiVersion) activeKeys.Add(_viewModel.SecondaryDbName);

            var nonActive = BibleVersionUtils.VersionMap.Keys
                .Where(k => !activeKeys.Contains(k))
                .ToList();

            if (SelectAllCheck.IsChecked == true)
            {
                var set = new HashSet<string>(_viewModel.DisabledVersions);
                foreach (var k in nonActive) set.Remove(k);
                _viewModel.DisabledVersions = set;
            }
            else
            {
                var set = new HashSet<string>(_viewModel.DisabledVersions);
                foreach (var k in nonActive) set.Add(k);
                _viewModel.DisabledVersions = set;
            }
        }

        private void UpdateSelectAllState()
        {
            var activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                _viewModel.CurrentDbName
            };
            if (_viewModel.MultiVersion) activeKeys.Add(_viewModel.SecondaryDbName);

            var nonActive = BibleVersionUtils.VersionMap.Keys
                .Where(k => !activeKeys.Contains(k))
                .ToList();

            _suppressSelectAll = true;
            SelectAllCheck.IsChecked = nonActive.All(k => !_viewModel.DisabledVersions.Contains(k));
            _suppressSelectAll = false;
        }

        // ═══════════════════════════════════════════════════════════
        //  Dismiss
        // ═══════════════════════════════════════════════════════════
        private void Close_Click(object sender, RoutedEventArgs e) => DismissWithFade();

        // ═══════════════════════════════════════════════════════════
        //  Cleanup
        // ═══════════════════════════════════════════════════════════
        protected override void OnClosed(EventArgs e)
        {
            if (Owner is not null)
            {
                Owner.LocationChanged -= Owner_LocationChanged;
                Owner.SizeChanged -= Owner_SizeChanged;
                Owner.StateChanged -= Owner_StateChanged;
                Owner.Closed -= Owner_Closed;
            }

            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;

            base.OnClosed(e);
        }
    }
}