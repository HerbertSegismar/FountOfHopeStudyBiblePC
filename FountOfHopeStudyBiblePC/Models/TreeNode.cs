// Models/TreeNode.cs
using System.Collections.Generic;

namespace FountOfHopeStudyBiblePC.Models
{
    public abstract record TreeNode
    {
        public sealed record TextNode(string Content) : TreeNode;
        public sealed record SelfClosingNode(string Tag, string FullTag) : TreeNode;

        // Was a class — must also be a record
        public sealed record ElementNode : TreeNode
        {
            public string Tag { get; init; } = "";
            public string FullTag { get; init; } = "";
            public List<TreeNode> Children { get; init; } = new();

            public ElementNode() { }
            public ElementNode(string tag, string fullTag, List<TreeNode> children)
            {
                Tag = tag;
                FullTag = fullTag;
                Children = children;
            }
        }
    }
}