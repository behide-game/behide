using Behide.Game;
using Godot;

namespace Behide.Prefabs.Player;

public abstract partial class Weapon : Node3D
{
    public abstract Control Hud { get; }
    public abstract Label PlayerUsernameLabel { get; }
    public abstract float Damage { get; }

    public override void _EnterTree()
    {

    }

    public abstract Node3D[]? PerformPrimaryAction();
    public abstract Node3D[]? PerformSecondaryAction();
    public abstract void PerformReloadAction();
}
