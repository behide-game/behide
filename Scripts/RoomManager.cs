using Godot;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Behide.Types;
using Behide.Networking;
using Behide.OnlineServices.Signaling;
using System.Linq;
using System.Threading;

namespace Behide;

public partial class RoomManager : Node3D
{
    private NetworkManager network = null!;

    /// <summary>
    /// The local player
    /// It's null if the player is not in a room
    /// </summary>
    public BehaviorSubject<Player>? LocalPlayer;

    /// <summary>
    /// The list of players in the room / of the peers connected
    /// </summary>
    public readonly Dictionary<int, BehaviorSubject<Player>> Players = [];

    private readonly Subject<BehaviorSubject<Player>> playerRegistered = new();
    // private readonly Subject<Player> playerLeft = new(); // TODO: Needed ?
    public IObservable<BehaviorSubject<Player>> PlayerRegistered => playerRegistered.AsObservable();
    public IObservable<Player> PlayerStateChanged => Players.Values.Merge();

    private Serilog.ILogger log = null!;

    public override void _EnterTree()
    {
        network = GameManager.Network;
        log = Serilog.Log.ForContext("Tag", "RoomManager");

        Multiplayer.PeerDisconnected += peerId =>
        {
            log.Debug("Player {PeerId} left the room", peerId);
            var player = Players.GetValueOrDefault((int)peerId);

            if (player is not null)
            {
                Players.Remove((int)peerId);
                player.OnCompleted();
            }
        };
    }


    public async Task<RoomId> CreateRoom()
    {
        var roomId = await network.CreateRoom();

        // Register local player
        var playerId = Multiplayer.GetUniqueId();
        var username = $"Player: {playerId}"; // TODO: Ask the player for his username

        var player = new Player(playerId, username, new PlayerStateInLobby(false));
        LocalPlayer = new BehaviorSubject<Player>(player);
        RegisterLocalPlayer();
        StartTimeSynchronization(1, LocalPlayer); // Take him self as clock reference

        return roomId;
    }

    public async Task JoinRoom(RoomId roomId)
    {
        var (peerId, numberOfPlayers) = await network.JoinRoom(roomId);
        var playerId = Multiplayer.GetUniqueId();
        var username = $"Player: {playerId}";
        log.Debug("Joined room as {PlayerId} {PeerId}. Already connected player count: {PlayerCount}", playerId, peerId, Players.Count);

        var player = new Player(playerId, username, new PlayerStateInLobby(false));
        LocalPlayer = new BehaviorSubject<Player>(player);
        RegisterLocalPlayer();

        // Wait for all players to be registered
        // The purpose is to ensure `SyncTime` take the correct peer as clock reference
        await Task.Run(() =>
        {
            while (Players.Count <= numberOfPlayers) { }
            StartTimeSynchronization(5, LocalPlayer);
        });
    }

    public async Task LeaveRoom()
    {
        // Clear players list
        Players.Clear();
        LocalPlayer?.OnCompleted();
        LocalPlayer = null;

        await network.LeaveRoom();
        log.Information("Room leaved");
    }


    /// <summary>
    /// Send the player's info to the other players in the room
    /// </summary>
    private void RegisterLocalPlayer()
    {
        if (LocalPlayer is null)
        {
            log.Warning("Not connected to a room, can't register the player");
            return;
        }

        // -- Register local player on already connected peers
        Rpc(nameof(RegisterPlayerRpc), LocalPlayer.Value);
        foreach (var _ in Multiplayer.GetPeers())
        {
            log.Debug("Registering us with already connected peer: {Username}", LocalPlayer.Value.Username);
        }

        // -- Register local player on future peers
        // Use Multiplayer.MultiplayerPeer instead of to avoid having
        // to unsubscribe from the event when leaving the room
        Multiplayer.MultiplayerPeer.PeerConnected += peerId =>
        {
            log.Debug("New peer connected, registering us with him: {Username}", LocalPlayer.Value.Username);
            RpcId(peerId, nameof(RegisterPlayerRpc), LocalPlayer.Value);
        };
    }

    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer, // Any peer can call this method
        CallLocal = true,               // This method is also called locally
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void RegisterPlayerRpc(Variant playerVariant)
    {
        Player player = playerVariant;
        var obs = new BehaviorSubject<Player>(player);

        Players.Add(player.PeerId, obs);
        playerRegistered.OnNext(obs);
    }

    public void SetPlayerState(PlayerState newState)
    {
        if (LocalPlayer is null)
        {
            log.Warning("Not connected to a room, can't set player state");
            return;
        }

        // Update the player state locally and on the other peers
        Rpc(nameof(SetPlayerStateRpc), newState);
    }

    /// <summary>
    /// Set a player state (the state of the caller)
    /// </summary>
    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer, // Any peer can call this method
        CallLocal = true,               // This method can be called locally
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void SetPlayerStateRpc(Variant newStateVariant)
    {
        PlayerState newState = newStateVariant;
        log.Debug("[RPC] Player {PlayerId} is now in state {State}", Multiplayer.GetRemoteSenderId(), newState);

        var playerId = Multiplayer.GetRemoteSenderId();
        var player = Players.GetValueOrDefault(playerId);

        if (player is null)
        {
            log.Warning("[SetPlayerStateRpc]: Player {PlayerId} not found", playerId);
            return;
        }

        var newPlayer = player.Value with { State = newState };
        player.OnNext(newPlayer);
        if (playerId == LocalPlayer?.Value.PeerId) LocalPlayer.OnNext(newPlayer);
    }


    #region Time Synchronization
    // See https://en.wikipedia.org/wiki/Network_Time_Protocol#Clock_synchronization_algorithm
    private CancellationToken? timeSyncCancellationToken;
    private Subject<long> clockDeltaMeasurements = new();

    /// <summary>
    /// The difference of time between local clock and reference clock
    /// </summary>
    public BehaviorSubject<TimeSpan?> ClockDelta = new(null);

    private void StartTimeSynchronization(int sampleCount, BehaviorSubject<Player> localPlayer)
    {
        // Cancel time synchronization when we quit the room
        var cts = new CancellationTokenSource();
        localPlayer.Subscribe(_ => { }, cts.Cancel);
        timeSyncCancellationToken = cts.Token;

        CallDeferred(nameof(SyncTime), sampleCount);
    }

    private async void SyncTime(int sampleCount)
    {
        if (timeSyncCancellationToken is null)
        {
            log.Error("Time synchronization should have a cancellation token set.");
            return;
        }
        var ct = (CancellationToken)timeSyncCancellationToken;
        // Reset observables
        clockDeltaMeasurements = new Subject<long>();
        ClockDelta = new BehaviorSubject<TimeSpan?>(null);

        // Log when clock delta is updated
        ClockDelta.Subscribe(
            delta => log.Debug("[Time Sync] Clock delta changed: {Delta}", delta),
            ct
        );

        var hostPlayerId = Players.Min(player => player.Key);

        // `clockDeltas` are the duration measured. They are independent
        // When a new delta is emitted we determine the average clock delta with the server.
        // We discard the deltas that differ by more than 1 standard deviation from the median.
        // The purpose is to eliminate the packets that were retransmitted by tcp.
        clockDeltaMeasurements.Scan(
            ([], TimeSpan.Zero),
            ((List<long> prevDeltas, TimeSpan avgDelta) acc, long newDelta) =>
            {
                List<long> deltas = [.. acc.prevDeltas, newDelta];

                // Calculating standard deviation
                var average = deltas.Average();
                var variance = deltas
                    .Select(delta => Math.Pow(delta - average, 2))
                    .Average();
                var standardDeviation = Math.Sqrt(variance);

                // Finding median
                deltas.Sort();
                var (q, r) = Math.DivRem(deltas.Count, 2);
                var median =
                    r == 1
                    ? deltas[q]
                    : (deltas[q] + deltas[q - 1]) / 2;

                // Filtering deltas
                var min = median - standardDeviation;
                var max = median + standardDeviation;
                var averageDelta = deltas
                    .Where(delta => min <= delta && delta <= max)
                    .Average();

                return (deltas, TimeSpan.FromMilliseconds(averageDelta));
            }
        )
        .Subscribe(acc => ClockDelta.OnNext(acc.avgDelta), ct);

        for (var i = 0; i < sampleCount; i++)
        {
            var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (ct.IsCancellationRequested) break;

            RpcId(hostPlayerId, nameof(SyncTimeRpc), time);
            try { await Task.Delay(1000, ct); }
            catch
            {
                // ignored
            }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncTimeRpc(long requestTime)
    {
        var now = DateTimeOffset.Now + (ClockDelta.Value ?? TimeSpan.Zero);
        var nowMs = now.ToUnixTimeMilliseconds();
        RpcId(Multiplayer.GetRemoteSenderId(), nameof(SyncTime2Rpc), requestTime, nowMs);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncTime2Rpc(long requestTime, long receivedTime)
    {
        var currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var clockDelta = (receivedTime - requestTime + receivedTime - currentTime) / 2;
        clockDeltaMeasurements.OnNext(clockDelta);
    }
    #endregion
}
