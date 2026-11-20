using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Views.Modals
{
    public partial class PromptForWordDialog : Window
    {
        private readonly ModalOverlayController _overlay;

        public string? Result { get; private set; }

        public PromptForWordDialog(AppViewModel viewModel, string initial)
        {
            InitializeComponent();

            _overlay = new ModalOverlayController(this) { RevealOnLoad = true };

            ApplyTheme(viewModel);

            WordTextBox.Text = initial;

            Loaded += (_, _) =>
            {
                WordTextBox.Focus();
                WordTextBox.SelectAll();
            };
        }

        private void ApplyTheme(AppViewModel vm)
        {
            var isDark = vm.DarkTheme;
            var bg = isDark ? vm.DarkModalBackgroundColor : vm.LightModalBackgroundColor;
            var fg = isDark ? Colors.White : Color.FromRgb(0x1A, 0x1A, 0x1A);
            var muted = isDark ? Color.FromRgb(0xAA, 0xB4, 0xC0) : Color.FromRgb(0x6B, 0x72, 0x80);
            var primary = vm.SelectedColor ?? ColorHelpers.DefaultPrimaryColor;

            RootBorder.Background = new SolidColorBrush(bg);

            TitleText.Foreground = new SolidColorBrush(fg);
            SubtitleText.Foreground = new SolidColorBrush(muted);

            WordBoxBorder.Background = new SolidColorBrush(
                isDark ? Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x11, 0x00, 0x00, 0x00));
            WordBoxBorder.BorderBrush = new SolidColorBrush(
                isDark ? Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x22, 0x00, 0x00, 0x00));
            WordTextBox.Foreground = new SolidColorBrush(fg);
            WordTextBox.CaretBrush = new SolidColorBrush(primary);

            CancelButton.Background = new SolidColorBrush(
                isDark ? Color.FromArgb(0x22, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x11, 0x00, 0x00, 0x00));
            CancelButton.BorderBrush = new SolidColorBrush(
                isDark ? Color.FromArgb(0x33, 0xFF, 0xFF, 0xFF) : Color.FromArgb(0x22, 0x00, 0x00, 0x00));
            CancelButton.BorderThickness = new Thickness(1);
            CancelButton.Foreground = new SolidColorBrush(fg);

            UpdateButton.Background = new SolidColorBrush(primary);
            UpdateButton.Foreground = Brushes.White;
            UpdateButton.FontWeight = FontWeights.SemiBold;
        }

        private void Scrim_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseDown(sender, e);

        private void Scrim_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
            => _overlay.HandleScrimMouseUp(sender, e);

        private void Update_Click(object sender, RoutedEventArgs e)
        {
            Result = string.IsNullOrWhiteSpace(WordTextBox.Text)
                ? null
                : WordTextBox.Text.Trim();
            _overlay.DismissWithFade();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = null;
            _overlay.DismissWithFade();
        }

        private void WordTextBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                Update_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
            else if (e.Key == Key.Escape)
            {
                Cancel_Click(this, new RoutedEventArgs());
                e.Handled = true;
            }
        }
    }
}