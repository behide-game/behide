namespace Behide.Game.Player;

using Godot;

// This script run on authority peer.
public partial class PlayerMovements : CharacterBody3D
{
    private Node3D cameraDisk = null!;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    // Move speed in m/s
    [Export] private int MoveSpeed = 5;
    // Rotation sensitivities
    [Export] private float MaxRotation = Mathf.DegToRad(90);
    [Export] private float VerticalSensitivity = 0.005f;
    [Export] private float HorizontalSensitivity = 0.005f;
    // Move speed in m/s
    [Export] private float PushForce = 0.1f;

    private Serilog.ILogger Log = null!;


    // --- Initialization ---

    public override void _EnterTree()
    {
        Log = Serilog.Log.ForContext("Tag", "Player/Movements");

        // Set authority
        var ownerPeerId = int.Parse(Name);
        SetMultiplayerAuthority(ownerPeerId);

        // Set spawn position
        var transform = Transform;
        transform.Origin = new Vector3(0, 1, ownerPeerId * 1.2f);
        Transform = transform;

        // Retrieve child node
        cameraDisk = GetNode<Node3D>("./CameraDisk");

        // Only when we are the authority
        if (IsMultiplayerAuthority())
        {
            // Set new camera
            cameraDisk.GetNode<Camera3D>("./Camera").MakeCurrent();

            // Lock mouse
            Input.MouseMode = Input.MouseModeEnum.Captured;
        }
    }


    // --- Movements ---

    public override void _PhysicsProcess(double delta)
    {
        if (!IsMultiplayerAuthority()) return;
        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor()) velocity.Y -= gravity * (float)delta;

        // Add movements
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
        if (direction != Vector3.Zero)
        {
            velocity.X = direction.X * MoveSpeed;
            velocity.Z = direction.Z * MoveSpeed;
        }
        else
        {
            velocity.X = Mathf.MoveToward(Velocity.X, 0, MoveSpeed);
            velocity.Z = Mathf.MoveToward(Velocity.Z, 0, MoveSpeed);
        }

        // Apply new velocity
        Velocity = velocity;
        MoveAndSlide();

        // Propagate collisions
        for (var i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            if (collision.GetCollider() is RigidBody3D rb)
            {
                if (!rb.IsMultiplayerAuthority()) {
                    Rpc(nameof(SetObjectAuthority), rb.GetPath());
                }
                rb.ApplyImpulse(
                    collision.GetNormal() * -PushForce,
                    collision.GetPosition() - rb.GlobalPosition
                );
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SetObjectAuthority(string nodePath)
    {
        var remoteId = Multiplayer.GetRemoteSenderId();
        GetNode<RigidBody3D>(nodePath).SetMultiplayerAuthority(remoteId);
        Log.Debug("Set authority of {NodePath} to {RemoteId}", nodePath, remoteId);
    }


    // --- Rotation ---

    // Run only on the peer who has the authority.
    public override void _Input(InputEvent rawEvent)
    {
        if (!IsMultiplayerAuthority()) return;

        // Escape
        if (rawEvent.IsActionPressed("ui_cancel"))
            Input.MouseMode = Input.MouseMode switch
            {
                Input.MouseModeEnum.Captured => Input.MouseModeEnum.Visible,
                _ => Input.MouseModeEnum.Captured,
            };

        // Rotation
        if (rawEvent is InputEventMouseMotion mouseMotion
            && Input.MouseMode == Input.MouseModeEnum.Captured)
            ProcessRotation(mouseMotion);
    }

    // Rotation accumulators
    private float rotationX = 0f;
    private float rotationY = 0f;

    private void ProcessRotation(InputEventMouseMotion mouseMotion)
    {
        rotationY -= mouseMotion.Relative.X * VerticalSensitivity;
        rotationX -= mouseMotion.Relative.Y * HorizontalSensitivity;
        rotationX = System.Math.Clamp(rotationX, -MaxRotation, MaxRotation);

        // Left / Right (rotate the whole player)
        var transform = Transform;
        transform.Basis = Basis.Identity;
        Transform = transform;

        Rotate(Vector3.Up, rotationY);

        // Up / Down (Rotate camera disk)
        var cameraTransform = cameraDisk.Transform;
        cameraTransform.Basis = Basis.Identity;
        cameraDisk.Transform = cameraTransform;
        cameraDisk.Rotate(Vector3.Right, rotationX);
    }
}
