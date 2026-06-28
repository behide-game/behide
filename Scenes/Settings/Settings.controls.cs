using Godot;
using System.Reactive;

namespace Behide.Game;

public partial class Settings
{
    private _SceneTree.__0_TabContainer.__1_Controls.__2_VBox Controls => nodes.TabContainer.Controls.VBox;

    public double HorizontalSensitivity => Controls.HorizontalSensitivity.Value;
    public double VerticalSensitivity => Controls.VerticalSensitivity.Value;
    public double Fov => Controls.FOV.Value;

    private void ControlsListenSettingsForSaving()
    {
        Controls.HorizontalSensitivity.Changed.Subscribe(_ => Changed.OnNext(Unit.Default));
        Controls.VerticalSensitivity.Changed.Subscribe(_ => Changed.OnNext(Unit.Default));
        Controls.FOV.Changed.Subscribe(_ => Changed.OnNext(Unit.Default));
    }

    private void ControlsApplyFromConfig(ConfigFile config)
    {
        var hSensi = (double)config.GetValue("Controls", "horizontal-sensitivity", 1);
        var vSensi = (double)config.GetValue("Controls", "vertical-sensitivity", 1);
        var fov = (double)config.GetValue("Controls", "fov", 110);
        Controls.HorizontalSensitivity.SetValue(hSensi);
        Controls.VerticalSensitivity.SetValue(vSensi);
        Controls.FOV.SetValue(fov);
    }

    private void ControlsApplyToConfig(ConfigFile config)
    {
        config.SetValue("Controls", "horizontal-sensitivity", HorizontalSensitivity);
        config.SetValue("Controls", "vertical-sensitivity", VerticalSensitivity);
        config.SetValue("Controls", "fov", Fov);
    }
}
