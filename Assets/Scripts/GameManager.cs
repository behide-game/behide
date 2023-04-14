using UnityEngine;
using BehideServer.Types;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private ConnectionsManager connectionsManager;

    #nullable enable
    [HideInInspector] public bool connected;
    [HideInInspector] public string? connectError;
    [HideInInspector] public bool playerRegistered;
    [HideInInspector] public bool inRoom;
    private PlayerId? playerId;
    private RoomId? roomId;


    void Awake()
    {
        DontDestroyOnLoad(this);

        playerRegistered = playerId != null;
        inRoom = roomId != null;
        connected = false;

        connectionsManager.OnConnected.AddListener(() => connected = true);
        connectionsManager.OnConnectError.AddListener(error => connectError = error);
    }


    public async void RegisterPlayer(string username) {
        playerId = await connectionsManager.RegisterPlayer(username);
        playerRegistered = true;
    }

    public async void JoinRoom(string rawRoomId) {
        Debug.Log("Join room");
        if (!RoomId.TryParse(rawRoomId, out RoomId targetRoomId)) return;

        await connectionsManager.JoinRoom(targetRoomId);
        roomId = targetRoomId;
        inRoom = true;
    }

    public async void CreateRoom() {
        Debug.Log("Create room");
        roomId = await connectionsManager.CreateRoom(playerId);
        inRoom = true;
    }
}