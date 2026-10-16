using System;
using System.Windows.Controls;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;

namespace FountOfHopeStudyBiblePC.Views
{
    public partial class SearchScreen : UserControl
    {
        public SearchScreen(
            DatabaseHelper databaseHelper,
            Action<PassageSelection> onPassageSelected,
            string currentVersionKey,
            Action<string> onVersionChange)
        {
            InitializeComponent();
        }
    }
}