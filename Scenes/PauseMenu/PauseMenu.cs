using Godot;

namespace Behide.Game.UI.PauseMenu;

[SceneTree("Pause_menu.tscn")]
public partial class PauseMenu : Control
{
    private Control BaseMenuUi => _.PauseMenu;
    private Settings SettingsMenuUi => _.SettingsMenu;
    public Settings Settings => SettingsMenuUi;

    private Input.MouseModeEnum mouseModeBefore;

    public string? GetUsername()
    {
        var lineEditText = _.PauseMenu.Username.LineEdit.Text;
        return string.IsNullOrWhiteSpace(lineEditText)
            ? null
            : lineEditText;
    }

    public override void _EnterTree()
    {
        ShowBaseMenu();
        SetVisible(false);
    }

    public void ToggleMenu()
    {
        if (!SettingsMenuUi.Visible)
        {
            if (Visible)
            {
                SetVisible(false);
                Input.MouseMode = mouseModeBefore;
            }
            else
            {
                SetVisible(true);
                mouseModeBefore = Input.MouseMode;
                if (Input.MouseMode != Input.MouseModeEnum.Visible)
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                MoveToFront();
            }
        }
        ShowBaseMenu();
    }

    private void ShowBaseMenu()
    {
        BaseMenuUi.SetVisible(true);
        SettingsMenuUi.SetVisible(false);
    }

    public void ShowSettingsMenu()
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
    }
}
