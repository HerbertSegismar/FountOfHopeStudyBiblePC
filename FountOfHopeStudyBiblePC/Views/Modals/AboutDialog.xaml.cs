// Views/Modals/AboutDialog.xaml.cs
using System.Windows;
using System.Windows.Input;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class AboutDialog : Window
    {
        private readonly ModalOverlayController _overlay;

        public AboutDialog()
        {
            InitializeComponent();

            _overlay = new ModalOverlayController(this)
            {
                CloseOnOwnerResize = false,
            };
        }

        // ── Thin XAML stubs → ModalOverlayController ─────────────
        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Card_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleCardMouseDown(sender, e);

        private void Card_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleCardMouseUp(sender, e);

        private void Window_KeyDown(object sender, KeyEventArgs e)
            => _overlay.HandleKeyDown(sender, e);

        private void Close_Click(object sender, RoutedEventArgs e)
            => _overlay.DismissWithFade();
    }
}