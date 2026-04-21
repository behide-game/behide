using Behide.Types;
using Behide.UI.Controls;
using Godot;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide.Game.Supervisors;

[SceneTree("../../../Prefabs/Supervisors/TabMenu.tscn", root: "nodes")]
public partial class TabMenu : Panel
{
    private readonly ILogger log = Log.CreateLogger("TabMenu");
    private PropHuntSupervisor supervisor = null!;
    [Export] private PackedScene playerListItem = null!;

    private readonly CancellationTokenSource nodeAliveCts = new();
    private CancellationToken NodeAliveCt => nodeAliveCts.Token;

    private Control HunterList => nodes.TabMenu.PlayersWithRole.Players.Hunters.PlayerList;
    private Control PropList => nodes.TabMenu.PlayersWithRole.Players.Props.PlayerList;

    public override void _EnterTree()
    {
        if (GameManager.Supervisor is not PropHuntSupervisor s)
        {
            log.Error("Supervisor is not or PropHuntSupervisor");
            return;
        }
        supervisor = s;

        SetVisible(false);
        supervisor.HuntersChose += AddPlayersToUi;
    }

    public override void _ExitTree() => nodeAliveCts.Cancel();

    public override void _Input(InputEvent rawEvent)
    {
        if (!rawEvent.IsActionPressed(InputActions.GameInfo)) return;
        SetVisible(!Visible);
    }

    private void AddPlayersToUi(object? _, int[] hunterIds)
    {
        foreach (var player in supervisor.Room.Players.Values)
        {
            var node = playerListItem.Instantiate<PlayerListItem>();
            node.Name = player.Value.PeerId.ToString();
            player.Subscribe(p =>
                {
                    node.SetPlayerName(player.Value.Username);
                    node.SetStatus(p.State switch
                    {
                        PlayerStateInGame(true) => "Alive",
                        PlayerStateInGame(false) => "Dead",
                        _ => "Not in game"
                    });
                },
                NodeAliveCt
            );

            if (hunterIds.Contains(player.Value.PeerId))
                HunterList.AddChild(node);
            else
                PropList.AddChild(node);
        }

        // Sort lists
        SortPlayerList(PropList);
        SortPlayerList(HunterList);
    }

    private static void SortPlayerList(Control list)
    {
        var nodes = list.GetChildren()
            .Skip(1)
            .OrderBy(n => n.Name.ToString())
            .ToArray();

        foreach (var n in nodes) {
            list.RemoveChild(n);
            n.SetOwner(null);
        }

        foreach (var n in nodes) list.AddChild(n);
    }
}
