using UnityEngine;
using Mirror;

public class BehideNetworkManager : NetworkManager
{
    private GameManager gameManager = null!;

    public struct PlayerJoinedRoomMessage : NetworkMessage
    {
        public string username;
    }

    public struct GameEndedMessage : NetworkMessage { } // Unused
    public struct RoomClosedMessage : NetworkMessage { }


    public override void Awake() { base.Awake(); gameManager = GameManager.instance; }

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<PlayerJoinedRoomMessage>((conn, msg) => gameManager.PlayerJoined(conn, msg.username));
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        if (gameManager.username == null) return;

        NetworkClient.RegisterHandler<GameEndedMessage>((msg) => {}); // Unused
        NetworkClient.RegisterHandler<RoomClosedMessage>((msg) => gameManager.RoomClosed());
        Debug.Log("Room closed message registered");

        var joinMessage = new PlayerJoinedRoomMessage() { username = gameManager.username };
        NetworkClient.Send(joinMessage);
    }
}