using Godot;
using Behide.Prefabs.Player;

[SceneTree("Sword.tscn")]
public partial class Sword : Weapon
{
    public override Control Hud => _.Hud;
    public override Label PlayerUsernameLabel => _.Hud.Center.PlayerUsername;
    public override float Damage => 25f;

    public override Node3D[]? PerformPrimaryAction()
    {
        GD.PushWarning("Primary Sword");
        return null;
    }

    public override Node3D[]? PerformSecondaryAction()
    {
        GD.PushWarning("Secondary Sword");
        return null;
    }

    public override void PerformReloadAction(){}
}
