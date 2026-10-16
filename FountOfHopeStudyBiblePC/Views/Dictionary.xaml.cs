using System;
using System.Windows.Controls;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Views
{
    public partial class DictionaryScreen : UserControl
    {
        public DictionaryScreen(Action<PassageSelection> onNavigateToReader)
        {
            InitializeComponent();
        }
    }
}