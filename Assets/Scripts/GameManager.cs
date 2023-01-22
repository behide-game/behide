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
        bool registered = playerId != null;
        bool inRoom = roomId != null;

        GUILayout.BeginArea(new Rect(10, 250, 220, 400));

        if (!inRoom) {
            GUILayout.BeginHorizontal();
            if (!registered) {
                GUIusername = GUILayout.TextField(GUIusername);
                if (GUILayout.Button("Register")) RegisterPlayer(GUIusername);
            }
            else
            {
                GUIroomId = GUILayout.TextField(GUIroomId);
                if (GUILayout.Button("Join room")) JoinRoom(GUIroomId);
            }
            GUILayout.EndHorizontal();

            if (registered && GUILayout.Button("Create a room")) CreateRoom();
        }
        else
        {
            GUILayout.Label("<b>Room ID</b>: " + roomId.ToString());
        }

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