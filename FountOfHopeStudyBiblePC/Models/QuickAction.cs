// Models/QuickAction.cs
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Models
{
    public class QuickAction : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void Raise([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public string Title { get; set; } = "";
        public string Icon { get; set; } = "";
        public Color Color { get; set; }
        public Action? OnClick { get; set; }

        public string? BackgroundImage
        {
            get => _backgroundImage;
            set { _backgroundImage = value; Raise(); }
        }
        private string? _backgroundImage;

        public ImageSource? BackgroundImageSource
        {
            get => _backgroundImageSource;
            set { _backgroundImageSource = value; Raise(); }
        }
        private ImageSource? _backgroundImageSource;
    }
}