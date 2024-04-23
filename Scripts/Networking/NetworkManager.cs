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

    public override void _EnterTree()
    {
        signaling = GetNode<Signaling>("/root/WebRtcSignaling");
    }

    /// <summary>
    /// Create a room and set the handler for the players who join
    /// </summary>
    public async Task<Result<RoomId, string>> StartHost()
    {
        // Instantiate the multiplayer peer
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
    public async Task<Result<Unit, string>> StartClient(RoomId roomId)
    {
        // Retrieve connection attempts and peer ids
        var joinRoomRes = await signaling.Hub.JoinRoom(roomId);
        if (joinRoomRes.HasError(out var error))
        {
            return error.ToLocalizedString();
        }


        var joinRoomInfo = joinRoomRes.ResultValue;

        multiplayer.CreateMesh(joinRoomInfo.PeerId);
        Multiplayer.MultiplayerPeer = multiplayer;

        // Handle following players connection
        signaling.Client.ConnAttemptIdCreationRequested += async askingPeerId =>
        {
            var peer = new OfferPeerConnector(signaling);
            multiplayer.AddPeer(peer.GetConnection(), askingPeerId);

            return (await peer.CreateConnectionAttempt()).ToOption();
        };

        // Connect to other players
        var tasks = joinRoomInfo.PlayersConnectionInfo.Select(async connInfo =>
        {
            // GameManager.Ui.Log($"Connecting to {connInfo.PeerId}");
            var peer = new AnswerPeerConnector(signaling, connInfo.ConnAttemptId);
            multiplayer.AddPeer(peer.GetConnection(), connInfo.PeerId);

            await peer.Connect();
        });

        await Task.WhenAll(tasks);
        return Unit.Value;
    }
}
