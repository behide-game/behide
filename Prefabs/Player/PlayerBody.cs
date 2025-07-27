using System.Globalization;
using Godot;


namespace Behide.Game.Player;

public abstract partial class PlayerBody : CharacterBody3D
{
    [Export] public float Mass = 30;

    protected Node3D CameraDisk = null!;
    protected Camera3D Camera = null!;
    protected BoxContainer Hud = null!;
    public MultiplayerSynchronizer PositionSynchronizer = null!;

    [ExportGroup("Camera rotation")]
    [Export] private float maxRotation = Mathf.DegToRad(90);
    [Export] private float verticalSensitivity = 0.005f;
    [Export] private float horizontalSensitivity = 0.005f;

    [ExportGroup("Movements")]
    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();
    [Export] private float jumpAcceleration = 400;
    [Export] private float moveSpeed = 6;
    [Export] private float pushCoefficient = 0.6f;

    private Serilog.ILogger log = null!;
    // Rotation accumulators
    private float rotationX;
    private float rotationY;
    private bool jumping;

    [ExportGroup("Stats")]

    public ProgressBar healthBar = null!;
    [Export] public double health;
    public double Health
    {
        get => health;
        set
        {
            // Set the bar and the text to match the health amount (for debug purpose)
            health = Mathf.Clamp(value, 0L, 100L);
            healthBar.Value = health;
            healthBar.GetNode<Label>("HealthAmountText").Text = health.ToString(CultureInfo.InvariantCulture);
        }
    }

    // --- Initialization ---
    protected abstract void InitializeNodes();
    public override void _EnterTree()
    {
        InitializeNodes();
        log = Serilog.Log.ForContext("Tag", "Player/Movements");


        // Set authority
        var ownerPeerId = int.Parse(Name);
        SetMultiplayerAuthority(ownerPeerId);

        // Set spawn position
        var transform = Transform;
        transform.Origin = new Vector3(0, 1, ownerPeerId * 1.2f);
        Transform = transform;

        // Only when we are the authority
        if (IsMultiplayerAuthority())
        {
            Camera.MakeCurrent();
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
        else Hud.Visible = false;
    }

    // --- Movements ---
    public override void _PhysicsProcess(double delta)
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

        ProcessRotation();
        PropagateCollision();
        MoveAndSlide();
    }

    private void ProcessRotation()
    {
        SetRotation(new Vector3(0, rotationY, 0)); // Left / Right (rotate the whole player)
        CameraDisk.SetRotation(new Vector3(rotationX, 0, 0)); // Up / Down (Rotate camera disk)
    }

    private void PropagateCollision()
    {
        for (var i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            if (collision.GetCollider() is not RigidBody3D rb) continue;
            if (!rb.IsMultiplayerAuthority()) Rpc(nameof(SetObjectAuthority), rb.GetPath());

            var pushDirection = -collision.GetNormal();
            pushDirection.Y = 0; // Remove verticality
            var velocityDiff = Mathf.Max(0, Velocity.Dot(pushDirection) - rb.LinearVelocity.Dot(pushDirection));
            var massRatio = Mass / rb.Mass;
            var massContribution = massRatio >= 1 ? rb.Mass : Mass;
            var impulse = pushDirection * velocityDiff * massContribution * pushCoefficient;
            rb.ApplyImpulse(impulse, collision.GetPosition() - rb.GlobalPosition);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SetObjectAuthority(NodePath nodePath)
    {
        var remoteId = Multiplayer.GetRemoteSenderId();
        GetNode<RigidBody3D>(nodePath).SetMultiplayerAuthority(remoteId);
        log.Debug("Set authority of {NodePath} to {RemoteId}", nodePath, remoteId);
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
