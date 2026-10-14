// Models/Screen.cs
namespace FountOfHopeStudyBiblePC.Models
{
    /// <summary>
    /// Discriminated-union style screen descriptor, mirroring the Kotlin `Screen` sealed class.
    /// </summary>
    public abstract record Screen
    {
        public sealed record Home : Screen;
        public sealed record Reader : Screen;
        public sealed record Bookmarks : Screen;
        public sealed record Notes : Screen;
        public sealed record Search : Screen;
        public sealed record Settings : Screen;
        public sealed record Quiz : Screen;
        public sealed record LearnHebrew : Screen;
        public sealed record LearnGreek : Screen;
        public sealed record Dictionary : Screen;
        public sealed record Animator : Screen;
        public sealed record Generations : Screen;
    }
}
