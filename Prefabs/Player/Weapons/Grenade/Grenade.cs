using Godot;

namespace Behide.Prefabs.Weapons;

[SceneTree(root: "nodes")]
public partial class Grenade : RigidBody3D
{
    public override void _EnterTree()
    {
        BodyEntered += _ => Explode();
    }

    private void Explode()
    {
        QueueFree();
    }
}
