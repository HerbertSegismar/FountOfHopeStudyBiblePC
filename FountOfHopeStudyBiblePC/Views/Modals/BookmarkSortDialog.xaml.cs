// Views/Modals/BookmarkSortDialog.xaml.cs
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class BookmarkSortDialog : Window
    {
        public BookmarkSortOrder SelectedOrder { get; private set; }

        public BookmarkSortDialog(BookmarkSortOrder current)
        {
            InitializeComponent();
            SelectedOrder = current;

            foreach (BookmarkSortOrder order in System.Enum.GetValues(typeof(BookmarkSortOrder)))
            {
                var radio = new RadioButton
                {
                    Content = order.DisplayName(),
                    IsChecked = order == current,
                    Foreground = new SolidColorBrush(Color.FromRgb(0xE6, 0xEA, 0xF0)),
                    Margin = new Thickness(0, 6, 0, 6),
                    Tag = order,
                };
                radio.Checked += (_, _) => SelectedOrder = (BookmarkSortOrder)radio.Tag;
                OptionsPanel.Children.Add(radio);
            }
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}