using Behide.Game.Supervisors;
using Godot;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide.Game.Player;

public abstract partial class PlayerBody : CharacterBody3D
{
    private readonly ILogger log = Log.CreateLogger("Player/Movements");

    protected abstract Node3D CameraDisk { get; }
    protected abstract Camera3D Camera { get; }
    protected abstract RayCast3D RayCast { get; }
    protected abstract Label PlayerUsername { get; }
    protected abstract ProgressBar HealthBar { get; }
    protected abstract Label HealthLabel { get; }
    public abstract MultiplayerSynchronizer PositionSynchronizer { get; }
    private Supervisor supervisor = null!;

    protected GodotObject? focusedObject;

    private double Health
    {
        get;
        set
        {
            field = Mathf.Clamp(value, 0, 1);
            HealthBar.Value = value * 100;
            HealthLabel.Text = ((int)Math.Ceiling(value * MaxHealth)).ToString();
        }
    }

    public void DecreaseHealth(PlayerBody damager, int amount)
    {
        Health -= (double)amount / MaxHealth;
        if (Health <= 0) Died(damager);
    }

    protected int MaxHealth
    {
        get;
        set
        {
            field = value;
            HealthLabel.Text = ((int)Math.Ceiling(Health * field)).ToString();
        }
    }

    private bool freeze;
    public bool Alive = true;
    private readonly CancellationTokenSource nodeAliveCts = new();
    private CancellationToken NodeAliveCt => nodeAliveCts.Token;
    public override void _ExitTree() => nodeAliveCts.Cancel();

    private void Died(PlayerBody killer)
    {
        Alive = false;
        SetVisible(false);
        SetHudsVisibility(false);
        SetProcessMode(ProcessModeEnum.Disabled); // Disable collisions
        supervisor.PlayerDied(killer, this);
        if (IsMultiplayerAuthority()) supervisor.LocalPlayerDied(this);
    }

    public void Freeze()
    {
        freeze = true;
        SetHudsVisibility(false);
    }

    // --- Initialization ---
    public override void _EnterTree()
    {
        if (GameManager.Supervisor is null) log.Error("Supervisor is null");
        else supervisor = GameManager.Supervisor;

        Health = 1;

        // Set authority
        var ownerPeerId = int.Parse(Name);
        SetMultiplayerAuthority(ownerPeerId);

        // Set spawn position
        var transform = Transform;
        transform.Origin = new Vector3(0, 1, ownerPeerId * 1.2f);
        Transform = transform;

        // Only when we are the authority
        if (!IsMultiplayerAuthority())
        {
            SetHudsVisibility(false);
            return;
        }

        Camera.MakeCurrent();
        Camera.Fov = (float)GameManager.Settings.Fov;
        GameManager.Settings.Changed.Subscribe(
            _ => Camera.Fov = (float)GameManager.Settings.Fov,
            NodeAliveCt
        );
    }

    // Show players names
    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;
        focusedObject = RayCast.GetCollider();

        var canShow = this switch
        {
            PropBody => focusedObject is HunterBody or PropBody,
            HunterBody => focusedObject is HunterBody,
            _ => false
        };

        if (canShow)
        {
            var owner = supervisor.GetBodyPlayer((PlayerBody) focusedObject);
            PlayerUsername.Text = owner?.Username;
        }
        else
            PlayerUsername.Text = string.Empty;
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

    protected abstract void SetHudsVisibility(bool value);
}
