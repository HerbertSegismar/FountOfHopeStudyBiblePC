// Controls/FountRain.cs — lightweight "digital rain" replacement for the Compose FountRain
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace FountOfHopeStudyBiblePC.Controls
{
    public class FountRain : Canvas
    {
        private sealed class Drop
        {
            public double X;
            public double Y;
            public double Speed;
            public double Length;
            public Rectangle Shape = null!;
        }

        private readonly List<Drop> _drops = new();
        private readonly Random _rng = new();
        private readonly DispatcherTimer _timer;
        private bool _built;

        public FountRain()
        {
            IsHitTestVisible = false;
            ClipToBounds = true;
            Background = Brushes.Transparent;
            Height = 160;

            _timer = new DispatcherTimer(DispatcherPriority.Render)
            {
                Interval = TimeSpan.FromMilliseconds(33)
            };
            _timer.Tick += (_, __) => Step();

            Loaded += (_, __) =>
            {
                Build();
                _timer.Start();
            };
            Unloaded += (_, __) => _timer.Stop();
            SizeChanged += (_, __) => Build();
        }

        private void Build()
        {
            _built = true;
            _drops.Clear();
            Children.Clear();

            var w = ActualWidth;
            var h = ActualHeight;
            if (w <= 0 || h <= 0) return;

            var count = Math.Max(18, (int)(w / 18));

            for (int i = 0; i < count; i++)
            {
                var drop = new Drop
                {
                    X = _rng.NextDouble() * w,
                    Y = _rng.NextDouble() * h - h,
                    Speed = 1.5 + _rng.NextDouble() * 4.5,
                    Length = 6 + _rng.NextDouble() * 16
                };

                drop.Shape = new Rectangle
                {
                    Width = 2,
                    Height = drop.Length,
                    RadiusX = 1,
                    RadiusY = 1,
                    Fill = new SolidColorBrush(Color.FromArgb(
                        (byte)(90 + _rng.Next(120)), 0x7F, 0xB2, 0xFF)),
                    Opacity = 0.35 + _rng.NextDouble() * 0.55
                };

                SetLeft(drop.Shape, drop.X);
                SetTop(drop.Shape, drop.Y);
                Children.Add(drop.Shape);
                _drops.Add(drop);
            }
        }

        private void Step()
        {
            if (!_built) return;

            var h = ActualHeight;
            var w = ActualWidth;

            foreach (var drop in _drops)
            {
                drop.Y += drop.Speed;

                if (drop.Y > h)
                {
                    drop.Y = -drop.Length;
                    drop.X = _rng.NextDouble() * w;
                    drop.Speed = 1.5 + _rng.NextDouble() * 4.5;
                }

                SetLeft(drop.Shape, drop.X);
                SetTop(drop.Shape, drop.Y);
            }
        }
    }
}