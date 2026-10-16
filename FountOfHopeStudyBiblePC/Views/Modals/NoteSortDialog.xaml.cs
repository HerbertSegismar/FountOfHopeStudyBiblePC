using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class NoteSortDialog : Window
    {
        public NoteSortOrder SelectedOrder { get; private set; }

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
        }

        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}