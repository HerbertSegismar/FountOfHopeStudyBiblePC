using FountOfHopeStudyBiblePC.Models;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public partial class VerseTextProcessor
    {
        private const int CacheSize = 200;

        [GeneratedRegex("(\\w+)=\"([^\"]*)\"")]
        private static partial Regex AttributeRegex();

        [GeneratedRegex(@"[.*+?^${}()|\[\]\\]")]
        private static partial Regex EscapeRegexPattern();

        [GeneratedRegex(@"^[\p{L}\p{M}'-]+$")]
        private static partial Regex WordRegex();

        [GeneratedRegex(@"<S[^>]*>.*?</S>", RegexOptions.Singleline)]
        private static partial Regex StrongsWithContentRegex();

        [GeneratedRegex(@"<S[^>]*/>")]
        private static partial Regex SelfClosingStrongsRegex();

        [GeneratedRegex(@"\s+")]
        private static partial Regex WhitespaceRegex();

        [GeneratedRegex(@"^\d+$")]
        private static partial Regex DigitsRegex();

        [GeneratedRegex(@"^[GH]\d+$", RegexOptions.IgnoreCase)]
        private static partial Regex StrongsNumberRegex();

        [GeneratedRegex(@"(?<=\S)(?=\s)|(?<=\s)(?=\S)")]
        private static partial Regex WhitespaceBoundaryRegex();

        [GeneratedRegex(@"(?<=\w)(?=\W)|(?<=\W)(?=\w)|\s+")]
        private static partial Regex WordBoundaryRegex();

        private readonly LinkedList<string> _cacheOrder = new();
        private readonly Dictionary<string, List<InlineSpec>> _cache = [];

        public ProcessedVerse ProcessVerse(
            string? verseText,
            double baseFontSize,
            ThemeColors themeColors,
            bool isOldTestament,
            ProcessingOptions options,
            Color? textColor = null,
            string? highlight = null,
            bool isHighlighted = false,
            HashSet<string>? wordHighlights = null,
            Action<string>? onTagPress = null,
            Action<string>? onWordPress = null,
            Action<string>? onStrongsPress = null)
        {
            if (string.IsNullOrWhiteSpace(verseText))
                return new ProcessedVerse(null, []);

            var processedText = verseText;
            if (!options.ShowStrongs)
            {
                processedText = StrongsWithContentRegex().Replace(processedText, "");
                processedText = SelfClosingStrongsRegex().Replace(processedText, "");
                processedText = WhitespaceRegex().Replace(processedText, " ");
            }

            var tagCb = options.EnableTagClick ? onTagPress : null;
            var wordCb = options.EnableWordClick ? onWordPress : null;
            var strongsCb = options.EnableStrongsClick ? onStrongsPress : null;

            var cacheKey = BuildCacheKey(verseText, baseFontSize, textColor, isOldTestament,
                highlight, isHighlighted, options, themeColors, wordHighlights,
                tagCb != null, strongsCb != null, wordCb != null);

            if (_cache.TryGetValue(cacheKey, out var cached))
            {
                if (_cacheOrder.Remove(cacheKey)) _cacheOrder.AddLast(cacheKey);
                return new ProcessedVerse(null, BuildInlines(cached, tagCb));
            }

            var ctx = new TraversalContext
            {
                TextColor = textColor ?? themeColors.TextColor,
                BaseFontSize = baseFontSize,
                IsOldTestament = isOldTestament,
            };

            var body = new List<InlineSpec>();
            TraverseChildren(BuildTree(ParseXmlTags(processedText)), body, ctx,
                highlight, themeColors, tagCb, wordCb, strongsCb,
                isHighlighted, options, wordHighlights);

            CacheAdd(cacheKey, body);
            return new ProcessedVerse(null, BuildInlines(body, tagCb));
        }

        public void ClearCache()
        {
            _cache.Clear();
            _cacheOrder.Clear();
        }

        private static List<Inline> BuildInlines(List<InlineSpec> specs, Action<string>? onTagPress)
        {
            var list = new List<Inline>(specs.Count);
            foreach (var s in specs)
            {
                var run = new Run(s.Text)
                {
                    Foreground = new SolidColorBrush(s.Foreground),
                    FontSize = s.FontSize,
                    BaselineAlignment = s.Baseline,
                };
                if (s.Background is { } bg) run.Background = new SolidColorBrush(bg);
                if (s.AnnotationKind != null)
                    run.Tag = new VerseAnnotation(s.AnnotationKind, s.AnnotationValue ?? "");

                if (s.MakeHyperlink && onTagPress != null && s.AnnotationValue != null)
                {
                    var cb = onTagPress;
                    var val = s.AnnotationValue;
                    var link = new Hyperlink(run) { TextDecorations = null, Cursor = Cursors.Hand };
                    if (s.AppendZeroWidthSpace)
                        link.Inlines.Add(new Run("\u200B") { Foreground = Brushes.Transparent, FontSize = s.FontSize });
                    link.Click += (_, _) => cb(val);
                    list.Add(link);
                }
                else
                {
                    list.Add(run);
                }
            }
            return list;
        }

        private void CacheAdd(string key, List<InlineSpec> value)
        {
            if (_cache.ContainsKey(key))
            {
                _cache.Remove(key);
                _cacheOrder.Remove(key);
            }
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
            string verseText, double baseFontSize, Color? textColor, bool isOldTestament,
            string? highlight, bool isHighlighted, ProcessingOptions options,
            ThemeColors themeColors, HashSet<string>? wordHighlights,
            bool enableTagClick, bool enableStrongsClick, bool enableWordClick) =>
            new StringBuilder()
                .Append(verseText.GetHashCode())
                .Append('|').Append(baseFontSize)
                .Append('|').Append(textColor?.GetHashCode() ?? 0)
                .Append('|').Append(isOldTestament)
                .Append('|').Append(highlight ?? "")
                .Append('|').Append(isHighlighted)
                .Append("|ss=").Append(options.ShowStrongs)
                .Append("|sf=").Append(options.ShowFootnotesInline)
                .Append("|pw=").Append(options.PreserveWhitespace)
                .Append('|').Append(themeColors.GetHashCode())
                .Append('|').Append(StableSetHash(wordHighlights))
                .Append("|tag=").Append(enableTagClick)
                .Append("|str=").Append(enableStrongsClick)
                .Append("|wrd=").Append(enableWordClick)
                .ToString();

        private static int StableSetHash(HashSet<string>? set)
        {
            if (set == null) return 0;
            unchecked
            {
                int h = 0;
                foreach (var s in set) h ^= s?.GetHashCode() ?? 0;
                return h;
            }
        }

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
                        buffer.Append(text[i..]);
                        break;
                    }
                    var fullTag = text.Substring(i, tagEnd - i + 1);

                    if (fullTag.StartsWith("</"))
                    {
                        var name = fullTag[2..^1].Trim().Split(' ')[0];
                        if (stack.Count > 0 && stack[^1] == name) stack.RemoveAt(stack.Count - 1);
                        nodes.Add(new ParsedNode.ClosingTag(name));
                    }
                    else if (fullTag.EndsWith("/>"))
                    {
                        var name = fullTag[1..^2].Trim().Split(' ')[0];
                        nodes.Add(new ParsedNode.SelfClosingTag(name, fullTag));
                    }
                    else
                    {
                        var name = fullTag[1..^1].Trim().Split(' ')[0];
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

            foreach (var node in nodes)
            {
                switch (node)
                {
                    case ParsedNode.TextNode t:
                        current.Add(new TreeNode.TextNode(t.Content));
                        break;
                    case ParsedNode.OpeningTag o:
                        var el = new TreeNode.ElementNode(o.Tag, o.FullTag, []);
                        current.Add(el);
                        stack.Push(current);
                        current = el.Children;
                        break;
                    case ParsedNode.ClosingTag:
                        if (stack.Count > 0) current = stack.Pop();
                        break;
                    case ParsedNode.SelfClosingTag sc:
                        current.Add(new TreeNode.SelfClosingNode(sc.Tag, sc.FullTag));
                        break;
                }
            }
            return root;
        }

        private void TraverseChildren(
            List<TreeNode> children, List<InlineSpec> outList, TraversalContext ctx,
            string? highlight, ThemeColors themeColors,
            Action<string>? onTag, Action<string>? onWord, Action<string>? onStrongs,
            bool isHighlighted, ProcessingOptions options, HashSet<string>? wordHighlights)
        {
            for (int i = 0; i < children.Count; i++)
            {
                if (i > 0) outList.Add(Spec(" ", ctx));
                TraverseNode(children[i], outList, ctx, highlight, themeColors,
                    onTag, onWord, onStrongs, isHighlighted, options, wordHighlights);
            }
        }

        private void TraverseNode(
            TreeNode node, List<InlineSpec> outList, TraversalContext ctx,
            string? highlight, ThemeColors themeColors,
            Action<string>? onTag, Action<string>? onWord, Action<string>? onStrongs,
            bool isHighlighted, ProcessingOptions options, HashSet<string>? wordHighlights)
        {
            switch (node)
            {
                case TreeNode.TextNode t:
                    ProcessTextNode(t, outList, ctx, highlight, themeColors, onWord, onStrongs, onTag, options, wordHighlights);
                    break;
                case TreeNode.SelfClosingNode sc:
                    if (options.ShowFootnotesInline) ProcessSelfClosingTag(sc, outList, ctx, themeColors);
                    break;
                case TreeNode.ElementNode el:
                    if (el.Tag == "n") return;
                    var next = el.Tag switch
                    {
                        "J" => ctx with { TextColor = isHighlighted ? ctx.TextColor : themeColors.WordsOfJesus, CurrentTag = el.Tag },
                        "t" => ctx with { CurrentTag = el.Tag },
                        "S" => ctx with { TextColor = themeColors.TagColor, FontSizeMultiplier = 0.7f, BaselineShiftPx = 0.2f, CurrentTag = el.Tag },
                        "f" => ctx with { TextColor = themeColors.TagColor, FontSizeMultiplier = 1.0f, BaselineShiftPx = 0f, CurrentTag = el.Tag },
                        _ => ctx with { CurrentTag = el.Tag },
                    };
                    TraverseChildren(el.Children, outList, next, highlight, themeColors,
                        onTag, onWord, onStrongs, isHighlighted, options, wordHighlights);
                    break;
            }
        }

        private void ProcessTextNode(
            TreeNode.TextNode node, List<InlineSpec> outList, TraversalContext ctx,
            string? highlight, ThemeColors themeColors,
            Action<string>? onWord, Action<string>? onStrongs, Action<string>? onTag,
            ProcessingOptions options, HashSet<string>? wordHighlights)
        {
            var multiplier = ctx.CurrentTag == "f" && IsEncircled(node.Content) ? 1.25f : ctx.FontSizeMultiplier;
            var tctx = ctx with { FontSizeMultiplier = multiplier };
            var rawText = options.PreserveWhitespace ? node.Content : node.Content.Trim();
            if (rawText.Length == 0) return;

            switch (ctx.CurrentTag)
            {
                case "S":
                    if (!options.ShowStrongs) return;
                    if (onStrongs != null && IsValidStrongsNumber(rawText))
                        outList.Add(Spec(rawText, tctx, kind: "strong",
                            val: FormatStrongsNumber(rawText, ctx.IsOldTestament)));
                    else
                        ProcessNormalText(rawText, outList, tctx, highlight, themeColors, onWord, options, wordHighlights);
                    break;
                case "f":
                    if (onTag != null && rawText.Trim().Length > 0)
                        outList.Add(Spec(rawText, tctx, kind: "tag",
                            val: rawText.Trim(), link: true, zw: IsEncircled(node.Content)));
                    else
                        ProcessNormalText(rawText, outList, tctx, highlight, themeColors, onWord, options, wordHighlights);
                    break;
                default:
                    ProcessNormalText(rawText, outList, tctx, highlight, themeColors, onWord, options, wordHighlights);
                    break;
            }
        }

        private void ProcessNormalText(
            string text, List<InlineSpec> outList, TraversalContext ctx,
            string? highlight, ThemeColors themeColors,
            Action<string>? onWord, ProcessingOptions options, HashSet<string>? wordHighlights)
        {
            if (onWord == null)
            {
                ProcessTextWithoutWordClick(text, outList, ctx, highlight, themeColors, wordHighlights);
                return;
            }

            var words = SplitIntoWords(text, options.PreserveWhitespace);
            bool addSpace = false;
            foreach (var word in words)
            {
                if (addSpace && word.Length > 0 && char.IsLetter(word[0]))
                    outList.Add(Spec(" ", ctx));

                if (IsWord(word))
                {
                    Color? bg = null;
                    if (highlight != null && word.Contains(highlight, StringComparison.OrdinalIgnoreCase))
                        bg = themeColors.SearchHighlightBg;
                    else if (wordHighlights != null && wordHighlights.Contains(word.ToLowerInvariant()))
                        bg = themeColors.WordHighlightBg;
                    outList.Add(Spec(word, ctx, bg: bg, kind: "word", val: word.ToLowerInvariant()));
                }
                else
                {
                    outList.Add(Spec(word, ctx));
                }

                var trimmed = word.Trim();
                addSpace = trimmed is "." or "," or ":" or ";" or "?" or "!";
            }
        }

        private void ProcessTextWithoutWordClick(
            string text, List<InlineSpec> outList, TraversalContext ctx,
            string? highlight, ThemeColors themeColors, HashSet<string>? wordHighlights)
        {
            if (highlight == null || !text.Contains(highlight, StringComparison.OrdinalIgnoreCase))
            {
                AppendWithWordHighlights(text, outList, ctx, wordHighlights, themeColors);
                return;
            }

            Regex regex;
            try { regex = new Regex(EscapeRegex(highlight), RegexOptions.IgnoreCase); }
            catch { return; }

            int last = 0;
            foreach (Match m in regex.Matches(text))
            {
                if (m.Index > last)
                    AppendWithWordHighlights(text[last..m.Index], outList, ctx, wordHighlights, themeColors);
                outList.Add(Spec(m.Value, ctx, bg: themeColors.SearchHighlightBg));
                last = m.Index + m.Length;
            }
            if (last < text.Length)
                AppendWithWordHighlights(text[last..], outList, ctx, wordHighlights, themeColors);
        }

        private void AppendWithWordHighlights(
            string text, List<InlineSpec> outList, TraversalContext ctx,
            HashSet<string>? wordHighlights, ThemeColors themeColors)
        {
            if (wordHighlights == null)
            {
                outList.Add(Spec(text, ctx));
                return;
            }
            foreach (var word in SplitIntoWords(text, false))
            {
                Color? bg = IsWord(word) && wordHighlights.Contains(word.ToLowerInvariant())
                    ? themeColors.WordHighlightBg : null;
                outList.Add(Spec(word, ctx, bg: bg));

                var trimmed = word.Trim();
                bool isPunct = trimmed is "." or "," or ":" or ";" or "?" or "!";
                if (!isPunct && trimmed != word) outList.Add(Spec(" ", ctx));
            }
        }

        private static void ProcessSelfClosingTag(
            TreeNode.SelfClosingNode node, List<InlineSpec> outList,
            TraversalContext ctx, ThemeColors themeColors)
        {
            var content = ExtractContentFromTag(node.FullTag).Trim();
            if (content.Length == 0) return;
            outList.Add(new InlineSpec(content, themeColors.TagColor, themeColors.TagBg,
                ctx.BaseFontSize * 0.8, BaselineAlignment.Baseline, null, null, false, false));
        }

        private static InlineSpec Spec(string text, TraversalContext ctx, Color? bg = null,
            string? kind = null, string? val = null, bool link = false, bool zw = false) =>
            new(text, ctx.TextColor, bg,
                ctx.BaseFontSize * ctx.FontSizeMultiplier,
                ctx.BaselineShiftPx > 0 ? BaselineAlignment.Superscript : BaselineAlignment.Baseline,
                kind, val, link, zw);

        private static string ExtractContentFromTag(string tag)
        {
            var m = AttributeRegex().Match(tag);
            return m.Success ? m.Groups[2].Value : "";
        }

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
            return DigitsRegex().IsMatch(t) || StrongsNumberRegex().IsMatch(t);
        }

        private static string FormatStrongsNumber(string text, bool isOldTestament)
        {
            var t = text.Trim();
            if (StrongsNumberRegex().IsMatch(t)) return t.ToUpperInvariant();
            if (t.Length > 0 && char.IsLetter(t[0])) return t;
            return (isOldTestament ? "H" : "G") + t;
        }

        private static List<string> SplitIntoWords(string text, bool preserveWhitespace) =>
            (preserveWhitespace ? WhitespaceBoundaryRegex() : WordBoundaryRegex())
                .Split(text).Where(s => s.Length > 0).ToList();

        private static bool IsWord(string text) => WordRegex().IsMatch(text);
        private static string EscapeRegex(string s) => EscapeRegexPattern().Replace(s, m => "\\" + m.Value);
    }

    public sealed record InlineSpec(
        string Text,
        Color Foreground,
        Color? Background,
        double FontSize,
        BaselineAlignment Baseline,
        string? AnnotationKind,
        string? AnnotationValue,
        bool MakeHyperlink,
        bool AppendZeroWidthSpace);

    public record TraversalContext
    {
        public Color TextColor { get; init; }
        public string? CurrentTag { get; init; }
        public double BaseFontSize { get; init; } = 16;
        public float FontSizeMultiplier { get; init; } = 1f;
        public float BaselineShiftPx { get; init; } = 0f;
        public bool IsOldTestament { get; init; }
    }
}