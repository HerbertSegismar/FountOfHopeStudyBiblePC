// Views/Modals/AboutDialog.xaml.cs
using System.Windows;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class AboutDialog : Window
    {
        public AboutDialog() { InitializeComponent(); }
        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}