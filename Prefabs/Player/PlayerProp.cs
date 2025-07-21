using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Concurrency;
using Godot;

namespace Behide.Game.Player;

public partial class PlayerProp : PlayerMovements
{
    [Export] private Node3D currentVisualNode = null!;
    [Export] private CollisionShape3D[] collisionNodes = null!;
    [Export] private RayCast3D rayCast = null!;
    private Vector3 initialCameraPosition = Vector3.Zero;
    private Node? focusedBehideObject;

    public override void _EnterTree()
    {
        base._EnterTree();
        initialCameraPosition = CameraDisk.Position;
    }

    public override void _Process(double delta)
    {
        focusedBehideObject = rayCast.GetCollider() as Node;
    }

    public override void _Input(InputEvent rawEvent)
    {
        base._Input(rawEvent);
        if (Input.IsActionJustPressed("morph") && focusedBehideObject is not null) Rpc(MethodName.Morph,focusedBehideObject.GetPath());
    }

[Rpc(CallLocal = true)]
    private void Morph(NodePath behideObjectPath)
    {
        BehideObject behideObject = GetNode(behideObjectPath) as BehideObject;
        if (behideObject.VisualNode.Duplicate() is not Node3D newVisualNode) return;
        GD.Print(newVisualNode);
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
        var globalAabb = currentVisualNode is MeshInstance3D meshInstance3d
            ? meshInstance3d.GetAabb()
            : currentVisualNode
                .FindChildren("*", nameof(MeshInstance3D))
                .Select(meshInstance3D => ((MeshInstance3D)meshInstance3D).GetAabb())
                .Aggregate(new Aabb(), (current, aabb) => current.Merge(aabb));

        CameraDisk.Position = initialCameraPosition + globalAabb.GetCenter();

        var newScale = Math.Max(0.1f, Mathf.Sqrt((globalAabb.Size.X + globalAabb.Size.Y + globalAabb.Size.Z) / 3f));
        CameraDisk.Scale = new Vector3(newScale, newScale, newScale);
    }
}
