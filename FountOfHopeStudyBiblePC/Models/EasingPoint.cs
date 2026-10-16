using System.Windows;

namespace FountOfHopeStudyBiblePC.Models
{
    public record EasingPoint
    {
        public float X { get; init; }
        public float Y { get; init; }
        public bool IsSmooth { get; init; }
        public Point HandleOut { get; init; }
        public Point HandleIn { get; init; }
    }
}