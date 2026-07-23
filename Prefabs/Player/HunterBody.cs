using Behide.Prefabs.Player;
using Behide.UI.Controls;
using Godot;

namespace Behide.Game.Player;

[SceneTree("hunter.tscn", traverseInstancedScenes:true)]
public partial class HunterBody : PlayerBody
{
    private Prefabs.Player.WeaponManager WeaponManager => _.Camera.WeaponManager;
    private Weapon Weapon => WeaponManager.ActiveWeapon;
    private CollisionShape3D StandingShape => _.CollisionShapeStanding;
    private MeshInstance3D StandingMesh => _.MeshInstanceStanding;
    private CollisionShape3D CrouchingShape => _.CollisionShapeCrouching;
    private MeshInstance3D CrouchingMesh => _.MeshInstanceCrouching;

    private bool isCrouching;

    protected override Node3D CameraDisk => _.Camera;
    protected override Camera3D Camera => _.Camera;
    protected override RayCast3D RayCast => _.Camera.RayCast;
    protected override Label PlayerUsername => Weapon.PlayerUsernameLabel;
    protected override BezelContainer HealthBar => _.HUD.Lifebar.Mask.HealthBar;
    protected override Label HealthLabel => _.HUD.Lifebar.HealthLabel;
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
        Weapon.Hud.SetVisible(value);
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;
        if (!Alive) return;
        // Show players names
        base._Process(delta);

        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        // Listen primary action
        if (Input.IsActionPressed(InputActions.Primary))
        {
            TryDealDamage(Weapon.PerformPrimaryAction());
        }

        // Listen secondary action
        if (Input.IsActionPressed(InputActions.Secondary))
        {
            TryDealDamage(Weapon.PerformSecondaryAction());
        }

        // Listen reload
        if (Input.IsActionJustPressed(InputActions.Reload)) Weapon.PerformReloadAction();

        // Listen crouch
        var crouchMode = GameManager.Settings.CrouchMode;
        if(crouchMode == 0)
        {
            var canStandUp = !_.Area3D.Get().HasOverlappingBodies();
            if (Input.IsActionPressed(InputActions.Crouch))
            {
                if (!isCrouching)
                {
                    ToggleCrouch(true);
                }
            }
            else
            {
                if (canStandUp && isCrouching)
                {
                    ToggleCrouch(false);
                }
            }
        }
        else if(crouchMode == 1)
        {
            if (Input.IsActionJustPressed(InputActions.Crouch))
            {
                var canStandUp = !_.Area3D.Get().HasOverlappingBodies();
                if (!isCrouching || canStandUp && isCrouching) ToggleCrouch(!isCrouching);
            }
        }

        // Listen change weapon
        if (Input.IsActionJustPressed(InputActions.ChangeWeapon))
        {
            WeaponManager.Change();
        }

    }

    private void TryDealDamage(Node3D[]? shootObjects)
    {
        if (shootObjects is null) return;
        foreach (var shootObject in shootObjects)
        {
            switch (shootObject)
            {
                case BehideObject:
                    Rpc(nameof(HunterMissedRpc));
                    break;
                case PropBody player:
                    Rpc(nameof(PlayerHitRpc), player.GetPath(), Weapon.Damage);
                    break;
            }
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
