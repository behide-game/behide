namespace Behide;

using Godot;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Behide.Types;
using Behide.Networking;
using Behide.OnlineServices.Signaling;
using System.Linq;
using System.Threading;

public partial class RoomManager : Node3D
{
    private NetworkManager network = null!;

    /// <summary>
    /// The local player
    /// It's null if the player is not in a room
    /// </summary>
    public BehaviorSubject<Player>? localPlayer;

    /// <summary>
    /// The list of players in the room / of the peers connected
    /// </summary>
    public readonly Dictionary<int, BehaviorSubject<Player>> players = [];

    private readonly Subject<BehaviorSubject<Player>> playerRegistered = new();
    private readonly Subject<Player> playerLeft = new();
    public IObservable<BehaviorSubject<Player>> PlayerRegistered => playerRegistered.AsObservable();
    public IObservable<Player> PlayerStateChanged => players.Values.Merge();

    private Serilog.ILogger Log = null!;

    public override void _EnterTree()
    {
        network = GameManager.Network;
        Log = Serilog.Log.ForContext("Tag", "RoomManager");

        Multiplayer.PeerDisconnected += peerId =>
        {
            Log.Debug("Player {PeerId} left the room", peerId);
            var player = players.GetValueOrDefault((int)peerId);

            if (player is not null)
            {
                players.Remove((int)peerId);
                player.OnCompleted();
            }
        };
    }


    public async Task<Result<RoomId, string>> CreateRoom()
    {
        var res = await network.CreateRoom();

        if (res.HasValue(out var value))
        {
            // Register local player
            var playerId = Multiplayer.GetUniqueId();
            var username = $"Player: {playerId}"; // TODO: Ask the player for his username

            var player = new Player(playerId, username, new PlayerStateInLobby(false));
            localPlayer = new BehaviorSubject<Player>(player);
            RegisterLocalPlayer();
            StartTimeSynchronization(1, localPlayer); // Take him self as clock reference
        }

        return res.MapError(error => $"Failed to create a room: {error}");
    }

    public async Task<Result<Unit, string>> JoinRoom(RoomId roomId)
    {
        var res = await network.JoinRoom(roomId);

        if (res.IsOk)
        {
            var playerId = Multiplayer.GetUniqueId();
            var username = $"Player: {playerId}";
            Log.Debug("Joined room as {PlayerId}. Already connected player count: {Players}", playerId, players.Count);

            var player = new Player(playerId, username, new PlayerStateInLobby(false));
            localPlayer = new BehaviorSubject<Player>(player);
            RegisterLocalPlayer();

            // Wait for all players to be registered
            // The purpose is to ensure `SyncTime` take the correct peer as clock reference
            await Task.Run(() =>
            {
                var expectedPlayerCount = res.Value + 1; // +1 because res.Value is number of players to connect to
                while (players.Count < expectedPlayerCount) { }
                StartTimeSynchronization(5, localPlayer);
            });
        }

        return res
            .Map(_ => Unit.Default)
            .MapError(error => $"Failed to join the room: {error}");
    }

    public async void LeaveRoom()
    {
        Log.Debug("Leaving room");

        // Clear players list
        players.Clear();
        localPlayer?.OnCompleted();
        localPlayer = null;

        (await network.LeaveRoom()).Match(
            success: _ => Log.Information("Left room"),
            failure: error => Log.Error($"Failed to leave the room: {error}")
        );
    }


    /// <summary>
    /// Send the player's info to the other players in the room
    /// </summary>
    public void RegisterLocalPlayer()
    {
        if (localPlayer is null)
        {
            Log.Warning("Not connected to a room, can't register the player");
            return;
        }

        // -- Register local player on already connected peers
        Rpc(nameof(RegisterPlayerRpc), localPlayer.Value);
        foreach (var peerId in Multiplayer.GetPeers())
        {
            Log.Debug("Registering us with already connected peer: {Username}", localPlayer.Value.Username);
        }

        // -- Register local player on future peers
        // Use Multiplayer.MultiplayerPeer instead of to avoid having
        // to unsubscribe from the event when leaving the room
        Multiplayer.MultiplayerPeer.PeerConnected += peerId =>
        {
            Log.Debug("New peer connected, registering us with him: {Username}", localPlayer.Value.Username);
            RpcId(peerId, nameof(RegisterPlayerRpc), localPlayer.Value);
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

        players.Add(player.PeerId, obs);
        playerRegistered.OnNext(obs);
    }

    public void SetPlayerState(PlayerState newState)
    {
        if (localPlayer is null)
        {
            Log.Warning("Not connected to a room, can't set player state");
            return;
        }

        // Update the player state locally and on the other peers
        Rpc(nameof(SetPlayerStateRpc), newState);
    }

    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer, // Any peer can call this method
        CallLocal = true,               // This method can be called locally
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    /// <summary>
    /// Set a player state (the state of the caller)
    /// </summary>
    private void SetPlayerStateRpc(Variant newStateVariant)
    {
        PlayerState newState = newStateVariant;
        Log.Debug("[RPC] Player {PlayerId} is now in state {State}", Multiplayer.GetRemoteSenderId(), newState);

        var playerId = Multiplayer.GetRemoteSenderId();
        var player = players.GetValueOrDefault(playerId);

        if (player is null)
        {
            Log.Warning("[SetPlayerStateRpc]: Player {PlayerId} not found", playerId);
            return;
        }

        var newPlayer = player.Value with { State = newState };
        player.OnNext(newPlayer);
        if (playerId == localPlayer?.Value.PeerId) localPlayer.OnNext(newPlayer);
    }


    #region Time Synchronization
    // See https://en.wikipedia.org/wiki/Network_Time_Protocol#Clock_synchronization_algorithm
    private CancellationToken? timeSyncCancellationToken = null;
    private Subject<long> clockDeltaMeasurements = new();

    /// <summary>
    /// The difference of time between local clock and reference clock
    /// </summary>
    public BehaviorSubject<TimeSpan?> clockDelta = new(null);

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
            Log.Error("Time synchronization should have a cancellation token set.");
            return;
        }
        var ct = (CancellationToken)timeSyncCancellationToken;
        // Reset observables
        clockDeltaMeasurements = new();
        clockDelta = new(null);

        // Log when clock delta is updated
        clockDelta.Subscribe(
            delta => Log.Debug("[Time Sync] Clock delta changed: {Delta}", delta),
            ct
        );

        var hostPlayerId = players.Min(player => player.Key);

        // `clockDeltas` are the duration measured. They are independent
        // When a new delta is emitted we determine the average clock delta with the server.
        // We discard the deltas that differ by more than 1 standard deviation from the median.
        // The purpose is to eliminate the packets that were retransmitted by tcp.
        clockDeltaMeasurements.Scan(
            ([], TimeSpan.Zero),
            ((List<long> prevDeltas, TimeSpan _) acc, long newDelta) =>
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
        .Subscribe(acc => clockDelta.OnNext(acc.Item2), ct);

        for (int i = 0; i < sampleCount; i++)
        {
            var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            if (ct.IsCancellationRequested) break;

            RpcId(hostPlayerId, nameof(SyncTimeRpc), time);
            try { await Task.Delay(1000, ct); } catch { }
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SyncTimeRpc(long requestTime)
    {
        var now = DateTimeOffset.Now + (clockDelta.Value ?? TimeSpan.Zero);
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
