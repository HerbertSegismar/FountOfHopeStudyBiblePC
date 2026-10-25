// Views/Modals/SearchScopeDialog.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class SearchScopeDialog : Window
    {
        private readonly string _currentScope;
        private readonly Action<string> _onScopeSelected;
        private readonly Color _primary;
        private readonly Color _text;
        private readonly Color _muted;
        private readonly Color _card;
        private readonly Color _border;

        private bool _isDismissing;
        private bool _dismissArmed;

        /// <summary>
        /// If true, the dialog dismisses when the owner window is resized.
        /// Default is true — the card is centred and fixed-size; keeping
        /// it on screen while the owner resizes feels odd.
        /// </summary>
        public bool CloseOnOwnerResize { get; set; } = true;

        public SearchScopeDialog(
            string currentScope,
            Action<string> onScopeSelected,
            SearchColors colors)
        {
            _currentScope = currentScope;
            _onScopeSelected = onScopeSelected ?? (_ => { });
            _primary = colors.Primary;
            _text = colors.Text;
            _muted = colors.Muted;
            _card = colors.Card;
            _border = colors.Border;

            InitializeComponent();
            BuildCategories();

            Loaded += OnLoaded;
        }

        // ═══════════════════════════════════════════════════════════
        //  Scrim dismiss infrastructure
        // ═══════════════════════════════════════════════════════════
        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Only arm dismissal when the press starts on the scrim.
            // Scope items are Borders with their own MouseLeftButtonUp —
            // capture on the card would kill their click lambdas.
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
        //  Categories
        // ═══════════════════════════════════════════════════════════
        private void BuildCategories()
        {
            CategoryList.Children.Clear();

            foreach (var kv in SearchScopes.ScopeCategories)
            {
                var category = kv.Key;
                var scopes = kv.Value;

                // Category header
                CategoryList.Children.Add(new Border
                {
                    Background = new SolidColorBrush(Color.FromArgb(
                        0xCC, _primary.R, _primary.G, _primary.B)),
                    Padding = new Thickness(12, 8, 12, 8),
                    Child = new TextBlock
                    {
                        Text = category,
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White,
                    }
                });

                // Scope items
                foreach (var scopeKey in scopes)
                {
                    var config = SearchScopes.GetScopeConfig(scopeKey);
                    var isSelected = scopeKey == _currentScope;

                    var item = new Border
                    {
                        Background = isSelected
                            ? new SolidColorBrush(Color.FromArgb(0x1A, _primary.R, _primary.G, _primary.B))
                            : new SolidColorBrush(_card),
                        Padding = new Thickness(14, 10, 14, 10),
                        Cursor = Cursors.Hand,
                        Tag = scopeKey,
                    };

                    var stack = new StackPanel();
                    stack.Children.Add(new TextBlock
                    {
                        Text = config.Label,
                        FontSize = 14,
                        FontWeight = FontWeights.Medium,
                        Foreground = new SolidColorBrush(isSelected ? _primary : _text),
                    });
                    stack.Children.Add(new TextBlock
                    {
                        Text = config.Description,
                        FontSize = 11,
                        Foreground = new SolidColorBrush(_muted),
                        Margin = new Thickness(0, 2, 0, 0),
                        TextWrapping = TextWrapping.Wrap,
                    });
                    item.Child = stack;

                    item.MouseLeftButtonUp += (_, _) =>
                    {
                        if (item.Tag is string scope)
                        {
                            _onScopeSelected(scope);
                            // Confirm action → immediate close, no fade.
                            Close();
                        }
                    };

                    CategoryList.Children.Add(item);

                    // Divider
                    CategoryList.Children.Add(new Border
                    {
                        Height = 1,
                        Background = new SolidColorBrush(_border),
                    });
                }
            }
        }

        // ═══════════════════════════════════════════════════════════
        //  Dismiss
        // ═══════════════════════════════════════════════════════════
        private void Close_Click(object sender, RoutedEventArgs e) => DismissWithFade();

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