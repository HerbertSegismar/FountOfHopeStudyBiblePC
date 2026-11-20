using System.Reflection;
using System.Windows;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public sealed class FullScreenManager(Window window)
    {
        private readonly Window _window = window ?? throw new ArgumentNullException(nameof(window));
        private readonly PropertyInfo? _showTitleBar = GetProperty(window, "ShowTitleBar");
        private readonly PropertyInfo? _useNoneWindowStyle = GetProperty(window, "UseNoneWindowStyle");
        private readonly PropertyInfo? _showCloseButton = GetProperty(window, "ShowCloseButton");
        private readonly PropertyInfo? _showMinButton = GetProperty(window, "ShowMinButton");
        private readonly PropertyInfo? _showMaxRestoreButton = GetProperty(window, "ShowMaxRestoreButton");

        private Snapshot? _snapshot;

        public bool IsActive => _snapshot != null;

        public void Apply(bool isFullScreen)
        {
            if (isFullScreen && _snapshot == null) Enter();
            else if (!isFullScreen && _snapshot != null) Exit();
        }

        public void Toggle() => Apply(!IsActive);

        private void Enter()
        {
            _snapshot = new Snapshot
            {
                WindowState = _window.WindowState,
                WindowStyle = _window.WindowStyle,
                ResizeMode = _window.ResizeMode,
                ShowTitleBar = Get(_showTitleBar, _window),
                UseNoneWindowStyle = Get(_useNoneWindowStyle, _window),
                ShowCloseButton = Get(_showCloseButton, _window),
                ShowMinButton = Get(_showMinButton, _window),
                ShowMaxRestoreButton = Get(_showMaxRestoreButton, _window),
            };

            if (_window.WindowState == WindowState.Maximized)
                _window.WindowState = WindowState.Normal;

            Set(_showTitleBar, _window, false);
            Set(_useNoneWindowStyle, _window, true);
            Set(_showCloseButton, _window, false);
            Set(_showMinButton, _window, false);
            Set(_showMaxRestoreButton, _window, false);

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

            if (saved.UseNoneWindowStyle.HasValue)
                Set(_useNoneWindowStyle, _window, saved.UseNoneWindowStyle.Value);
            if (saved.ShowTitleBar.HasValue)
                Set(_showTitleBar, _window, saved.ShowTitleBar.Value);
            if (saved.ShowCloseButton.HasValue)
                Set(_showCloseButton, _window, saved.ShowCloseButton.Value);
            if (saved.ShowMinButton.HasValue)
                Set(_showMinButton, _window, saved.ShowMinButton.Value);
            if (saved.ShowMaxRestoreButton.HasValue)
                Set(_showMaxRestoreButton, _window, saved.ShowMaxRestoreButton.Value);

            _window.WindowStyle = saved.WindowStyle;
            _window.ResizeMode = saved.ResizeMode;
            _window.WindowState = saved.WindowState;

            _snapshot = null;
        }

        private static PropertyInfo? GetProperty(object target, string name)
        {
            try
            {
                return target.GetType().GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
            }
            catch
            {
                return null;
            }
        }

        private static bool? Get(PropertyInfo? prop, object target)
        {
            if (prop is null || !prop.CanRead) return null;
            try { return prop.GetValue(target) is bool b ? b : (bool?)null; }
            catch { return null; }
        }

        private static void Set(PropertyInfo? prop, object target, bool value)
        {
            if (prop is null || !prop.CanWrite) return;
            try { prop.SetValue(target, value); }
            catch { }
        }

        private sealed class Snapshot
        {
            public WindowState WindowState;
            public WindowStyle WindowStyle;
            public ResizeMode ResizeMode;
            public bool? ShowTitleBar;
            public bool? UseNoneWindowStyle;
            public bool? ShowCloseButton;
            public bool? ShowMinButton;
            public bool? ShowMaxRestoreButton;
        }
    }
}