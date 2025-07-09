using System;
using Godot;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Behide.Game.Supervisors;
using Types;

/// <summary>
/// It just spawns players
/// </summary>
public partial class BasicSupervisor : Node
{
    private const string Tag = "Supervisor/Basic";
    private readonly Serilog.ILogger log = Serilog.Log.ForContext("Tag", Tag);

    [Export] private InputEventAction openMenuAction = null!;
    [Export] private PackedScene playerPrefab = null!;
    [Export] private NodePath playersNodePath = null!;
    [Export] private NodePath behideObjectNodePath = null!;

    private BehaviorSubject<Player> localPlayer = null!;
    private readonly Dictionary<int, BehaviorSubject<Player>> players = GameManager.Room.Players;

    private MultiplayerSynchronizer localPositionSynchronizer = null!;

    public override void _EnterTree()
    {
        if (GameManager.Room.LocalPlayer is null)
        {
            log.Error("No local player found. It means we are not connected to a room...");
            return;
        }
        localPlayer = GameManager.Room.LocalPlayer;

        // Set authority to first player to join
        int firstPlayerToJoin;
        try
        {
            firstPlayerToJoin = players.Min(kv => kv.Key);
        }
        catch (Exception)
        {
            log.Error("No player found. This means that the room is empty. Strange...");
            _ = GameManager.Room.LeaveRoom();
            GameManager.instance.SetGameState(GameManager.GameState.Home);
            return;
        }
        SetMultiplayerAuthority(firstPlayerToJoin);
    }

    public override void _Input(InputEvent @event)
    {
        if (!@event.IsAction(openMenuAction.Action)) return;
        _ = GameManager.Room.LeaveRoom();
        GameManager.instance.SetGameState(GameManager.GameState.Home);
    }

    protected async Task SpawnPlayers()
    {
        var behideObjects = GetTree().Root.GetNode<Node3D>(behideObjectNodePath);
        var behideObjectParent = behideObjects.GetParent();
        behideObjectParent.RemoveChild(behideObjects);

        var visibleToAllPlayers = SpawnPlayerNodes();
        SetReadyWhenSceneLoaded();

        await visibleToAllPlayers;
        behideObjectParent.AddChild(behideObjects);
    }

    private Task SpawnPlayerNodes()
    {
        var tcs = new TaskCompletionSource();
        var numberOfPlayersWeAreVisibleFor = 0;
        var playersNode = GetNode<Node3D>(playersNodePath);

        foreach (var playerObservable in players.Values)
        {
            var player = playerObservable.Value;

            // Create node
            var playerNode = playerPrefab.Instantiate<Node3D>();
            playerNode.Name = player.PeerId.ToString();
            playerNode.Position = new Vector3(0, 0, player.PeerId * 4);

            // Disable visibility
            if (player.PeerId == localPlayer.Value.PeerId)
            {
                var synchronizer = playerNode.GetNode<MultiplayerSynchronizer>("PositionSynchronizer");
                localPositionSynchronizer = synchronizer;
                synchronizer.SetVisibilityPublic(false);
                synchronizer.VisibilityChanged += _ =>
                {
                    log.Debug("Now visible for {x}/{y}", numberOfPlayersWeAreVisibleFor, players.Count);
                    numberOfPlayersWeAreVisibleFor += 1;
                    if (numberOfPlayersWeAreVisibleFor == players.Count) tcs.SetResult();
                };
            }

            // Add node to the scene
            playersNode.AddChild(playerNode, true);

            // Enable communications when it is ready (to prevent MultiplayerSynchronizer bugs)
            Task.Run(async () =>
            {
                var p = await playerObservable
                    .Where(p => p.State is PlayerStateInGame)
                    .Take(1)
                    .ToTask();

                localPositionSynchronizer.CallDeferred(
                    MultiplayerSynchronizer.MethodName.SetVisibilityFor,
                    p.PeerId,
                    true
                );
            });
        }

        return tcs.Task;
    }

    private void SetReadyWhenSceneLoaded()
    {
        var sceneNode = GetTree().CurrentScene;
        if (sceneNode.IsNodeReady()) GameManager.Room.SetPlayerState(new PlayerStateInGame());
        else sceneNode.Ready += () => GameManager.Room.SetPlayerState(new PlayerStateInGame());
    }
}
