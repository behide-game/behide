using Behide.Game.Supervisors;
using Godot;

namespace Behide.Game.Player;

[SceneTree("prop.tscn")]
public partial class PropBody : PlayerBody
{
    private Node3D currentVisualNode = null!;
    private CollisionShape3D[] collisionNodes = null!;
    private RayCast3D rayCast = null!;
    private PropHuntSupervisor supervisor = null!;

    [ExportGroup("Camera adjust transition")]
    [Export] private float cameraAdjustDuration = 0.4f;
    [Export] private Tween.TransitionType cameraAdjustTransitionType = Tween.TransitionType.Bounce;
    [Export] private Tween.EaseType cameraAdjustEaseType = Tween.EaseType.Out;
    private BehideObject? focusedBehideObject;

    private Vector3 initialCameraPosition = Vector3.Zero;
    private Tween? cameraAdjustTween;

    protected override void InitializeNodes()
    {
        // PlayerBody nodes
        CameraDisk = _.CameraDisk;
        Camera = _.CameraDisk.SpringArm3D.Camera;
        PositionSynchronizer = _.PositionSynchronizer;
        Hud = _.HUD;
        HealthBar = _.HUD.Health.HealthBar;
        HealthLabel = _.HUD.Health.HealthLabel;

        // PropBody nodes
        currentVisualNode = _.MeshInstance3D;
        collisionNodes = [_.CollisionShape3D];
        initialCameraPosition = CameraDisk.Position;
        rayCast = _.CameraDisk.SpringArm3D.Camera.RayCast;
        supervisor = GetNode<PropHuntSupervisor>("/root/multiplayer/Supervisor");

        AdjustProperties();
    }

    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;
        focusedBehideObject = rayCast.GetCollider() as BehideObject;
    }

    public override void _Input(InputEvent rawEvent)
    {
        base._Input(rawEvent);
        if (!IsMultiplayerAuthority()) return;
        if (!Alive) return;
        if (Input.IsActionJustPressed(InputActions.Morph) && focusedBehideObject is not null)
            Rpc(nameof(Morph), focusedBehideObject.GetPath());
    }

    [Rpc(CallLocal = true)]
    private void Morph(NodePath behideObjectPath)
    {
        if (GetNode(behideObjectPath) is not BehideObject behideObject) return;
        if (behideObject.VisualNode.Duplicate() is not Node3D newVisualNode) return;

        // Remove current visual node and collision nodes
        currentVisualNode.QueueFree();
        RemoveChild(currentVisualNode);
        foreach (var collisionNode in collisionNodes)
        {
            collisionNode.QueueFree();
            RemoveChild(collisionNode);
        }

        // Set new visual node
        var initialVisualNodePos = newVisualNode.Position;
        currentVisualNode = newVisualNode;
        newVisualNode.Position = Vector3.Zero;
        AddChild(newVisualNode);

        // Set new collision shapes
        var newCollisionNodes = new List<CollisionShape3D>(behideObject.CollisionNodes.Length);
        foreach (var collisionNode in behideObject.CollisionNodes)
        {
            var newNode = collisionNode.Duplicate();
            if (newNode is not CollisionShape3D newCollisionNode)
            {
                newNode.QueueFree();
                continue;
            }

            newCollisionNode.Position -= initialVisualNodePos;
            AddChild(newCollisionNode);
            newCollisionNodes.Add(newCollisionNode);
        }

        collisionNodes = newCollisionNodes.ToArray();

        // Set mass
        Mass = behideObject.Mass;

        // Set health and camera position
        AdjustProperties();
    }

    private void AdjustProperties()
    {
        var aabb = currentVisualNode is MeshInstance3D meshInstance3D
            ? meshInstance3D.GetAabb()
            : currentVisualNode
                .FindChildren("*", nameof(MeshInstance3D))
                .Select(mi3D => ((MeshInstance3D)mi3D).GetAabb())
                .Aggregate(new Aabb(), (current, aabb) => current.Merge(aabb));

        // Adjust health
        var volume = Math.Round(25 * aabb.Size.X * aabb.Size.Y * aabb.Size.Z);
        var maxHealth = Math.Log(volume, 1.096) + 1;
        MaxHealth = Math.Max(1, (int)maxHealth);

        // Adjust camera position
        var newPosition = initialCameraPosition + aabb.GetCenter();
        var newScale = Math.Max(0.1f, Mathf.Sqrt((aabb.Size.X + aabb.Size.Y + aabb.Size.Z) / 3f));
        var newScaleVector = new Vector3(newScale, newScale, newScale);

        cameraAdjustTween?.Kill();
        cameraAdjustTween = GetTree().CreateTween();
        cameraAdjustTween.SetTrans(cameraAdjustTransitionType);
        cameraAdjustTween.SetEase(cameraAdjustEaseType);

        cameraAdjustTween.TweenProperty(CameraDisk, "position", newPosition, cameraAdjustDuration);
        cameraAdjustTween.Parallel().TweenProperty(CameraDisk, "scale", newScaleVector, cameraAdjustDuration);
    }
}
