namespace Behide.Networking;

using Godot;
using Behide.OnlineServices;
using System.Threading.Tasks;

public partial class NetworkManager : Node3D
{
    private Signaling signaling = null!;
    private WebRtcMultiplayerPeer multiplayer = new();
    private int nextPeerId = 2;

    public override void _EnterTree()
    {
        signaling = GetNode<Signaling>("/root/WebRtcSignaling");
        multiplayer.PeerConnected += peerId => GD.Print("Peer connected: ", peerId);
    }

    public async Task<Result<RoomId>> StartHost()
    {
        multiplayer.CreateServer();
        Multiplayer.MultiplayerPeer = multiplayer;

        // Handle new connection
        signaling.OfferIdCreationRequested += async () =>
        {
            var peer = new OfferPeerConnection(signaling);
            var peerId = nextPeerId++;

            multiplayer.AddPeer(peer.GetPeerConnection(), peerId);

            return (peerId, await peer.CreateOffer());
        };

        return await signaling.CreateRoom();
    }

    public async Task StartClient(RoomId roomId)
    {
        // Retrieve offer and peer id
        var (peerId, offerId) = await signaling.JoinRoom(roomId) switch
        {
            Result<(int, OfferId)>.Ok res => res.Value,
            Result<(int, OfferId)>.Error error => throw new System.Exception("Failed to retrieve offerId: " + error.Failure),
            _ => throw new System.NotImplementedException()
        };

        multiplayer.CreateClient(peerId);
        Multiplayer.MultiplayerPeer = multiplayer;

        // Connect to host
        var peer = new AnswerPeerConnection(signaling, offerId);
        multiplayer.AddPeer(peer.GetPeerConnection(), 1);

        await peer.Connect();
    }
}
