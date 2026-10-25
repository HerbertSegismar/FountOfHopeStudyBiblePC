// Views/Modals/VersionSelectionModal.xaml.cs
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
        private readonly AppViewModel _viewModel;
        private readonly string _currentVersionKey;
        private readonly bool _isSecondary;
        private readonly Action<string> _onVersionSelected;
        private readonly Action _onDismiss;
        private readonly ModalOverlayController _overlay;

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
            _onVersionSelected = onVersionSelected ?? (_ => { });
            _onDismiss = onDismiss ?? (() => { });

            InitializeComponent();

            _overlay = new ModalOverlayController(this)
            {
                CloseOnOwnerResize = true,
                RevealOnLoad = true,
            };

            BuildList();

            Loaded += (_, _) => SearchBox.Focus();
        }

        // ── Thin XAML stubs → ModalOverlayController ─────────────
        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Window_KeyDown(object sender, KeyEventArgs e)
            => _overlay.HandleKeyDown(sender, e);

        // ═══════════════════════════════════════════════════════════
        //  Build / filter the version list
        // ═══════════════════════════════════════════════════════════
        private void BuildList()
        {
            var activeKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                _viewModel.CurrentDbName
            };
            if (_viewModel.MultiVersion)
                activeKeys.Add(_viewModel.SecondaryDbName);

            var effectiveDisabled = new HashSet<string>(
                _viewModel.DisabledVersions
                    .Where(k => !activeKeys.Contains(k)),
                StringComparer.OrdinalIgnoreCase);

            var grouped = BibleVersionUtils.GetFilteredVersionsGroupedByLanguage(effectiveDisabled);

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
        //  Version click (confirm action → immediate close)
        // ═══════════════════════════════════════════════════════════
        private void VersionRow_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is not Border { Tag: VersionEntry entry }) return;

            App.Log($"VersionSelectionModal: picked {entry.Key} ({entry.ShortName})");

            if (_isSecondary)
                _viewModel.UpdateSecondaryDictionaryForBibleLanguage(entry.Key);
            else
                _viewModel.UpdateDictionaryForBibleLanguage(entry.Key);

            _onVersionSelected(entry.Key);

            // Confirm action → immediate Close. OnClosed fires _onDismiss once
            // (the caller treats that as "dropdown closed", same as cancel).
            Close();
        }

        // ═══════════════════════════════════════════════════════════
        //  Dismiss
        // ═══════════════════════════════════════════════════════════
        private void CloseButton_Click(object sender, RoutedEventArgs e)
            => _overlay.DismissWithFade();

        protected override void OnClosed(EventArgs e)
        {
            base.OnClosed(e);   // raises Closed → controller detaches owner handlers
            _onDismiss();
        }
    }
}