namespace Behide.Game;

using Godot;

[Tool]
public partial class BehideObject : RigidBody3D
{
    private Resource? _resource;
    [Export] public Resource? resource
    {
        get => _resource;
        set
        {
            _resource = value;
            if (Engine.IsEditorHint())
            {
                if (value is null) return;
                ReloadResource(value);
            }
        }
    }

    public override void _EnterTree()
    {
        if (Engine.IsEditorHint()) return;
        ReloadResource(resource!);
    }

    public void ReloadResource(Resource behideObject)
    {
        var mass = behideObject.Get("Mass").As<float>();
        var mesh = behideObject.Get("Mesh").As<Mesh>();
        var shape = behideObject.Get("Shape").As<Shape3D>();

        Mass = mass;
        GetNode<MeshInstance3D>("./MeshInstance3D").Mesh = mesh;
        GetNode<CollisionShape3D>("./CollisionShape3D").Shape = shape;
    }
}
