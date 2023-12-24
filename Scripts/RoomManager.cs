namespace Behide;

using Godot;
using Behide.Networking;
using Behide.OnlineServices;

public partial class RoomManager : Node3D
{
    record Player(int Id, string Username);

    private NetworkManager network = null!;
    public override void _EnterTree() => network = GetNode<NetworkManager>("/root/multiplayer/Managers/NetworkManager");

    public async void CreateRoom()
    {
        switch (await network.StartHost())
        {
            case Result<RoomId>.Error error:
                GameManager.Ui.Log($"Failed to create a room: {error.Failure}");
                break;
            case Result<RoomId>.Ok roomId:
                GameManager.Ui.Log($"Room created: {roomId.Value.ToString().ToUpper()}");
                break;
        }

        Multiplayer.PeerConnected += peerId =>
        {
            GameManager.Ui.Log($"New peer connected: {peerId}");
            if (Multiplayer.GetPeers().Length == 3) SpawnPlayers();
        };
    }

    public async void JoinRoom()
    {
        // Retrieve roomId
        var rawRoomId = GetNode<TextEdit>("/root/multiplayer/UI/RoomIdField").Text;

        // Parse roomId
        var roomId = RoomId.tryParse(rawRoomId);
        if (Option<RoomId>.IsNone(roomId))
        {
            GameManager.Ui.LogError($"Invalid room id");
            return;
        }

        // Connect
        GameManager.Ui.Log($"Connecting...");
        await network.StartClient(roomId.Value);
        GameManager.Ui.Log("Connected as " + Multiplayer.GetUniqueId());
    }

    public void SpawnPlayers()
    {
        var mainNode = GetNode("/root/multiplayer");

        void spawnPlayer(int playerId)
        {
            var playerPrefab = GD.Load<PackedScene>("res://Prefabs/player.tscn");
            var playerNode = playerPrefab.Instantiate<Node3D>();
            playerNode.Name = playerId.ToString();

            var transform = playerNode.Transform;
            transform.Origin = new Vector3(0, playerId * 2, 0);
            playerNode.Transform = transform;

            mainNode.AddChild(playerNode);
        }

        // Spawn host
        spawnPlayer(Multiplayer.GetUniqueId());
        // Spawn other players
        foreach (var peerId in Multiplayer.GetPeers()) spawnPlayer(peerId);
    }
}
