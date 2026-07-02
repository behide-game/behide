using System.Reactive.Subjects;
using Behide.UI.Controls;
using Godot;

namespace Behide.Game.UI.Lobby;

using Types;

internal static class ControlExtensions
{
    extension(Control control)
    {
        public void SortChildren()
        {
            var nodes = control.GetChildren()
                .OrderBy(n => n.Name.ToString())
                .ToArray();

            foreach (var n in nodes) {
                control.RemoveChild(n);
                n.SetOwner(null);
            }

            foreach (var n in nodes) control.AddChild(n);
        }
    }
}

public partial class Lobby
{
    private LabelCountdown Countdown => nodes.Countdown;
    private Label RoomCode => nodes.UI.Others.Info.Code.Value.Label;

    private _SceneTree.__0_UI.__1_Players.__2_ScrollContainer.__3_MarginContainer.__4_Groups Groups =>
        nodes.UI.Players.ScrollContainer.MarginContainer.Groups;
    private Control HunterList => Groups.Hunters.VBox;
    private Control PropList => Groups.Props.VBox;
    private Control AllPlayerList => Groups.All.VBox;

    private Label ReadyButton => nodes.UI.Others.Buttons.Ready.MarginContainer.Label;
    private Label RoleButton => nodes.UI.Others.Buttons.Role.MarginContainer.Label;

    private _SceneTree.__0_UI.__1_Others.__2_Settings.__3_Margin.__4_VBox.__5_HunterSelection HunterSelection =>
        nodes.UI.Others.Settings.Margin.VBox.HunterSelection;

    private Label UsernameLabel => nodes.UI.Players.LocalPlayer.MarginContainer.Label;

    /// <summary>
    /// Switch between the player groups view or the global view
    /// </summary>
    private void ChangePlayerList()
    {
        var showGroups = room.Configuration.HunterCount == 0;
        Groups.All.Get().Visible = !showGroups;
        Groups.Hunters.Get().Visible = showGroups;
        Groups.HuntersDelimitor.Get().Visible = showGroups;
        Groups.Props.Get().Visible = showGroups;
        Groups.PropsDelimitor.Get().Visible = showGroups;

        if (showGroups) RearrangePlayerLists();
    }

    /// <summary>
    /// Place player cards in the correct category
    /// </summary>
    private void RearrangePlayerLists()
    {
        foreach (var child in HunterList.GetChildren())
        {
            if (child is not PlayerCard card) continue;
            card.GetParent().RemoveChild(card);

            if (room.Configuration.IsHunter(card.PeerId))
                HunterList.AddChild(card);
            else
                PropList.AddChild(card);
        }
        foreach (var child in PropList.GetChildren())
        {
            if (child is not PlayerCard card) continue;
            card.GetParent().RemoveChild(card);

            if (room.Configuration.IsHunter(card.PeerId))
                HunterList.AddChild(card);
            else
                PropList.AddChild(card);
        }

        HunterList.SortChildren();
        PropList.SortChildren();
    }

    /// <summary>
    /// Change role button text according to game state
    /// </summary>
    private void UpdateRoleButton()
    {
        var config = room.Configuration;
        var peerId = room.LocalPlayer.Value.PeerId;
        RoleButton.Text = config.IsHunter(peerId) ? "Be prop" : "Be hunter";
    }

    /// <summary>
    /// Set the hunter count input text according to room configuration
    /// </summary>
    private void UpdateHunterCountInput() =>
        HunterSelection.HBox.Input.Value.Label.Text = room.Configuration.HunterCount switch
        {
            0 => "Manual",
            _ => $"Randomly {room.Configuration.HunterCount}"
        };

    private void AddPlayerToUi(BehaviorSubject<Player> player)
    {
        AddPlayerToRolesList(player);

        // Create control
        var card = playerCard.Instantiate<PlayerCard>();
        card.Name = player.Value.PeerId.ToString();
        card.BindPlayer(player);
        card.SetOwner(room.IsPeerOwner(player.Value.PeerId));

        // Add to global player list
        AllPlayerList.AddChild(card);
        AllPlayerList.SortChildren();
    }

    private void AddPlayerToRolesList(BehaviorSubject<Player> player)
    {
        var card = playerCard.Instantiate<PlayerCard>();
        card.Name = player.Value.PeerId.ToString();
        card.BindPlayer(player);
        card.SetOwner(room.IsPeerOwner(player.Value.PeerId));

        PropList.AddChild(card);
        RearrangePlayerLists();
    }

    // private void ChangeMapName()
    // {
    //     var label = nodes.UI.Boxes.LeftPanel.HostPanel.SelectedMap.MapName.Label;
    //     label.Text = room.Configuration.Map switch
    //     {
    //         GameManager.GameMap.Dungeon => "Dungeon",
    //         GameManager.GameMap.Restaurant => "Restaurant",
    //         _ => throw new Exception("Invalid map")
    //     };
    // }
}
