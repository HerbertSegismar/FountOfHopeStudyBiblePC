using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class WordDragHelper
    {
        public const double ThresholdPixels = 3.0;

        public static TextBlock? FindWordAt(DependencyObject? source)
        {
            var current = source;
            while (current != null)
            {
                if (current is TextBlock tb)
                    return IsWordElement(tb) ? tb : null;
                current = VisualTreeHelper.GetParent(current);
            }
            return null;
        }

        public static bool IsWordElement(TextBlock tb)
        {
            if (tb.Tag is string tag && tag == "noword") return false;

            var text = tb.Text;
            if (string.IsNullOrWhiteSpace(text)) return false;

            int start = 0, end = text.Length;
            while (start < end && char.IsWhiteSpace(text[start])) start++;
            while (end > start && char.IsWhiteSpace(text[end - 1])) end--;
            if (start == end) return false;

            var hasLetter = false;
            for (int i = start; i < end; i++)
            {
                var ch = text[i];
                if (char.IsWhiteSpace(ch)) return false;
                if (!hasLetter && char.IsLetter(ch)) hasLetter = true;
            }
            return hasLetter;
        }

        public static IEnumerable<TextBlock> EnumerateWords(DependencyObject root)
        {
            var stack = new Stack<DependencyObject>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var node = stack.Pop();
                if (node is TextBlock tb && IsWordElement(tb))
                    yield return tb;

                var childCount = VisualTreeHelper.GetChildrenCount(node);
                for (int i = childCount - 1; i >= 0; i--)
                    stack.Push(VisualTreeHelper.GetChild(node, i));
            }
        }

        public static (int from, int to)? RangeIndices(
            IList<TextBlock> words, TextBlock start, TextBlock end)
        {
            int a = words.IndexOf(start);
            int b = words.IndexOf(end);
            if (a < 0 || b < 0) return null;
            return (Math.Min(a, b), Math.Max(a, b));
        }

        public static void UpdatePreview(
            IDictionary<TextBlock, Brush?> originals,
            IList<TextBlock> words,
            int from,
            int to,
            Color markerColor)
        {
            HashSet<TextBlock>? inRange = null;
            if (from <= to)
            {
                inRange = new HashSet<TextBlock>(to - from + 1);
                for (int i = from; i <= to; i++)
                    inRange.Add(words[i]);
            }

            if (originals.Count > 0)
            {
                List<TextBlock>? toRemove = null;
                foreach (var kvp in originals)
                {
                    if (inRange == null || !inRange.Contains(kvp.Key))
                        (toRemove ??= new List<TextBlock>()).Add(kvp.Key);
                }

                if (toRemove != null)
                {
                    foreach (var w in toRemove)
                    {
                        w.Background = originals[w];
                        originals.Remove(w);
                    }
                }
            }

            if (inRange == null) return;

            SolidColorBrush? previewBrush = null;
            foreach (var word in inRange)
            {
                if (originals.ContainsKey(word)) continue;

                previewBrush ??= new SolidColorBrush(markerColor) { Opacity = 0.4 };
                originals[word] = word.Background;
                word.Background = previewBrush;
            }
        }

        public static void ClearPreview(IDictionary<TextBlock, Brush?> originals)
        {
            foreach (var kvp in originals)
                kvp.Key.Background = kvp.Value;
            originals.Clear();
        }

        public static void ApplySolid(
            IList<TextBlock> words,
            int from,
            int to,
            Color markerColor)
        {
            var brush = new SolidColorBrush(markerColor);
            for (int i = from; i <= to; i++)
                words[i].Background = brush;
        }
    }
}