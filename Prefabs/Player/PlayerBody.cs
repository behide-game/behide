using Behide.Game.Supervisors;
using Godot;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide.Game.Player;

public abstract partial class PlayerBody : CharacterBody3D
{
    private readonly ILogger log = Log.CreateLogger("Player/Movements");

    protected Node3D CameraDisk = null!;
    protected Camera3D Camera = null!;
    protected Control Hud = null!;
    protected ProgressBar HealthBar = null!;
    protected Label HealthLabel = null!;
    private Supervisor supervisor = null!;
    public MultiplayerSynchronizer PositionSynchronizer = null!;

    public double Health
    {
        get;
        set
        {
            field = Mathf.Clamp(value, 0, 1);
            HealthBar.Value = value * 100;
            HealthLabel.Text = ((int)Math.Ceiling(value * MaxHealth)).ToString();
            if (field == 0) Died();
        }
    }

    public int MaxHealth
    {
        get;
        protected set
        {
            field = value;
            HealthLabel.Text = ((int)Math.Ceiling(Health * field)).ToString();
        }
    }

    private bool freeze;
    public bool Alive = true;

    private void Died()
    {
        Alive = false;
        SetVisible(false);
        SetProcessMode(ProcessModeEnum.Disabled); // Disable collisions
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
        if (GameManager.Supervisor is null) log.Error("Supervisor is null");
        else supervisor = GameManager.Supervisor;

        InitializeNodes();
        Health = 1;

        // Set authority
        var ownerPeerId = int.Parse(Name);
        SetMultiplayerAuthority(ownerPeerId);

        // Set spawn position
        var transform = Transform;
        transform.Origin = new Vector3(0, 1, ownerPeerId * 1.2f);
        Transform = transform;

        // Only when we are the authority
        if (IsMultiplayerAuthority()) Camera.MakeCurrent();
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
