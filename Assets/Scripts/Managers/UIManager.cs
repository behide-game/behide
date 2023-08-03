using UnityEngine;
using UnityEngine.EventSystems;

public class UIManager : MonoBehaviour
{
    private UIEventStack eventStack = null!;

    void Awake()
    {
        GameManager.instance.scenes.sceneChanged += RefreshUINavigation;
        GameManager.instance.scenes.sceneChanged += RefreshEventStack;
        RefreshEventStack();
    }

    private void RefreshEventStack()
    {
        var uiGameObject = GameObject.Find("UI");
        if (uiGameObject != null) eventStack = uiGameObject.GetComponent<UIEventStack>();
    }
    private void RefreshUINavigation()
    {
        var panelSetting = GameObject.Find("PanelSettings");
        if (panelSetting != null) EventSystem.current.SetSelectedGameObject(panelSetting);
    }

    public void LogInfo(string message) => eventStack?.Info(message);
    public void LogError(string message) => eventStack?.Error(message);
    public Result LogFail(string message)
    {
        LogError(message);
        return Result.Fail(message);
    }
}