using Godot;
using System.Reactive;

namespace Behide.Game;

public partial class Settings
{
    private _SceneTree.__0_TabContainer.__1_General.__2_VBox General => nodes.TabContainer.General.VBox;
    private ColorPickerButton ColorPickerButton => General.Color.ColorPickerButton;

    private Color defaultColor = new(1f, 0.8f, 0f);

    public override void _EnterTree()
    {
        base._EnterTree();
        var colorPicker = ColorPickerButton.GetPicker();
        colorPicker.ColorMode = 0;
        colorPicker.PickerShape = ColorPicker.PickerShapeType.HsvWheel;
        colorPicker.ColorModesVisible = false;
        colorPicker.PresetsVisible = false;
    }

    public string? GetUsername()
    {
        var lineEditText = General.Username.LineEdit.Text;
        return string.IsNullOrWhiteSpace(lineEditText)
            ? null
            : lineEditText;
    }

    public Color GetColor() => ColorPickerButton.Color;

    private void GeneralListenSettingsForSaving()
    {
        General.Username.LineEdit.TextChanged += _ => Changed.OnNext(Unit.Default);
        General.Color.ColorPickerButton.ColorChanged += _ => {
            Changed.OnNext(Unit.Default);
            var color = General.Color.ColorPickerButton.Color;
            var room = GameManager.Room.Room;
            room?.SetPlayerColor(color);
        };
    }

    private void GeneralApplyFromConfig(ConfigFile config)
    {
        try
        {
            var username = config.GetValue("User", "username").AsString();
            General.Username.LineEdit.Text = username;
        }
        catch (Exception) { /* ignored */ }

        try
        {
            var colorString = config.GetValue("User", "color").AsString();
            var color = Color.FromString(colorString, defaultColor);
            ColorPickerButton.Color = color;
        }
        catch (Exception) { ColorPickerButton.Color = defaultColor; }
    }

    private void GeneralApplyToConfig(ConfigFile config)
    {
        config.SetValue("User", "username", GetUsername() ?? string.Empty);
        config.SetValue("User", "color", GetColor().ToHtml(false));
    }
}
