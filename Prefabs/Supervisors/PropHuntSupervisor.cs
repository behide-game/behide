using Godot;
using Behide.UI.Controls;

namespace Behide.Game.Supervisors;

[SceneTree]
public partial class PropHuntSupervisor : Supervisor
{
    [Export]
    private Node PlayersNode
    {
        get => Spawner.PlayersNode;
        set => Spawner.PlayersNode = value;
    }
    [Export]
    private new Node BehideObjects
    {
        get => ((Supervisor)this).BehideObjects;
        set => ((Supervisor)this).BehideObjects = value;
    }

    private AdvancedLabelCountdown preGameCountdown = null!;
    private LabelCountdown inGameCountdown = null!;

    private Control preGameProp = null!;
    private Control preGameHunter = null!;
    private Control inGame = null!;
    private Control endGame = null!;

    private Label isPropLabel = null!;
    private Label isHunterLabel = null!;

    private Label propsWonLabel = null!;
    private Label hunterWinLabel = null!;

    private readonly Serilog.ILogger log = Serilog.Log.ForContext("Tag", "Supervisor/PropHunt");
    private static readonly TimeSpan preGameDuration = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan inGameDuration = TimeSpan.FromMinutes(5);

    private EventHandler<int>? hunterChosen;
    private int? hunterPeerId;
}
