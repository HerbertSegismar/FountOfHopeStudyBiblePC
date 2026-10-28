using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using FountOfHopeStudyBiblePC.Helpers;

namespace FountOfHopeStudyBiblePC.Controls
{
    public class ColorWheelControl : FrameworkElement
    {
        public static readonly DependencyProperty SelectedColorProperty =
            DependencyProperty.Register(
                nameof(SelectedColor),
                typeof(Color),
                typeof(ColorWheelControl),
                new FrameworkPropertyMetadata(
                    Color.FromRgb(0xFF, 0x44, 0x44),
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public static readonly DependencyProperty BrightnessProperty =
            DependencyProperty.Register(
                nameof(Brightness),
                typeof(float),
                typeof(ColorWheelControl),
                new FrameworkPropertyMetadata(
                    0.8f,
                    FrameworkPropertyMetadataOptions.AffectsRender));

        public Color SelectedColor
        {
            get => (Color)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        public float Brightness
        {
            get => (float)GetValue(BrightnessProperty);
            set => SetValue(BrightnessProperty, value);
        }

        public event EventHandler<Color>? ColorSelected;

        private bool _isDragging;

        public ColorWheelControl()
        {
            Focusable = true;
            Cursor = Cursors.Hand;
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            double w = ActualWidth;
            double h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            double radius = Math.Min(w, h) / 2.0 - 4;
            Point center = new(w / 2.0, h / 2.0);
            double brightness = Brightness;

            for (int i = 0; i < 90; i++)
            {
                double startAngle = i * 4.0;
                double sweepAngle = 4.5;
                double hue = startAngle + sweepAngle / 2.0;
                var color = ColorHelpers.FromHsv(hue, 1, brightness);
                var brush = new SolidColorBrush(color);
                brush.Freeze();

                DrawArc(dc, center, radius, startAngle, sweepAngle, brush);
            }

            var overlayBrush = new RadialGradientBrush(
                Color.FromArgb(0xB3, 0xFF, 0xFF, 0xFF),
                Color.FromArgb(0x00, 0xFF, 0xFF, 0xFF))
            {
                GradientOrigin = new Point(0.5, 0.5),
                Center = new Point(0.5, 0.5),
                RadiusX = 0.5,
                RadiusY = 0.5,
            };
            overlayBrush.Freeze();
            dc.DrawEllipse(overlayBrush, null, center, radius, radius);

            var (hueNow, satNow, _) = ColorHelpers.ToHsv(SelectedColor);
            double rad = hueNow * Math.PI / 180.0;
            double indicatorRadius = satNow * radius;
            Point indicator = new(
                center.X + indicatorRadius * Math.Cos(rad),
                center.Y + indicatorRadius * Math.Sin(rad));

            var linePen = new Pen(
                new SolidColorBrush(Color.FromArgb(0x66, 0xFF, 0xFF, 0xFF)), 1);
            linePen.Freeze();
            dc.DrawLine(linePen, center, indicator);

            var ringPen = new Pen(Brushes.White, 2);
            ringPen.Freeze();
            dc.DrawEllipse(null, ringPen, indicator, 11, 11);

            var fillBrush = new SolidColorBrush(SelectedColor);
            fillBrush.Freeze();
            dc.DrawEllipse(fillBrush, null, indicator, 8, 8);

            var centerDot = new SolidColorBrush(Color.FromArgb(0x88, 0xFF, 0xFF, 0xFF));
            centerDot.Freeze();
            dc.DrawEllipse(centerDot, null, center, 2, 2);
        }

        private static void DrawArc(
            DrawingContext dc, Point center, double radius,
            double startAngleDeg, double sweepAngleDeg, Brush brush)
        {
            double startRad = startAngleDeg * Math.PI / 180.0;
            double endRad = (startAngleDeg + sweepAngleDeg) * Math.PI / 180.0;

            var startPt = new Point(
                center.X + radius * Math.Cos(startRad),
                center.Y + radius * Math.Sin(startRad));
            var endPt = new Point(
                center.X + radius * Math.Cos(endRad),
                center.Y + radius * Math.Sin(endRad));

            var geometry = new StreamGeometry();
            using (var ctx = geometry.Open())
            {
                ctx.BeginFigure(center, isFilled: true, isClosed: true);
                ctx.LineTo(startPt, isStroked: false, isSmoothJoin: false);
                ctx.ArcTo(
                    endPt,
                    new Size(radius, radius),
                    rotationAngle: 0,
                    isLargeArc: false,
                    sweepDirection: SweepDirection.Clockwise,
                    isStroked: false,
                    isSmoothJoin: false);
            }
            geometry.Freeze();
            dc.DrawGeometry(brush, null, geometry);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.ChangedButton != MouseButton.Left) return;

            _isDragging = true;
            CaptureMouse();
            UpdateFromPoint(e.GetPosition(this));
            e.Handled = true;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (!_isDragging) return;

            UpdateFromPoint(e.GetPosition(this));
            e.Handled = true;
        }

        protected override void OnMouseUp(MouseButtonEventArgs e)
        {
            base.OnMouseUp(e);
            if (e.ChangedButton != MouseButton.Left) return;

            _isDragging = false;
            ReleaseMouseCapture();
            e.Handled = true;
        }

        private void UpdateFromPoint(Point pt)
        {
            double radius = Math.Min(ActualWidth, ActualHeight) / 2.0 - 4;
            Point center = new(ActualWidth / 2.0, ActualHeight / 2.0);

            double dx = pt.X - center.X;
            double dy = pt.Y - center.Y;
            double dist = Math.Sqrt(dx * dx + dy * dy);
            if (dist > radius)
            {
                double scale = radius / dist;
                pt = new Point(center.X + dx * scale, center.Y + dy * scale);
            }

            var newColor = ColorHelpers.UpdateFromOffset(
                pt,
                new Size(ActualWidth, ActualHeight),
                Brightness);

            newColor = Color.FromArgb(SelectedColor.A, newColor.R, newColor.G, newColor.B);

            SelectedColor = newColor;
            ColorSelected?.Invoke(this, newColor);
        }
    }
}