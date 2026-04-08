using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Behide.Game.Player;
using Behide.Logging;
using Godot;

namespace Behide.Game.Supervisors;

[GlobalClass]
public partial class PlayerSpawner : Node
{
    [Export] public Node PlayersNode = null!;
    [Export] private PackedScene playerPropPrefab = null!;
    [Export] private PackedScene playerHunterPrefab = null!;

    private readonly Serilog.ILogger log = Log.CreateLogger("PlayerSpawner");

    private Room room = null!;
    private readonly Dictionary<int, ReplaySubject<Unit>> onPlayerSpawned = new();

    public override void _EnterTree()
    {
        if (GameManager.Room.Room is null)
        {
            log.Error("Not in a room");
            return;
        }
        room = GameManager.Room.Room;

        foreach (var peerId in room.Players.Keys)
            onPlayerSpawned.Add(peerId, new ReplaySubject<Unit>());

        // Authority is set by the parent node that must be a supervisor
    }

    public void SpawnPlayer(int peerId, bool isHunter)
    {
        Rpc(nameof(SpawnPlayerRpc), peerId, isHunter);
    }

    [Rpc(CallLocal = true)]
    private void SpawnPlayerRpc(int peerIdToSpawn, bool isHunter)
    {
        var playerObservable = room.Players[peerIdToSpawn];
        var playerToSpawn = playerObservable.Value;

        // Create node
        var playerNode = isHunter
            ? playerHunterPrefab.Instantiate<PlayerBody>()
            : playerPropPrefab.Instantiate<PlayerBody>();

        playerNode.Name = playerToSpawn.PeerId.ToString();
        playerNode.Position = new Vector3(0, 0, playerToSpawn.PeerId * 4);

        // Add node to the scene
        PlayersNode.AddChild(playerNode, true);

        // Disable visibility if authority
        if (playerToSpawn.PeerId != room.LocalPlayer.Value.PeerId)
            RpcId(playerToSpawn.PeerId, nameof(SpawnedPlayerRpc));

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

        if (GameManager.Supervisor == null)
        {
            log.Error("Could not notify player spawned: Supervisor is null");
            return;
        }
        GameManager.Supervisor.PlayerSpawned(playerNode);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void SpawnedPlayerRpc() => onPlayerSpawned[Multiplayer.GetRemoteSenderId()].OnNext(Unit.Default);
}
