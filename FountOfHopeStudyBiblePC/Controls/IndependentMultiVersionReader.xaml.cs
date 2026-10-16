using System;
using System.Collections.Generic;
using System.Windows.Controls;
using FountOfHopeStudyBiblePC.Models;
using FountOfHopeStudyBiblePC.Services;
using FountOfHopeStudyBiblePC.ViewModels;

namespace FountOfHopeStudyBiblePC.Controls
{
    public partial class IndependentMultiVersionReader : UserControl
    {
        public IndependentMultiVersionReader(
            PassageSelection primaryCurrent,
            PassageSelection secondaryCurrent,
            int? targetVerse,
            int? secondaryTargetVerse,
            DatabaseHelper? databaseHelper,
            DatabaseHelper? secondaryDatabaseHelper,
            DatabaseHelper? subheadingsDbHelper,
            Dictionary<(int book, int chapter), List<VerseContent>> primaryLoadedVerses,
            Dictionary<(int book, int chapter), List<VerseContent>> secondaryLoadedVerses,
            AppViewModel viewModel,
            Action<PassageSelection> onPrimaryPassageChange,
            Action<PassageSelection> onSecondaryPassageChange,
            Action onScheduleFade,
            Action<string> onPrimaryWordPress,
            Action<string> onSecondaryWordPress,
            Action<string, int, bool> onStrongsPress,
            Action<string, int, int, int, bool> onTagPress,
            Action<Verse, PassageSelection, bool> onVerseLongPress,
            Action<int, int, int, bool> onCrossRefClick,
            DatabaseHelper? crossRefHelper,
            int refreshKey,
            Action<int, int, int> onVerseCommentaryClick,
            Action onWordHighlightAction)
        {
            InitializeComponent();
        }
    }
}