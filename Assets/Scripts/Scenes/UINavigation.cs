#nullable enable

using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

class VisualElementNavInfo
{
    public VisualElement visualElement;
    public VisualElement? up;
    public VisualElement? down;
    public VisualElement? left;
    public VisualElement? right;

    public VisualElementNavInfo(VisualElement baseVisualElement, VisualElement[] otherElements)
    {
        Vector2 rect = baseVisualElement.worldBound.center;

        var elementToUp = otherElements.Where(e => e.worldBound.center.y <= rect.y).FirstOrDefault();
        var elementToDown = otherElements.Where(e => e.worldBound.center.y >= rect.y).FirstOrDefault();
        var elementToLeft = otherElements.Where(e => e.worldBound.center.x <= rect.x).FirstOrDefault();
        var elementToRight = otherElements.Where(e => e.worldBound.center.x >= rect.x).FirstOrDefault();

        visualElement = baseVisualElement;
        up = elementToUp;
        down = elementToDown;
        left = elementToLeft;
        right = elementToRight;
    }

    public VisualElement? GetElementFromDirection(NavigationMoveEvent e)
    {
        switch (e.direction)
        {
            case NavigationMoveEvent.Direction.Up: return up;
            case NavigationMoveEvent.Direction.Down: return down;
            case NavigationMoveEvent.Direction.Left: return left;
            case NavigationMoveEvent.Direction.Right: return right;
            default: return null;
        }
    }
}

public class UINavigation : MonoBehaviour
{
    private UIDocument? uiAsset;
    private VisualElementNavInfo[] focusableElements = { };
    private int focusedElementIndex;

    private VisualElementNavInfo focusedElement() => focusableElements[focusedElementIndex];
    private void setFocusedElement(VisualElement newElement) => focusedElementIndex = Array.IndexOf(focusableElements.Select(e => e.visualElement.name).ToArray(), newElement.name);

    void Awake()
    {
        uiAsset = this.GetComponent<UIDocument>();
        var children = VisualElementUtils.FindFocusableChildren(uiAsset.rootVisualElement);

        uiAsset.rootVisualElement.RegisterCallback<GeometryChangedEvent>(evt =>
        {
            // Find navigable elements
            focusableElements = children.Select((element, index) => new VisualElementNavInfo(element, children.Where((_, i) => i != index).ToArray())).ToArray();

            // Focus first element
            VisualElementUtils.Focus(focusableElements.First().visualElement);
            focusedElementIndex = 0;

            // Await navigation events
            uiAsset.rootVisualElement.RegisterCallback<NavigationMoveEvent>(moveEvent, TrickleDown.TrickleDown);
        });
    }

    private void moveEvent(NavigationMoveEvent evt)
    {
        if (focusedElement() == null) return;

        // Determine new element
        var nextElement = focusedElement().GetElementFromDirection(evt);
        if (nextElement == null) {
            switch (evt.direction)
            {
                case NavigationMoveEvent.Direction.Up: nextElement = focusableElements.LastOrDefault().visualElement; break;
                case NavigationMoveEvent.Direction.Down: nextElement = focusableElements.FirstOrDefault().visualElement; break;
                case NavigationMoveEvent.Direction.Left: nextElement = focusedElement().right; break;
                case NavigationMoveEvent.Direction.Right: nextElement = focusedElement().left; break;
            }
        }

        // Focus new element
        if (nextElement != null)
        {
            if (nextElement.ElementAt(0) != null) nextElement.ElementAt(0).Focus();
            else nextElement.Focus();
            setFocusedElement(nextElement);
        }

        evt.PreventDefault();
    }
}
