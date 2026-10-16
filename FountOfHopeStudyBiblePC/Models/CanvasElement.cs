using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Models
{
    public record CanvasElement
    {
        public string Id { get; init; } = Guid.NewGuid().ToString();
        public string Content { get; init; } = "";
        public Point Offset { get; init; }
        public float Width { get; init; } = 200f;
        public float Height { get; init; } = 200f;
        public float Rotation { get; init; }
        public Color BackgroundColor { get; init; } = Colors.White;
        public Point Position { get; init; }
        public bool IsVisible { get; init; } = true;
        public bool IsLocked { get; init; }
        public string? CustomName { get; init; }
        public string? GroupId { get; init; }
        public float ScaleX { get; init; } = 1f;
        public float ScaleY { get; init; } = 1f;
        public Color? TextColor { get; init; }
        public Color? ShadowColor { get; init; }
        public float ShadowOffsetX { get; init; } = 2f;
        public float ShadowOffsetY { get; init; } = 2f;
        public float BorderThickness { get; init; } = 1f;
        public Color? BorderColor { get; init; }
        public List<CanvasKeyframe> Keyframes { get; init; } = new();
        public float Alpha { get; init; } = 1f;
        public long StartTimeMs { get; init; }
        public long EndTimeMs { get; init; } = long.MaxValue;
        public float PivotX { get; init; } = 0.5f;
        public float PivotY { get; init; } = 0.5f;
        public string? FontFamily { get; init; }
        public string? TextAlign { get; init; }
        public float FontSize { get; init; } = 60f;
        public bool TextWrapMode { get; init; }
    }
}