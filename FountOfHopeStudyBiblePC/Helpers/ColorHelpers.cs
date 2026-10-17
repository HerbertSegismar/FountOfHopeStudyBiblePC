// Helpers/ColorHelpers.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class ColorHelpers
    {
        // ═══════════════════════════════════════════════════════════
        //  HSV
        // ═══════════════════════════════════════════════════════════
        public static (double Hue, double Sat, double Val) ToHsv(Color c)
        {
            double r = c.R / 255.0, g = c.G / 255.0, b = c.B / 255.0;
            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            double h = 0;
            if (delta > 0)
            {
                if (max == r) h = 60 * (((g - b) / delta) % 6);
                else if (max == g) h = 60 * (((b - r) / delta) + 2);
                else h = 60 * (((r - g) / delta) + 4);
            }
            if (h < 0) h += 360;

            double s = max == 0 ? 0 : delta / max;
            double v = max;

            return (h, s, v);
        }

        public static Color FromHsv(double hue, double sat, double val, byte alpha = 255)
        {
            hue = ((hue % 360) + 360) % 360;
            sat = Math.Clamp(sat, 0, 1);
            val = Math.Clamp(val, 0, 1);

            double c = val * sat;
            double x = c * (1 - Math.Abs((hue / 60.0) % 2 - 1));
            double m = val - c;

            double r = 0, g = 0, b = 0;
            if (hue < 60) { r = c; g = x; }
            else if (hue < 120) { r = x; g = c; }
            else if (hue < 180) { g = c; b = x; }
            else if (hue < 240) { g = x; b = c; }
            else if (hue < 300) { r = x; b = c; }
            else { r = c; b = x; }

            return Color.FromArgb(alpha,
                (byte)Math.Round((r + m) * 255),
                (byte)Math.Round((g + m) * 255),
                (byte)Math.Round((b + m) * 255));
        }

        public static float GetBrightness(Color c) => (float)ToHsv(c).Val;
        public static float GetSaturation(Color c) => (float)ToHsv(c).Sat;

        public static Color AdjustBrightness(Color c, float brightness)
        {
            var (h, s, _) = ToHsv(c);
            return FromHsv(h, s, brightness, c.A);
        }

        public static Color AdjustSaturation(Color c, float saturation)
        {
            var (h, _, v) = ToHsv(c);
            return FromHsv(h, saturation, v, c.A);
        }

        public static Color AdjustOpacity(Color c, float opacity)
        {
            return Color.FromArgb((byte)Math.Round(Math.Clamp(opacity, 0, 1) * 255),
                                  c.R, c.G, c.B);
        }

        // ═══════════════════════════════════════════════════════════
        //  Hex
        // ═══════════════════════════════════════════════════════════
        public static string ToHexString(Color c)
        {
            return c.A == 255
                ? $"#{c.R:X2}{c.G:X2}{c.B:X2}"
                : $"#{c.A:X2}{c.R:X2}{c.G:X2}{c.B:X2}";
        }

        public static bool TryParseHex(string input, out Color color)
        {
            color = Colors.Transparent;
            if (string.IsNullOrEmpty(input) || !input.StartsWith("#")) return false;
            if (input.Length != 4 && input.Length != 7 && input.Length != 9) return false;

            for (int i = 1; i < input.Length; i++)
            {
                var ch = char.ToUpperInvariant(input[i]);
                if (!((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'F'))) return false;
            }

            try
            {
                if (input.Length == 4)
                {
                    var h = input.Substring(1);
                    color = Color.FromRgb(
                        (byte)Convert.ToInt32($"{h[0]}{h[0]}", 16),
                        (byte)Convert.ToInt32($"{h[1]}{h[1]}", 16),
                        (byte)Convert.ToInt32($"{h[2]}{h[2]}", 16));
                }
                else if (input.Length == 7)
                {
                    color = Color.FromRgb(
                        (byte)Convert.ToInt32(input.Substring(1, 2), 16),
                        (byte)Convert.ToInt32(input.Substring(3, 2), 16),
                        (byte)Convert.ToInt32(input.Substring(5, 2), 16));
                }
                else // length 9
                {
                    color = Color.FromArgb(
                        (byte)Convert.ToInt32(input.Substring(1, 2), 16),
                        (byte)Convert.ToInt32(input.Substring(3, 2), 16),
                        (byte)Convert.ToInt32(input.Substring(5, 2), 16),
                        (byte)Convert.ToInt32(input.Substring(7, 2), 16));
                }
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Filters a raw user-typed string into a valid hex prefix.
        /// Ensures leading '#' and only [0-9A-F] chars, max 9 chars (#AARRGGBB).
        /// </summary>
        public static string SanitizeHexInput(string input)
        {
            if (string.IsNullOrEmpty(input)) return "#";

            var sb = new System.Text.StringBuilder("#");
            var upper = input.ToUpperInvariant();
            foreach (var ch in upper)
            {
                if (ch == '#') continue;
                if ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'F'))
                    sb.Append(ch);
            }

            if (sb.Length > 9) sb.Length = 9;
            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════
        //  Wheel interaction — convert pointer offset to color
        // ═══════════════════════════════════════════════════════════
        public static Color UpdateFromOffset(Point offset, Size size, float brightness)
        {
            double radius = Math.Min(size.Width, size.Height) / 2.0;
            double cx = size.Width / 2.0;
            double cy = size.Height / 2.0;

            double dx = offset.X - cx;
            double dy = offset.Y - cy;

            double hue = (Math.Atan2(dy, dx) * 180.0 / Math.PI + 360.0) % 360.0;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            double sat = Math.Clamp(dist / radius, 0, 1);

            return FromHsv(hue, sat, brightness);
        }
    }
}