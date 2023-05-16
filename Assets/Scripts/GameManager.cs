#nullable enable
using UnityEngine;
using BehideServer.Types;

public class GameManager : MonoBehaviour
{
    public ConnectionsManager connections = null!;

    public PlayerId? playerId { get; private set; }
    public RoomId? roomId { get; private set; }


    void Awake() => DontDestroyOnLoad(this);


    public async void RegisterPlayer(string username) => playerId = await connections.RegisterPlayer(username);

    public async void JoinRoom(string rawRoomId)
    {
        Debug.Log("Join room");
        if (!RoomId.TryParse(rawRoomId, out RoomId targetRoomId)) return;

        await connections.JoinRoom(targetRoomId);
        roomId = targetRoomId;
    }

    public async void CreateRoom()
    {
        if (playerId == null) { Debug.LogError("Player not registered"); return; }
        Debug.Log("Create room");
        roomId = await connections.CreateRoom(playerId);
    }
}