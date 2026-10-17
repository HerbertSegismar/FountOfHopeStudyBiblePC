// Views/Modals/VersionSelectionModal.xaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class VersionSelectionModal : Window
    {
        private readonly AppViewModel _viewModel;
        private readonly string _currentVersionKey;
        private readonly bool _isSecondary;
        private readonly Action<string> _onVersionSelected;
        private readonly Action _onDismiss;

        private string _searchQuery = "";

        // Constructor — fields first, then InitializeComponent
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
            _onVersionSelected = onVersionSelected ?? (_ => { });
            _onDismiss = onDismiss ?? (() => { });

            InitializeComponent();

            Loaded += (_, _) => SearchBox.Focus();
            BuildList();
        }

        // ═══════════════════════════════════════════════════════════
        //  Build / filter the version list
        // ═══════════════════════════════════════════════════════════
        private void BuildList()
        {
            // Build active version set (excluded from the disabled list)
            var activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                _viewModel.CurrentDbName
            };
            if (_viewModel.MultiVersion)
                activeKeys.Add(_viewModel.SecondaryDbName);

            // Effective disabled set = disabled − active
            var effectiveDisabled = new HashSet<string>(
                _viewModel.DisabledVersions
                    .Where(k => !activeKeys.Contains(k)),
                StringComparer.OrdinalIgnoreCase);

            // Get the grouped versions
            var grouped = BibleVersionUtils.GetFilteredVersionsGroupedByLanguage(effectiveDisabled);

            // Apply search filter
            var groups = new List<VersionGroup>();
            foreach (var kv in grouped)
            {
                var language = kv.Key;
                var versions = kv.Value;
                var matchedList = new List<VersionEntry>();

                bool languageMatches = !string.IsNullOrWhiteSpace(_searchQuery)
                    && language.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase);

                foreach (var (dbFile, shortName) in versions)
                {
                    var description = BibleVersionUtils.GetDescriptionForVersion(dbFile);

                    var isMatch =
                        string.IsNullOrWhiteSpace(_searchQuery)
                        || languageMatches
                        || shortName.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)
                        || description.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase)
                        || dbFile.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase);

                    if (isMatch)
                    {
                        matchedList.Add(new VersionEntry
                        {
                            Language = language,
                            Key = dbFile,
                            ShortName = shortName,
                            Description = description,
                            IsSelected = string.Equals(dbFile, _currentVersionKey,
                                                         StringComparison.OrdinalIgnoreCase),
                        });
                    }
                }

                if (matchedList.Count > 0)
                {
                    groups.Add(new VersionGroup
                    {
                        Language = language,
                        Versions = matchedList,
                    });
                }
            }

            VersionList.ItemsSource = groups;

            // Toggle empty state
            bool anyResults = groups.Count > 0;
            VersionScroller.Visibility = anyResults ? Visibility.Visible : Visibility.Collapsed;
            EmptyState.Visibility = anyResults ? Visibility.Collapsed : Visibility.Visible;

            ClearSearchBtn.Visibility = string.IsNullOrEmpty(_searchQuery)
                ? Visibility.Collapsed : Visibility.Visible;
        }

        // ═══════════════════════════════════════════════════════════
        //  Search handlers
        // ═══════════════════════════════════════════════════════════
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

        // ═══════════════════════════════════════════════════════════
        //  Version click
        // ═══════════════════════════════════════════════════════════
        private void VersionRow_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border { Tag: VersionEntry entry }) return;

            App.Log($"VersionSelectionModal: picked {entry.Key} ({entry.ShortName})");

            // Update the dictionary language for this Bible
            if (_isSecondary)
                _viewModel.UpdateSecondaryDictionaryForBibleLanguage(entry.Key);
            else
                _viewModel.UpdateDictionaryForBibleLanguage(entry.Key);

            // Notify the caller
            _onVersionSelected(entry.Key);

            // Close
            Close();
        }

        // ═══════════════════════════════════════════════════════════
        //  Close
        // ═══════════════════════════════════════════════════════════
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            _onDismiss();
            Close();
        }

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);
            _onDismiss();
        }
    }
}