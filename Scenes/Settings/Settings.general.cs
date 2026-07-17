using Godot;
using System.Reactive;
using static System.Math;

namespace Behide.Game;

public partial class Settings
{
    private _SceneTree.__0_TabContainer.__1_General.__2_VBox General => nodes.TabContainer.General.VBox;
    private ColorPicker ColorPicker => General.Color.ColorPickerButton.GetPicker();

    public override void _EnterTree()
    {
        base._EnterTree();
        ColorPicker.ColorMode = 0;
        ColorPicker.PickerShape = ColorPicker.PickerShapeType.HsvWheel;
        ColorPicker.ColorModesVisible = false;
        ColorPicker.PresetsVisible = false;
    }

    public string? GetUsername()
    {
        var lineEditText = General.Username.LineEdit.Text;
        return string.IsNullOrWhiteSpace(lineEditText)
            ? null
            : lineEditText;
    }

    public Color GetColor()
    {
        return ColorPicker.Color;
    }

    public string GetColorString()
    {
        var color = GetColor();
        var r = (int) (Clamp(color.R, 0f, 1f)*255f);
        var g = (int) (Clamp(color.G, 0f, 1f)*255f);
        var b = (int) (Clamp(color.B, 0f, 1f)*255f);

        var str = "#" + r.ToString("X2") + g.ToString("X2") + b.ToString("X2");
        return str;
    }

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
            var colorString = config.GetValue("User", "color").AsString()[1..];
            var r = int.Parse(colorString.Substr(0, 2), System.Globalization.NumberStyles.HexNumber);
            var g = int.Parse(colorString.Substr(2, 2), System.Globalization.NumberStyles.HexNumber);
            var b = int.Parse(colorString.Substr(4, 2), System.Globalization.NumberStyles.HexNumber);
            var color = new Color ((float)r/255, (float)g/255, (float)b/255);
            General.Color.ColorPickerButton.Color = color;
        }
        catch (Exception) { General.Color.ColorPickerButton.Color = new Color(1f, 0.8f, 0f); }
    }

    private void GeneralApplyToConfig(ConfigFile config)
    {
        config.SetValue("User", "username", GetUsername() ?? string.Empty);
        config.SetValue("User", "color", GetColorString());
    }
}
