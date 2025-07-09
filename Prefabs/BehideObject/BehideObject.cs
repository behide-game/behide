namespace Behide.Game;

using Godot;

[Tool]
public partial class BehideObject : RigidBody3D
{
    private BehideObjectData? resource;
    [Export] public BehideObjectData? Resource
    {
        get => resource;
        set
        {
            resource = value;
            if (Engine.IsEditorHint())
            {
                if (value is null) return;
                if (!IsNodeReady()) return;
                ReloadResource(value);
            }
        }
    }

    public override void _EnterTree()
    {
        ReloadResource(Resource!);
    }

    private void ReloadResource(BehideObjectData behideObject)
    {
        Mass = behideObject.Mass;
        GetNode<MeshInstance3D>("./MeshInstance3D").Mesh = behideObject.Mesh;
        GetNode<CollisionShape3D>("./CollisionShape3D").Shape = behideObject.Shape;
    }
}
