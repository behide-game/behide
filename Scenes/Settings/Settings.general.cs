using Godot;
using Unit = System.Reactive.Unit;

namespace Behide.Game;

public partial class Settings
{
    private _SceneTree.__0_TabContainer.__1_General.__2_VBox General => nodes.TabContainer.General.VBox;

    public string? GetUsername()
    {
        var lineEditText = General.Username.LineEdit.Text;
        return string.IsNullOrWhiteSpace(lineEditText)
            ? null
            : lineEditText;
    }

    private void GeneralListenSettingsForSaving()
    {
        General.Username.LineEdit.TextChanged += _ => Changed.OnNext(Unit.Default);
    }

    private void GeneralApplyFromConfig(ConfigFile config)
    {
        try
        {
            var username = config.GetValue("User", "username").AsString();
            General.Username.LineEdit.Text = username;
        }
        catch (Exception) { /* ignored */ }
    }

    private void GeneralApplyToConfig(ConfigFile config)
    {
        config.SetValue("User", "username", GetUsername() ?? string.Empty);
    }
}
