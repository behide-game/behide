using System.Linq;
using UnityEngine.UIElements;

class VisualElementUtils
{
    public static VisualElement[] FindFocusableChildren(VisualElement root)
    {
        var children = root.Children();

        VisualElement[] currentFocusableElements = children.Where(e => e.focusable).ToArray();
        VisualElement[] currentNotFocusableElements = children.Where(e => !e.focusable).ToArray();
        VisualElement[] otherFocusableElements = currentNotFocusableElements.SelectMany(FindFocusableChildren).ToArray();

        return currentFocusableElements.Concat(otherFocusableElements).ToArray();
    }
}