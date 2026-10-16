using System.Collections.Generic;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Models
{
    public record CanvasKeyframe
    {
        public long TimestampMs { get; init; }
        public float? X { get; init; }
        public float? Y { get; init; }
        public float? ScaleX { get; init; }
        public float? ScaleY { get; init; }
        public float? Rotation { get; init; }
        public Color? Color { get; init; }
        public GradientConfig? GradientConfig { get; init; }
        public TweenType TweenType { get; init; } = TweenType.Linear;
        public List<EasingPoint>? CustomPoints { get; init; }
        public float? PivotX { get; init; }
        public float? PivotY { get; init; }
        public bool EllipticalRotation { get; init; }
        public float EllipticalStretchX { get; init; } = 1f;
        public float EllipticalStretchY { get; init; } = 0.5f;
        public Color? ShadowColor { get; init; }
        public float? ShadowOffsetX { get; init; }
        public float? ShadowOffsetY { get; init; }
        public float? BorderThickness { get; init; }
        public Color? BorderColor { get; init; }
        public string? FontFamily { get; init; }
        public string? TextAlign { get; init; }
    }
}