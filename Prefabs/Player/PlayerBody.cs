using Behide.Game.Supervisors;
using Behide.UI.Controls;
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
    protected abstract BezelContainer HealthBar { get; }
    protected abstract Label HealthLabel { get; }
    public abstract MultiplayerSynchronizer PositionSynchronizer { get; }
    private Supervisor supervisor = null!;

    protected GodotObject? FocusedObject;
    protected Vector3 FocusedPoint;

    private double Health
    {
        get;
        set
        {
            field = Mathf.Clamp(value, 0, 1);
            HealthBar.OffsetTransformPositionRatio = new Vector2((float)field - 1, 0);
            HealthLabel.Text = ((int)Math.Ceiling(field * MaxHealth)).ToString();
        }
    }

    public void DecreaseHealth(PlayerBody damager, int amount)
    {
        Health -= (double)amount / MaxHealth;

        const float greenHueAngle = 2f / 3f * float.Pi;
        const float redHueAngle = 0f;

        HealthBar.Color = Color.FromHsv(
            Mathf.LerpAngle(redHueAngle, greenHueAngle, (float)Health) / (2f * float.Pi),
            1f,
            1f
        );

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
        HealthBar.Color = new Color(0f, 1f, 0f);

        // Set authority
        var ownerPeerId = int.Parse(Name);
        SetMultiplayerAuthority(ownerPeerId);

        // Subscribe to change color
        if(GameManager.Room.Room is null) log.Error("Not in a room");
        else if (GameManager.Room.Room.Players.TryGetValue(GetMultiplayerAuthority(), out var behaviorSubject))
            behaviorSubject.Subscribe(_ => UpdatePlayerProperties(behaviorSubject.Value));

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

        // Ignore PlayerBody
        RayCast.AddException(this);
    }

    // Show players names
    public override void _Process(double delta)
    {
        if (!IsMultiplayerAuthority()) return;

        // Register focused object
        FocusedObject = RayCast.GetCollider();
        if (RayCast.IsColliding())
            FocusedPoint = RayCast.GetCollisionPoint();

        // Manage displayed username
        var canShowUsername = this switch
        {
            PropBody => FocusedObject is HunterBody or PropBody,
            HunterBody => FocusedObject is HunterBody,
            _ => false
        };

        if (canShowUsername)
        {
            var owner = supervisor.GetBodyPlayer((PlayerBody)FocusedObject);
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

    protected virtual void UpdatePlayerProperties(Types.Player player)
    {

    }
}
