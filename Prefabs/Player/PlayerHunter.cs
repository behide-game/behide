using Godot;

namespace Behide.Game.Player;

[SceneTree("hunter.tscn")]
public partial class PlayerHunter : PlayerBody
{
    private RayCast3D rayCastView = null!;
    private RayCast3D rayCastGun = null!;
    private const float rayLength = 1000.0f;

    protected override void InitializeNodes()
    {
        CameraDisk = _.Camera;
        Camera = _.Camera;
        PositionSynchronizer = _.PositionSynchronizer;
        Hud = _.HUD;
        HealthBar = _.HUD.HealthBar;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Health = 100;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (!IsMultiplayerAuthority()) return;
        if (Input.IsActionJustPressed("morph")) // TODO: Create a new action for firing
        {
            var windowSize = GetViewport().GetVisibleRect().Size;
            var spaceState = GetWorld3D().DirectSpaceState;

            var from = Camera.ProjectRayOrigin(windowSize / 2);
            var to = from + Camera.ProjectRayNormal(windowSize / 2) * rayLength;

            var query = PhysicsRayQueryParameters3D.Create(from, to);
            var result = spaceState.IntersectRay(query);

            if (result.TryGetValue("collider", out var collider)
                && collider.AsGodotObject() is PlayerProp player)
                Rpc(MethodName.PlayerHitRpc, player.GetPath());
            else
                Rpc(MethodName.PlayerMissRpc);
        }
    }

    public override void _Input(InputEvent rawEvent)
    {
        base._Input(rawEvent);
        if (Input.IsActionJustPressed("suffer")) Health -= 10; // TODO: Remove
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void PlayerHitRpc(NodePath playerPath)
    {
        var node = GetNode(playerPath);
        if (node is PlayerProp player) player.Health -= 20;
    }

    [Rpc(CallLocal = true)]
    private void PlayerMissRpc() => Health -= 5;
}
