using System.Windows.Media;

namespace FountOfHopeStudyBiblePC.Models
{
    public record SelectedWord(int VerseNumber, int Start, int End, Color Color)
    {
        public virtual bool Equals(SelectedWord? other) =>
            other is not null
            && VerseNumber == other.VerseNumber
            && Start == other.Start
            && End == other.End;

        public override int GetHashCode() => HashCode.Combine(VerseNumber, Start, End);
    }
}