namespace Behide.Game.Player;

using Godot;

// This script run on every peers.
public partial class PlayerMovements : CharacterBody3D
{
    private InputSynchronizer input = null!;
    private Node3D cameraDisk = null!;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    public float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

    // Move speed in m/s
    [Export] private const int MoveSpeed = 5;
    // Rotation sensitivities
    [Export] private float MaxRotation = Mathf.DegToRad(90);
    [Export] private const float VerticalSensitivity = 0.005f;
    [Export] private const float HorizontalSensitivity = 0.005f;
    // Move speed in m/s
    [Export] private const float PushForce = 0.1f;


    // --- Initialization ---

    public override void _EnterTree()
    {
        // Set authority
        var ownerPeerId = int.Parse(Name);
        SetMultiplayerAuthority(ownerPeerId);

        // Set spawn position
        var transform = Transform;
        transform.Origin = new Vector3(0, 1, ownerPeerId * 1.2f);
        Transform = transform;

        // Retrieve child node
        input = GetNode<InputSynchronizer>("./InputSynchronizer");
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
        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor()) velocity.Y -= gravity * (float)delta;

        // Add movements
        Vector3 direction = (Transform.Basis * new Vector3(input.direction.X, 0, input.direction.Y)).Normalized();
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
            var collider = collision.GetCollider();

            if (collider is RigidBody3D rb)
                RpcId(
                    rb.GetMultiplayerAuthority(),
                    nameof(PushRigidbody),
                    rb.GetPath(),
                    collision.GetNormal() * -PushForce,
                    collision.GetPosition() - rb.GlobalPosition
                );
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void PushRigidbody(string rbNodePath, Vector3 impulse, Vector3 pos)
    {
        GetNode<RigidBody3D>(rbNodePath).ApplyImpulse(impulse, pos);
    }


    // --- Rotation ---

    // Run only on the peer who has the authority.
    public override void _Input(InputEvent rawEvent)
    {
        if (!IsMultiplayerAuthority()) return;

        if (rawEvent.IsActionPressed("ui_cancel"))
            Input.MouseMode = Input.MouseMode switch
            {
                Input.MouseModeEnum.Captured => Input.MouseModeEnum.Visible,
                _ => Input.MouseModeEnum.Captured,
            };

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
