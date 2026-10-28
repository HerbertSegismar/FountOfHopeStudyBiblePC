// Views/IWordHighlightHost.cs
using System.Windows;

namespace FountOfHopeStudyBiblePC.Views
{
    /// <summary>
    /// Optional contract implemented by reader controls that want to participate
    /// in drag-to-highlight. When present, ReaderScreen delegates the drag
    /// lifecycle to the control so that per-word ranges are computed by the
    /// control that owns the rendered words.
    /// </summary>
    public interface IWordHighlightHost
    {
        /// <summary>
        /// Called on left-mouse-down in word-highlight mode.
        /// Return <c>false</c> if the point is not on a word (then the drag is ignored).
        /// </summary>
        bool BeginWordDrag(Point positionInHost);

        /// <summary>
        /// Called on mouse move while dragging. <paramref name="positionInHost"/>
        /// is relative to the ReaderHost content control.
        /// </summary>
        void UpdateWordDrag(Point positionInHost);

        /// <summary>
        /// Called on mouse-up after a real drag. The host should apply the
        /// highlight to every word between the tracked start and end.
        /// </summary>
        void CommitWordDrag();

        /// <summary>
        /// Called when the drag ends without a real range (click) or the mode
        /// is dismissed. The host should drop any transient selection state.
        /// </summary>
        void CancelWordDrag();
    }
}