using Behide.Prefabs.Spectator;
using Godot;
using Behide.UI.Controls;
using Behide.Logging;

namespace Behide.Game.Supervisors;

[SceneTree("../../../Prefabs/Supervisors/PropHuntSupervisor.tscn", root: "nodes")]
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
    [Export] private PackedScene playerListItem = null!;

    private bool gameFinished;

    private AdvancedLabelCountdown PreGameCountdown => nodes.UI.AdvancedLabelCountdown;
    private LabelCountdown InGameCountdown => nodes.UI.In_game.Countdown;

    private Control PreGameProp => nodes.UI.Pre_gameProp;
    private Control PreGameHunter => nodes.UI.Pre_gameHunter;
    private Control InGame => nodes.UI.In_game;
    private Control EndGame => nodes.UI.End_game;

    private Label IsPropLabel => nodes.UI.In_game.IsProp;
    private Label IsHunterLabel => nodes.UI.In_game.IsHunter;
    private Label PropsWonLabel => nodes.UI.End_game.LeftPanel.Winner.PropsWin;
    private Label HunterWinLabel => nodes.UI.End_game.LeftPanel.Winner.HunterWins;
    private Control TimedOut => nodes.UI.End_game.TimedOut;
    private Control HunterList => nodes.UI.End_game.LeftPanel.Hunters.PlayerList;
    private Control PropList => nodes.UI.End_game.LeftPanel.Props.PlayerList;

    private Spectator Spectator => nodes.Spectator;

    private readonly Serilog.ILogger log = Log.CreateLogger("Supervisor/PropHunt");
    private static readonly TimeSpan preGameDuration = TimeSpan.FromSeconds(30);
    private static readonly TimeSpan inGameDuration = TimeSpan.FromMinutes(7);

    private EventHandler<int[]>? huntersChose;
    private int[]? hunterPeerIds;
}
