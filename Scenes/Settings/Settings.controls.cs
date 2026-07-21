using Godot;
using System.Reactive;

namespace Behide.Game;

public enum CrouchMode { Push, Toggle }

public partial class Settings
{
    private _SceneTree.__0_TabContainer.__1_Controls.__2_VBox Controls => nodes.TabContainer.Controls.VBox;

    public double HorizontalSensitivity => Controls.HorizontalSensitivity.Value;
    public double VerticalSensitivity => Controls.VerticalSensitivity.Value;
    public double Fov => Controls.FOV.Value;
    public CrouchMode CrouchMode => Controls.Crouch.OptionButton.Selected switch
    {
        0 => CrouchMode.Push,
        1 => CrouchMode.Toggle,
        _ => 0
    };

    private void ControlsListenSettingsForSaving()
    {
        Controls.HorizontalSensitivity.Changed.Subscribe(_ => Changed.OnNext(Unit.Default));
        Controls.VerticalSensitivity.Changed.Subscribe(_ => Changed.OnNext(Unit.Default));
        Controls.FOV.Changed.Subscribe(_ => Changed.OnNext(Unit.Default));
        Controls.Crouch.OptionButton.ItemSelected += _ => Changed.OnNext(Unit.Default);
    }

    private void ControlsApplyFromConfig(ConfigFile config)
    {
        var hSensi = config.GetValue("Controls", "horizontal-sensitivity", 1).AsDouble();
        var vSensi = config.GetValue("Controls", "vertical-sensitivity", 1).AsDouble();
        var fov = config.GetValue("Controls", "fov", 110).AsDouble();
        var crouchMode = config.GetValue("Controls", "crouch-mode", "push").AsString();
        Controls.HorizontalSensitivity.SetValue(hSensi);
        Controls.VerticalSensitivity.SetValue(vSensi);
        Controls.FOV.SetValue(fov);
        Controls.Crouch.OptionButton.Select(crouchMode switch
        {
            "push" => 0,
            "toggle" => 1,
            _ => 0
        });
    }

    private void ControlsApplyToConfig(ConfigFile config)
    {
        config.SetValue("Controls", "horizontal-sensitivity", HorizontalSensitivity);
        config.SetValue("Controls", "vertical-sensitivity", VerticalSensitivity);
        config.SetValue("Controls", "fov", Fov);
        config.SetValue("Controls", "crouch-mode", Controls.Crouch.OptionButton.Selected switch
        {
            0 => "push",
            1 => "toggle",
            _ => "push"
        });
    }
}
