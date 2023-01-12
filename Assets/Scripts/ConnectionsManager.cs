using UnityEngine;
using Mirror;
using EpicTransport;
using LightReflectiveMirror;
using System.Threading;
using System.Threading.Tasks;

public class GameManager : MonoBehaviour
{
    [SerializeField] private int epicConnectionTimeout = 5000;

    private EosTransport epicTransport;
    private LightReflectiveMirrorTransport LRMTransport;
    private NetworkManager networkManager;
    private NetworkManagerHUD networkManagerHud;
    private EOSSDKComponent epicSdk;

    private Task<bool> epicConnected;
    private bool UsingEpicTransport() => networkManager.transport is EosTransport;

    async void Awake()
    {
        DontDestroyOnLoad(this);

        epicTransport = GetComponentInChildren<EosTransport>();
        LRMTransport = GetComponentInChildren<LightReflectiveMirrorTransport>();
        networkManager = GetComponentInChildren<NetworkManager>();
        networkManagerHud = GetComponentInChildren<NetworkManagerHUD>();
        epicSdk = GetComponentInChildren<EOSSDKComponent>();

        networkManagerHud.enabled = false;


        var epicConnectedTcs = new TaskCompletionSource<bool>();
        var epicConnectedCts = new CancellationTokenSource(epicConnectionTimeout);
        epicConnected = epicConnectedTcs.Task;

        epicConnectedCts.Token.Register(() => epicConnectedTcs.SetResult(false));
        epicSdk.OnConnected.AddListener(() => epicConnectedTcs.SetResult(true));

        if (await epicConnected)
        {
            networkManager.transport = epicTransport;
        }
        else
        {
            networkManager.transport = LRMTransport;
            epicSdk.enabled = false;
        }
        networkManagerHud.enabled = true;
        Transport.active = networkManager.transport;
    }

    void OnGUI()
    {
        if (networkManager.transport == null || UsingEpicTransport()) return;
        GUI.Label(new Rect(10, 240, 220, 400), LRMTransport.serverId);
    }
}

using System;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;
using BehideServer.Types;
using Mirror;
using Unity.VisualScripting;
using EpicTransport;

public class ConnectionsManager : MonoBehaviour
{
    private const string EPIC_SCHEME = "epic";
    private enum State { ConnectingEpic, ConnectingBehideServer, RegisteringPlayer, Ready }

    public string username;

    [Header("Behide server")]
    public string ip;
    public int port;

    private Guid currentEpicId;
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

        EOSSDKComponent eosSdk = GetComponentInChildren<EOSSDKComponent>();
        eosSdk.OnConnected.AddListener(_ => StartServerConnection());

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
        var rect = new Rect(10, 250, 200, Screen.height);

        GUILayout.BeginArea(rect);

        GUILayout.Label("<b>State:</b> " + state.ToString());
        GUILayout.Label("<b>Current roomId:</b> " + currentRoomId?.ToString());
        GUItextInput = GUILayout.TextField(GUItextInput);

        if (state == State.Finished && GUILayout.Button("Create a room")) CreateRoom();
        if (state == State.Finished && GUILayout.Button("Join room") && RoomId.TryParse(textInput, out RoomId roomId)) JoinRoom(roomId);

        GUILayout.EndArea();
    }


    void StartServerConnection()
    {
        if (!Guid.TryParse(EOSSDKComponent.LocalUserProductIdString, out Guid epicId))
        {
            Debug.LogError("Failed to parse epicId.");
            return;
        };
        currentEpicId = epicId;

        state = State.ConnectingBehideServer;
        server.Start();

        state = State.RegisteringPlayer;
        RegisterPlayer();

        state = State.Ready;
    }


    async void RegisterPlayer()
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

    async void CreateRoom()
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

    async void JoinRoom(RoomId roomId)
    {
        Response response = await server.SendMessage(Msg.NewGetRoom(roomId), ResponseHeader.RoomFound);

        if (!Guid.TryParseBytes(response.Content, out Guid epicId)) {
            Debug.LogError("Failed to parse Guid.");
            return;
        }

        currentRoomId = roomId;
        targetEpicId = epicId;

        Uri uri = new UriBuilder(EPIC_SCHEME, targetEpicId.ToString("N")).Uri;
        networkManager.StartClient(uri);
    }
}
