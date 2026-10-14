// Models/QuickAction.cs
using System;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Models
{
    public class QuickAction
    {
        public string Title { get; init; } = string.Empty;
        public string Icon { get; init; } = string.Empty;   // glyph or image path
        public Brush Color { get; init; } = Brushes.White;
        public string BackgroundImage { get; init; } = string.Empty;
        public Action? OnClick { get; init; }
    }
}
