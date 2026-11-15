using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class VersionSelectionModal : Window
    {
        private static readonly Action NoOp = () => { };
        private static readonly Action<string> NoOpString = _ => { };

        private readonly AppViewModel _viewModel;
        private readonly string _currentVersionKey;
        private readonly bool _isSecondary;
        private readonly Action<string> _onVersionSelected;
        private readonly Action _onDismiss;
        private readonly ModalOverlayController _overlay;

        private readonly List<VersionEntry> _catalog;
        private string _searchQuery = "";

        public VersionSelectionModal(
            AppViewModel viewModel,
            string currentVersionKey,
            bool isSecondary,
            Action<string> onVersionSelected,
            Action onDismiss)
        {
            _viewModel = viewModel;
            _currentVersionKey = currentVersionKey;
            _isSecondary = isSecondary;
            _onVersionSelected = onVersionSelected ?? NoOpString;
            _onDismiss = onDismiss ?? NoOp;

            InitializeComponent();

            _overlay = new ModalOverlayController(this)
            {
                CloseOnOwnerResize = true,
                RevealOnLoad = true,
            };

            _catalog = BuildCatalog();

            BuildList();

            Loaded += (_, _) => SearchBox.Focus();
        }

        private List<VersionEntry> BuildCatalog()
        {
            var activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                _viewModel.CurrentDbName
            };
            if (_viewModel.MultiVersion)
                activeKeys.Add(_viewModel.SecondaryDbName);

            var effectiveDisabled = new HashSet<string>(
                _viewModel.DisabledVersions.Where(k => !activeKeys.Contains(k)),
                StringComparer.OrdinalIgnoreCase);

            var grouped = BibleVersionUtils.GetFilteredVersionsGroupedByLanguage(effectiveDisabled);

            var catalog = new List<VersionEntry>();
            foreach (var kv in grouped)
            {
                var language = kv.Key;
                foreach (var (dbFile, shortName) in kv.Value)
                {
                    catalog.Add(new VersionEntry
                    {
                        Language = language,
                        Key = dbFile,
                        ShortName = shortName,
                        Description = BibleVersionUtils.GetDescriptionForVersion(dbFile),
                        IsSelected = string.Equals(dbFile, _currentVersionKey, StringComparison.OrdinalIgnoreCase),
                    });
                }
            }
            return catalog;
        }

        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Window_KeyDown(object sender, KeyEventArgs e)
            => _overlay.HandleKeyDown(sender, e);

        private void BuildList()
        {
            IEnumerable<VersionEntry> filtered;
            if (string.IsNullOrWhiteSpace(_searchQuery))
            {
                filtered = _catalog;
            }
            else
            {
                var q = _searchQuery;
                filtered = _catalog.Where(v =>
                    v.Language.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    v.ShortName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    v.Description.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    v.Key.Contains(q, StringComparison.OrdinalIgnoreCase));
            }

            var groups = filtered
                .GroupBy(v => v.Language)
                .Select(g => new VersionGroup { Language = g.Key, Versions = g.ToList() })
                .ToList();

            VersionList.ItemsSource = groups;

            var anyResults = groups.Count > 0;
            VersionScroller.Visibility = anyResults ? Visibility.Visible : Visibility.Collapsed;
            EmptyState.Visibility = anyResults ? Visibility.Collapsed : Visibility.Visible;
            ClearSearchBtn.Visibility = string.IsNullOrEmpty(_searchQuery)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            _searchQuery = SearchBox.Text ?? "";
            BuildList();
        }

        private void ClearSearchBtn_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            _searchQuery = "";
            BuildList();
            SearchBox.Focus();
        }

        private void VersionRow_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border { Tag: VersionEntry entry }) return;

            if (_isSecondary)
                _viewModel.UpdateSecondaryDictionaryForBibleLanguage(entry.Key);
            else
                _viewModel.UpdateDictionaryForBibleLanguage(entry.Key);

            _onVersionSelected(entry.Key);
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => _overlay.DismissWithFade();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _onDismiss();
        }
    }
}