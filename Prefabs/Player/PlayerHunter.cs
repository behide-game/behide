using Godot;
using System.Globalization;
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
        healthBar = _.HealthBar3D;
        if (!IsMultiplayerAuthority()) healthBar.Visible = false;
    }

    public override void _EnterTree()
    {
        base._EnterTree();
        Health = 100;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (Input.IsActionJustPressed("morph"))
        {
            var windowSize = GetViewport().GetVisibleRect().Size;
            var spaceState = GetWorld3D().DirectSpaceState;

            var from = Camera.ProjectRayOrigin(windowSize / 2);
            var to = from + Camera.ProjectRayNormal(windowSize / 2) * rayLength;

            var query = PhysicsRayQueryParameters3D.Create(from, to);
            var result = spaceState.IntersectRay(query);
            //if (result["collider"] is PlayerProp)
            //{
            var collider = result["collider"].As<Node>();
            if (collider is PlayerProp) Rpc(MethodName.healthUpdate, collider.GetPath(), 20);;
        }
    }

    public override void _Input(InputEvent rawEvent)
    {
        base._Input(rawEvent);
        if (Input.IsActionJustPressed("suffer")) Health -= 10;
    }

    [Rpc(CallLocal = true)]
    private void healthUpdate(NodePath playerPath, int healthDealt)
    {
        var player = (PlayerProp)GetNode(playerPath);
        player.Health -= healthDealt;
    }
}
