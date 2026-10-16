using System;
using System.Windows.Controls;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Views
{
    public partial class BookmarksScreen : UserControl
    {
        public BookmarksScreen(Action<PassageSelection> onNavigateToReader)
        {
            InitializeComponent();
        }
    }
}