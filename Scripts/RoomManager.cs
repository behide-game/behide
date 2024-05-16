namespace Behide;

using Godot;
using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using Behide.Types;
using Behide.Networking;
using Behide.OnlineServices.Signaling;

public class Player(int peerId, string username)
{
    /// <summary>
    /// The peer id of the player to identify him on the network
    /// </summary>
    public readonly int PeerId = peerId;

    /// <summary>
    /// The username of the player
    /// </summary>
    public readonly string Username = username;

    /// <summary>
    /// True if the player is ready to start the game
    /// </summary>
    public bool Ready = false;
}

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

    public event Action<Player>? PlayerRegistered;
    public event Action<Player>? PlayerLeft;
    public event Action<Player, bool>? PlayerReady;

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
                PlayerLeft?.Invoke(player);
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

            localPlayer = new Player(playerId, username);
            RegisterPlayer(username);
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

            localPlayer = new Player(playerId, username);
            RegisterPlayer(username);
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
    public void RegisterPlayer(string username)
    {
        // Register local player on already connected peers
        Rpc(nameof(RegisterPlayerRpc), username);
        foreach (var peerId in Multiplayer.GetPeers())
        {
            Log.Debug("Registering us with already connected peer: {Username}", username);
        }

        // Register local player on futur peers
        // Use Multiplayer.MultiplayerPeer to don't have to unsubscribe from the event
        Multiplayer.MultiplayerPeer.PeerConnected += peerId =>
        {
            Log.Debug("New peer connected, registering us with him: {Username}", username);
            RpcId(peerId, nameof(RegisterPlayerRpc), username);
        };
    }

    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer, // Any peer can call this method
        CallLocal = true,               // This method is also called locally
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void RegisterPlayerRpc(string username)
    {
        var playerId = Multiplayer.GetRemoteSenderId();
        var player = new Player(playerId, username);

        players.Add(player);
        PlayerRegistered?.Invoke(player);
    }


    public void ToggleReady()
    {
        if (localPlayer is null)
        {
            Log.Warning("Not connected to a room, can't toggle ready");
            return;
        }

        localPlayer.Ready = !localPlayer.Ready;
        Rpc(nameof(SetReadyRpc), localPlayer.Ready);

        Log.Information("Toggled ready: {Ready}", localPlayer.Ready);
    }

    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer, // Any peer can call this method
        CallLocal = true,               // This method is also called locally
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void SetReadyRpc(bool ready)
    {
        Log.Information("Player {PlayerId} is ready: {Ready}", Multiplayer.GetRemoteSenderId(), ready);
        var playerId = Multiplayer.GetRemoteSenderId();
        var player = players.Find(p => p.PeerId == playerId);

        if (player is not null)
        {
            player.Ready = ready;
            PlayerReady?.Invoke(player, ready);
        }
        else
            Log.Warning("[SetReadyRpc]: Player {PlayerId} not found", playerId);

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
