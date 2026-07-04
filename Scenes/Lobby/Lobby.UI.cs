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
    private Label RoomCode => nodes.UI.HBox.Others.Info.Code.Value.Label;

    private _SceneTree.__0_UI.__1_HBox.__2_Players.__3_ScrollContainer.__4_MarginContainer.__5_Groups Groups =>
        nodes.UI.HBox.Players.ScrollContainer.MarginContainer.Groups;
    private Control HunterList => Groups.Hunters.VBox;
    private Control PropList => Groups.Props.VBox;
    private Control AllPlayerList => Groups.All.VBox;

    private Label ReadyButton => nodes.UI.HBox.Others.Buttons.Ready.MarginContainer.Label;
    private Label RoleButton => nodes.UI.HBox.Others.Buttons.Role.MarginContainer.Label;

    private _SceneTree.__0_UI.__1_HBox.__2_Others.__3_Settings.__4_Margin.__5_VBox.__6_HunterSelection HunterSelection =>
        nodes.UI.HBox.Others.Settings.Margin.VBox.HunterSelection;

    private _SceneTree.__0_UI.__1_HBox.__2_Others.__3_Settings.__4_Margin.__5_VBox.__6_MapSelection MapSelection =>
        nodes.UI.HBox.Others.Settings.Margin.VBox.MapSelection;

    private Label UsernameLabel => nodes.UI.HBox.Players.LocalPlayer.MarginContainer.Label;

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

    private void RemoveLoadedMap()
    {
        if (nodes.Presentation.GetChildCount() > 0)
            nodes.Presentation.RemoveChild(nodes.Presentation.GetChild(0));
    }

    private void LoadMap(GameManager.GameMap map)
    {
        MapSelection.Get().SetVisible(false);

        var mapIdx = GameManager.Maps.IndexOf(map);
        var cachedNode = presentationScenes[mapIdx];

        if (cachedNode is not null)
        {
            nodes.Presentation.AddChild(cachedNode);
            MapSelection.Get().CallThreadSafe(CanvasItem.MethodName.SetVisible, true);
            return;
        }

        var scenePath = presentationScenePaths[mapIdx];
        var err = ResourceLoader.LoadThreadedRequest(scenePath);
        if (err != Error.Ok)
        {
            log.Error("Failed to load map: {Error}", err);
            return;
        }

        var uiNode = nodes.UI.Get();
        var presentationNode = nodes.Presentation;
        Task.Run(() =>
        {
            while (true)
            {
                var status = ResourceLoader.LoadThreadedGetStatus(scenePath);
                if (status == ResourceLoader.ThreadLoadStatus.InProgress) continue;
                if (status == ResourceLoader.ThreadLoadStatus.Loaded)
                {
                    // Retrieve scene
                    var scene = (PackedScene)ResourceLoader.LoadThreadedGet(scenePath);
                    var sceneNode = scene.Instantiate();
                    sceneNode.Name = mapIdx.ToString();

                    // Add scene to tree
                    presentationScenes[mapIdx] = sceneNode;
                    presentationNode.CallThreadSafe(Node.MethodName.AddChild, sceneNode);
                    uiNode.CallThreadSafe(CanvasItem.MethodName.MoveToFront);

                    // Show button
                    MapSelection.Get().CallThreadSafe(CanvasItem.MethodName.SetVisible, true);
                    break;
                }

                log.Error("Failed to load scene: {Status}", status);
                break;
            }
        }, NodeAliveCt);
    }

    private void UpdateMap()
    {
        MapSelection.HBox.Input.Value.Label.Text = room.Configuration.Map switch
        {
            GameManager.GameMap.Dungeon => "Dungeon",
            GameManager.GameMap.Restaurant => "Restaurant",
            _ => throw new Exception("Invalid map")
        };

        RemoveLoadedMap();
        LoadMap(room.Configuration.Map);
    }
}
