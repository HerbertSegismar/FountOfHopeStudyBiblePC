using System.Collections.Generic;

namespace FountOfHopeStudyBiblePC.Models
{
    public record ModalPage
    {
        public string Title { get; init; } = "";
        public string Type { get; init; } = "";   // "definition" | "strong" | "commentary" | "crossreference" | "versecommentary" | "verses"
        public string Content { get; init; } = "";
        public string? Description { get; init; }
        public string? Word { get; init; }
        public string? StrongNumber { get; init; }
        public List<Verse>? Verses { get; init; }
        public PassageSelection? Passage { get; init; }
        public bool IsOldTestament { get; init; }
        public int? BookNumber { get; init; }
        public int? Chapter { get; init; }
        public int? Verse { get; init; }
    }
}