#nullable enable
using System.Linq;
using UnityEngine;
using Mirror;
using BehideServer.Types;

public class GameManager : MonoBehaviour
{
    public struct PlayerJoinedRoomMessage : NetworkMessage
    {
        public string username;
    }

    static public GameManager instance { get; private set; } = null!;
    public ConnectionsManager connections = null!;

    // Game state
    public string? username { get; private set; }
    public (RoomId id, bool isHost, (int connectionId, string username)[] connectedPlayers)? room { get; private set; }

    void Awake()
    {
        if (instance != null) { Destroy(this); return; }

        instance = this;
        DontDestroyOnLoad(this);
    }


    public void SetUsername(string newUsername) => username = newUsername;

    public async void CreateRoom()
    {
        Debug.Log("Creating room");

        NetworkServer.RegisterHandler<PlayerJoinedRoomMessage>((connection, message) =>
        {
            if (room == null || room?.isHost != true) return;
            room = (room.Value.id, room.Value.isHost, room.Value.connectedPlayers.Append((connection.connectionId, message.username)).ToArray());
        });

        var roomId = await connections.CreateRoom();
        room = (roomId, true, new (int, string)[] { });
    }
    public async void JoinRoom(string rawRoomId)
    {
        if (username == null) return;
        if (!RoomId.TryParse(rawRoomId, out RoomId targetRoomId)) return;
        Debug.Log("Joining room");

        await connections.JoinRoom(targetRoomId);
        room = (targetRoomId, false, new (int, string)[] { });

        Transport.active.OnClientConnected += () =>
        {
            var joinMessage = new PlayerJoinedRoomMessage() { username = username };
            NetworkClient.Send(joinMessage);
        };

    }

}