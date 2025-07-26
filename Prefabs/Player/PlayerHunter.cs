using Godot;

namespace Behide.Game.Player;

[SceneTree("hunter.tscn")]
public partial class PlayerHunter : PlayerBody
{
    private RayCast3D rayCastView = null!;
    private RayCast3D rayCastGun = null!;
    private const float rayLength = 1000.0f;
    private Vector2 winSize;

    protected override void InitializeNodes()
    {
        CameraDisk = _.Camera;
        Camera = _.Camera;
        PositionSynchronizer = _.PositionSynchronizer;
        healthBar = _.HealthBar3D;
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
            winSize = GetViewport().GetVisibleRect().Size;
            var spaceState = GetWorld3D().DirectSpaceState;

            var from = Camera.ProjectRayOrigin(winSize / 2);
            var to = from + Camera.ProjectRayNormal(winSize / 2) * rayLength;

            var query = PhysicsRayQueryParameters3D.Create(from, to);
            var result = spaceState.IntersectRay(query);
            //if (result["collider"] is PlayerProp)
            //{
            var collider = result["collider"].As<Node>();
            if (collider is PlayerProp) GD.Print("Hit");
        }
    }

    public override void _Input(InputEvent rawEvent)
    {
        base._Input(rawEvent);
        if (Input.IsActionJustPressed("suffer")) Health -= 10;
    }
}
