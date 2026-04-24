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
        PositionSynchronizer = _.PositionSynchronizer;
        HealthBar = _.HUD.Health.HealthBar;
        HealthLabel = _.HUD.Health.HealthLabel;

        MaxHealth = 100;
        MoveSpeed = 1.2f;

        Gun.InitializeNodes(this);
        Gun.InitializeProperties();

        Huds = [_.HUD, Gun.Hud];
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;
        if (!Alive) return;
        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        // Listen shoot
        if (Input.IsActionPressed(InputActions.Shoot))
        {
            var Object = Gun.TryShoot();
            Rpc(nameof(ObjectHitRpc), Object, Gun.damagePerAmmo);
            return;
        }

        // Listen reload
        if (!Input.IsActionJustPressed(InputActions.Reload)) return;
        Gun.TryReload();
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    public void ObjectHitRpc(Node3D? Object, int damageAmount)
    {
        switch (Object)
        {
            case BehideObject:
                Rpc(nameof(HunterMissedRpc));
                break;
            case PropBody player:
                player.DecreaseHealth(player, damageAmount);
                break;
            default:
                return;
        }
    }

    [Rpc(CallLocal = true)]
    public void HunterMissedRpc() => DecreaseHealth(this, 2);
}
