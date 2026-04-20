using Behide.Game;
using Behide.Game.Player;
using Behide.Game.Supervisors;
using Godot;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide.Prefabs.Spectator;

[SceneTree(root: "nodes")]
public partial class Spectator : CharacterBody3D
{
    private readonly ILogger log = Log.CreateLogger("Spectator");

    private bool enabled;
    private Supervisor supervisor = null!;
    [Export] private float speed = 10;
    [Export] private float acceleration = 4;
    [Export] private float deceleration = 9;
    [Export] private float mouseSensitivity = 0.01f;

    private readonly CancellationTokenSource nodeAliceCts = new();
    private CancellationToken NodeAliceCt => nodeAliceCts.Token;
    public override void _ExitTree() => nodeAliceCts.Cancel();

    public void Enable()
    {
        nodes.Camera.MakeCurrent();
        enabled = true;
    }

    public void Disable() => enabled = false;

    public override void _EnterTree()
    {
        if (GameManager.Supervisor is null) log.Error("Supervisor is null");
        else supervisor = GameManager.Supervisor;

        GameManager.Settings.Changed.Subscribe(
            _ => nodes.Camera.Fov = (float)GameManager.Settings.Fov,
            NodeAliceCt
        );
    }

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

    public override void _Process(double delta)
    {
        var focusedObject = nodes.Camera.RayCast.GetCollider();

        // Set player name in HUD
        if (focusedObject is PlayerBody body)
        {
            var owner = supervisor.GetBodyPlayer(body);
            nodes.HUD.Center.PlayerUsername.Text = owner?.Username;
        }
        else
            nodes.HUD.Center.PlayerUsername.Text = string.Empty;
    }

    public override void _PhysicsProcess(double delta)
    {
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
