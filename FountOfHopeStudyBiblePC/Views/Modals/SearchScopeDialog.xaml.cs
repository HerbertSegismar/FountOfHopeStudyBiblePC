using System;
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
        private static readonly Action<string> NoOpString = _ => { };

        private readonly string _currentScope;
        private readonly Action<string> _onScopeSelected;
        private readonly ModalOverlayController _overlay;

        private readonly SolidColorBrush _primaryBrush;
        private readonly SolidColorBrush _categoryHeaderBrush;
        private readonly SolidColorBrush _selectedBgBrush;
        private readonly SolidColorBrush _textBrush;
        private readonly SolidColorBrush _mutedBrush;
        private readonly SolidColorBrush _cardBrush;
        private readonly SolidColorBrush _borderBrush;

        public SearchScopeDialog(
            string currentScope,
            Action<string> onScopeSelected,
            SearchColors colors)
        {
            _currentScope = currentScope;
            _onScopeSelected = onScopeSelected ?? NoOpString;

            _primaryBrush = Frozen(colors.Primary);
            _categoryHeaderBrush = Frozen(Color.FromArgb(0xCC, colors.Primary.R, colors.Primary.G, colors.Primary.B));
            _selectedBgBrush = Frozen(Color.FromArgb(0x1A, colors.Primary.R, colors.Primary.G, colors.Primary.B));
            _textBrush = Frozen(colors.Text);
            _mutedBrush = Frozen(colors.Muted);
            _cardBrush = Frozen(colors.Card);
            _borderBrush = Frozen(colors.Border);

            InitializeComponent();

            _overlay = new ModalOverlayController(this)
            {
                CloseOnOwnerResize = true,
                RevealOnLoad = true,
            };

            BuildCategories();
        }

        private static SolidColorBrush Frozen(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Window_KeyDown(object sender, KeyEventArgs e)
            => _overlay.HandleKeyDown(sender, e);

        private void BuildCategories()
        {
            CategoryList.Children.Clear();

            foreach (var kv in SearchScopes.ScopeCategories)
            {
                CategoryList.Children.Add(new Border
                {
                    Background = _categoryHeaderBrush,
                    Padding = new Thickness(12, 8, 12, 8),
                    Child = new TextBlock
                    {
                        Text = kv.Key,
                        FontSize = 13,
                        FontWeight = FontWeights.SemiBold,
                        Foreground = Brushes.White,
                    }
                });

                foreach (var scopeKey in kv.Value)
                {
                    var config = SearchScopes.GetScopeConfig(scopeKey);
                    var isSelected = scopeKey == _currentScope;

                    var stack = new StackPanel();
                    stack.Children.Add(new TextBlock
                    {
                        Text = config.Label,
                        FontSize = 14,
                        FontWeight = FontWeights.Medium,
                        Foreground = isSelected ? _primaryBrush : _textBrush,
                    });
                    stack.Children.Add(new TextBlock
                    {
                        Text = config.Description,
                        FontSize = 11,
                        Foreground = _mutedBrush,
                        Margin = new Thickness(0, 2, 0, 0),
                        TextWrapping = TextWrapping.Wrap,
                    });

                    var item = new Border
                    {
                        Background = isSelected ? _selectedBgBrush : _cardBrush,
                        Padding = new Thickness(14, 10, 14, 10),
                        Cursor = Cursors.Hand,
                        Child = stack,
                    };

                    var captured = scopeKey;
                    item.MouseLeftButtonUp += (_, _) =>
                    {
                        _onScopeSelected(captured);
                        Close();
                    };

                    CategoryList.Children.Add(item);

                    CategoryList.Children.Add(new Border
                    {
                        Height = 1,
                        Background = _borderBrush,
                    });
                }
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
            => _overlay.DismissWithFade();
    }
}