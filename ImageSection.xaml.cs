// Controls/ImageSection.xaml.cs
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using FountOfHopeStudyBiblePC.Services;

namespace FountOfHopeStudyBiblePC.Controls
{
    public partial class ImageSection : UserControl
    {
        public event Action? ContinueReadingRequested;

        public DatabaseHelper? DatabaseHelper { get; set; }

        public ImageSection()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            var path = Helpers.ImageAssets.PickHeroImage();
            if (string.IsNullOrEmpty(path)) return;

            try
            {
                HeroImage.Source = new BitmapImage(new Uri(path, UriKind.RelativeOrAbsolute));
            }
            catch
            {
                // Asset missing — leave the placeholder background.
            }
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e) =>
            ContinueReadingRequested?.Invoke();
    }
}