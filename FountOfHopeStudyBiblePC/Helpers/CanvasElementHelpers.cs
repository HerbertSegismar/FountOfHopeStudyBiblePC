using System.Windows;
using FountOfHopeStudyBiblePC.Models;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class CanvasElementHelpers
    {
        /// <summary>Kotlin `offsetForPivotChange` — recompute the offset so the
        /// visual position of the element does not jump when the pivot moves.</summary>
        public static Point OffsetForPivotChange(
            CanvasElement element,
            float oldPivotX, float oldPivotY,
            float newPivotX, float newPivotY)
        {
            var dx = (newPivotX - oldPivotX) * element.Width * element.ScaleX;
            var dy = (newPivotY - oldPivotY) * element.Height * element.ScaleY;
            return new Point(element.Offset.X + dx, element.Offset.Y + dy);
        }
    }
}