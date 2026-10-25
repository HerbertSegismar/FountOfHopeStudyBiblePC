// Views/Modals/SearchScopeDialog.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class SearchScopeDialog : Window
    {
        private readonly string _currentScope;
        private readonly Action<string> _onScopeSelected;
        private readonly ModalOverlayController _overlay;
        private readonly Color _primary;
        private readonly Color _text;
        private readonly Color _muted;
        private readonly Color _card;
        private readonly Color _border;

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

            _overlay = new ModalOverlayController(this)
            {
                CloseOnOwnerResize = true,
                RevealOnLoad = true,
            };

            BuildCategories();
        }

        // ── Thin XAML stubs → ModalOverlayController ─────────────
        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Window_KeyDown(object sender, KeyEventArgs e)
            => _overlay.HandleKeyDown(sender, e);

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
        private void Close_Click(object sender, RoutedEventArgs e)
            => _overlay.DismissWithFade();
    }
}