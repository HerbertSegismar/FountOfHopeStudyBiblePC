// Helpers/VisualTreeHelpers.cs
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace FountOfHopeStudyBiblePC.Helpers
{
    public static class VisualTreeHelpers
    {
        public static DependencyObject? GetParent(DependencyObject? node)
        {
            if (node is null) return null;

            if (node is Visual || node is Visual3D)
                return VisualTreeHelper.GetParent(node);

            if (node is FrameworkContentElement fce)
                return fce.Parent;

            if (node is ContentElement ce)
                return ContentOperations.GetParent(ce);

            return null;
        }

        public static bool IsDescendantOf(DependencyObject? element, DependencyObject? ancestor)
        {
            while (element is not null)
            {
                if (ReferenceEquals(element, ancestor)) return true;
                element = GetParent(element);
            }
            return false;
        }
    }
}