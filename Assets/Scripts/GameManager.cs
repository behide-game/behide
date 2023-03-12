using UnityEngine;
using Mirror;
using BehideServer.Types;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private ConnectionsManager connectionManager;
    public bool connected;

    public bool playerRegistered;
    public bool inRoom;
    private PlayerId playerId;
    public RoomId roomId;

    // GUI
    private string GUIusername;
    private string GUIroomId;

    void Awake()
    {
        DontDestroyOnLoad(this);

        playerRegistered = playerId != null;
        inRoom = roomId != null;
        connected = false;

        connectionManager.OnConnected.AddListener(() => connected = true);
    }

    void OnGUI() {
        // if (!connected) return;

        // GUILayout.BeginArea(new Rect(10, 120, 220, 400));

        // if (!inRoom) {
        //     GUILayout.BeginHorizontal();
        //     if (!registered) {
        //         GUIusername = GUILayout.TextField(GUIusername);
        //         if (GUILayout.Button("Register")) RegisterPlayer(GUIusername);
        //     }
        //     else
        //     {
        //         GUIroomId = GUILayout.TextField(GUIroomId);
        //         if (GUILayout.Button("Join room")) JoinRoom(GUIroomId);
        //     }
        //     GUILayout.EndHorizontal();

        //     if (registered && GUILayout.Button("Create a room")) CreateRoom();
        // }
        // else
        // {
        //     GUILayout.Label($"<b>Room ID</b>: {roomId.ToString()}\n<b>Ping</b>: {System.Math.Round(NetworkTime.rtt * 1000)}ms");
        // }

        // GUILayout.EndArea();
    }


    public async void RegisterPlayer(string username) {
        playerId = await connectionManager.RegisterPlayer(username);
        playerRegistered = true;
    }

    public async void JoinRoom(string rawRoomId) {
        Debug.Log("Join room");
        if (!RoomId.TryParse(rawRoomId, out RoomId targetRoomId)) return;
        await connectionManager.JoinRoom(targetRoomId);
        roomId = targetRoomId;
        inRoom = true;
    }

    public async void CreateRoom() {
        Debug.Log("Create room");
        roomId = await connectionManager.CreateRoom(playerId);
        inRoom = true;
    }
}