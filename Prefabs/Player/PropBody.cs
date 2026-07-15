using Behide.UI.Controls;
using Godot;

namespace Behide.Game.Player;

[SceneTree("prop.tscn", traverseInstancedScenes:true)]
public partial class PropBody : PlayerBody
{
    private Node3D currentVisualNode = null!;
    private CollisionShape3D[] collisionNodes = null!;

    [Export] private float maxKickForce = 17f;
    [Export] private ShaderMaterial outlineMaterial = null!;

    [ExportGroup("Camera adjust transition")]
    [Export] private double cameraAdjustDuration = 0.4;
    [Export] private Tween.TransitionType cameraAdjustTransitionType = Tween.TransitionType.Bounce;
    [Export] private Tween.EaseType cameraAdjustEaseType = Tween.EaseType.Out;

    private Vector3 initialCameraPosition = Vector3.Zero;
    private Tween? cameraAdjustTween;
    private bool outlineEnabled;

    private const float speed = 1.55f;
    private const float slowSpeed = 0.3f;

    protected override Node3D CameraDisk => _.CameraDisk;
    protected override Camera3D Camera => _.CameraDisk.SpringArm3D.Camera;
    protected override RayCast3D RayCast => _.CameraDisk.SpringArm3D.Camera.RayCast;
    protected override BezelContainer HealthBar => _.HUD.BottomLeft.Health.Border.Mask.HealthBar;
    protected override Label HealthLabel => _.HUD.BottomLeft.Health.Border.HealthLabel;
    protected override Label PlayerUsername => _.HUD.Center.PlayerUsername;
    public override MultiplayerSynchronizer PositionSynchronizer => _.PositionSynchronizer;

    public override void _EnterTree()
    {
        base._EnterTree();
        currentVisualNode = _.MeshInstance3D;
        collisionNodes = [_.CollisionShape3D];
        initialCameraPosition = CameraDisk.Position;

        AddOutlineMaterial(currentVisualNode);
        ShowLockedLogo(false);
        AdjustProperties();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        // var time = Time.GetTicksMsec()/1000.0;
        // outlineMaterial.SetShaderParameter("albedo", new Color(
        //     (float)Math.Cos(time*2),
        //     (float)Math.Sin(time),
        //     (float)Math.Cos(time*4),
        //     outlineEnabled ? 1f : 0f
        // ));
    }

    protected override void SetHudsVisibility(bool value) => _.HUD.Get().SetVisible(value);

    public override void _UnhandledInput(InputEvent rawEvent)
    {
        base._UnhandledInput(rawEvent);

        // Toggle outline
        if (Input.IsActionJustPressed(InputActions.ToggleOutline))
            outlineEnabled = !outlineEnabled;

        if (!IsMultiplayerAuthority()) return;
        if (!Alive) return;
        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        // Morph
        if (Input.IsActionJustPressed(InputActions.Morph) && FocusedObject is BehideObject focusedBehideObject)
            Rpc(nameof(Morph), focusedBehideObject.GetPath());

        // Kick
        if (Input.IsActionJustPressed(InputActions.Kick) && FocusedObject is BehideObject objectToKick)
        {
            var kickForce =
                Input.IsActionPressed(InputActions.Slow)
                    ? maxKickForce / 5
                    : maxKickForce;

            var distanceFade = (float)Math.Exp(-GlobalPosition.DistanceTo(FocusedPoint) / 3f);
            var finalForce = kickForce * distanceFade;

            var direction = -Camera.GlobalBasis.Z;
            direction.Y = Math.Max(0, direction.Y);
            direction = direction.Normalized();

            SetObjectAuthorityRpc(objectToKick.GetPath());
            objectToKick.ApplyImpulse(
                finalForce * objectToKick.Mass * direction,
                FocusedPoint - objectToKick.GlobalPosition
            );
        }

        // Adjust speed
        MoveSpeed =
            Input.IsActionPressed(InputActions.Slow)
                ? slowSpeed
                : speed;
    }

    [Rpc(CallLocal = true)]
    private void Morph(NodePath behideObjectPath)
    {
        if (GetNode(behideObjectPath) is not BehideObject behideObject) return;
        if (behideObject.VisualNode.Duplicate() is not Node3D newVisualNode) return;

        // Remove current visual node, outline node and collision nodes
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
        currentVisualNode.Position = Vector3.Zero;
        AddOutlineMaterial(currentVisualNode);
        AddChild(currentVisualNode);


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
                .FindChildren("*", nameof(MeshInstance3D), true, false)
                .Select(mi3D => ((MeshInstance3D)mi3D).GetAabb())
                .Aggregate(new Aabb(), (current, aabb) => current.Merge(aabb));

        // Adjust health
        var volume = aabb.Size.X * aabb.Size.Y * aabb.Size.Z;
        var maxHealth = Math.Log(Math.Round(25 * volume), 1.096);
        MaxHealth = Math.Max(1, (int)maxHealth);

        // Adjust camera position
        var newPosition = initialCameraPosition + aabb.GetCenter();
        var newScale = Math.Max(0.1f, Mathf.Sqrt((aabb.Size.X + aabb.Size.Y + aabb.Size.Z) / 3f));
        var newScaleVector = new Vector3(newScale, newScale, newScale);

        cameraAdjustTween?.Kill();
        cameraAdjustTween = CreateTween();
        cameraAdjustTween.SetTrans(cameraAdjustTransitionType);
        cameraAdjustTween.SetEase(cameraAdjustEaseType);

        cameraAdjustTween.TweenProperty(CameraDisk, "position", newPosition, cameraAdjustDuration);
        cameraAdjustTween.Parallel().TweenProperty(CameraDisk, "scale", newScaleVector, cameraAdjustDuration);
    }

    public void ShowLockedLogo(bool show) => _.HUD.BottomLeft.RotationLockedLabel.SetVisible(show);

    private void AddOutlineMaterial(Node node)
    {
        if (node is MeshInstance3D mesh)
        {
            mesh.MaterialOverlay = outlineMaterial;
            return;
        }

        foreach (var childNode in node.FindChildren("*", nameof(MeshInstance3D), true, false))
        {
            if (childNode is not MeshInstance3D childMesh) continue;
            childMesh.MaterialOverlay = outlineMaterial;
            AddOutlineMaterial(childMesh);
        }
    }
}
