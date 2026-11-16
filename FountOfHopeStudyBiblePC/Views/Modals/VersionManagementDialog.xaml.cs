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
        private readonly List<LanguageGroup> _catalog;

        private bool _suppressSelectAll;
        private bool _rebuildQueued;

        private Brush _cardBg = Brushes.Transparent;
        private Brush _appPrimary = Brushes.Transparent;
        private Brush _onSurface = Brushes.White;
        private Brush _onSurfaceVariant = Brushes.Gray;
        private Brush _divider = Brushes.Transparent;

        public VersionManagementDialog(AppViewModel viewModel)
        {
            _viewModel = viewModel;

            InitializeComponent();

            _overlay = new ModalOverlayController(this)
            {
                CloseOnOwnerResize = true,
                RevealOnLoad = true,
            };

            _catalog = BuildCatalog();

            Loaded += (_, _) => BuildList();
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
        }

        private static List<LanguageGroup> BuildCatalog()
        {
            var groups = new Dictionary<string, List<VersionEntry>>();
            foreach (var kv in BibleVersionUtils.VersionMap)
            {
                var lang = BibleVersionUtils.GetLanguageForVersion(kv.Key);
                if (!groups.TryGetValue(lang, out var list))
                    groups[lang] = list = [];
                list.Add(new VersionEntry(
                    kv.Key,
                    kv.Value,
                    BibleVersionUtils.GetDescriptionForVersion(kv.Key)));
            }

            foreach (var list in groups.Values)
                list.Sort((a, b) => string.CompareOrdinal(a.ShortName, b.ShortName));

            var orderedLanguages = new List<string> { "English", "English Messianic" };
            orderedLanguages.AddRange(groups.Keys
                .Where(k => k != "English" && k != "English Messianic")
                .OrderBy(k => k, StringComparer.Ordinal));

            var catalog = new List<LanguageGroup>(groups.Count);
            foreach (var lang in orderedLanguages)
            {
                if (groups.TryGetValue(lang, out var versions))
                    catalog.Add(new LanguageGroup(lang, versions));
            }
            return catalog;
        }

        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Window_KeyDown(object sender, KeyEventArgs e)
            => _overlay.HandleKeyDown(sender, e);

        private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName != nameof(AppViewModel.DarkTheme)) return;
            if (_rebuildQueued) return;
            _rebuildQueued = true;
            Dispatcher.BeginInvoke(new Action(() =>
            {
                _rebuildQueued = false;
                BuildList();
            }));
        }

        private HashSet<string> GetActiveKeys()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                _viewModel.CurrentDbName
            };
            if (_viewModel.MultiVersion) set.Add(_viewModel.SecondaryDbName);
            return set;
        }

        private static List<string> GetNonActiveKeys(HashSet<string> activeKeys)
            => BibleVersionUtils.VersionMap.Keys
                .Where(k => !activeKeys.Contains(k))
                .ToList();

        private void CacheBrushes()
        {
            _cardBg = (Brush)FindResource("CardBackground");
            _appPrimary = (Brush)FindResource("AppPrimaryBrush");
            _onSurface = (Brush)FindResource("OnSurface");
            _onSurfaceVariant = (Brush)FindResource("OnSurfaceVariant");
            _divider = (Brush)FindResource("DividerBrush");
        }

        private void BuildList()
        {
            VersionsList.Children.Clear();
            CacheBrushes();

            var activeKeys = GetActiveKeys();
            var nonActive = GetNonActiveKeys(activeKeys);

            _suppressSelectAll = true;
            SelectAllCheck.IsChecked = nonActive.All(k => !_viewModel.DisabledVersions.Contains(k));
            _suppressSelectAll = false;

            foreach (var group in _catalog)
            {
                VersionsList.Children.Add(BuildLanguageHeader(group.Language));
                foreach (var v in group.Versions)
                {
                    var isActive = activeKeys.Contains(v.Key);
                    var enabled = !_viewModel.DisabledVersions.Contains(v.Key) || isActive;
                    VersionsList.Children.Add(BuildVersionRow(v.Key, v.ShortName, v.Description, isActive, enabled));
                }
            }
        }

        private FrameworkElement BuildLanguageHeader(string language) => new Border
        {
            Background = _cardBg,
            Padding = new Thickness(20, 10, 20, 6),
            Margin = new Thickness(0, 8, 0, 0),
            Child = new TextBlock
            {
                Text = language,
                FontSize = 13,
                FontWeight = FontWeights.Bold,
                Foreground = _appPrimary,
            },
        };

        private FrameworkElement BuildVersionRow(
            string key, string shortName, string description, bool isActive, bool enabled)
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
                Foreground = isActive ? _onSurfaceVariant : _onSurface,
            });
            left.Children.Add(new TextBlock
            {
                Text = description,
                FontSize = 11,
                Foreground = _onSurfaceVariant,
                TextTrimming = TextTrimming.CharacterEllipsis,
            });
            if (isActive)
            {
                left.Children.Add(new TextBlock
                {
                    Text = "Currently active",
                    FontSize = 10,
                    FontWeight = FontWeights.SemiBold,
                    Foreground = _appPrimary,
                });
            }
            Grid.SetColumn(left, 0);
            grid.Children.Add(left);

            var captured = key;
            var cb = new CheckBox
            {
                IsChecked = isActive || enabled,
                IsEnabled = !isActive,
                VerticalAlignment = VerticalAlignment.Center,
            };
            cb.Checked += (_, _) => { if (!isActive) SetVersionEnabled(captured, true); };
            cb.Unchecked += (_, _) => { if (!isActive) SetVersionEnabled(captured, false); };
            Grid.SetColumn(cb, 1);
            grid.Children.Add(cb);

            return new Border
            {
                BorderBrush = _divider,
                BorderThickness = new Thickness(0, 0, 0, 1),
                Child = grid,
            };
        }

        private void SetVersionEnabled(string key, bool enable)
        {
            var set = new HashSet<string>(_viewModel.DisabledVersions);
            if (enable) set.Remove(key);
            else set.Add(key);
            _viewModel.DisabledVersions = set;
            UpdateSelectAllState();
        }

        private void SelectAll_Changed(object sender, RoutedEventArgs e)
        {
            if (_suppressSelectAll) return;

            var nonActive = GetNonActiveKeys(GetActiveKeys());
            var set = new HashSet<string>(_viewModel.DisabledVersions);

            if (SelectAllCheck.IsChecked == true)
                foreach (var k in nonActive) set.Remove(k);
            else
                foreach (var k in nonActive) set.Add(k);

            _viewModel.DisabledVersions = set;
        }

        private void UpdateSelectAllState()
        {
            var nonActive = GetNonActiveKeys(GetActiveKeys());
            _suppressSelectAll = true;
            SelectAllCheck.IsChecked = nonActive.All(k => !_viewModel.DisabledVersions.Contains(k));
            _suppressSelectAll = false;
        }

        private void Close_Click(object sender, RoutedEventArgs e)
            => _overlay.DismissWithFade();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        private sealed record VersionEntry(string Key, string ShortName, string Description);
        private sealed record LanguageGroup(string Language, List<VersionEntry> Versions);
    }
}