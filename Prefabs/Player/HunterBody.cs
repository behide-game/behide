using Godot;

namespace Behide.Game.Player;

[SceneTree("hunter.tscn")]
public partial class HunterBody : PlayerBody
{
    private RayCast3D rayCastView = null!;
    private RayCast3D rayCastGun = null!;
    private const float rayLength = 1000.0f;

    private Tween? crosshairHitTween;
    private Control CrosshairHit => _.HUD.Crosshair.CrosshairHit;
    private double crosshairHitDuration = 0.3;

    protected override void InitializeNodes()
    {
        CameraDisk = _.Camera;
        Camera = _.Camera;
        PositionSynchronizer = _.PositionSynchronizer;
        Hud = _.HUD;
        HealthBar = _.HUD.Health.HealthBar;
        HealthLabel = _.HUD.Health.HealthLabel;

        MaxHealth = 150;
        MoveSpeed = 1.2f;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (!IsMultiplayerAuthority()) return;
        if (!Alive) return;
        if (Input.IsActionJustPressed(InputActions.Shoot))
        {
            var windowSize = GetViewport().GetVisibleRect().Size;
            var spaceState = GetWorld3D().DirectSpaceState;

            var from = Camera.ProjectRayOrigin(windowSize / 2);
            var to = from + Camera.ProjectRayNormal(windowSize / 2) * rayLength;
            const uint mask =
                (uint)LayerNames.Physics3DLayerMask.Players
                | (uint)LayerNames.Physics3DLayerMask.Props;

            var query = PhysicsRayQueryParameters3D.Create(from, to, mask);
            var result = spaceState.IntersectRay(query);

            if (!result.TryGetValue("collider", out var collider)) return;

            if (collider.AsGodotObject() is PropBody player)
            {
                Rpc(MethodName.PlayerHitRpc, player.GetPath());

                crosshairHitTween?.Kill();
                crosshairHitTween = CreateTween();
                crosshairHitTween.SetEase(Tween.EaseType.In);
                crosshairHitTween.TweenProperty(CrosshairHit, "modulate", new Color(0xFFFFFF00), crosshairHitDuration);
                CrosshairHit.Modulate = new Color(0xFFFFFFFF);
            }
            else
                Rpc(MethodName.PlayerMissRpc);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void PlayerHitRpc(NodePath playerPath)
    {
        var node = GetNode(playerPath);
        if (node is not PropBody player) return;
        player.Health -= 10.0 / player.MaxHealth;
    }

    [Rpc(CallLocal = true)]
    private void PlayerMissRpc() => Health -= 2.0 / MaxHealth;
}
