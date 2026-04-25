using Behide.Prefabs.Player;
using Godot;

namespace Behide.Game.Player;

[SceneTree("hunter.tscn")]
public partial class HunterBody : PlayerBody
{
    private SubmachineGun Gun => _.Camera.SubmachineGun;

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
        // Show players names
        base._Process(delta);

        if (!IsMultiplayerAuthority()) return;
        if (!Alive) return;

        // Update gun properties
        Gun.TickGun(delta);

        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        // Listen shoot
        if (Input.IsActionPressed(InputActions.Shoot))
        {
            var objectPath = Gun.TryShoot();
            if (objectPath == null) return;
            Rpc(nameof(ObjectHitRpc), objectPath, Gun.damagePerAmmo);
            return;
        }

        // Listen reload
        if (!Input.IsActionJustPressed(InputActions.Reload)) return;
        Gun.TryReload();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void ObjectHitRpc(NodePath objectPath, int damageAmount)
    {
        switch (GetNode(objectPath))
        {
            case BehideObject:
                Rpc(nameof(HunterMissedRpc));
                break;
            case PropBody player:
                player.DecreaseHealth(player, damageAmount);
                break;
        }
    }

    [Rpc(CallLocal = true)]
    public void HunterMissedRpc() => DecreaseHealth(this, 2);
}
