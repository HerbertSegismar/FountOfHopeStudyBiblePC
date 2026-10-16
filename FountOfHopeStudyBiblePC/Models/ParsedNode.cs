// Models/ParsedNode.cs
namespace FountOfHopeStudyBiblePC.Models
{
    public abstract record ParsedNode
    {
        public sealed record TextNode(string Content) : ParsedNode;
        public sealed record OpeningTag(string Tag, string FullTag) : ParsedNode;
        public sealed record ClosingTag(string Tag) : ParsedNode;
        public sealed record SelfClosingTag(string Tag, string FullTag) : ParsedNode;
    }
}
