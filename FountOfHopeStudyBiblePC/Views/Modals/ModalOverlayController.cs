using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Animation;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public sealed class ModalOverlayController
    {
        private readonly Window _window;
        private bool _isDismissing;
        private bool _dismissArmed;

        public bool CloseOnOwnerResize { get; set; } = true;

        public bool RevealOnLoad { get; set; } = false;

        public ModalOverlayController(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
            _window.Loaded += OnLoaded;
            _window.Closed += OnWindowClosed;
        }

        public void HandleScrimMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (!ReferenceEquals(e.OriginalSource, sender))
            {
                _dismissArmed = false;
                return;
            }

            _dismissArmed = true;
            (sender as UIElement)?.CaptureMouse();
            e.Handled = true;
        }

        public void HandleScrimMouseUp(object sender, MouseButtonEventArgs e)
        {
            bool shouldDismiss = _dismissArmed;
            _dismissArmed = false;

            if (sender is UIElement el && el.IsMouseCaptured)
                el.ReleaseMouseCapture();

            if (shouldDismiss)
                DismissWithFade();

            e.Handled = true;
        }

        public void HandleCardMouseDown(object sender, MouseButtonEventArgs e)
        {
            _dismissArmed = false;
            if (sender is UIElement el) el.CaptureMouse();
            e.Handled = true;
        }

        public void HandleCardMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is UIElement el && el.IsMouseCaptured)
                el.ReleaseMouseCapture();
            e.Handled = true;
        }

        public void HandleKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DismissWithFade();
                e.Handled = true;
            }
        }

        public void DismissWithFade()
        {
            if (_isDismissing) return;
            _isDismissing = true;

            var fade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120));
            fade.Completed += (_, _) => _window.Close();
            _window.BeginAnimation(Window.OpacityProperty, fade);
        }

        private void OnLoaded(object? sender, RoutedEventArgs e)
        {
            var owner = _window.Owner;
            if (owner is not null)
            {
                owner.LocationChanged += Owner_LocationChanged;
                owner.SizeChanged += Owner_SizeChanged;
                owner.StateChanged += Owner_StateChanged;
                owner.Closed += Owner_Closed;

                SyncBoundsToOwner();
            }

            if (RevealOnLoad)
                _window.Opacity = 1;
        }

        private void OnWindowClosed(object? sender, EventArgs e)
        {
            var owner = _window.Owner;
            if (owner is null) return;

            owner.LocationChanged -= Owner_LocationChanged;
            owner.SizeChanged -= Owner_SizeChanged;
            owner.StateChanged -= Owner_StateChanged;
            owner.Closed -= Owner_Closed;
        }

        private void SyncBoundsToOwner()
        {
            var owner = _window.Owner;
            if (owner is null) return;

            _window.WindowStartupLocation = WindowStartupLocation.Manual;

            if (owner.WindowState == WindowState.Maximized)
            {
                _window.WindowState = WindowState.Maximized;
                return;
            }
            _window.WindowState = WindowState.Normal;
            _window.Left = owner.Left;
            _window.Top = owner.Top;
            _window.Width = owner.ActualWidth;
            _window.Height = owner.ActualHeight;
        }

        private void Owner_LocationChanged(object? sender, EventArgs e)
        {
            var owner = _window.Owner;
            if (owner is null) return;
            _window.Left = owner.Left;
            _window.Top = owner.Top;
        }

        private void Owner_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (CloseOnOwnerResize) { DismissWithFade(); return; }
            var owner = _window.Owner;
            if (owner is null) return;
            _window.Left = owner.Left;
            _window.Top = owner.Top;
            _window.Width = owner.ActualWidth;
            _window.Height = owner.ActualHeight;
        }

        private void Owner_StateChanged(object? sender, EventArgs e)
        {
            if (_window.Owner?.WindowState == WindowState.Minimized)
                DismissWithFade();
        }

        private void Owner_Closed(object? sender, EventArgs e) => DismissWithFade();
    }
}