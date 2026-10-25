// Views/Modals/NoteSortDialog.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class NoteSortDialog : Window
    {
        public NoteSortOrder SelectedOrder { get; private set; }

        private bool _isDismissing;
        private bool _dismissArmed;

        /// <summary>
        /// If true, the dialog dismisses when the owner window is resized.
        /// Default is true — the card is small and centred; keeping it on
        /// screen while the owner resizes feels odd.
        /// </summary>
        public bool CloseOnOwnerResize { get; set; } = true;

        public NoteSortDialog(NoteSortOrder current)
        {
            InitializeComponent();
            SelectedOrder = current;

            foreach (NoteSortOrder order in System.Enum.GetValues(typeof(NoteSortOrder)))
            {
                var radio = new RadioButton
                {
                    Content = order.DisplayName(),
                    IsChecked = order == current,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xEA, 0xF0)),
                    Margin = new Thickness(0, 6, 0, 6),
                    Tag = order,
                };
                radio.Checked += (_, _) => SelectedOrder = (NoteSortOrder)radio.Tag;
                OptionsPanel.Children.Add(radio);
            }

            Loaded += OnLoaded;
        }

        // ═══════════════════════════════════════════════════════════
        //  Scrim dismiss infrastructure
        // ═══════════════════════════════════════════════════════════
        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only arm dismissal when the press starts on the scrim.
            // RadioButtons and the Done button handle their own clicks.
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
        //  Confirm
        // ═══════════════════════════════════════════════════════════
        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;   // requires ShowDialog() — see caller note below
            Close();
        }

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
        }
    }
}