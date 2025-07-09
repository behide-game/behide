using Godot;
using System.Linq;
using System.Threading.Tasks;

using Behide.OnlineServices.Signaling;

namespace Behide.Networking;

/// <summary>
/// Create or join a room and handle the connection between the players.
/// </summary>
public partial class NetworkManager : Node3D
{
    private Signaling signaling = null!;
    private WebRtcMultiplayerPeer multiplayer = new();

    private Serilog.ILogger log = null!;

    public override void _EnterTree()
    {
        log = Serilog.Log.ForContext("Tag", "Network");
        signaling = GetNode<Signaling>("/root/WebRtcSignaling");
    }

    /// <summary>
    /// Create a room and set the handler for the players who join
    /// </summary>
    public async Task<RoomId> CreateRoom()
    {
        // Create room
        var roomId = await signaling.CreateRoom();

        // Instantiate the multiplayer peer
        multiplayer = new WebRtcMultiplayerPeer();
        multiplayer.CreateMesh(1); // The room creator has always the peer id 1
        Multiplayer.MultiplayerPeer = multiplayer;

        return roomId;
    }

    /// <summary>
    /// Join a room and connect to the other players
    /// </summary>
    public async Task<(int, int)> JoinRoom(RoomId roomId)
    {
        // Retrieve connection attempts and peer ids
        var peerId = await signaling.JoinRoom(roomId);
        var playersConnectionInfo = await signaling.ConnectToRoomPlayers();
        log.Debug("Retrieved connection info");

        if (playersConnectionInfo.FailedCreations.Length > 0)
            log.Error("Failed to create connection info for some players: {Error}", playersConnectionInfo.FailedCreations);

        multiplayer = new WebRtcMultiplayerPeer();
        multiplayer.CreateMesh(peerId);
        Multiplayer.MultiplayerPeer = multiplayer;

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
        return (peerId, playersConnectionInfo.PlayersConnectionInfo.Length);
    }

    /// <summary>
    /// Disconnect from the room peers and leave the room
    /// </summary>
    public async Task LeaveRoom()
    {
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
    }
}
