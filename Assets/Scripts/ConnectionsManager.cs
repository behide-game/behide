using System;
using System.Net;
using UnityEngine;
using Mirror;
using BehideServer.Types;


public class ConnectionsManager : MonoBehaviour
{
    private const string EPIC_SCHEME = "epic";
    private enum State { ConnectingEpic, ConnectingBehideServer, RegisteringPlayer, Ready }

    public string username;

    [Header("Behide server")]
    public string ip;
    public int port;

    private Guid targetEpicId;
    private PlayerId currentPlayerId;
    private RoomId currentRoomId;

    private ServerConnection server;
    private State state;

    private NetworkManager networkManager;

    private string GUItextInput;

    void Awake()
    {
        DontDestroyOnLoad(this);

        state = State.ConnectingEpic;
        networkManager = GetComponentInChildren<NetworkManager>();

        // Create connection with BehideServer
        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        server = new ServerConnection(ipEndPoint);
        server.Start();
    }

    void OnDestroy()
    {
        server?.Dispose();
    }

    private void OnGUI()
    {
        if (state != State.Ready) return;

        GUILayout.BeginArea(new Rect(10, 250, 220, 400));

        GUILayout.Label("<b>State:</b> " + state.ToString());
        GUILayout.Label("<b>Current roomId:</b> " + currentRoomId?.ToString());
        GUItextInput = GUILayout.TextField(GUItextInput);

        if (GUILayout.Button("Create a room")) CreateRoom();
        if (GUILayout.Button("Join room") && RoomId.TryParse(GUItextInput, out RoomId roomId)) JoinRoom(roomId);

        GUILayout.EndArea();
    }


    public async void RegisterPlayer()
    {
        Msg msg = Msg.NewRegisterPlayer(server.serverVersion, username);
        Response response = await server.SendMessage(msg, ResponseHeader.PlayerRegistered);

        if (!PlayerId.TryParseBytes(response.Content, out PlayerId playerId))
        {
            Debug.LogError("Failed to parse PlayerId");
            return;
        }

        currentPlayerId = playerId;
    }

    public async void CreateRoom()
    {
        Msg msg = Msg.NewCreateRoom(currentPlayerId, currentEpicId);
        Response response = await server.SendMessage(msg, ResponseHeader.RoomCreated);

        if (!RoomId.TryParseBytes(response.Content, out RoomId roomId))
        {
            Debug.LogError("Failed to parse RoomId");
            return;
        }

        currentRoomId = roomId;
    }

    public async void JoinRoom(RoomId roomId)
    {
        Response response = await server.SendMessage(Msg.NewGetRoom(roomId), ResponseHeader.RoomFound);

        if (!GuidHelper.TryParseBytes(response.Content, out Guid epicId))
        {
            Debug.LogError("Failed to parse Guid.");
            return;
        }

        currentRoomId = roomId;
        targetEpicId = epicId;

        Uri uri = new UriBuilder(EPIC_SCHEME, targetEpicId.ToString("N")).Uri;
        networkManager.StartClient(uri);
    }
}