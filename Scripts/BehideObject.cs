#nullable disable
namespace Behide.Game;

using Godot;

public partial class BehideObject : RigidBody3D
{
    [Export] private BehideObjectData behideObject;

    public override void _Ready()
    {
        // Set mass
        Mass = behideObject.Mass;

        // Set visual mesh
        GetNode<MeshInstance3D>("./MeshInstance3D").Mesh = behideObject.Mesh;

        // // Set collision shape
        var collisionShapeNode = GetNode<CollisionShape3D>("./CollisionShape3D");
        // collisionShapeNode.Shape = behideObject.Shape;
        collisionShapeNode.Disabled = false;
    }
}
