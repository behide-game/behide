using Godot;

namespace Behide.Game.Player;

[SceneTree("hunter.tscn")]
public partial class HunterBody : PlayerBody
{
    private Tween? crosshairHitTween;
    private Control CrosshairHit => _.HUD.Center.Crosshair.CrosshairHit;
    private double crosshairHitDuration = 0.3;

    private GodotObject? focusedObject;

    protected override void InitializeNodes()
    {
        CameraDisk = _.Camera;
        Camera = _.Camera;
        PositionSynchronizer = _.PositionSynchronizer;
        Hud = _.HUD;
        HealthBar = _.HUD.Health.HealthBar;
        HealthLabel = _.HUD.Health.HealthLabel;

        MaxHealth = 100;
        MoveSpeed = 1.2f;
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;
        if (!Alive) return;
        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        focusedObject = _.Camera.RayCast.GetCollider();

        // Set player name in HUD
        if (focusedObject is HunterBody hunterBody)
        {
            var owner = Supervisor.GetBodyPlayer(hunterBody);
            _.HUD.Center.PlayerUsername.Text = owner?.Username;
        }
        else
            _.HUD.Center.PlayerUsername.Text = string.Empty;
    }

    public override void _UnhandledInput(InputEvent rawEvent)
    {
        base._UnhandledInput(rawEvent);
        if (!IsMultiplayerAuthority()) return;
        if (!Alive) return;
        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        // Listen shoot
        if (!Input.IsActionJustPressed(InputActions.Shoot)) return;
        if (focusedObject is PropBody player)
        {
            Rpc(MethodName.PlayerHitRpc, player.GetPath());

            crosshairHitTween?.Kill();
            crosshairHitTween = CreateTween();
            crosshairHitTween.SetEase(Tween.EaseType.In);
            crosshairHitTween.TweenProperty(CrosshairHit, "modulate", new Color(0xFFFFFF00), crosshairHitDuration);
            CrosshairHit.Modulate = new Color(0xFFFFFFFF);
        }
        else if (focusedObject is BehideObject)
            Rpc(MethodName.PlayerMissRpc);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void PlayerHitRpc(NodePath playerPath)
    {
        var node = GetNode(playerPath);
        if (node is not PropBody player) return;
        player.DecreaseHealth(this, 10);
    }

    [Rpc(CallLocal = true)]
    private void PlayerMissRpc() => DecreaseHealth(this, 2);
}
