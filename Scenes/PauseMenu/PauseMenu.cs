using Godot;

namespace Behide.Game.UI.PauseMenu;

[SceneTree("Pause_menu.tscn")]
public partial class PauseMenu : Control
{
    private Control BaseMenuUi => _.PauseMenu;
    private Settings SettingsMenuUi => _.SettingsMenu;
    public Settings Settings => SettingsMenuUi;

    private Input.MouseModeEnum mouseModeBefore;

    public override void _EnterTree()
    {
        SwitchToBaseMenu();
        SetVisible(false);
    }

    public new void Show()
    {
        SetVisible(true);
        mouseModeBefore = Input.MouseMode;
        if (Input.MouseMode != Input.MouseModeEnum.Visible)
            Input.MouseMode = Input.MouseModeEnum.Visible;
        MoveToFront();
    }

    public new void Hide()
    {
        SetVisible(false);
        Input.MouseMode = mouseModeBefore;
    }

    private void ToggleMenu()
    {
        if (Visible) Hide();
        else Show();
        SwitchToBaseMenu();
    }

    private void SwitchToBaseMenu()
    {
        BaseMenuUi.SetVisible(true);
        SettingsMenuUi.SetVisible(false);
    }

    public void SwitchToSettingsMenu()
    {
        BaseMenuUi.SetVisible(false);
        SettingsMenuUi.SetVisible(true);
    }

    public override void _Input(InputEvent evt)
    {
        if (evt.IsActionPressed(BuiltinInputActions.UiCancel))
        {
            ToggleMenu();
            GetViewport().SetInputAsHandled();
        }
        else if (evt is InputEventMouseButton e && e.IsPressed() && e.ButtonIndex == MouseButton.Left)
        {
            GetViewport().GuiReleaseFocus();
        }
    }
}
