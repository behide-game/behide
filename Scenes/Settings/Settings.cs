using Godot;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide.Game;

[SceneTree(root: "nodes")]
public partial class Settings : Control
{
    private readonly ILogger log = Log.CreateLogger("Settings");

    #region SettingAccssors
    private _SceneTree.__0_TabContainer.__1_General.__2_VBox General => nodes.TabContainer.General.VBox;

    public string? GetUsername()
    {
        var lineEditText = General.Username.LineEdit.Text;
        return string.IsNullOrWhiteSpace(lineEditText)
            ? null
            : lineEditText;
    }
    public double HorizontalSensitivity => General.HorizontalSensitivity.Value;
    public double VerticalSensitivity => General.VerticalSensitivity.Value;
    public double Fov => General.FOV.Value;
    #endregion

    public override void _EnterTree()
    {
        LoadConfig();
        SaveConfigOnChanged();

        ListenVideoSettings();
        ListenGraphicsSettings();
    }
}
