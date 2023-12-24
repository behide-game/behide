namespace Behide.Game.Player;

using Godot;

public partial class PlayerMovements : CharacterBody3D
{
    private Node3D camera = null!;

    // Get the gravity from the project settings to be synced with RigidBody nodes.
    private float gravity = ProjectSettings.GetSetting("physics/3d/default_gravity").AsSingle();

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
        if (!IsMultiplayerAuthority())
        {
            SetProcess(false);
            SetPhysicsProcess(false);

            // Delete player's camera
            GetNode<Node3D>("./CameraDisk").QueueFree();
            return;
        }

        // Delete main camera
        Node? cameraNode = GetNodeOrNull("/root/multiplayer/Camera");
        cameraNode.QueueFree();

        // Set new camera
        camera = GetNode<Node3D>("./CameraDisk");

        // Lock mouse
        Input.MouseMode = Input.MouseModeEnum.Captured;
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector3 velocity = Velocity;

        // Add the gravity.
        if (!IsOnFloor()) velocity.Y -= gravity * (float)delta;

        // Get the input direction and handle the movement/deceleration.
        Vector2 inputDir = Input.GetVector("move_left", "move_right", "move_forward", "move_back");
        Vector3 direction = (Transform.Basis * new Vector3(inputDir.X, 0, inputDir.Y)).Normalized();
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
                    collision.GetNormal() * -0.3f,
                    collision.GetPosition() - rb.GlobalPosition
                );
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void PushRigidbody(string rbNodePath, Vector3 impulse, Vector3 pos) =>
        GetNode<RigidBody3D>(rbNodePath).ApplyImpulse(impulse, pos);

    public override void _Input(InputEvent rawEvent)
    {
        if (!IsMultiplayerAuthority()) return;

        if (rawEvent.IsActionPressed("ui_cancel"))
            Input.MouseMode = Input.MouseMode switch
            {
                Input.MouseModeEnum.Captured => Input.MouseModeEnum.Visible,
                _ => Input.MouseModeEnum.Captured,
            };

        if (rawEvent is InputEventMouseMotion mouseMotion) ProcessRotation(mouseMotion);
    }

    private void ProcessRotation(InputEventMouseMotion mouseMotion)
    {
        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        var maxRot = Mathf.DegToRad(90);

        rotationY -= mouseMotion.Relative.X * verticalSensitivity;
        rotationX -= mouseMotion.Relative.Y * horizontalSensitivity;
        rotationX = System.Math.Clamp(rotationX, -maxRot, maxRot);

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
