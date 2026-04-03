using Behide.Game.Supervisors;
using Godot;

namespace Behide.Game.Player;

public abstract partial class PlayerBody : CharacterBody3D
{
    private Serilog.ILogger log = null!;

    protected Node3D CameraDisk = null!;
    protected Camera3D Camera = null!;
    protected Control Hud = null!;
    protected ProgressBar HealthBar = null!;
    private Supervisor supervisor = null!;
    public MultiplayerSynchronizer PositionSynchronizer = null!;

    public double Health
    {
        get;
        set
        {
            field = Mathf.Clamp(value, 0L, 100L);
            HealthBar.Value = value;
            if (field == 0) Died();
        }
    }

    private bool freeze;
    public bool Alive = true;

    private void Died()
    {
        Alive = false;
        supervisor.PlayerDied(this);
        if (IsMultiplayerAuthority()) supervisor.LocalPlayerDied(this);
    }

    public void Freeze()
    {
        freeze = true;
        Hud.Visible = false;
    }

    // --- Initialization ---
    protected abstract void InitializeNodes();
    public override void _EnterTree()
    {
        log = Serilog.Log.ForContext("Tag", "Player/Movements");
        if (GameManager.Supervisor is null) log.Error("Supervisor is null");
        else supervisor = GameManager.Supervisor;

        InitializeNodes();
        Health = 100;

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
