using System;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Models
{
    public class QuickAction
    {
        public string Title { get; set; } = "";
        public string Icon { get; set; } = "";   // unicode glyph — Compose used ImageVector
        public Color Color { get; set; }
        public Action? OnClick { get; set; }
        public string? BackgroundImage { get; set; }         // asset path or null
    }
}