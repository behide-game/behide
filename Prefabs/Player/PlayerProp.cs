using Godot;

namespace Behide.Game.Player;

public partial class PlayerProp : PlayerMovements
{
    [Export] public Node3D VisualNode = null!;
    [Export] public CollisionShape3D[] CollisionNodes = null!;
    [Export] private RayCast3D RayCast = null!;
    private BehideObject? focusedBehideObject;

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
            Node Candidate = focusedBehideObject.VisualNode.Duplicate();
            if (Candidate is Node3D)
            {
                Node3D NewMesh = Candidate as Node3D;
                VisualNode.QueueFree();
                RemoveChild(VisualNode);
                AddChild(NewMesh);
                NewMesh.Position = Vector3.Zero;
                VisualNode = NewMesh;
            }
        }
    }
}