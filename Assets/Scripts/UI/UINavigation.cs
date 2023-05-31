using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UINavigation : MonoBehaviour
{
    private UIDocument uiAsset;

    void Awake()
    {
        uiAsset = this.GetComponent<UIDocument>();
    }

    void Start()
    {
        EventSystem.current.SetSelectedGameObject(GameObject.Find("PanelSettings"));

        // var firstElement = VisualElementUtils.FindFirstFocusableChild(uiAsset.rootVisualElement);
        // firstElement.Focus();
    }
}
