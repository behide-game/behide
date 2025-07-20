using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Behide.Game.Player;

public partial class PlayerProp : PlayerMovements
{
    [Export] private Node3D currentVisualNode = null!;
    [Export] private CollisionShape3D[] collisionNodes = null!;
    [Export] private RayCast3D rayCast = null!;
    private Vector3 initialCameraPosition = Vector3.Zero;
    private BehideObject? focusedBehideObject;

    public override void _EnterTree()
    {
        base._EnterTree();
        initialCameraPosition = cameraDisk.Position;
    }

    public override void _Process(double delta)
    {
        focusedBehideObject = rayCast.GetCollider() as BehideObject;
    }

    public override void _Input(InputEvent rawEvent)
    {
        base._Input(rawEvent);
        if (Input.IsActionJustPressed("morph") && focusedBehideObject is not null) Morph(focusedBehideObject);
    }

    private void Morph(BehideObject behideObject)
    {
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

        AdjustCameraPosition();
    }

    private void AdjustCameraPosition()
    {
        var aabbs = currentVisualNode
            .FindChildren("*", nameof(MeshInstance3D))
            .Select(meshInstance3D => ((MeshInstance3D)meshInstance3D).GetAabb());

        var globalAabb = aabbs.Aggregate(new Aabb(), (current, aabb) => current.Merge(aabb));
        cameraDisk.Position = initialCameraPosition + globalAabb.GetCenter();
    }
}
