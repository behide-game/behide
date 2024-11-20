namespace Behide;

using Godot;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Behide.Types;
using Behide.Networking;
using Behide.OnlineServices.Signaling;

public partial class RoomManager : Node3D
{
    private NetworkManager network = null!;

    /// <summary>
    /// The local player
    /// It's null if the player is not in a room
    /// </summary>
    public Player? localPlayer;

    /// <summary>
    /// The list of players in the room / of the peers connected
    /// </summary>
    public readonly List<Player> players = [];

    public Subject<Player> playerRegistered = new();
    public Subject<Player> playerLeft = new();
    public Subject<Player> playerStateChanged = new();
    public IObservable<Player> PlayerRegistered => playerRegistered.AsObservable();
    public IObservable<Player> PlayerLeft => playerLeft.AsObservable();
    public IObservable<Player> PlayerStateChanged => playerStateChanged.AsObservable();

    private Serilog.ILogger Log = null!;

    public override void _EnterTree()
    {
        network = GameManager.Network;
        Log = Serilog.Log.ForContext("Tag", "RoomManager");

        Multiplayer.PeerDisconnected += peerId =>
        {
            Log.Debug($"Player {peerId} left the room");

            var player = players.Find(p => p.PeerId == peerId);

            if (player is not null)
            {
                players.Remove(player);
                playerLeft.OnNext(player);
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

            localPlayer = new Player(playerId, username, new PlayerStateInLobby(false));
            RegisterLocalPlayer();
        }

        return res.MapError(error => $"Failed to create a room: {error}");
    }

    public async Task<Result<Unit, string>> JoinRoom(RoomId roomId)
    {
        var res = await network.JoinRoom(roomId);

        if (res.IsOk)
        {
            Log.Debug("Joined room: {Players}", players);

            var playerId = Multiplayer.GetUniqueId();
            var username = $"Player: {playerId}";

            localPlayer = new Player(playerId, username, new PlayerStateInLobby(false));
            RegisterLocalPlayer();
        }

        return res.MapError(error => $"Failed to join the room: {error}");
    }

    public async void LeaveRoom()
    {
        Log.Debug("Leaving room");

        // Clear players list
        players.Clear();
        localPlayer = null;

        (await network.LeaveRoom()).Match(
            success: _ => Log.Information("Left room"),
            failure: error => Log.Error($"Failed to leave the room: {error}")
        );
    }


    /// <summary>
    /// Send the player's info to the other players in the room
    /// </summary>
    /// <param name="username">The username of the player</param>
    public void RegisterLocalPlayer() // Todo: remove the parameter and register localPlayer
    {
        if (localPlayer is null)
        {
            Log.Warning("Not connected to a room, can't register the player");
            return;
        }

        // Register local player on already connected peers
        Rpc(nameof(RegisterPlayerRpc), localPlayer);
        foreach (var peerId in Multiplayer.GetPeers())
        {
            Log.Debug("Registering us with already connected peer: {Username}", localPlayer.Username);
        }

        // Register local player on futur peers
        // Use Multiplayer.MultiplayerPeer to don't have to unsubscribe from the event when leaving the room
        Multiplayer.MultiplayerPeer.PeerConnected += peerId =>
        {
            Log.Debug("New peer connected, registering us with him: {Username}", localPlayer.Username);
            RpcId(peerId, nameof(RegisterPlayerRpc), localPlayer);
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
        var playerId = Multiplayer.GetRemoteSenderId(); // TODO: Why isn't it used?

        players.Add(player);
        playerRegistered.OnNext(player);
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
        CallLocal = true,               // This method can be called locally (and must because the local player is the authority)
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    /// <summary>
    /// Set a player state (the state of the caller)
    /// </summary>
    private void SetPlayerStateRpc(Variant newStateVariant)
    {
        PlayerState newState = newStateVariant;
        Log.Information("[RPC] Player {PlayerId} is now in state {State}", Multiplayer.GetRemoteSenderId(), newState);

        var playerId = Multiplayer.GetRemoteSenderId();
        var player = players.Find(p => p.PeerId == playerId);

        if (player is null)
        {
            Log.Warning("[SetPlayerStateRpc]: Player {PlayerId} not found", playerId);
            return;
        }

        player.State = newState;

        if (playerId == localPlayer?.PeerId)
            localPlayer = player;

        playerStateChanged.OnNext(player);
    }


    // public void SpawnPlayer(long playerId)
    // {
    //     GD.Print($"Spawning {playerId}");
    //     var mainNode = GetNode("/root/multiplayer");

    //     // Create node and set his name
    //     var playerPrefab = GD.Load<PackedScene>("res://Prefabs/player.tscn");
    //     var playerNode = playerPrefab.Instantiate<Node3D>();
    //     playerNode.Name = playerId.ToString();

    //     // Put node in the world
    //     mainNode.AddChild(playerNode, true);
    // }
}
