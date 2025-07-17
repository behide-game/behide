// ReSharper disable ConvertToPrimaryConstructor
#pragma warning disable IDE0290 // Use primary constructor: They cannot be used with ExportAttribute
namespace Behide.Game;

using Godot;

[GlobalClass, Tool]
public partial class BehideObjectData : Resource
{
    [Export] public Mesh Mesh;
    [Export] public Shape3D Shape;
    [Export(PropertyHint.Range, "0.001,1000")] public float Mass;

    public BehideObjectData() : this(new BoxMesh(), new BoxShape3D(), 1) { }
    public BehideObjectData(Mesh mesh, Shape3D shape, float mass)
    {
        Mesh = mesh;
        Shape = shape;
        Mass = mass;
    }
}
