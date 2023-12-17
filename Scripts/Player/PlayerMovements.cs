namespace Behide.Game.Player;

using Godot;

public partial class PlayerMovements : CharacterBody3D
{
    private Node3D camera = null!;

    // Move speed in m/s
    [Export] private int moveSpeed = 8;
    // Rotation sensitivities
    [Export] private float verticalSensitivity = 0.005f;
    [Export] private float horizontalSensitivity = 0.005f;

    // Rotation accumulators
    private float rotationX = 0f;
    private float rotationY = 0f;

    public override void _EnterTree() => SetMultiplayerAuthority(int.Parse(Name));
    public override void _Ready()
    {
        // Delete main camera
        Node? cameraNode = GetNodeOrNull("/root/multiplayer/Camera");
        cameraNode?.QueueFree();

        // Set camera
        camera = GetNode<Node3D>("./CameraDisk");
        if (!IsMultiplayerAuthority()) camera.QueueFree();
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority())
        {
            MoveAndSlide();
            return;
        }

        var direction = Vector3.Zero;

        // We check for each move input and update the direction accordingly.
        if (Input.IsActionPressed("move_right")) direction.X += 1.0f;
        if (Input.IsActionPressed("move_left")) direction.X -= 1.0f;
        if (Input.IsActionPressed("move_back")) direction.Z += 1.0f;
        if (Input.IsActionPressed("move_forward")) direction.Z -= 1.0f;

        if (direction != Vector3.Zero) direction = direction.Normalized().Rotated(Vector3.Up, rotationY);
        Velocity = direction * moveSpeed;

        MoveAndSlide();
    }

    public override void _Input(InputEvent rawEvent)
    {
        if (!IsMultiplayerAuthority()) return;
        if (rawEvent is not InputEventMouseMotion mouseMotion) return;

        rotationY -= mouseMotion.Relative.X * verticalSensitivity;
        rotationX -= mouseMotion.Relative.Y * horizontalSensitivity;
        rotationX = System.Math.Clamp(rotationX, -1, 1);

        // Left / Right (rotate the whole player)
        var transform = Transform;
        transform.Basis = Basis.Identity;
        Transform = transform;

        Rotate(Vector3.Up, rotationY);

        // Up / Down (Rotate camera disk)
        var cameraTransform = camera.Transform;
        cameraTransform.Basis = Basis.Identity;
        camera.Transform = cameraTransform;
        camera.Rotate(Vector3.Right, rotationX);
    }
}
