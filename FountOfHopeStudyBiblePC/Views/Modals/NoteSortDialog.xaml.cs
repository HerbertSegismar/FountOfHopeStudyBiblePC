// Views/Modals/NoteSortDialog.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class NoteSortDialog : Window
    {
        public NoteSortOrder SelectedOrder { get; private set; }

        private readonly ModalOverlayController _overlay;

        public NoteSortDialog(NoteSortOrder current)
        {
            InitializeComponent();

            _overlay = new ModalOverlayController(this)
            {
                CloseOnOwnerResize = true,
                RevealOnLoad = true,
            };

            SelectedOrder = current;

            foreach (NoteSortOrder order in Enum.GetValues(typeof(NoteSortOrder)))
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

        // ── Thin XAML stubs → ModalOverlayController ─────────────
        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Window_KeyDown(object sender, KeyEventArgs e)
            => _overlay.HandleKeyDown(sender, e);

        // ═══════════════════════════════════════════════════════════
        //  Confirm
        // ═══════════════════════════════════════════════════════════
        private void DoneButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;   // requires ShowDialog()
            Close();
        }
    }
}