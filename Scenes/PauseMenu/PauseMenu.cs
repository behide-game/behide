using Godot;
using System;

public partial class PauseMenu : Control
{
    [Export]
    private Control MainButtons;
    [Export]
    private Control Settings;
    [Export]
    private Control TextHSlider;
    [Export]
    private Control TextVSlider;

    private float multiplierHorizontalSensitivity;
    private float multiplierVerticalSensitivity;

	// Called when the node enters the scene tree for the first time.
    public override void _EnterTree()
    {
        base._EnterTree();
        SetVisible(false);
    }

    private void OnECHAPPressed()
	{
        MainButtons.SetVisible(true);
        Settings.SetVisible(false);
	}

    private void OnSettingsPressed()
    {
        MainButtons.SetVisible(false);
        Settings.SetVisible(true);
        GD.PushWarning("Settings pressed");
    }

    private void OnApplyPressed()
    {
        MainButtons.SetVisible(true);
        Settings.SetVisible(false);

        GD.PushWarning("Apply pressed");
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
