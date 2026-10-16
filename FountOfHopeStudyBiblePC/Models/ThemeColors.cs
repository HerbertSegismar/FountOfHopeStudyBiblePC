// Models/ThemeColors.cs
using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Models
{
    public record ThemeColors(
        Color TextColor,
        Color VerseNumber,
        Color Primary,
        Color TagColor,
        Color TagBg,
        Color WordsOfJesus,
        Color SearchHighlightBg,
        Color HighlightIcon,
        Color WordHighlightBg);
}