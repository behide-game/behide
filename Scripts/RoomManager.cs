using Godot;
using Behide.Types;
using Behide.Networking;
using Behide.OnlineServices.Signaling;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide;

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

        var username = GameManager.PauseMenu.GetUsername() ?? $"Player: {playerId}";
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
        var username = GameManager.PauseMenu.GetUsername() ?? $"Player: {playerId}";
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
