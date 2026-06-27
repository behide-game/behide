using Behide.Prefabs.Player;
using Godot;

namespace Behide.Game.Player;

[SceneTree("hunter.tscn")]
public partial class HunterBody : PlayerBody
{
    private SubmachineGun Gun => _.Camera.SubmachineGun;
    private CollisionShape3D StandingShape => _.CollisionShapeStanding;
    private MeshInstance3D StandingMesh => _.MeshInstanceStanding;
    private CollisionShape3D CrouchingShape => _.CollisionShapeCrouching;
    private MeshInstance3D CrouchingMesh => _.MeshInstanceCrouching;

    private bool isCrouching;

    protected override Node3D CameraDisk => _.Camera;
    protected override Camera3D Camera => _.Camera;
    protected override RayCast3D RayCast => _.Camera.RayCast;
    protected override Label PlayerUsername => Gun.PlayerUsernameLabel;
    protected override ProgressBar HealthBar => _.HUD.Health.HealthBar;
    protected override Label HealthLabel => _.HUD.Health.HealthLabel;
    public override MultiplayerSynchronizer PositionSynchronizer => _.PositionSynchronizer;

    public override void _EnterTree()
    {
        MaxHealth = 100;
        MoveSpeed = 1.2f;
        base._EnterTree();
    }

    protected override void SetHudsVisibility(bool value)
    {
        _.HUD.Get().SetVisible(value);
        Gun.Hud.SetVisible(value);
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;
        if (!Alive) return;

        // Show players names
        base._Process(delta);

        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        // Listen shoot
        if (Input.IsActionPressed(InputActions.Shoot))
        {
            var shootObject = Gun.TryShoot();
            switch (shootObject)
            {
                case BehideObject:
                    Rpc(nameof(HunterMissedRpc));
                    break;
                case PropBody player:
                    Rpc(nameof(PlayerHitRpc), player.GetPath(), Gun.DamagePerAmmo);
                    break;
            }
        }

        // Listen reload
        if (Input.IsActionJustPressed(InputActions.Reload)) Gun.Reload();

        // Listen crouch
        if (Input.IsActionJustPressed(InputActions.Crouch))
        {
            var canStandUp = !_.Area3D.Get().HasOverlappingBodies();
            if (!isCrouching || canStandUp && isCrouching) ToggleCrouch(!isCrouching);
        }
    }

    private void ToggleCrouch(bool wantsToCrouch)
    {
        isCrouching = wantsToCrouch;
        _.Area3D.Get().SetDisableMode(wantsToCrouch ? DisableModeEnum.KeepActive : DisableModeEnum.Remove);

        VisuallyTriggerCrouchRpc(wantsToCrouch);
    }

    [Rpc(CallLocal = true)]
    private void VisuallyTriggerCrouch(bool wantsToCrouch)
    {
        CameraDisk.SetPosition(new Vector3(0, wantsToCrouch ? 0.45f : 1.05f, 0));
        StandingShape.SetDisabled(wantsToCrouch);
        StandingMesh.SetVisible(!wantsToCrouch);
        CrouchingShape.SetDisabled(!wantsToCrouch);
        CrouchingMesh.SetVisible(wantsToCrouch);
    }

    [Rpc(CallLocal = true)]
    public void PlayerHitRpc(NodePath playerPath, int damageAmount)
    {
        var node = GetNode(playerPath);
        if (node is not PropBody player) return;
        player.DecreaseHealth(this, damageAmount);
    }

    [Rpc(CallLocal = true)]
    public void HunterMissedRpc() => DecreaseHealth(this, 2);
}
