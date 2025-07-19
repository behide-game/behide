using System.Security.Cryptography.X509Certificates;
using Godot;

namespace Behide.Game.Player;

public partial class PlayerProp : PlayerMovements
{
    private Node3D cameraDisk = null!;
    [Export] public Node3D VisualNode = null!;
    [Export] public CollisionShape3D[] CollisionNodes = null!;
    [Export] private RayCast3D RayCast = null!;
    private BehideObject? focusedBehideObject;

    public override void _EnterTree()
    {
        base._EnterTree();
    }

    public override void _Process(double delta)
    {
        // RayCast
        var ColliderObject = RayCast.GetCollider();
        if (ColliderObject is BehideObject Object) focusedBehideObject = Object;
        else focusedBehideObject = null;
    }

    public override void _Input(InputEvent rawEvent)
    {
        base._Input(rawEvent);

        // Morph
        if (Input.IsActionJustPressed("morph") && focusedBehideObject is not null)
        {
            Node3D? NewMesh = focusedBehideObject.VisualNode.Duplicate() as Node3D;
            if (NewMesh != null)
            {
                VisualNode.QueueFree();
                AddChild(NewMesh);
                NewMesh.Position = Vector3.Zero;
                VisualNode = NewMesh;
            }
        }
    }
}