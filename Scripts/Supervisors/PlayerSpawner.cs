using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Behide.Game.Player;
using Godot;

namespace Behide.Game.Supervisors;

using Types;

[GlobalClass]
public partial class PlayerSpawner : Node
{
    [Export] public Node PlayersNode = null!;
    [Export] private PackedScene playerPrefab = null!;

    private readonly Serilog.ILogger log = Serilog.Log.ForContext("Tag", "PlayerSpawner");

    private readonly Dictionary<int, BehaviorSubject<Player>> players = GameManager.Room.Players;
    private BehaviorSubject<Player> localPlayer = null!;
    private readonly Dictionary<int, ReplaySubject<Unit>> onPlayerSpawned = new();

    public override void _EnterTree()
    {
        if (GameManager.Room.LocalPlayer is null)
        {
            log.Error("No local player found. It means we are not connected to a room...");
            return;
        }

        localPlayer = GameManager.Room.LocalPlayer;
        foreach (var peerId in GameManager.Room.Players.Keys)
            onPlayerSpawned.Add(peerId, new ReplaySubject<Unit>());

        // Authority is set by the parent node that must be a supervisor
    }

    public void SpawnPlayer(int peerId)
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
        var playerNode = playerPrefab.Instantiate<PlayerBody>();
        playerNode.Name = playerToSpawn.PeerId.ToString();
        playerNode.Position = new Vector3(0, 0, playerToSpawn.PeerId * 4);

        // Add node to the scene
        PlayersNode.AddChild(playerNode, true);

        // Disable visibility if authority
        if (playerToSpawn.PeerId != localPlayer.Value.PeerId) RpcId(playerToSpawn.PeerId, nameof(SpawnedPlayerRpc));

        playerNode.PositionSynchronizer.SetVisibilityPublic(false);

        // Enable visibility when it is ready on others peers (to prevent MultiplayerSynchronizer bugs)
        foreach (var (spawnedOnPeerId, onNodeSpawned) in onPlayerSpawned)
            Task.Run(async () =>
            {
                await onNodeSpawned.Take(1); // Wait the player's node to have spawned

                // Enable visibility
                playerNode.PositionSynchronizer.CallDeferred(
                    MultiplayerSynchronizer.MethodName.SetVisibilityFor,
                    spawnedOnPeerId,
                    true
                );
            });
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SpawnedPlayerRpc() => onPlayerSpawned[Multiplayer.GetRemoteSenderId()].OnNext(Unit.Default);
}
