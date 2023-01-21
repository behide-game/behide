using UnityEngine;
using Mirror;
using BehideServer.Types;

public class GameManager : MonoBehaviour
{
    [SerializeField]
    private ConnectionsManager connectionManager;
    private NetworkManagerHUD networkManagerHud;

    private PlayerId playerId;
    private RoomId roomId;

    // GUI
    private string GUIusername;
    private string GUIroomId;

    void Awake()
    {
        DontDestroyOnLoad(this);
        networkManagerHud = GetComponentInChildren<NetworkManagerHUD>();
        networkManagerHud.enabled = false;

        connectionManager.OnConnected.AddListener(() => networkManagerHud.enabled = true);
    }

    void OnGUI() {
        if (!networkManagerHud.enabled) return;

        GUILayout.BeginArea(new Rect(10, 250, 220, 400));

        if (playerId == null) {
            GUILayout.BeginHorizontal();
            GUIusername = GUILayout.TextField(GUIusername);
            if (GUILayout.Button("Register")) RegisterPlayer(GUIusername);
            GUILayout.EndHorizontal();
        }
        else
        {
            GUILayout.BeginHorizontal();
            GUIroomId = GUILayout.TextField(GUIroomId);
            if (GUILayout.Button("Join room")) JoinRoom(GUIroomId);
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Create a room")) CreateRoom();
        }


        // GUILayout.Label("<b>Current roomId:</b> " + roomId?.ToString());
        // username = GUILayout.TextField(GUItextInput);

        // if (GUILayout.Button("Create a room")) CreateRoom();
        // if (GUILayout.Button("Join room") && RoomId.TryParse(GUItextInput, out RoomId roomId)) JoinRoom(roomId);

        GUILayout.EndArea();
    }

    async void RegisterPlayer(string username) {
        playerId = await connectionManager.RegisterPlayer(username);
    }

    async void JoinRoom(string rawRoomId) {
        Debug.Log("Join room");
        if (!RoomId.TryParse(rawRoomId, out RoomId roomId)) return;
        await connectionManager.JoinRoom(roomId);
    }

    async void CreateRoom() {
        Debug.Log("Create room");
        roomId = await connectionManager.CreateRoom(playerId);
    }
}