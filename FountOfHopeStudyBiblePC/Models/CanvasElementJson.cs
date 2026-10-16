using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Models
{
    public static class CanvasElementJson
    {
        // ── CanvasElement ───────────────────────────────────────────
        public static JsonObject ToJson(this CanvasElement e) => new()
        {
            ["id"] = e.Id,
            ["content"] = e.Content,
            ["offsetX"] = e.Offset.X,
            ["offsetY"] = e.Offset.Y,
            ["width"] = e.Width,
            ["height"] = e.Height,
            ["rotation"] = e.Rotation,
            ["backgroundColor"] = ColorToArgb(e.BackgroundColor),
            ["positionX"] = e.Position.X,
            ["positionY"] = e.Position.Y,
            ["isVisible"] = e.IsVisible,
            ["isLocked"] = e.IsLocked,
            ["customName"] = e.CustomName,
            ["groupId"] = e.GroupId,
            ["scaleX"] = e.ScaleX,
            ["scaleY"] = e.ScaleY,
            ["textColor"] = e.TextColor.HasValue ? ColorToArgb(e.TextColor.Value) : null,
            ["shadowColor"] = e.ShadowColor.HasValue ? ColorToArgb(e.ShadowColor.Value) : null,
            ["shadowOffsetX"] = e.ShadowOffsetX,
            ["shadowOffsetY"] = e.ShadowOffsetY,
            ["borderThickness"] = e.BorderThickness,
            ["borderColor"] = e.BorderColor.HasValue ? ColorToArgb(e.BorderColor.Value) : null,
            ["keyframes"] = new JsonArray(e.Keyframes.Select(k => k.ToJson()).ToArray()),
            ["alpha"] = e.Alpha,
            ["startTimeMs"] = e.StartTimeMs,
            ["endTimeMs"] = e.EndTimeMs,
            ["pivotX"] = e.PivotX,
            ["pivotY"] = e.PivotY,
            ["fontFamily"] = e.FontFamily,
            ["textAlign"] = e.TextAlign,
            ["fontSize"] = e.FontSize,
            ["textWrapMode"] = e.TextWrapMode,
        };

        public static CanvasElement FromJson(JsonObject j) => new()
        {
            Id = j["id"]?.GetValue<string>() ?? Guid.NewGuid().ToString(),
            Content = j["content"]?.GetValue<string>() ?? "",
            Offset = new Point(
                                j["offsetX"]?.GetValue<double>() ?? 0,
                                j["offsetY"]?.GetValue<double>() ?? 0),
            Width = (float)(j["width"]?.GetValue<double>() ?? 200),
            Height = (float)(j["height"]?.GetValue<double>() ?? 200),
            Rotation = (float)(j["rotation"]?.GetValue<double>() ?? 0),
            BackgroundColor = j["backgroundColor"] is { } bg
                                ? ArgbToColor(bg.GetValue<int>()) : Colors.White,
            Position = new Point(
                                j["positionX"]?.GetValue<double>() ?? 0,
                                j["positionY"]?.GetValue<double>() ?? 0),
            IsVisible = j["isVisible"]?.GetValue<bool>() ?? true,
            IsLocked = j["isLocked"]?.GetValue<bool>() ?? false,
            CustomName = j["customName"]?.GetValue<string>(),
            GroupId = j["groupId"]?.GetValue<string>(),
            ScaleX = (float)(j["scaleX"]?.GetValue<double>() ?? 1),
            ScaleY = (float)(j["scaleY"]?.GetValue<double>() ?? 1),
            TextColor = j["textColor"] is { } tc ? ArgbToColor(tc.GetValue<int>()) : null,
            ShadowColor = j["shadowColor"] is { } sc ? ArgbToColor(sc.GetValue<int>()) : null,
            ShadowOffsetX = (float)(j["shadowOffsetX"]?.GetValue<double>() ?? 2),
            ShadowOffsetY = (float)(j["shadowOffsetY"]?.GetValue<double>() ?? 2),
            BorderThickness = (float)(j["borderThickness"]?.GetValue<double>() ?? 1),
            BorderColor = j["borderColor"] is { } bc ? ArgbToColor(bc.GetValue<int>()) : null,
            Keyframes = (j["keyframes"] as JsonArray)?
                                .Select(n => CanvasKeyframeJson.FromJson((JsonObject)n!))
                                .ToList() ?? new List<CanvasKeyframe>(),
            Alpha = (float)(j["alpha"]?.GetValue<double>() ?? 1),
            StartTimeMs = j["startTimeMs"]?.GetValue<long>() ?? 0,
            EndTimeMs = j["endTimeMs"]?.GetValue<long>() ?? long.MaxValue,
            PivotX = (float)(j["pivotX"]?.GetValue<double>() ?? 0.5),
            PivotY = (float)(j["pivotY"]?.GetValue<double>() ?? 0.5),
            FontFamily = j["fontFamily"]?.GetValue<string>(),
            TextAlign = j["textAlign"]?.GetValue<string>(),
            FontSize = (float)(j["fontSize"]?.GetValue<double>() ?? 60),
            TextWrapMode = j["textWrapMode"]?.GetValue<bool>() ?? false,
        };

        // ── helpers ─────────────────────────────────────────────────
        public static int ColorToArgb(Color c) =>
            (c.A << 24) | (c.R << 16) | (c.G << 8) | c.B;

        public static Color ArgbToColor(int argb) => Color.FromArgb(
            (byte)((argb >> 24) & 0xFF),
            (byte)((argb >> 16) & 0xFF),
            (byte)((argb >> 8) & 0xFF),
            (byte)(argb & 0xFF));
    }

    public static class CanvasKeyframeJson
    {
        public static JsonObject ToJson(this CanvasKeyframe k) => new()
        {
            ["timestampMs"] = k.TimestampMs,
            ["x"] = k.X,
            ["y"] = k.Y,
            ["scaleX"] = k.ScaleX,
            ["scaleY"] = k.ScaleY,
            ["rotation"] = k.Rotation,
            ["color"] = k.Color.HasValue ? CanvasElementJson.ColorToArgb(k.Color.Value) : null,
            ["gradientConfig"] = k.GradientConfig?.ToJson(),
            ["tweenType"] = k.TweenType.ToString(),
            ["customPoints"] = k.CustomPoints is null
                                        ? null
                                        : new JsonArray(k.CustomPoints.Select(p => p.ToJson()).ToArray()),
            ["pivotX"] = k.PivotX,
            ["pivotY"] = k.PivotY,
            ["ellipticalRotation"] = k.EllipticalRotation,
            ["ellipticalStretchX"] = k.EllipticalStretchX,
            ["ellipticalStretchY"] = k.EllipticalStretchY,
            ["shadowColor"] = k.ShadowColor.HasValue ? CanvasElementJson.ColorToArgb(k.ShadowColor.Value) : null,
            ["shadowOffsetX"] = k.ShadowOffsetX,
            ["shadowOffsetY"] = k.ShadowOffsetY,
            ["borderThickness"] = k.BorderThickness,
            ["borderColor"] = k.BorderColor.HasValue ? CanvasElementJson.ColorToArgb(k.BorderColor.Value) : null,
            ["fontFamily"] = k.FontFamily,
            ["textAlign"] = k.TextAlign,
        };

        public static CanvasKeyframe FromJson(JsonObject j)
        {
            var k = new CanvasKeyframe
            {
                TimestampMs = j["timestampMs"]?.GetValue<long>() ?? 0,
                X = (float?)j["x"]?.GetValue<double>(),
                Y = (float?)j["y"]?.GetValue<double>(),
                ScaleX = (float?)j["scaleX"]?.GetValue<double>(),
                ScaleY = (float?)j["scaleY"]?.GetValue<double>(),
                Rotation = (float?)j["rotation"]?.GetValue<double>(),
                Color = j["color"] is { } c ? CanvasElementJson.ArgbToColor(c.GetValue<int>()) : null,
                GradientConfig = j["gradientConfig"] is JsonObject g
                                    ? GradientConfigJson.FromJson(g) : null,
                TweenType = Enum.TryParse<TweenType>(j["tweenType"]?.GetValue<string>(), out var tt)
                                ? tt : TweenType.Linear,
                CustomPoints = (j["customPoints"] as JsonArray)?
                                .Select(n => EasingPointJson.FromJson((JsonObject)n!)).ToList(),
                PivotX = (float?)j["pivotX"]?.GetValue<double>(),
                PivotY = (float?)j["pivotY"]?.GetValue<double>(),
                EllipticalRotation = j["ellipticalRotation"]?.GetValue<bool>() ?? false,
                EllipticalStretchX = (float)(j["ellipticalStretchX"]?.GetValue<double>() ?? 1.0),
                EllipticalStretchY = (float)(j["ellipticalStretchY"]?.GetValue<double>() ?? 0.5),
                ShadowColor = j["shadowColor"] is { } sc ? CanvasElementJson.ArgbToColor(sc.GetValue<int>()) : null,
                ShadowOffsetX = (float?)j["shadowOffsetX"]?.GetValue<double>(),
                ShadowOffsetY = (float?)j["shadowOffsetY"]?.GetValue<double>(),
                BorderThickness = (float?)j["borderThickness"]?.GetValue<double>(),
                BorderColor = j["borderColor"] is { } bc ? CanvasElementJson.ArgbToColor(bc.GetValue<int>()) : null,
                FontFamily = j["fontFamily"]?.GetValue<string>(),
                TextAlign = j["textAlign"]?.GetValue<string>(),
            };
            return k;
        }
    }

    public static class GradientConfigJson
    {
        public static JsonObject ToJson(this GradientConfig g) => new()
        {
            ["startColor"] = CanvasElementJson.ColorToArgb(g.StartColor),
            ["endColor"] = CanvasElementJson.ColorToArgb(g.EndColor),
            ["startOffsetX"] = g.StartOffset.X,
            ["startOffsetY"] = g.StartOffset.Y,
            ["endOffsetX"] = g.EndOffset.X,
            ["endOffsetY"] = g.EndOffset.Y,
        };

        public static GradientConfig FromJson(JsonObject j) => new()
        {
            StartColor = CanvasElementJson.ArgbToColor(j["startColor"]?.GetValue<int>() ?? 0),
            EndColor = CanvasElementJson.ArgbToColor(j["endColor"]?.GetValue<int>() ?? 0),
            StartOffset = new Point(j["startOffsetX"]?.GetValue<double>() ?? 0,
                                    j["startOffsetY"]?.GetValue<double>() ?? 0),
            EndOffset = new Point(j["endOffsetX"]?.GetValue<double>() ?? 0,
                                    j["endOffsetY"]?.GetValue<double>() ?? 0),
        };
    }

    public static class EasingPointJson
    {
        public static JsonObject ToJson(this EasingPoint p) => new()
        {
            ["x"] = p.X,
            ["y"] = p.Y,
            ["isSmooth"] = p.IsSmooth,
            ["handleOutX"] = p.HandleOut.X,
            ["handleOutY"] = p.HandleOut.Y,
            ["handleInX"] = p.HandleIn.X,
            ["handleInY"] = p.HandleIn.Y,
        };

        public static EasingPoint FromJson(JsonObject j) => new()
        {
            X = (float)(j["x"]?.GetValue<double>() ?? 0),
            Y = (float)(j["y"]?.GetValue<double>() ?? 0),
            IsSmooth = j["isSmooth"]?.GetValue<bool>() ?? false,
            HandleOut = new Point(j["handleOutX"]?.GetValue<double>() ?? 0,
                                  j["handleOutY"]?.GetValue<double>() ?? 0),
            HandleIn = new Point(j["handleInX"]?.GetValue<double>() ?? 0,
                                  j["handleInY"]?.GetValue<double>() ?? 0),
        };
    }
}