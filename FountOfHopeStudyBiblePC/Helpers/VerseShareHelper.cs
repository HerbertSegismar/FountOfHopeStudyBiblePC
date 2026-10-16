// Helpers/VerseShareHelper.cs
using System.Collections.Generic;
using System.Text;
using System.Windows;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class VerseShareHelper
    {
        public static string BuildShareText(IEnumerable<Verse> verses)
        {
            var sb = new StringBuilder();
            foreach (var verse in verses)
            {
                var cleaned = SimpleVerseProcessor.StripXmlTags(verse.Text);
                sb.Append(verse.BookName ?? string.Empty)
                  .Append(' ')
                  .Append(verse.Chapter)
                  .Append(':')
                  .Append(verse.VerseNumber)
                  .Append(' ')
                  .Append(cleaned)
                  .Append('\n');
            }
            return sb.ToString();
        }

        /// <summary>
        /// WPF has no ACTION_SEND intent; copy to the clipboard and,
        /// optionally, hand off to the default mail client.
        /// </summary>
        public static void ShareVerses(Window owner, IEnumerable<Verse> verses)
        {
            var shareText = BuildShareText(verses);

            try
            {
                Clipboard.SetText(shareText);
                MessageBox.Show(owner,
                    "Verses copied to the clipboard.",
                    "Share verses via",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch
            {
                // Clipboard can be locked by another process — fall back to a dialog.
                MessageBox.Show(owner, shareText, "Share verses via");
            }
        }
    }
}