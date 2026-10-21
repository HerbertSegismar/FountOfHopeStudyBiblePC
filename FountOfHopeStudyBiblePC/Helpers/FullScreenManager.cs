// Helpers/FullScreenManager.cs
using System;
using System.Reflection;
using System.Windows;

namespace FountOfHopeStudyBiblePC.Helpers
{
    /// <summary>
    /// Centralised, app-wide fullscreen chrome management.
    ///
    /// The window chrome (title bar row, caption buttons, taskbar coverage)
    /// is applied/restored here so that any screen can request fullscreen
    /// by toggling the shared ViewModel flag, and the state persists across
    /// screen navigation (unlike a per-screen implementation, which would
    /// reset when the screen unloads).
    ///
    /// Works identically for a plain <see cref="Window"/> and for a
    /// MahApps.Metro <c>MetroWindow</c> — the Metro-specific chrome flags
    /// are touched via reflection so this assembly never needs a hard
    /// reference to MahApps.Metro.
    /// </summary>
    public sealed class FullScreenManager
    {
        private readonly Window _window;
        private Snapshot? _snapshot;

        public FullScreenManager(Window window)
        {
            _window = window ?? throw new ArgumentNullException(nameof(window));
        }

        public bool IsActive => _snapshot != null;

        /// <summary>
        /// Idempotent: entering when already in fullscreen or exiting when
        /// already windowed is a no-op, so it's safe to call from a
        /// PropertyChanged handler that may fire more than once.
        /// </summary>
        public void Apply(bool isFullScreen)
        {
            if (isFullScreen && _snapshot == null) Enter();
            else if (!isFullScreen && _snapshot != null) Exit();
        }

        public void Toggle() => Apply(!IsActive);

        // ── enter / exit ───────────────────────────────────────────────
        private void Enter()
        {
            // Snapshot the exact pre-fullscreen state so we can restore it.
            _snapshot = new Snapshot
            {
                WindowState = _window.WindowState,
                WindowStyle = _window.WindowStyle,
                ResizeMode = _window.ResizeMode,
                MetroShowTitleBar = TryGetBool(_window, "ShowTitleBar"),
                MetroUseNoneWindowStyle = TryGetBool(_window, "UseNoneWindowStyle"),
                MetroShowCloseButton = TryGetBool(_window, "ShowCloseButton"),
                MetroShowMinButton = TryGetBool(_window, "ShowMinButton"),
                MetroShowMaxRestoreButton = TryGetBool(_window, "ShowMaxRestoreButton"),
            };

            // Un-maximize first: changing WindowStyle while maximized leaves
            // the window clipped/offset on some Windows versions.
            if (_window.WindowState == WindowState.Maximized)
                _window.WindowState = WindowState.Normal;

            // MahApps.Metro chrome — reflection so we don't require the package.
            TrySetProperty(_window, "ShowTitleBar", false);
            TrySetProperty(_window, "UseNoneWindowStyle", true);
            TrySetProperty(_window, "ShowCloseButton", false);
            TrySetProperty(_window, "ShowMinButton", false);
            TrySetProperty(_window, "ShowMaxRestoreButton", false);

            // Standard WPF chrome removal — covers the plain Window case too.
            _window.WindowStyle = WindowStyle.None;
            _window.ResizeMode = ResizeMode.NoResize;
            _window.WindowState = WindowState.Maximized;
        }

        private void Exit()
        {
            var saved = _snapshot;
            if (saved == null) return;

            if (_window.WindowState == WindowState.Maximized)
                _window.WindowState = WindowState.Normal;

            // Restore Metro flags first — some internally adjust WindowStyle,
            // so we want our explicit saved values to win.
            if (saved.MetroUseNoneWindowStyle.HasValue)
                TrySetProperty(_window, "UseNoneWindowStyle", saved.MetroUseNoneWindowStyle.Value);
            if (saved.MetroShowTitleBar.HasValue)
                TrySetProperty(_window, "ShowTitleBar", saved.MetroShowTitleBar.Value);
            if (saved.MetroShowCloseButton.HasValue)
                TrySetProperty(_window, "ShowCloseButton", saved.MetroShowCloseButton.Value);
            if (saved.MetroShowMinButton.HasValue)
                TrySetProperty(_window, "ShowMinButton", saved.MetroShowMinButton.Value);
            if (saved.MetroShowMaxRestoreButton.HasValue)
                TrySetProperty(_window, "ShowMaxRestoreButton", saved.MetroShowMaxRestoreButton.Value);

            // Base WPF properties.
            _window.WindowStyle = saved.WindowStyle;
            _window.ResizeMode = saved.ResizeMode;
            _window.WindowState = saved.WindowState;

            _snapshot = null;
        }

        // ── reflection helpers ─────────────────────────────────────────
        private static bool? TryGetBool(object target, string propertyName)
        {
            try
            {
                var prop = target.GetType().GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.Instance);
                if (prop == null || !prop.CanRead) return null;
                return prop.GetValue(target) as bool?;
            }
            catch { return null; }
        }

        private static void TrySetProperty(object target, string propertyName, object value)
        {
            try
            {
                var prop = target.GetType().GetProperty(propertyName,
                    BindingFlags.Public | BindingFlags.Instance);
                if (prop == null || !prop.CanWrite) return;
                if (!prop.PropertyType.IsInstanceOfType(value) &&
                    !(prop.PropertyType == typeof(bool) && value is bool)) return;
                prop.SetValue(target, value);
            }
            catch { /* property unsupported on this window type */ }
        }

        // ── saved state ────────────────────────────────────────────────
        private sealed class Snapshot
        {
            public WindowState WindowState;
            public WindowStyle WindowStyle;
            public ResizeMode ResizeMode;
            public bool? MetroShowTitleBar;
            public bool? MetroUseNoneWindowStyle;
            public bool? MetroShowCloseButton;
            public bool? MetroShowMinButton;
            public bool? MetroShowMaxRestoreButton;
        }
    }
}