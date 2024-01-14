namespace Behide.Game.Player;

using Godot;

// This script run only on the peer who has the authority.
public partial class InputSynchronizer : MultiplayerSynchronizer
{
    [Export] public Vector2 direction = Vector2.Zero;

    public override void _Ready()
    {
        SetProcess(IsMultiplayerAuthority());
        SetPhysicsProcess(IsMultiplayerAuthority());
    }

    public override void _PhysicsProcess(double delta)
    {
        direction = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
    }
}
