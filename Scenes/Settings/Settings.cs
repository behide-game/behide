using Godot;

namespace Behide.Game;

[SceneTree]
public partial class Settings : VBoxContainer
{
    public double HorizontalSensitivity => _.HorizontalSensitivity.Value;
    public double VerticalSensitivity => _.VerticalSensitivity.Value;
}
