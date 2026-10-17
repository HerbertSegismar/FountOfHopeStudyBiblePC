using System.Collections.Generic;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Models
{
    public record ColorTheme(string Name, Color PrimaryColor, Color SecondaryColor);

    public static class PredefinedColorThemes
    {
        public static List<ColorTheme> All { get; } = new()
        {
            new("Deep Purple",  Color.FromRgb(0x67, 0x50, 0xA4), Color.FromRgb(0xB1, 0x7D, 0xF5)),
            new("Blue",         Color.FromRgb(0x2A, 0x6F, 0xD9), Color.FromRgb(0x7F, 0xB2, 0xFF)),
            new("Teal",         Color.FromRgb(0x14, 0xB8, 0xA6), Color.FromRgb(0x6D, 0xF3, 0xCE)),
            new("Green",        Color.FromRgb(0x10, 0xB9, 0x81), Color.FromRgb(0x45, 0xF3, 0x4A)),
            new("Lime",         Color.FromRgb(0x84, 0xCC, 0x16), Color.FromRgb(0xE3, 0xDA, 0x57)),
            new("Amber",        Color.FromRgb(0xF5, 0x9E, 0x0B), Color.FromRgb(0xF5, 0xAB, 0x7D)),
            new("Orange",       Color.FromRgb(0xF9, 0x73, 0x16), Color.FromRgb(0xE8, 0x80, 0x54)),
            new("Red",          Color.FromRgb(0xEF, 0x44, 0x44), Color.FromRgb(0xFA, 0x6E, 0x6E)),
            new("Pink",         Color.FromRgb(0xEC, 0x48, 0x99), Color.FromRgb(0xEB, 0x4E, 0x9E)),
            new("Magenta",      Color.FromRgb(0xD9, 0x46, 0xEF), Color.FromRgb(0xEB, 0x4E, 0x9E)),
            new("Indigo",       Color.FromRgb(0x63, 0x66, 0xF1), Color.FromRgb(0x7F, 0xB2, 0xFF)),
            new("Slate",        Color.FromRgb(0x64, 0x74, 0x8B), Color.FromRgb(0xA0, 0xAE, 0xC0)),
        };
    }
}