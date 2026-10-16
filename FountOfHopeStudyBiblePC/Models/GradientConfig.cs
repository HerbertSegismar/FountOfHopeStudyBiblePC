using System.Windows;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Models
{
    public record GradientConfig
    {
        public Color StartColor { get; init; }
        public Color EndColor { get; init; }
        public Point StartOffset { get; init; }
        public Point EndOffset { get; init; }
    }
}