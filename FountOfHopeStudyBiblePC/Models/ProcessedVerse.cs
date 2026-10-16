using System.Collections.Generic;
using System.Windows.Documents;

namespace FountOfHopeStudyBiblePC.Models
{
    /// <summary>
    /// WPF equivalent of the Kotlin AnnotatedString pair.
    /// Header is optional; Body is the verse text with styled Inlines.
    /// </summary>
    public record ProcessedVerse(List<Inline>? Header, List<Inline> Body);

    /// <summary>Metadata attached to clickable Inlines via Run.Tag.</summary>
    public record VerseAnnotation(string Tag, string Value);
}