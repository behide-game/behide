using Godot;

namespace Behide.Game.Player;

public abstract partial class PlayerBody
{
    [Export] public float Mass = 30;

    [ExportGroup("Camera rotation")]
    [Export] private float maxRotation = Mathf.DegToRad(90);
    [Export] private float verticalSensitivity = 0.005f;
    [Export] private float horizontalSensitivity = 0.005f;

    [ExportGroup("Movements")]
    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    [Export] private float jumpAcceleration = 400;
    [Export] private float moveSpeed = 6;
    [Export] private float pushCoefficient = 0.6f;

    // Rotation accumulators
    private float rotationX;
    private float rotationY;
    private bool jumping;

    // --- Movements ---
    private void ProcessPhysics(double delta)
    {
        if (!IsMultiplayerAuthority()) return;
        var velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor()) velocity += Vector3.Down * (float)(gravity * delta);
        if (jumping)
        {
            velocity += Vector3.Up * (float)(jumpAcceleration * delta);
            jumping = false;
        }

        // Add movements
        var inputDir = Input.GetVector(InputActions.MoveLeft, InputActions.MoveRight, InputActions.MoveForward, InputActions.MoveBack);
        var direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * moveSpeed;
            velocity.Z = direction.Z * moveSpeed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, moveSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, moveSpeed);
        }

        // Apply new velocity
        Velocity = velocity;
    }

    private void ProcessRotation()
    {
        SetRotation(new Vector3(0, rotationY, 0)); // Left / Right (rotate the whole player)
        CameraDisk.SetRotation(new Vector3(rotationX, 0, 0)); // Up / Down (Rotate camera disk)
    }
    // Run only on the peer who has the authority.
    public override void _Input(InputEvent rawEvent)
    {
        if (!IsMultiplayerAuthority()) return;

        // Escape
        if (rawEvent.IsActionPressed(BuiltinInputActions.UiCancel))
            Input.MouseMode = Input.MouseMode == Input.MouseModeEnum.Captured
                ? Input.MouseModeEnum.Visible
                : Input.MouseModeEnum.Captured;

        // Rotation
        if (rawEvent is InputEventMouseMotion mouseMotion && Input.MouseMode == Input.MouseModeEnum.Captured)
        {
            rotationY -= mouseMotion.Relative.X * verticalSensitivity;
            rotationX -= mouseMotion.Relative.Y * horizontalSensitivity;
            rotationX = System.Math.Clamp(rotationX, -maxRotation, maxRotation);
        }

        // Jump
        if (Input.IsActionJustPressed(InputActions.Jump) && IsOnFloor()) jumping = true;
    }
}
