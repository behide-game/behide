#nullable disable
#pragma warning disable IDE0290 // Use primary constructor
namespace Behide.Game;

using Godot;

[GlobalClass, Tool]
public partial class BehideObjectData : Resource
{
    [Export] public Mesh Mesh = new BoxMesh();
    [Export] public Shape3D Shape = new BoxShape3D();
    [Export(PropertyHint.Range, "0.001,1000")] public float Mass = 1;

    public BehideObjectData() : this(new BoxMesh(), new BoxShape3D(), 1) {}
    public BehideObjectData(Mesh mesh, Shape3D shape, float mass)
    {
        Mesh = mesh;
        Shape = shape;
        Mass = mass;
    }
}
