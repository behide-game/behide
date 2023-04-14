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

    private static bool IsValidateArrow(VisualElement e) => e.name == "ValidateArrow" || e.ElementAt(0)?.name == "ValidateArrowContainer";

    public static void Focus(VisualElement element)
    {
        if (element is TextField || IsValidateArrow(element)) {
            element.ElementAt(0).Focus();
        }
        else
        {
            element.Focus();
        }
    }
}