namespace Behide.Networking;

using Godot;
using System.Threading.Tasks;
using System.Linq;

using Behide.Types;
using Behide.OnlineServices.Client;
using Behide.OnlineServices.Signaling;
using Behide.I18n.BOS.Errors;

/// <summary>
///  NetworkManager is responsible for handling the connection between the game and the signaling server.
///  It create or join a room and handle the connection between the players.
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
    public async Task<Result<RoomId, string>> CreateRoom()
    {
        // Instantiate the multiplayer peer
        multiplayer = new WebRtcMultiplayerPeer();
        multiplayer.CreateMesh(1);
        Multiplayer.MultiplayerPeer = multiplayer;

        // Handle new connection
        signaling.Client.ConnAttemptIdCreationRequested += async askingPeerId =>
        {
            var peer = new OfferPeerConnector(signaling);
            multiplayer.AddPeer(peer.GetConnection(), askingPeerId);

            return (await peer.CreateConnectionAttempt()).ToOption();
        };

        // Create room
        return (await signaling.Hub.CreateRoom())
            .ToResult()
            .MapError(err => err.ToLocalizedString());
    }

    /// <summary>
    /// Join a room and connect to the other players
    /// </summary>
    public async Task<Result<Unit, string>> JoinRoom(RoomId roomId)
    {
        // Retrieve connection attempts and peer ids
        var joinRoomRes = await signaling.Hub.JoinRoom(roomId);
        if (joinRoomRes.HasError(out var joinRoomError))
        {
            return joinRoomError.ToLocalizedString();
        }

        var peerId = joinRoomRes.ResultValue;

        var playersConnectionInfoRes = await signaling.Hub.ConnectToRoomPlayers();
        if (playersConnectionInfoRes.HasError(out var connectToRoomPlayersError))
        {
            return connectToRoomPlayersError.ToLocalizedString();
        }
        var playersConnectionInfo = playersConnectionInfoRes.ResultValue;

        multiplayer = new WebRtcMultiplayerPeer();
        multiplayer.CreateMesh(peerId);
        Multiplayer.MultiplayerPeer = multiplayer;

        // Handle following players connection
        signaling.Client.ConnAttemptIdCreationRequested += async askingPeerId =>
        {
            var peer = new OfferPeerConnector(signaling);
            multiplayer.AddPeer(peer.GetConnection(), askingPeerId);

            return (await peer.CreateConnectionAttempt()).ToOption();
        };

        // Connect to other players
        var tasks = playersConnectionInfo.PlayersConnInfo.Select(async connInfo =>
        {
            // GameManager.Ui.Log($"Connecting to {connInfo.PeerId}");
            var peer = new AnswerPeerConnector(signaling, connInfo.ConnAttemptId);
            multiplayer.AddPeer(peer.GetConnection(), connInfo.PeerId);

            await peer.Connect();
        });

        if (playersConnectionInfo.FailedCreations.Length > 0)
        {
            log.Error("Failed to create connection info for some players: {Error}", playersConnectionInfo.FailedCreations);
        }

        await Task.WhenAll(tasks);
        return Unit.Value;
    }

    /// <summary>
    /// Disconnect from the room peers and leave the room
    /// </summary>
    public async Task<Result<Unit, string>> LeaveRoom()
    {
        // Disconnect from peers
        foreach (var peer in multiplayer.GetPeers())
        {
            multiplayer.DisconnectPeer(peer.Key.As<int>());
        }

        Multiplayer.MultiplayerPeer = null;

        // Leave room
        return (await signaling.Hub.LeaveRoom())
            .ToResult()
            .Map(_ => Unit.Value)
            .MapError(err => err.ToLocalizedString());
    }
}
