// Helpers/WordDragHelper.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Helpers
{
    /// <summary>
    /// Shared drag-to-highlight primitives.
    /// Used by <c>ReaderScreen</c>'s visual-tree fallback and by every reader
    /// control that implements <c>IWordHighlightHost</c>.
    /// </summary>
    public static class WordDragHelper
    {
        /// <summary>Drags shorter than this (in device-independent px) are treated as clicks.</summary>
        public const double ThresholdPixels = 3.0;

        /// <summary>
        /// Walks up from a hit-test result to the nearest word <see cref="TextBlock"/>.
        /// Returns <c>null</c> if we hit a non-word TextBlock (verse number,
        /// container paragraph, whitespace). Setting <c>Tag = "noword"</c> on a
        /// TextBlock opts it out entirely.
        /// </summary>
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

        /// <summary>
        /// A word element is a TextBlock whose trimmed text is a single,
        /// non-empty token containing at least one letter.
        /// </summary>
        public static bool IsWordElement(TextBlock tb)
        {
            if (tb.Tag is string tag && tag == "noword") return false;

            var text = tb.Text;
            if (string.IsNullOrWhiteSpace(text)) return false;

            var trimmed = text.Trim();
            bool hasLetter = false;
            foreach (var ch in trimmed)
            {
                if (char.IsWhiteSpace(ch)) return false;   // multi-word → container
                if (!hasLetter && char.IsLetter(ch)) hasLetter = true;
            }
            return hasLetter;
        }

        /// <summary>
        /// Enumerates word TextBlocks in visual (top-to-bottom, left-to-right) order
        /// below <paramref name="root"/>.
        /// </summary>
        public static IEnumerable<TextBlock> EnumerateWords(DependencyObject root)
        {
            foreach (var d in EnumerateVisualOrder(root))
                if (d is TextBlock tb && IsWordElement(tb))
                    yield return tb;
        }

        private static IEnumerable<DependencyObject> EnumerateVisualOrder(DependencyObject root)
        {
            yield return root;
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                foreach (var d in EnumerateVisualOrder(child))
                    yield return d;
            }
        }

        /// <summary>
        /// Normalised [from,to] indices into <paramref name="words"/> covering the
        /// drag from <paramref name="start"/> to <paramref name="end"/>. Order-agnostic.
        /// Returns <c>null</c> if either block isn't in the list.
        /// </summary>
        public static (int from, int to)? RangeIndices(
            IList<TextBlock> words, TextBlock start, TextBlock end)
        {
            int a = words.IndexOf(start);
            int b = words.IndexOf(end);
            if (a < 0 || b < 0) return null;
            return (Math.Min(a, b), Math.Max(a, b));
        }

        /// <summary>
        /// Repaints the translucent drag preview so only the words currently
        /// in [from..to] carry a preview brush. Restores words that fall out
        /// of range back to their original brush.
        /// </summary>
        public static void UpdatePreview(
            IDictionary<TextBlock, Brush?> originals,
            IList<TextBlock> words,
            int from,
            int to,
            Color markerColor)
        {
            var inRange = new HashSet<TextBlock>();
            for (int i = from; i <= to; i++) inRange.Add(words[i]);

            // Restore words that left the range.
            foreach (var word in originals.Keys.ToList())
            {
                if (!inRange.Contains(word))
                {
                    word.Background = originals[word];
                    originals.Remove(word);
                }
            }

            // Add a translucent marker to newly-entered words.
            var previewBrush = new SolidColorBrush(markerColor) { Opacity = 0.4 };
            foreach (var word in inRange)
            {
                if (!originals.ContainsKey(word))
                {
                    originals[word] = word.Background;
                    word.Background = previewBrush;
                }
            }
        }

        /// <summary>Restores every previewed word back to its original brush.</summary>
        public static void ClearPreview(IDictionary<TextBlock, Brush?> originals)
        {
            foreach (var kvp in originals)
                kvp.Key.Background = kvp.Value;
            originals.Clear();
        }

        /// <summary>Applies the final solid highlight to words [from..to].</summary>
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