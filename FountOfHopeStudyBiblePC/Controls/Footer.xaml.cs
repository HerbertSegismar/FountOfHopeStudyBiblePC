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
                Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri)
                {
                    UseShellExecute = true
                });
            }
            catch (Exception)
            {
            }
            e.Handled = true;
        }
    }
}