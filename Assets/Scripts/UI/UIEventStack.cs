using UnityEngine;
using UnityEngine.UIElements;

// public enum EventType { info, error }
// public class EventStack : IEnumerable<(DateTimeOffset emittedTime, EventType eventType, string message)>
// {
//     private Stack<(DateTimeOffset emittedTime, EventType eventType, string message)> eventStack;

//     public EventStack()
//     {
//         eventStack = new();
//     }

//     public void Info(string message) => eventStack.Push((DateTimeOffset.Now, EventType.info, message));
//     public void Error(string message) => eventStack.Push((DateTimeOffset.Now, EventType.error, message));
//     public IEnumerator<(DateTimeOffset emittedTime, EventType eventType, string message)> GetEnumerator()
//     {
//         foreach (var evt in eventStack)
//         {
//             yield return evt;
//         }
//     }

//     IEnumerator IEnumerable.GetEnumerator()
//     {
//         return GetEnumerator();
//     }
// }

[RequireComponent(typeof(UIDocument))]
public class UIEventStack : MonoBehaviour
{
    private TextElement uiElement;
    private UIDocument uiDocument;

    void Awake()
    {
        uiDocument = GetComponent<UIDocument>();
        uiElement = uiDocument.rootVisualElement.Query<TextElement>("EventStack");
    }

    public void Info(string message)
    {
        var newText = $"{uiElement.text}\n{message}";
        uiElement.text = newText;
    }

    public void Error(string message)
    {
        var newText = $"{uiElement.text}\n<color=red>{message}</color>";
        uiElement.text = newText;
    }
}