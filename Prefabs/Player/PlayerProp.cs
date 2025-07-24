using System;
using System.Collections.Generic;
using System.Linq;
using Behide.Game.Supervisors;
using Godot;

namespace Behide.Game.Player;

[SceneTree("prop.tscn")]
public partial class PlayerProp : PlayerBody
{
    private Node3D currentVisualNode = null!;
    private CollisionShape3D[] collisionNodes = null!;
    private RayCast3D rayCast = null!;
    [ExportGroup("Camera adjust transition")]
    [Export] private float cameraAdjustDuration = 0.4f;
    [Export] private Tween.TransitionType cameraAdjustTransitionType = Tween.TransitionType.Bounce;
    [Export] private Tween.EaseType cameraAdjustEaseType = Tween.EaseType.Out;
    private BehideObject? focusedBehideObject;

    private Vector3 initialCameraPosition = Vector3.Zero;
    private Tween? cameraAdjustTween;

    private PropHuntSupervisor supervisor = null!;

    protected override void InitializeNodes()
    {
        CameraDisk = _.CameraDisk;
        Camera = _.CameraDisk.SpringArm3D.Camera;
        PositionSynchronizer = _.PositionSynchronizer;
        healthBar = _.SubViewport.HealthBar3D;
    }

    public override void _EnterTree()
    {
        GD.Print(supervisor);
        base._EnterTree();
        currentVisualNode = _.MeshInstance3D;
        collisionNodes = [_.CollisionShape3D];
        initialCameraPosition = CameraDisk.Position;
        rayCast = _.CameraDisk.SpringArm3D.Camera.RayCast;

        Health = 100;
        supervisor = GetNode<PropHuntSupervisor>("./../../Supervisor");
        GD.Print(Multiplayer.GetUniqueId());
        GD.Print(supervisor.HunterPeerIdToExport);
        if (Multiplayer.GetUniqueId() == supervisor.HunterPeerIdToExport)
        {
            healthBar.Visible = false;
        }
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
        if (Input.IsActionJustPressed(InputActions.Morph) && focusedBehideObject is not null) Rpc(MethodName.Morph, focusedBehideObject.GetPath());
        if (Input.IsActionJustPressed("suffer")) Health -= 10;
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
            if (collisionNode.Duplicate() is not CollisionShape3D newCollisionNode) continue;
            newCollisionNode.Position -= initialVisualNodePos;
            AddChild(newCollisionNode);
            newCollisionNodes.Add(newCollisionNode);
        }

        collisionNodes = newCollisionNodes.ToArray();

        // Set mass
        Mass = behideObject.Mass;

        AdjustCameraPosition();
    }

    private void AdjustCameraPosition()
    {
        var globalAabb = currentVisualNode is MeshInstance3D meshInstance3d
            ? meshInstance3d.GetAabb()
            : currentVisualNode
                .FindChildren("*", nameof(MeshInstance3D))
                .Select(meshInstance3D => ((MeshInstance3D)meshInstance3D).GetAabb())
                .Aggregate(new Aabb(), (current, aabb) => current.Merge(aabb));

        var newPosition = initialCameraPosition + globalAabb.GetCenter();
        var newScale = Math.Max(0.1f, Mathf.Sqrt((globalAabb.Size.X + globalAabb.Size.Y + globalAabb.Size.Z) / 3f));
        var newScaleVector = new Vector3(newScale, newScale, newScale);

        cameraAdjustTween?.Kill();
        cameraAdjustTween = GetTree().CreateTween();
        cameraAdjustTween.SetTrans(cameraAdjustTransitionType);
        cameraAdjustTween.SetEase(cameraAdjustEaseType);

        cameraAdjustTween.TweenProperty(CameraDisk, "position", newPosition, cameraAdjustDuration);
        cameraAdjustTween.Parallel().TweenProperty(CameraDisk, "scale", newScaleVector, cameraAdjustDuration);
    }
}
