using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class AboutDialog : Window
    {
        public bool CloseOnOwnerResize { get; set; } = false;

        public AboutDialog()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            Closed += OnClosed;
        }

        // ───────── Compose-equivalent dismiss ─────────

        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Click originated on the card → let the card deal with it.
            if (!ReferenceEquals(e.OriginalSource, Scrim))
                return;

            // Take capture so the corresponding MouseUp comes back to *us*,
            // even if the pointer moves off the scrim. This is what prevents
            // the up from landing on the owner window's controls.
            Scrim.CaptureMouse();
            e.Handled = true;
        }

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            bool onScrim = ReferenceEquals(e.OriginalSource, Scrim);

            Scrim.ReleaseMouseCapture();
            e.Handled = true;

            // Only dismiss when the *up* also happened on the scrim.
            // If the user pressed on scrim and released on the card, do nothing.
            if (!onScrim)
                return;

            var fade = new System.Windows.Media.Animation.DoubleAnimation(1, 0,
                    TimeSpan.FromMilliseconds(120));
                    fade.Completed += (_, _) => Close();
                    BeginAnimation(OpacityProperty, fade);
        }

        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Capture on the card too, so a press-drag-release started
            // on the card never reaches the scrim (and thus never dismisses).
            ((UIElement)sender).CaptureMouse();
            e.Handled = true;
        }

        private void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ((UIElement)sender).ReleaseMouseCapture();
            e.Handled = true;
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
                e.Handled = true;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();

        // ───────── Owner bounds tracking (unchanged) ─────────

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            if (Owner is null) return;
            Owner.LocationChanged += Owner_LocationChanged;
            Owner.SizeChanged += Owner_SizeChanged;
            Owner.StateChanged += Owner_StateChanged;
            Owner.Closed += Owner_Closed;
            SyncBoundsToOwner();
        }

        private void OnClosed(object? sender, EventArgs e)
        {
            if (Owner is null) return;
            Owner.LocationChanged -= Owner_LocationChanged;
            Owner.SizeChanged -= Owner_SizeChanged;
            Owner.StateChanged -= Owner_StateChanged;
            Owner.Closed -= Owner_Closed;
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
            if (CloseOnOwnerResize) { Close(); return; }
            if (Owner is null) return;
            Left = Owner.Left;
            Top = Owner.Top;
            Width = Owner.ActualWidth;
            Height = Owner.ActualHeight;
        }

        private void Owner_StateChanged(object? sender, EventArgs e)
        {
            if (Owner?.WindowState == WindowState.Minimized) Close();
        }

        private void Owner_Closed(object? sender, EventArgs e) => Close();
    }
}