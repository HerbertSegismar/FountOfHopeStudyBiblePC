// MainWindow.xaml.cs
using System.Windows;
using FountOfHopeStudyBiblePC.Helpers;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;
using FountOfHopeStudyBiblePC.Views;

namespace FountOfHopeStudyBiblePC
{
    public partial class MainWindow : Window
    {
        private readonly AppViewModel _viewModel;
        private readonly DatabaseHelper _databaseHelper;

        public MainWindow()
        {
            InitializeComponent();

            _viewModel = new AppViewModel();
            _databaseHelper = new DatabaseHelper();

            ShowHome();
        }

        private void ShowHome()
        {
            var home = new HomeScreen(
                viewModel: _viewModel,
                onNavigateToReader: NavigateToReader,
                onNavigateToScreen: NavigateToScreen,
                databaseHelper: _databaseHelper,
                onTakeBibleQuizClick: () => NavigateToScreen(new Screen.Quiz()),
                onLearnHebrewClick: () => NavigateToScreen(new Screen.LearnHebrew()),
                onLearnGreekClick: () => NavigateToScreen(new Screen.LearnGreek()),
                onOpenDictionaryClick: () => NavigateToScreen(new Screen.Dictionary()),
                onOpenAnimatorClick: () => NavigateToScreen(new Screen.Animator()),
                onOpenGenerationsClick: () => NavigateToScreen(new Screen.Generations()));

            RootContent.Content = home;
        }

        // ── Navigation callbacks ────────────────────────────────────────────

        private void NavigateToReader(PassageSelection selection)
        {
            // TODO: RootContent.Content = new ReaderScreen(_viewModel, selection);
            MessageBox.Show(
                $"Open reader:\n{selection.BookName} {selection.Chapter}:{selection.Verse}",
                "Reader");
        }

        private void NavigateToScreen(Screen screen)
        {
            // TODO: swap RootContent.Content for the real screen.
            switch (screen)
            {
                case Screen.Bookmarks: MessageBox.Show("Bookmarks"); break;
                case Screen.Notes: MessageBox.Show("Notes"); break;
                case Screen.Search: MessageBox.Show("Search"); break;
                case Screen.Settings: MessageBox.Show("Settings"); break;
                case Screen.Quiz: MessageBox.Show("Bible Quiz"); break;
                case Screen.LearnHebrew: MessageBox.Show("Learn Hebrew"); break;
                case Screen.LearnGreek: MessageBox.Show("Learn Greek"); break;
                case Screen.Dictionary: MessageBox.Show("Dictionary"); break;
                case Screen.Animator: MessageBox.Show("Animator"); break;
                case Screen.Generations: MessageBox.Show("Generations"); break;
                default:
                    ShowHome();
                    break;
            }
        }
    }
}