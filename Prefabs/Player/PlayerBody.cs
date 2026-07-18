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
    protected abstract HealthBar HealthBar { get; }
    public abstract MultiplayerSynchronizer PositionSynchronizer { get; }
    private Supervisor supervisor = null!;

    protected GodotObject? FocusedObject;
    protected Vector3 FocusedPoint;

    private Color healthMaxColor;

    private double Health
    {
        get;
        set
        {
            field = Mathf.Clamp(value, 0, 1);
            HealthBar.SetHealth(field, MaxHealth);
        }
    }

    protected int MaxHealth
    {
        get;
        set
        {
            field = value;
            HealthBar.SetHealth(Health, field);
        }
    }

    public void DecreaseHealth(PlayerBody damager, int amount)
    {
        Health -= (double)amount / MaxHealth;
        if (Health <= 0) Died(damager);
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
        healthMaxColor = HealthBar.Modulate;

        // Set authority
        var ownerPeerId = int.Parse(Name);
        SetMultiplayerAuthority(ownerPeerId);

        // Subscribe to color changes
        var players = GameManager.Room.Room?.Players;
        if (players is null)
            log.Error("Could not get local player: Not in a room");
        else if (players.TryGetValue(ownerPeerId, out var player))
            player.Subscribe(
                PlayerPropertiesChanged,
                NodeAliveCt
            );

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
    protected virtual void PlayerPropertiesChanged(Types.Player player) { }

    public override void _Input(InputEvent rawEvent)
    {
        if (!IsMultiplayerAuthority()) return;
        if (!rawEvent.IsActionPressed(BuiltinInputActions.UiCancel)) return;
        GameManager.PauseMenu.ToggleMenu();
        GetWindow().SetInputAsHandled();
    }
}
