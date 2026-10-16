// Models/ProcessingOptions.cs
namespace FountOfHopeStudyBiblePC.Models
{
    public record ProcessingOptions
    {
        public bool EnableWordClick { get; init; } = true;
        public bool EnableStrongsClick { get; init; } = true;
        public bool EnableTagClick { get; init; } = true;
        public bool ShowFootnotesInline { get; init; } = true;
        public bool PreserveWhitespace { get; init; } = false;
        public bool ShowHeaders { get; init; } = true;
        public bool ShowStrongs { get; init; } = true;
    }
}