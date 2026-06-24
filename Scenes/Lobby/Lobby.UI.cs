using System.Reactive.Subjects;
using Behide.UI.Controls;
using Godot;

namespace Behide.Game.UI.Lobby;

using Types;

public partial class Lobby
{
    private Control HostPanel => nodes.Lobby.Boxes.LeftPanel.HostPanel;
    private LabelCountdown Countdown => nodes.Countdown;
    private Label RoomCode => nodes.Lobby.Header.Code.Value.Value;

    private Control HunterList => nodes.Lobby.Boxes.LeftPanel.PlayersWithRole.Players.Hunters.PlayerList;
    private Control PropList => nodes.Lobby.Boxes.LeftPanel.PlayersWithRole.Players.Props.PlayerList;
    private Control PlayerList => nodes.Lobby.Boxes.LeftPanel.Players.Players.PlayerList;

    private Label RoleButton => nodes.Lobby.Boxes.Buttons.Role.MarginContainer.Label;
    private Label ReadyButton => nodes.Lobby.Boxes.Buttons.Ready.MarginContainer.Label;

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


    private void ChangePlayerList()
    {
        var playersWithRole = (Control)nodes.Lobby.Boxes.LeftPanel.PlayersWithRole;
        var players = (Control)nodes.Lobby.Boxes.LeftPanel.Players;
        var countLabel = nodes.Lobby.Boxes.LeftPanel.HostPanel.HunterCount.Count.Label;

        playersWithRole.SetVisible(room.Configuration.HunterCount == 0);
        players.SetVisible(room.Configuration.HunterCount != 0);
        countLabel.Text = room.Configuration.HunterCount.ToString();
    }

    private void UpdateRoleButton()
    {
        var config = room.Configuration;
        var peerId = room.LocalPlayer.Value.PeerId;
        RoleButton.Text = config.IsHunter(peerId) ? "Be prop" : "Be hunter";
    }

    private void AddPlayerToUi(BehaviorSubject<Player> player)
    {
        AddPlayerToRolesList(player);

        // Add to global player list
        var node = playerListItemScene.Instantiate<PlayerListItem>();
        node.Name = player.Value.PeerId.ToString();
        player.Subscribe(p =>
            {
                node.SetPlayerName(p.Username);
                node.SetStatus(p.State switch
                {
                    PlayerStateInLobby isReady => isReady.IsReady ? "Ready" : "Not ready",
                    PlayerStateInGame => "In game",
                    _ => "Gone"
                });
            },
            onCompleted: node.QueueFree,
            NodeAliveCt
        );

        PlayerList.AddChild(node);
        SortPlayerList(PlayerList);
    }

    private void AddPlayerToRolesList(BehaviorSubject<Player> player)
    {
        var node = playerListItemScene.Instantiate<PlayerListItem>();
        node.Name = player.Value.PeerId.ToString();

        // Sync ready state
        player.Subscribe(
            p =>
            {
                node.SetPlayerName(p.Username);
                node.SetStatus(p.State switch
                {
                    PlayerStateInLobby isReady => isReady.IsReady ? "Ready" : "Not ready",
                    PlayerStateInGame => "In game",
                    _ => "Gone"
                });
            },
            onCompleted: () =>
            {
                if (room.Configuration.HunterCount > room.Players.Count)
                    room.Configuration.HunterCount -= 1;
                node.QueueFree();
            },
            NodeAliveCt
        );

        // Sync prop/hunter state
        var sub = room.Configuration.Changed.Subscribe(_ => ChangePlayerRole());
        NodeAliveCt.Register(sub.Dispose);
        player.Subscribe(_ => { }, onCompleted: sub.Dispose);

        ChangePlayerRole();
        return;

        void ChangePlayerRole()
        {
            if (room.Configuration.IsHunter(player.Value.PeerId))
            {
                if (node.GetParent() == PropList) PropList.RemoveChild(node);
                if (node.GetParent() == HunterList) return;
                HunterList.AddChild(node);
                SortPlayerList(HunterList);
            }
            else
            {
                if (node.GetParent() == HunterList) HunterList.RemoveChild(node);
                if (node.GetParent() == PropList) return;
                PropList.AddChild(node);
                SortPlayerList(PropList);
            }
        }
    }

    private void ChangeMapName()
    {
        var label = nodes.Lobby.Boxes.LeftPanel.HostPanel.SelectedMap.MapName.Label;
        label.Text = room.Configuration.Map switch
        {
            GameManager.GameMap.Dungeon => "Dungeon",
            GameManager.GameMap.Restaurant => "Restaurant",
            _ => throw new Exception("Invalid map")
        };
    }
}
