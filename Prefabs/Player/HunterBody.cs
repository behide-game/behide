using Behide.Prefabs.Player;
using Godot;

namespace Behide.Game.Player;

[SceneTree("hunter.tscn")]
public partial class HunterBody : PlayerBody
{
    private SubmachineGun Gun => _.Camera.SubmachineGun;
    private CollisionShape3D StandingShape => _.CollisionShapeStanding;
    private MeshInstance3D StandingMesh => _.CollisionShapeStanding;
    private CollisionShape3D CrouchingShape => _.CollisionShapeCrouching;
    private MeshInstance3D CrouchingMesh => _.CollisionShapeCrouching;

    protected override void InitializeNodes()
    {
        CameraDisk = _.Camera;
        Camera = _.Camera;
        RayCast = _.Camera.RayCast;
        PositionSynchronizer = _.PositionSynchronizer;
        HealthBar = _.HUD.Health.HealthBar;
        HealthLabel = _.HUD.Health.HealthLabel;

        MaxHealth = 100;
        MoveSpeed = 1.2f;

        Gun.InitializeNodes(this);
        Gun.InitializeProperties();

        PlayerUsername = Gun.PlayerUsername;
        Huds = [_.HUD, Gun.Hud];
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;
        if (!Alive) return;

        // Show players names
        base._Process(delta);

        // Update gun properties
        Gun.TickGun(delta);

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
                    Rpc(nameof(PlayerHitRpc), player.GetPath(), Gun.damagePerAmmo);
                    break;
            }
        }

        // Listen reload
        if (Input.IsActionJustPressed(InputActions.Reload))
            Gun.TryReload();
        var tryingToCrouch = Input.IsActionPressed(InputActions.Crouch);
        var cantStandUp = StandingShape.get;
        StandingShape.SetDisabled(tryingToCrouch || cantStandUp);
        StandingShape.SetDisabled(tryingToCrouch || cantStandUp);
        CrouchingShape.SetDisabled(!tryingToCrouch || !cantStandUp);
        CrouchingShape.SetDisabled(!tryingToCrouch || !cantStandUp);
        CameraDisk.SetPosition(new Vector3(0, tryingToCrouch ? 0.45f : 1.05f, 0));
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
