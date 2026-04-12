using System.Reactive;
using Godot;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Behide.Types;
using Behide.Networking;
using Behide.OnlineServices.Signaling;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide;

public partial class Room : Node
{
    public readonly RoomId RoomId;
    public readonly BehaviorSubject<Player> LocalPlayer;
    public readonly Dictionary<int, BehaviorSubject<Player>> Players = [];

    private readonly Subject<Player> playerJoined = new();
    private readonly Subject<Player> playerLeft = new();
    private readonly Subject<Player> playerStateChanged = new();
    private readonly Subject<Unit> roomLeft = new();
    public IObservable<Player> PlayerJoined => playerJoined.AsObservable();
    public IObservable<Player> PlayerLeft => playerLeft.AsObservable();
    public IObservable<Player> PlayerStateChanged => playerStateChanged.AsObservable();
    public IObservable<Unit> RoomLeft => roomLeft.AsObservable();

    private readonly ILogger log = Log.CreateLogger("Room");

    public Room(RoomId roomId, Player player)
    {
        RoomId = roomId;
        LocalPlayer = new BehaviorSubject<Player>(player);
        Name = "Room";
    }

    public override void _EnterTree()
    {
        log.Debug("Room enters tree");
        Multiplayer.PeerConnected += MultiplayerOnPeerConnected;
        Multiplayer.PeerDisconnected += MultiplayerOnPeerDisconnected;

        // Register with already connected players
        Rpc(nameof(RegisterPlayerRpc), LocalPlayer.Value);
        foreach (var _ in Multiplayer.GetPeers())
            log.Debug("Registering us with already connected peer: {Username}", LocalPlayer.Value.Username);
    }

    private void MultiplayerOnPeerConnected(long peerId)
    {
        log.Debug("New peer connected, registering us with him: {Username}", LocalPlayer.Value.Username);
        RpcId(peerId, nameof(RegisterPlayerRpc), LocalPlayer.Value);
    }

    private void MultiplayerOnPeerDisconnected(long peerId)
    {
        log.Debug("Player {PeerId} left the room", peerId);
        var playerObservable = Players.GetValueOrDefault((int)peerId);
        if (playerObservable is null) return;

        Players.Remove((int)peerId);
        playerLeft.OnNext(playerObservable.Value);
        playerObservable.OnCompleted();
    }

    public void Leave()
    {
        roomLeft.OnNext(Unit.Default);
        roomLeft.OnCompleted();
        roomLeft.Dispose();

        foreach (var player in Players.Values) player.OnCompleted();
        Players.Clear();
        LocalPlayer.OnCompleted();
        playerJoined.OnCompleted();
        playerLeft.OnCompleted();
        playerStateChanged.OnCompleted();

        LocalPlayer.Dispose();
        playerJoined.Dispose();
        playerLeft.Dispose();
        playerStateChanged.Dispose();
        roomLeft.Dispose();
    }

    /// <summary>Update the player state locally and on the other peers</summary>
    public void SetPlayerState(PlayerState newState) => Rpc(nameof(SetPlayerStateRpc), newState);

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RegisterPlayerRpc(Variant playerVariant)
    // private void RegisterPlayerRpc(Player player) TODO: Test
    {
        var player = (Player)playerVariant;
        Players.Add(
            player.PeerId,
            new BehaviorSubject<Player>(player)
        );
        playerJoined.OnNext(player);
    }

    /// <summary>Set a player state (the state of the caller)</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SetPlayerStateRpc(Variant newStateVariant) // TODO: Test
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
        playerStateChanged.OnNext(newPlayer);
        if (playerId == LocalPlayer.Value.PeerId) LocalPlayer.OnNext(newPlayer);
    }
}

public partial class RoomManager : Node
{
    private Signaling signaling = null!;
    public Room? Room;

    private readonly ILogger log = Log.CreateLogger("RoomManager");

    public override void _EnterTree()
    {
        signaling = GetNode<Signaling>("/root/WebRtcSignaling");
    }

    public async Task<RoomId> CreateRoom()
    {
        // Create room
        var roomId = await signaling.CreateRoom();

        // Instantiate the multiplayer peer
        const int playerId = 1; // The room creator has always the peer id 1
        var multiplayer = new WebRtcMultiplayerPeer();
        multiplayer.CreateMesh(playerId);
        Multiplayer.MultiplayerPeer = multiplayer;

        var username = $"Player: {playerId}"; // TODO: Ask the player for his username
        var player = new Player(playerId, username, new PlayerStateInLobby(false));
        Room = new Room(roomId, player);
        AddChild(Room);

        // Not starting time sync because we are the time reference
        return roomId;
    }

    public async Task JoinRoom(RoomId roomId)
    {
        if (Room is not null)
        {
            log.Error("Cannot join a room: Already in a room");
            return;
        }

        // Retrieve connection attempts and peer id
        var playerId = await signaling.JoinRoom(roomId);
        var playersConnectionInfo = await signaling.GetConnectionInfo();
        var numberOfPlayers = playersConnectionInfo.PlayersConnectionInfo.Length;
        log.Debug("Retrieved connection info");

        if (playersConnectionInfo.FailedCreations.Length > 0)
            log.Error("Failed to create connection info for some players: {Error}", playersConnectionInfo.FailedCreations);

        // Instantiate the multiplayer peer
        var multiplayer = new WebRtcMultiplayerPeer();
        multiplayer.CreateMesh(playerId);
        Multiplayer.MultiplayerPeer = multiplayer;

        // Create the room management object
        var username = $"Player: {playerId}";
        var player = new Player(playerId, username, new PlayerStateInLobby(false));
        Room = new Room(roomId, player);
        AddChild(Room);

        log.Debug("Joined room as {PeerId}. Already connected player count: {PlayerCount}", playerId, Room.Players.Count);

        // Connect to other players
        var tasks =
            playersConnectionInfo.PlayersConnectionInfo
                .Select(async connInfo =>
                {
                    var peer = new PeerConnection();
                    multiplayer.AddPeer(peer, connInfo.PeerId);
                    await peer.AnswerConnectionOffer(signaling, connInfo.ConnectionAttemptId); // This return only when peer is actually connected
                })
                .ToArray();

        await Task.WhenAll(tasks);
        log.Debug("Initiated connections");

        // Wait for all players to be registered
        // The purpose is to ensure `SyncTime` take the correct peer as clock reference
        await Task.Run(() =>
        {
            while (Room.Players.Count <= numberOfPlayers) { }
            // TODO: Room.Players.Count counts the local player, but numberOfPlayers doesn't (?)
            GameManager.TimeSync.Start();
        });
    }

    public async Task LeaveRoom()
    {
        if (Room is null)
        {
            log.Error("Failed to leave room: Not in a room");
            return;
        }

        Room.Leave();
        CallDeferred(Node.MethodName.RemoveChild, Room);
        Room.QueueFree();
        Room = null;

        if (Multiplayer.MultiplayerPeer is not WebRtcMultiplayerPeer multiplayer)
        {
            log.Warning("Leaving a room without being connected to a room.");
            return;
        }

        // Disconnect from all peers
        foreach (var peer in multiplayer.GetPeers())
        {
            var peerId = peer.Key.As<int>();
            multiplayer.DisconnectPeer(peerId);
        }

        // Close the multiplayer
        multiplayer.Close();
        Multiplayer.MultiplayerPeer = null;

        // Leave room
        await signaling.LeaveRoom();

        log.Information("Room leaved");
    }
}
