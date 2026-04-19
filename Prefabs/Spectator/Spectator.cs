using Behide.Game;
using Godot;

namespace Behide.Prefabs.Spectator;

[SceneTree]
public partial class Spectator : CharacterBody3D
{
    private bool enabled;
    [Export] private float speed = 10;
    [Export] private float acceleration = 4;
    [Export] private float deceleration = 9;
    [Export] private float mouseSensitivity = 0.01f;

    public void Enable()
    {
        _.Camera.MakeCurrent();
        enabled = true;
    }

    public void Disable() => enabled = false;

    public override void _UnhandledInput(InputEvent rawEvent)
    {
        if (!enabled) return;
        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        if (rawEvent is InputEventMouseMotion mouseMotion)
        {
            RotateY(-mouseMotion.Relative.X * mouseSensitivity);

            var rotation = Rotation;
            rotation.X -= mouseMotion.Relative.Y * mouseSensitivity;
            rotation.X = Mathf.Clamp(rotation.X, -1.5f, 1.5f);
            Rotation = rotation;
        }
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        if (!enabled) return;
        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        var horizontalDirection = Input.GetVector(
            InputActions.MoveLeft,
            InputActions.MoveRight,
            InputActions.MoveForward,
            InputActions.MoveBack
        );
        var yInput = Input.GetAxis(BuiltinInputActions.UiDown, InputActions.Jump);

        var direction =
            Basis * new Vector3(
                horizontalDirection.X,
                yInput,
                horizontalDirection.Y
            ).Normalized();

        var targetVelocity = direction * speed;

        if (targetVelocity == Vector3.Zero)
            Velocity = Velocity.Lerp(targetVelocity, (float)(deceleration * delta));
        else
            Velocity = Velocity.Lerp(targetVelocity, (float)(acceleration * delta));

        MoveAndSlide();
    }
}
