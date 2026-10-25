// Views/Modals/VersionManagementDialog.xaml.cs
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class VersionManagementDialog : Window
    {
        private readonly AppViewModel _viewModel;
        private readonly ModalOverlayController _overlay;
        private bool _suppressSelectAll;

        public VersionManagementDialog(AppViewModel viewModel)
        {
            _viewModel = viewModel;

            InitializeComponent();

            _overlay = new ModalOverlayController(this)
            {
                CloseOnOwnerResize = true,
                RevealOnLoad = true,
            };

            Loaded += (_, _) => BuildList();
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        // ── Thin XAML stubs → ModalOverlayController ─────────────
        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Window_KeyDown(object sender, KeyEventArgs e)
            => _overlay.HandleKeyDown(sender, e);

        // ═══════════════════════════════════════════════════════════
        //  ViewModel subscription (detached in OnClosed)
        // ═══════════════════════════════════════════════════════════
        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(AppViewModel.DarkTheme))
                Dispatcher.BeginInvoke(new Action(BuildList));
        }

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
        private void Close_Click(object sender, RoutedEventArgs e)
            => _overlay.DismissWithFade();

        // ═══════════════════════════════════════════════════════════
        //  Cleanup
        // ═══════════════════════════════════════════════════════════
        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);   // raises Closed → controller detaches owner handlers

            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }
    }
}