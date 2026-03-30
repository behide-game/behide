using Godot;


namespace Behide.Game.Player;

public abstract partial class PlayerBody : CharacterBody3D
{
    private Serilog.ILogger log = null!;

    protected Node3D CameraDisk = null!;
    protected Camera3D Camera = null!;
    protected Control Hud = null!;
    protected ProgressBar HealthBar = null!;
    public MultiplayerSynchronizer PositionSynchronizer = null!;

    [ExportGroup("Stats")]
    public double Health
    {
        get;
        set
        {
            field = Mathf.Clamp(value, 0L, 100L);
            HealthBar.Value = value;
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
        ProcessPhysics(delta);
        ProcessRotation();
        PropagateCollision();
        MoveAndSlide();
    }
}
