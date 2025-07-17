using Godot;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Behide.Types;
using Behide.Networking;
using Behide.OnlineServices.Signaling;

namespace Behide;

public partial class RoomManager : Node
{
    [Export] private TimeSynchronizer timeSynchronizer = null!;
    private NetworkManager network = null!;

    /// <summary>
    /// The local player.
    /// It's null if the player is not in a room.
    /// </summary>
    public BehaviorSubject<Player>? LocalPlayer;

    /// <summary>
    /// The list of players in the room / of the peers connected.
    /// </summary>
    public readonly Dictionary<int, BehaviorSubject<Player>> Players = [];

    private readonly Subject<BehaviorSubject<Player>> playerRegistered = new();
    private readonly Subject<Player> playerLeft = new();
    public IObservable<BehaviorSubject<Player>> PlayerRegistered => playerRegistered.AsObservable();
    public IObservable<Player> PlayerLeft => playerLeft.AsObservable();

    private Serilog.ILogger log = null!;

    public override void _EnterTree()
    {
        network = GameManager.Network;
        timeSynchronizer = GameManager.TimeSync;
        log = Serilog.Log.ForContext("Tag", "RoomManager");

        Multiplayer.PeerDisconnected += peerId =>
        {
            log.Debug("Player {PeerId} left the room", peerId);
            var playerObservable = Players.GetValueOrDefault((int)peerId);
            if (playerObservable is null) return;

            Players.Remove((int)peerId);
            playerLeft.OnNext(playerObservable.Value);
            playerObservable.OnCompleted();
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
        // Not starting time sync because we are the time reference

        return roomId;
    }

    public async Task JoinRoom(RoomId roomId)
    {
        var (peerId, numberOfPlayers) = await network.JoinRoom(roomId);
        var username = $"Player: {peerId}";
        log.Debug("Joined room as {PeerId}. Already connected player count: {PlayerCount}", peerId, Players.Count);

        var player = new Player(peerId, username, new PlayerStateInLobby(false));
        LocalPlayer = new BehaviorSubject<Player>(player);
        RegisterLocalPlayer();

        // Wait for all players to be registered
        // The purpose is to ensure `SyncTime` take the correct peer as clock reference
        await Task.Run(() =>
        {
            while (Players.Count <= numberOfPlayers) { }
            timeSynchronizer.Start(LocalPlayer);
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
        CallLocal = true                // This method is also called locally
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
        CallLocal = true                // This method can be called locally
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
}
