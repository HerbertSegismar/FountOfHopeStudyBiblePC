using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC
{
    public abstract record Screen
    {
        public sealed record Home : Screen;
        public sealed record Reader(PassageSelection? Passage = null) : Screen;
        public sealed record Bookmarks : Screen;
        public sealed record Notes : Screen;
        public sealed record Settings : Screen;
        public sealed record Search : Screen;
        public sealed record Quiz : Screen;
        public sealed record LearnHebrew : Screen;
        public sealed record LearnGreek : Screen;
        public sealed record Dictionary : Screen;
        public sealed record Animator : Screen;
        public sealed record Generations : Screen;
    }
}