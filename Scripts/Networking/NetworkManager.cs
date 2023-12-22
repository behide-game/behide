namespace Behide.Networking;

using Godot;
using Behide.OnlineServices;

public partial class NetworkManager : Node3D
{
    private Signaling signaling = null!;
    private WebRtcMultiplayerPeer multiplayer = new();
    private int nextPeerId = 2;
    private bool ready = false;

    public override void _EnterTree()
    {
        signaling = GetNode<Signaling>("/root/WebRtcSignaling");
        multiplayer.PeerConnected += peerId => GD.Print("Peer connected: ", peerId);
    }

    public async void StartHost()
    {
        ready = true;
        multiplayer.CreateServer();
        Multiplayer.MultiplayerPeer = multiplayer;

        // Handle new connection
        signaling.OfferIdCreationRequested += () =>
        {
            var peer = new OfferPeerConnection(signaling);
            multiplayer.AddPeer(peer.GetPeerConnection(), nextPeerId++);

            return peer.CreateOffer();
        };

        var res = await signaling.CreateRoom();
        GetNode<TextEdit>("/root/multiplayer/UI/RoomIdField").Text = res switch
        {
            Result<RoomId>.Ok roomId => RoomId.raw(roomId.Value),
            _ => "error",
        };
    }

    public async void StartClient()
    {
        if (ready)
        {
            SpawnPlayers();
            return;
        }

        multiplayer.CreateClient(2);
        Multiplayer.MultiplayerPeer = multiplayer;

        // Retrieve roomId
        var rawRoomId = GetNode<TextEdit>("/root/multiplayer/UI/RoomIdField").Text;

        // Parse roomId
        var roomId = RoomId.tryParse(rawRoomId);
        if (Option<RoomId>.IsNone(roomId))
        {
            GD.PrintErr("Failed to parse roomId");
            return;
        }

        // Connect
        var offerId = await signaling.JoinRoom(roomId.Value) switch
        {
            Result<OfferId>.Ok res => res.Value,
            Result<OfferId>.Error error => throw new System.Exception("Failed to retrieve offerId: " + error.Failure),
            _ => throw new System.NotImplementedException()
        };

        var peer = new AnswerPeerConnection(signaling, offerId);
        multiplayer.AddPeer(peer.GetPeerConnection(), 1);

        _ = peer.Connect();
    }

    public void SpawnPlayers()
    {
        var mainNode = GetNode("/root/multiplayer");

        void spawnPlayer(string playerId)
        {
            var playerPrefab = GD.Load<PackedScene>("res://Prefabs/player.tscn");
            var playerNode = playerPrefab.Instantiate();
            playerNode.Name = playerId;

            mainNode.AddChild(playerNode);
        }

        spawnPlayer("1");

        foreach (var peer in multiplayer.GetPeers())
            spawnPlayer(peer.Key.AsInt32().ToString());
    }
}
