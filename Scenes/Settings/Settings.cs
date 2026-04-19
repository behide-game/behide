using Godot;

namespace Behide.Game;

[SceneTree]
public partial class Settings : VBoxContainer
{
    public double HorizontalSensitivity => _.HorizontalSensitivity.Controls.Slider.Value;
    public double VerticalSensitivity => _.VerticalSensitivity.Controls.Slider.Value;
}
