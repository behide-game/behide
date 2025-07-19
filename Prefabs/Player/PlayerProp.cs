using Godot;

namespace Behide.Game.Player;

public partial class PlayerProp : PlayerMovements
{
    [Export] private Node3D currentVisualNode = null!;
    [Export] private CollisionShape3D[] CollisionNodes = null!;
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
            Node candidate = focusedBehideObject.VisualNode.Duplicate();
            if (candidate is Node3D newVisualNode)
            {
                currentVisualNode.QueueFree();
                RemoveChild(currentVisualNode);
                AddChild(newVisualNode);
                newVisualNode.Position = Vector3.Zero;
                currentVisualNode = newVisualNode;
            }
        }
    }
}
