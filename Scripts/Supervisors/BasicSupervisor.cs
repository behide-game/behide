using System;
using Godot;
using System.Linq;
using System.Collections.Generic;
using System.Reactive;
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

    [Export] private string openMenuAction = null!;
    [Export] private PackedScene playerPrefab = null!;
    [Export] private Node playersNode = null!;
    [Export] private Node behideObjects = null!;
    private Node behideObjectsParent = null!;

    private BehaviorSubject<Player> localPlayer = null!;
    private readonly Dictionary<int, BehaviorSubject<Player>> players = GameManager.Room.Players;

    // private MultiplayerSynchronizer localPositionSynchronizer = null!;
    private readonly Dictionary<int, ReplaySubject<Unit>> onPlayerSpawned = new();

    public override void _EnterTree()
    {
        if (GameManager.Room.LocalPlayer is null)
        {
            log.Error("No local player found. It means we are not connected to a room...");
            return;
        }

        // Initialize properties
        localPlayer = GameManager.Room.LocalPlayer;
        foreach (var peerId in GameManager.Room.Players.Keys)
            onPlayerSpawned.Add(peerId, new ReplaySubject<Unit>());

        // Set authority
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

        // Hide behide objects on the authority to prevent bugs
        behideObjects.SetMultiplayerAuthority(firstPlayerToJoin);
        if (IsMultiplayerAuthority())
        {
            behideObjectsParent = behideObjects.GetParent();
            behideObjectsParent.RemoveChild(behideObjects);
        }

        // Wait players to be ready
        Task.Run(async () =>
        {
            var tasks = players.Select(kv => kv.Value
                .Where(p => p.State is PlayerStateInGame)
                .Take(1)
                .ToTask()
            );

            await Task.WhenAll(tasks);
            CallDeferred(nameof(PlayersReady));
            behideObjectsParent.CallDeferred(Node.MethodName.AddChild, behideObjects);
        });

        SetReadyWhenSceneLoaded();
    }

    protected virtual void PlayersReady() {}


    protected void SpawnPlayer(int peerId)
    {
        if (!IsMultiplayerAuthority()) return;
        Rpc(nameof(SpawnPlayerRpc), peerId);
    }

    [Rpc(CallLocal = true)]
    private void SpawnPlayerRpc(int peerIdToSpawn)
    {
        var playerObservable = players[peerIdToSpawn];
        var playerToSpawn = playerObservable.Value;

        // Create node
        var playerNode = playerPrefab.Instantiate<Node3D>();
        playerNode.Name = playerToSpawn.PeerId.ToString();
        playerNode.Position = new Vector3(0, 0, playerToSpawn.PeerId * 4);

        // Add node to the scene
        playersNode.AddChild(playerNode, true);

        // Disable visibility if authority
        if (playerToSpawn.PeerId != localPlayer.Value.PeerId) RpcId(playerToSpawn.PeerId, nameof(SpawnedPlayerRpc));

        var synchronizer = playerNode.GetNode<MultiplayerSynchronizer>("PositionSynchronizer");
        synchronizer.SetVisibilityPublic(false);

        // Enable visibility when it is ready on others peers (to prevent MultiplayerSynchronizer bugs)
        foreach (var (spawnedOnPeerId, onNodeSpawned) in onPlayerSpawned)
            Task.Run(async () =>
            {
                await onNodeSpawned.Take(1); // Wait the player's node to have spawned

                // Enable visibility
                synchronizer.CallDeferred(
                    MultiplayerSynchronizer.MethodName.SetVisibilityFor,
                    spawnedOnPeerId,
                    true
                );
            });
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SpawnedPlayerRpc() => onPlayerSpawned[Multiplayer.GetRemoteSenderId()].OnNext(Unit.Default);


    public override void _Input(InputEvent @event)
    {
        if (!@event.IsAction(openMenuAction, true)) return;
        log.Debug("{0}", @event);
        _ = GameManager.Room.LeaveRoom();
        GameManager.instance.SetGameState(GameManager.GameState.Lobby);
    }

    // protected async Task SpawnPlayers()
    // {
    //     var visibleToAllPlayers = SpawnPlayerNodes();
    //     SetReadyWhenSceneLoaded();
    //
    //     // Show behide objects
    //     await visibleToAllPlayers;
    //     if (IsMultiplayerAuthority()) behideObjectsParent.AddChild(behideObjects);
    // }
    //
    // protected async Task SpawnPlayer(int peerId)
    // {
    //     var playerObservable = players[peerId];
    //     var player = playerObservable.Value;
    //
    //     // Create node
    //     var playerNode = playerPrefab.Instantiate<Node3D>();
    //     playerNode.Name = player.PeerId.ToString();
    //     playerNode.Position = new Vector3(0, 0, player.PeerId * 4);
    //
    //     // Disable visibility if authority
    //     if (player.PeerId == localPlayer.Value.PeerId)
    //     {
    //         var synchronizer = playerNode.GetNode<MultiplayerSynchronizer>("PositionSynchronizer");
    //         synchronizer.SetVisibilityPublic(false);
    //         // synchronizer.VisibilityChanged += _ =>
    //         // {
    //         //     numberOfPlayersWeAreVisibleFor += 1;
    //         //     if (numberOfPlayersWeAreVisibleFor == players.Count) tcs.SetResult();
    //         // };
    //     }
    //
    //     // Add node to the scene
    //     playersNode.AddChild(playerNode, true);
    //
    //     // Enable visibility when it is ready on others peers (to prevent MultiplayerSynchronizer bugs)
    //     Task.Run(async () =>
    //     {
    //         var p = await playerObservable
    //             .Where(p => p.State is PlayerStateInGame)
    //             .Take(1)
    //             .ToTask();
    //
    //         localPositionSynchronizer.CallDeferred(
    //             MultiplayerSynchronizer.MethodName.SetVisibilityFor,
    //             p.PeerId,
    //             true
    //         );
    //     });
    // }

    // private Task SpawnPlayerNodes()
    // {
    //     var tcs = new TaskCompletionSource();
    //     var numberOfPlayersWeAreVisibleFor = 0;
    //
    //     foreach (var playerObservable in players.Values)
    //     {
    //         var player = playerObservable.Value;
    //
    //         // Create node
    //         var playerNode = playerPrefab.Instantiate<Node3D>();
    //         playerNode.Name = player.PeerId.ToString();
    //         playerNode.Position = new Vector3(0, 0, player.PeerId * 4);
    //
    //         // Disable visibility
    //         if (player.PeerId == localPlayer.Value.PeerId)
    //         {
    //             var synchronizer = playerNode.GetNode<MultiplayerSynchronizer>("PositionSynchronizer");
    //             localPositionSynchronizer = synchronizer;
    //             synchronizer.SetVisibilityPublic(false);
    //             synchronizer.VisibilityChanged += _ =>
    //             {
    //                 numberOfPlayersWeAreVisibleFor += 1;
    //                 if (numberOfPlayersWeAreVisibleFor == players.Count) tcs.SetResult();
    //             };
    //         }
    //
    //         // Add node to the scene
    //         playersNode.AddChild(playerNode, true);
    //
    //         // Enable communications when it is ready (to prevent MultiplayerSynchronizer bugs)
    //         Task.Run(async () =>
    //         {
    //             var p = await playerObservable
    //                 .Where(p => p.State is PlayerStateInGame)
    //                 .Take(1)
    //                 .ToTask();
    //
    //             localPositionSynchronizer.CallDeferred(
    //                 MultiplayerSynchronizer.MethodName.SetVisibilityFor,
    //                 p.PeerId,
    //                 true
    //             );
    //         });
    //     }
    //
    //     return tcs.Task;
    // }

    private void SetReadyWhenSceneLoaded()
    {
        var sceneNode = GetTree().CurrentScene;
        if (sceneNode.IsNodeReady()) GameManager.Room.SetPlayerState(new PlayerStateInGame());
        else sceneNode.Ready += () => GameManager.Room.SetPlayerState(new PlayerStateInGame());
    }
}
