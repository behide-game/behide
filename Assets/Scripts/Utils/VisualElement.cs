#nullable enable

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.UIElements;

public class VisualElementUtils
{
    public static VisualElement[] FindFocusableChildren(VisualElement root)
    {
        var children = root.Children();

        VisualElement[] currentFocusableElements = children.Where(e => e.focusable).ToArray();
        VisualElement[] currentNotFocusableElements = children.Where(e => !e.focusable).ToArray();
        VisualElement[] otherFocusableElements = currentNotFocusableElements.SelectMany(FindFocusableChildren).ToArray();

        return currentFocusableElements.Concat(otherFocusableElements).ToArray();
    }

    public static VisualElement? FindFirstFocusableChild(VisualElement root)
    {
        var children = root.Children();
        VisualElement? foundElement = null;

        for (int i = 0; i < children.Count() && foundElement == null; i++)
        {
            var child = children.ElementAt(i);

            if (child.focusable)
                foundElement = child;
            else if (child.childCount > 0)
                foundElement = FindFirstFocusableChild(child);
        }

        return foundElement;
    }

    public static VisualElement[] FindAndFilterAllChildren(VisualElement root, Func<VisualElement, bool> predicate)
    {
        List<VisualElement> elements = new();

        foreach (var child in root.Children())
        {
            if (predicate.Invoke(child)) elements.Add(child);
            elements.AddRange(FindAndFilterAllChildren(child, predicate));
        }

        return elements.ToArray();
    }
}