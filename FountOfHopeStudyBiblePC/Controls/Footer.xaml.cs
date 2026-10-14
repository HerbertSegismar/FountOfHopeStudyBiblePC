// Controls/Footer.xaml.cs
using System;
using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

namespace FountOfHopeStudyBiblePC.Controls
{
    public partial class Footer : UserControl
    {
        public Footer()
        {
            InitializeComponent();
            CopyrightText.Text = $"© Copyright {DateTime.Now.Year}";
        }

        private void OnRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                // Open the URI in the default browser or mail client
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                // Optionally log or show a message
                // MessageBox.Show($"Unable to open link: {ex.Message}");
            }
            e.Handled = true;
        }
    }
}