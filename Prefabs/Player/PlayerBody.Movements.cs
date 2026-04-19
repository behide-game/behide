using Godot;

namespace Behide.Game.Player;

public abstract partial class PlayerBody
{
    [Export] public float Mass = 30;

    [ExportGroup("Camera rotation")]
    [Export] private float maxRotation = Mathf.DegToRad(90);
    private static float HorizontalSensitivity => (float)(0.005 * (GameManager.Settings.HorizontalSensitivity) / 10);
    private static float VerticalSensitivity => (float)(0.005 * GameManager.Settings.VerticalSensitivity);

    [ExportGroup("Movements")]
    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    [Export] private float jumpAcceleration = 400;
    [Export] private float pushCoefficient = 0.6f;
    [Export] protected float BaseMoveSpeed = 6;

    protected float MoveSpeed = 1;

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
        if (!Alive || freeze || Input.MouseMode != Input.MouseModeEnum.Captured)
        {
            Velocity = Vector3.Zero;
            return;
        }

        var inputDir = Input.GetVector(
            InputActions.MoveLeft,
            InputActions.MoveRight,
            InputActions.MoveForward,
            InputActions.MoveBack
        );
        var direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * MoveSpeed * BaseMoveSpeed;
            velocity.Z = direction.Z * MoveSpeed * BaseMoveSpeed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, MoveSpeed * BaseMoveSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, MoveSpeed * BaseMoveSpeed);
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
    public override void _UnhandledInput(InputEvent rawEvent)
    {
        if (!IsMultiplayerAuthority()) return;
        if (!Alive || freeze) return;
        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        // Rotation
        if (rawEvent is InputEventMouseMotion mouseMotion)
        {
            rotationY -= mouseMotion.Relative.X * VerticalSensitivity;
            rotationX -= mouseMotion.Relative.Y * HorizontalSensitivity;
            rotationX = Math.Clamp(rotationX, -maxRotation, maxRotation);
        }

        // Jump
        if (Input.IsActionPressed(InputActions.Jump) && IsOnFloor()) jumping = true;
    }
}
