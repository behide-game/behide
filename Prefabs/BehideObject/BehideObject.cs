using Godot;

namespace Behide.Game;

public partial class BehideObject : RigidBody3D
{
    [Export] public Node3D VisualNode = null!;
    [Export] public CollisionShape3D[] CollisionNodes = null!;
    [Export] public new float Mass
    {
        get => ((RigidBody3D)this).Mass;
        set => ((RigidBody3D)this).Mass = value;
    }
}
