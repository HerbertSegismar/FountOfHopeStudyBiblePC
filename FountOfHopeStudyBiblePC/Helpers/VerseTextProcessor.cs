// Helpers/VerseTextProcessor.cs
using FountOfHopeStudyBiblePC.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public class VerseTextProcessor
    {
        private const int CacheSize = 200;

        private static readonly Regex AttributeRegex =
            new("(\\w+)=\"([^\"]*)\"", RegexOptions.Compiled);
        private static readonly Regex EscapeRegexPattern =
            new(@"[.*+?^${}()|\[\]\\]", RegexOptions.Compiled);
        private static readonly Regex WordRegex =
            new(@"^[\p{L}\p{M}'-]+$", RegexOptions.Compiled);

        private static readonly Regex BareStrongsRegex =
            new(@"<S[^>]*>[HG]?\d+</S>", RegexOptions.IgnoreCase | RegexOptions.Compiled);
        private static readonly Regex SelfClosingStrongsRegex =
            new(@"<S[^>]*/>", RegexOptions.Compiled);
        private static readonly Regex StrongsTagOnlyRegex =
            new(@"</?S[^>]*>", RegexOptions.Compiled);

        private readonly LinkedList<string> _cacheOrder = new();
        private readonly Dictionary<string, ProcessedVerse> _cache = new();

        // ═══════════════════════════════════════════════════════════════
        //  Public entry point  — signature matches Kotlin exactly.
        //  Callbacks come in as PARAMETERS, not through options.
        // ═══════════════════════════════════════════════════════════════
        public ProcessedVerse ProcessVerse(
            string? verseText,
            double baseFontSize,
            ThemeColors themeColors,
            Color textColor,
            bool isOldTestament,
            ProcessingOptions options,
            string? highlight = null,
            bool isHighlighted = false,
            HashSet<string>? wordHighlights = null,
            Action<string>? onTagPress = null,
            Action<string>? onWordPress = null,
            Action<string>? onStrongsPress = null)
        {
            if (string.IsNullOrWhiteSpace(verseText))
                return new ProcessedVerse(null, new List<Inline>());

            var processedText = verseText;
            if (!options.ShowStrongs)
            {
                processedText = BareStrongsRegex.Replace(processedText, "");
                processedText = SelfClosingStrongsRegex.Replace(processedText, "");
                processedText = StrongsTagOnlyRegex.Replace(processedText, "");
                processedText = Regex.Replace(processedText, "\\s+", " ");
            }

            // ── Cache key matches Kotlin's enable* pattern ─────────
            var cacheKey = BuildCacheKey(
                verseText, baseFontSize, textColor, isOldTestament,
                highlight, isHighlighted, options, themeColors, wordHighlights,
                enableTagClick: options.EnableTagClick && onTagPress != null,
                enableStrongsClick: options.EnableStrongsClick && onStrongsPress != null,
                enableWordClick: options.EnableWordClick && onWordPress != null);

            if (_cache.TryGetValue(cacheKey, out var cached))
                return cached;

            var nodes = ParseXmlTags(processedText);
            var tree = BuildTree(nodes);

            var context = new TraversalContext
            {
                TextColor = textColor,
                IsTextContainer = false,
                IsHeader = false,
                CurrentTag = null,
                BaseFontSize = baseFontSize,
                FontSizeMultiplier = 1f,
                IsOldTestament = isOldTestament,
            };

            var headerInlines = new List<Inline>();
            var bodyInlines = new List<Inline>();

            // ── Pass the actual parameters down (Kotlin does the same) ──
            TraverseChildren(tree, headerInlines, bodyInlines, context,
                highlight, themeColors, onTagPress, onWordPress, onStrongsPress,
                isHighlighted, options, wordHighlights);

            var result = new ProcessedVerse(
                options.ShowHeaders && headerInlines.Count > 0 ? headerInlines : null,
                bodyInlines);

            CacheAdd(cacheKey, result);
            return result;
        }

        public void ClearCache()
        {
            _cache.Clear();
            _cacheOrder.Clear();
        }

        // ═══════════════════════════════════════════════════════════════
        //  Cache (LRU)
        // ═══════════════════════════════════════════════════════════════
        private void CacheAdd(string key, ProcessedVerse value)
        {
            if (_cache.ContainsKey(key)) _cache.Remove(key);
            else if (_cache.Count >= CacheSize && _cacheOrder.Count > 0)
            {
                var oldest = _cacheOrder.First!.Value;
                _cacheOrder.RemoveFirst();
                _cache.Remove(oldest);
            }
            _cache[key] = value;
            _cacheOrder.AddLast(key);
        }

        private static string BuildCacheKey(
            string verseText, double baseFontSize, Color textColor,
            bool isOldTestament, string? highlight, bool isHighlighted,
            ProcessingOptions options, ThemeColors themeColors,
            HashSet<string>? wordHighlights,
            bool enableTagClick, bool enableStrongsClick, bool enableWordClick)
        {
            var sb = new StringBuilder();
            sb.Append(verseText.GetHashCode())
              .Append('|').Append(baseFontSize)
              .Append('|').Append(textColor.GetHashCode())
              .Append('|').Append(isOldTestament)
              .Append('|').Append(highlight ?? "")
              .Append('|').Append(isHighlighted)
              .Append("|ss=").Append(options.ShowStrongs)
              .Append("|sh=").Append(options.ShowHeaders)
              .Append("|sf=").Append(options.ShowFootnotesInline)
              .Append("|pw=").Append(options.PreserveWhitespace)
              .Append('|').Append(themeColors.GetHashCode())
              .Append('|').Append(wordHighlights?.Count ?? 0)
              .Append("|tag=").Append(enableTagClick)
              .Append("|str=").Append(enableStrongsClick)
              .Append("|wrd=").Append(enableWordClick);
            return sb.ToString();
        }

        // ═══════════════════════════════════════════════════════════════
        //  XML parsing
        // ═══════════════════════════════════════════════════════════════
        private static List<ParsedNode> ParseXmlTags(string text)
        {
            var nodes = new List<ParsedNode>();
            if (string.IsNullOrEmpty(text)) return nodes;

            var buffer = new StringBuilder();
            var stack = new List<string>();
            int i = 0;

            while (i < text.Length)
            {
                if (text[i] == '<')
                {
                    if (buffer.Length > 0)
                    {
                        nodes.Add(new ParsedNode.TextNode(buffer.ToString()));
                        buffer.Clear();
                    }
                    var tagEnd = text.IndexOf('>', i);
                    if (tagEnd == -1)
                    {
                        buffer.Append(text.Substring(i));
                        break;
                    }
                    var fullTag = text.Substring(i, tagEnd - i + 1);

                    if (fullTag.StartsWith("</"))
                    {
                        var name = fullTag.Substring(2, fullTag.Length - 3).Trim()
                            .Split(' ')[0];
                        if (stack.Count > 0 && stack[^1] == name) stack.RemoveAt(stack.Count - 1);
                        nodes.Add(new ParsedNode.ClosingTag(name));
                    }
                    else if (fullTag.EndsWith("/>"))
                    {
                        var name = fullTag.Substring(1, fullTag.Length - 3).Trim()
                            .Split(' ')[0];
                        nodes.Add(new ParsedNode.SelfClosingTag(name, fullTag));
                    }
                    else
                    {
                        var name = fullTag.Substring(1, fullTag.Length - 2).Trim()
                            .Split(' ')[0];
                        stack.Add(name);
                        nodes.Add(new ParsedNode.OpeningTag(name, fullTag));
                    }
                    i = tagEnd + 1;
                }
                else
                {
                    buffer.Append(text[i]);
                    i++;
                }
            }
            if (buffer.Length > 0) nodes.Add(new ParsedNode.TextNode(buffer.ToString()));
            for (int s = stack.Count - 1; s >= 0; s--)
                nodes.Add(new ParsedNode.ClosingTag(stack[s]));
            return nodes;
        }

        private static List<TreeNode> BuildTree(List<ParsedNode> nodes)
        {
            var root = new List<TreeNode>();
            var stack = new Stack<List<TreeNode>>();
            var current = root;
            var elementStack = new List<TreeNode.ElementNode>();

            foreach (var node in nodes)
            {
                switch (node)
                {
                    case ParsedNode.TextNode t:
                        current.Add(new TreeNode.TextNode(t.Content));
                        break;
                    case ParsedNode.OpeningTag o:
                        var el = new TreeNode.ElementNode(o.Tag, o.FullTag, new List<TreeNode>());
                        current.Add(el);
                        stack.Push(current);
                        elementStack.Add(el);
                        current = el.Children;
                        break;
                    case ParsedNode.ClosingTag:
                        if (stack.Count > 0)
                        {
                            if (elementStack.Count > 0) elementStack.RemoveAt(elementStack.Count - 1);
                            current = stack.Pop();
                        }
                        break;
                    case ParsedNode.SelfClosingTag sc:
                        current.Add(new TreeNode.SelfClosingNode(sc.Tag, sc.FullTag));
                        break;
                }
            }
            return root;
        }

        // ═══════════════════════════════════════════════════════════════
        //  Tree traversal → inlines
        // ═══════════════════════════════════════════════════════════════
        private void TraverseChildren(
            List<TreeNode> children,
            List<Inline> headerOut, List<Inline> bodyOut,
            TraversalContext context,
            string? highlight, ThemeColors themeColors,
            Action<string>? onTagPress, Action<string>? onWordPress, Action<string>? onStrongsPress,
            bool isHighlighted, ProcessingOptions options, HashSet<string>? wordHighlights)
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (i > 0)
                {
                    var target = context.IsHeader ? headerOut : bodyOut;
                    target.Add(new Run(" "));
                }
                TraverseNode(children[i], headerOut, bodyOut, context,
                    highlight, themeColors, onTagPress, onWordPress, onStrongsPress,
                    isHighlighted, options, wordHighlights);
            }
        }

        private void TraverseNode(
            TreeNode node,
            List<Inline> headerOut, List<Inline> bodyOut,
            TraversalContext context,
            string? highlight, ThemeColors themeColors,
            Action<string>? onTagPress, Action<string>? onWordPress, Action<string>? onStrongsPress,
            bool isHighlighted, ProcessingOptions options, HashSet<string>? wordHighlights)
        {
            switch (node)
            {
                case TreeNode.TextNode t:
                    var target = context.IsHeader ? headerOut : bodyOut;
                    ProcessTextNode(t, target, context, highlight, themeColors,
                        onWordPress, onStrongsPress, onTagPress, options, wordHighlights);
                    break;

                case TreeNode.SelfClosingNode sc:
                    if (!options.ShowFootnotesInline) return;
                    var target2 = context.IsHeader ? headerOut : bodyOut;
                    ProcessSelfClosingTag(sc, target2, context, themeColors);
                    break;

                case TreeNode.ElementNode el:
                    if (el.Tag == "n") return;

                    var newContext = el.Tag switch
                    {
                        "J" => context with
                        {
                            IsTextContainer = true,
                            TextColor = isHighlighted ? context.TextColor : themeColors.WordsOfJesus,
                            CurrentTag = el.Tag,
                        },
                        "t" => context with { IsTextContainer = true, CurrentTag = el.Tag },
                        "S" => context with
                        {
                            IsTextContainer = true,
                            TextColor = themeColors.TagColor,
                            FontSizeMultiplier = 0.7f,
                            BaselineShiftPx = 0.2f,
                            CurrentTag = el.Tag,
                        },
                        "f" => context with
                        {
                            IsTextContainer = true,
                            TextColor = themeColors.TagColor,
                            FontSizeMultiplier = 1.0f,
                            BaselineShiftPx = 0f,
                            CurrentTag = el.Tag,
                        },
                        _ => context with { CurrentTag = el.Tag },
                    };
                    TraverseChildren(el.Children, headerOut, bodyOut, newContext,
                        highlight, themeColors, onTagPress, onWordPress, onStrongsPress,
                        isHighlighted, options, wordHighlights);
                    break;
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Text node processing
        // ═══════════════════════════════════════════════════════════════
        private void ProcessTextNode(
            TreeNode.TextNode node, List<Inline> target,
            TraversalContext context,
            string? highlight, ThemeColors themeColors,
            Action<string>? onWordPress, Action<string>? onStrongsPress, Action<string>? onTagPress,
            ProcessingOptions options, HashSet<string>? wordHighlights)
        {
            var effectiveMultiplier =
                context.CurrentTag == "f" && IsEncircled(node.Content)
                    ? 1.25f
                    : context.FontSizeMultiplier;

            var textContext = context with { FontSizeMultiplier = effectiveMultiplier };
            var rawText = options.PreserveWhitespace ? node.Content : node.Content.Trim();
            if (rawText.Length == 0) return;

            switch (context.CurrentTag)
            {
                case "S":
                    if (!options.ShowStrongs) return;
                    if (options.EnableStrongsClick && onStrongsPress != null
                        && IsValidStrongsNumber(rawText))
                    {
                        var prefixed = FormatStrongsNumber(rawText, context.IsOldTestament);
                        var run = MakeRun(rawText, textContext, themeColors);
                        run.Tag = new VerseAnnotation("strong", prefixed);
                        run.FontSize = textContext.BaseFontSize * textContext.FontSizeMultiplier;
                        target.Add(run);
                    }
                    else ProcessNormalText(rawText, target, textContext, highlight, themeColors,
                        onWordPress, options, wordHighlights);
                    break;

                // ══ <f>…</f>: matches Kotlin's three sub-cases ══
                case "f":
                    if (options.EnableTagClick && onTagPress != null)
                    {
                        var marker = rawText.Trim();
                        var innerRun = MakeRun(rawText, textContext, themeColors);
                        innerRun.FontSize = textContext.BaseFontSize * textContext.FontSizeMultiplier;

                        // Mirrors Kotlin's `builder.pushStringAnnotation("tag", trimmed)`
                        innerRun.Tag = new VerseAnnotation("tag", marker);

                        var link = new Hyperlink(innerRun)
                        {
                            TextDecorations = null,
                            Cursor = Cursors.Hand,
                        };

                        // Encircled marker (① ② ③ …) — add a trailing
                        // zero-width space inside the link so the clickable
                        // area isn't a single narrow character.
                        if (IsEncircled(node.Content) && marker.Length > 0)
                        {
                            link.Inlines.Add(new Run("\u200B")
                            {
                                Foreground = Brushes.Transparent,
                                FontSize = textContext.BaseFontSize * textContext.FontSizeMultiplier,
                            });
                        }

                        var capturedCallback = onTagPress;
                        var capturedMarker = marker;
                        link.Click += (_, _) =>
                        {
                            System.Diagnostics.Debug.WriteLine(
                                $"[VerseTextProcessor] OnTagPress fired: '{capturedMarker}'");
                            capturedCallback.Invoke(capturedMarker);
                        };

                        System.Diagnostics.Debug.WriteLine(
                            $"[VerseTextProcessor] <f> marker wired: '{marker}' " +
                            $"encircled={IsEncircled(node.Content)}");
                        System.Diagnostics.Debug.WriteLine(
                            $"[VerseTextProcessor] <f> wired: '{marker}' codepoints=[{string.Join(" ", marker.Select(c => $"U+{(int)c:X4}"))}]");
                        target.Add(link);
                    }
                    else
                    {
                        // No callback → render as plain text (Kotlin does the same).
                        System.Diagnostics.Debug.WriteLine(
                            $"[VerseTextProcessor] <f> marker '{rawText}' " +
                            $"NOT clickable (onTagPress null or EnableTagClick false)");
                        ProcessNormalText(rawText, target, textContext, highlight, themeColors,
                            onWordPress, options, wordHighlights);
                    }
                    break;

                default:
                    ProcessNormalText(rawText, target, textContext, highlight, themeColors,
                        onWordPress, options, wordHighlights);
                    break;
            }
        }

        private static Run MakeRun(string text, TraversalContext ctx, ThemeColors themeColors)
        {
            return new Run(text)
            {
                Foreground = new SolidColorBrush(ctx.TextColor),
                FontSize = ctx.BaseFontSize * ctx.FontSizeMultiplier,
                BaselineAlignment = ctx.BaselineShiftPx > 0
                    ? BaselineAlignment.Superscript
                    : BaselineAlignment.Baseline,
            };
        }

        // ═══════════════════════════════════════════════════════════════
        //  Normal text
        // ═══════════════════════════════════════════════════════════════
        private void ProcessNormalText(
            string text, List<Inline> target, TraversalContext context,
            string? highlight, ThemeColors themeColors,
            Action<string>? onWordPress, ProcessingOptions options,
            HashSet<string>? wordHighlights)
        {
            if (options.EnableWordClick && onWordPress != null)
            {
                var words = SplitIntoWords(text, options.PreserveWhitespace);
                bool addSpaceAfterPunct = false;

                foreach (var word in words)
                {
                    if (addSpaceAfterPunct && word.Length > 0 && char.IsLetter(word[0]))
                        target.Add(new Run(" "));

                    if (IsWord(word))
                    {
                        Color? bg = null;
                        if (highlight != null && word.Contains(highlight, StringComparison.OrdinalIgnoreCase))
                            bg = themeColors.SearchHighlightBg;
                        else if (wordHighlights != null &&
                                 wordHighlights.Contains(word.ToLowerInvariant()))
                            bg = themeColors.WordHighlightBg;

                        var run = new Run(word)
                        {
                            Foreground = new SolidColorBrush(context.TextColor),
                            FontSize = context.BaseFontSize * context.FontSizeMultiplier,
                            Tag = new VerseAnnotation("word", word.ToLowerInvariant()),
                        };
                        if (bg.HasValue)
                            run.Background = new SolidColorBrush(bg.Value);
                        target.Add(run);
                    }
                    else
                    {
                        target.Add(new Run(word)
                        {
                            Foreground = new SolidColorBrush(context.TextColor),
                            FontSize = context.BaseFontSize * context.FontSizeMultiplier,
                        });
                    }

                    var trimmed = word.Trim();
                    addSpaceAfterPunct = trimmed is "." or "," or ":" or ";" or "?" or "!";
                }
            }
            else
            {
                target.Add(new Run(text)
                {
                    Foreground = new SolidColorBrush(context.TextColor),
                    FontSize = context.BaseFontSize * context.FontSizeMultiplier,
                });
            }
        }

        // ═══════════════════════════════════════════════════════════════
        //  Self-closing tag
        // ═══════════════════════════════════════════════════════════════
        private static void ProcessSelfClosingTag(
            TreeNode.SelfClosingNode node, List<Inline> target,
            TraversalContext context, ThemeColors themeColors)
        {
            var content = ExtractContentFromTag(node.FullTag).Trim();
            if (content.Length == 0) return;

            target.Add(new Run(content)
            {
                Foreground = new SolidColorBrush(themeColors.TagColor),
                Background = new SolidColorBrush(themeColors.TagBg),
                FontSize = context.BaseFontSize * 0.8,
            });
        }

        private static string ExtractContentFromTag(string tag)
        {
            var m = AttributeRegex.Match(tag);
            return m.Success ? m.Groups[2].Value : "";
        }

        // ═══════════════════════════════════════════════════════════════
        //  Helpers
        // ═══════════════════════════════════════════════════════════════
        private static bool IsEncircled(string text)
        {
            var t = text.Trim();
            if (t.Length != 1) return false;
            var code = t[0];
            return code >= 0x2460 && code <= 0x24FF;
        }

        private static bool IsValidStrongsNumber(string text)
        {
            var t = text.Trim();
            if (t.Length == 0) return false;
            if (Regex.IsMatch(t, @"^\d+$")) return true;
            if (Regex.IsMatch(t, @"^[GH]\d+$", RegexOptions.IgnoreCase)) return true;
            return false;
        }

        private static string FormatStrongsNumber(string text, bool isOldTestament)
        {
            var t = text.Trim();
            if (Regex.IsMatch(t, @"^[GH]\d+$", RegexOptions.IgnoreCase))
                return t.ToUpperInvariant();
            if (t.Length > 0 && char.IsLetter(t[0])) return t;
            return (isOldTestament ? "H" : "G") + t;
        }

        private static List<string> SplitIntoWords(string text, bool preserveWhitespace)
        {
            if (preserveWhitespace)
                return Regex.Split(text, @"(?<=\S)(?=\s)|(?<=\s)(?=\S)")
                    .Where(s => s.Length > 0).ToList();

            return Regex.Split(text, @"(?<=\w)(?=\W)|(?<=\W)(?=\w)|\s+")
                .Where(s => s.Length > 0).ToList();
        }

        private static bool IsWord(string text) => WordRegex.IsMatch(text);
        private static string EscapeRegex(string s) =>
            EscapeRegexPattern.Replace(s, m => "\\" + m.Value);
    }

    // ═══════════════════════════════════════════════════════════════════
    //  Supporting types
    // ═══════════════════════════════════════════════════════════════════
    public record TraversalContext
    {
        public Color TextColor { get; init; }
        public bool IsTextContainer { get; init; }
        public bool IsHeader { get; init; }
        public string? CurrentTag { get; init; }
        public double BaseFontSize { get; init; } = 16;
        public float FontSizeMultiplier { get; init; } = 1f;
        public float BaselineShiftPx { get; init; } = 0f;
        public bool IsOldTestament { get; init; }
    }
}