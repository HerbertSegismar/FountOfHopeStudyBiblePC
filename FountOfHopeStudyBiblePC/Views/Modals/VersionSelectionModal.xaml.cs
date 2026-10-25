// Views/Modals/VersionSelectionModal.xaml.cs
using System;
using System.Collections.Generic;
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
    public partial class VersionSelectionModal : Window
    {
        private readonly AppViewModel _viewModel;
        private readonly string _currentVersionKey;
        private readonly bool _isSecondary;
        private readonly Action<string> _onVersionSelected;
        private readonly Action _onDismiss;

        private string _searchQuery = "";

        // ── Scrim dismiss state ─────────────────────────────────────
        private bool _isDismissing;
        private bool _dismissArmed;

        /// <summary>
        /// If true, the dialog dismisses when the owner window is resized.
        /// Default is true — the card is centred and fixed-size.
        /// </summary>
        public bool CloseOnOwnerResize { get; set; } = true;

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
            BuildList();

            Loaded += OnLoaded;
            Loaded += (_, _) => SearchBox.Focus();
        }

        // ═══════════════════════════════════════════════════════════
        //  Scrim dismiss infrastructure
        // ═══════════════════════════════════════════════════════════
        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only arm dismissal when the press starts on the scrim.
            // SearchBox, ScrollViewer, and version rows all need their own
            // mouse lifecycle untouched.
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
        private void CloseButton_Click(object sender, RoutedEventArgs e) => DismissWithFade();

        protected override void OnClosed(EventArgs e)
        {
            if (Owner is not null)
            {
                Owner.LocationChanged -= Owner_LocationChanged;
                Owner.SizeChanged -= Owner_SizeChanged;
                Owner.StateChanged -= Owner_StateChanged;
                Owner.Closed -= Owner_Closed;
            }

            base.OnClosed(e);
            _onDismiss();
        }
    }
}