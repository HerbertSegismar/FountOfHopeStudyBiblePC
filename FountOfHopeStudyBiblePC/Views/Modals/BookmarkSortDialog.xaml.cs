using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class BookmarkSortDialog : Window
    {
        private static readonly SolidColorBrush OptionForegroundBrush =
            Frozen(Color.FromRgb(0xE6, 0xEA, 0xF0));

        private static SolidColorBrush Frozen(Color c)
        {
            var b = new SolidColorBrush(c);
            b.Freeze();
            return b;
        }

        public BookmarkSortOrder SelectedOrder { get; private set; }

        private readonly ModalOverlayController _overlay;

        public BookmarkSortDialog(BookmarkSortOrder current)
        {
            InitializeComponent();

            _overlay = new ModalOverlayController(this)
            {
                CloseOnOwnerResize = true,
                RevealOnLoad = true,
            };

            SelectedOrder = current;

            foreach (var order in Enum.GetValues<BookmarkSortOrder>())
            {
                var radio = new RadioButton
                {
                    Content = order.DisplayName(),
                    IsChecked = order == current,
                    Foreground = OptionForegroundBrush,
                    Margin = new Thickness(0, 6, 0, 6),
                };
                radio.Checked += (_, _) => SelectedOrder = order;
                OptionsPanel.Children.Add(radio);
            }
        }

        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Window_KeyDown(object sender, KeyEventArgs e)
            => _overlay.HandleKeyDown(sender, e);

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}