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

public class GameManager : MonoBehaviour
{
    private enum State { Initializing, Connecting, RegisteringPlayer, CreatingRoom, Finished }

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

    void Awake()
    {
        DontDestroyOnLoad(this);
        state = State.Initializing;

        EOSSDKComponent eosSdk = GetComponentInChildren<EOSSDKComponent>();
        eosSdk.OnConnected.AddListener(_ => StartServerConnection());

        networkManager = GetComponentInChildren<NetworkManager>();
    }

    void OnDestroy()
    {
        server?.Dispose();
    }

    private string textInput;
    private void OnGUI()
    {
        var rect = new Rect(10, 240, 200, Screen.height);

        GUILayout.BeginArea(rect);

        GUILayout.Label(state.ToString());
        GUILayout.Label("<b>Current roomId:</b> " + currentRoomId?.ToString());
        textInput = GUILayout.TextField(textInput);

        if (GUILayout.Button("Connect to to room") && state == State.Finished && RoomId.TryParse(textInput, out RoomId roomId))
        {
            ConnectToRoom(roomId);
        }

        GUILayout.EndArea();
    }


    async void StartServerConnection()
    {
        if (!Guid.TryParse(EOSSDKComponent.LocalUserProductIdString, out Guid epicId))
        {
            Debug.LogError("Failed to parse epicId.");
            return;
        };
        currentEpicId = epicId;

        IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
        server = new ServerConnection(ipEndPoint);

        state = State.Connecting;
        server.Start();

        state = State.RegisteringPlayer;
        await RegisterPlayer();
        state = State.CreatingRoom;
        await CreateRoom();

        state = State.Finished;
    }

    async void ConnectToRoom(RoomId roomId)
    {
        await GetRoom(roomId);

        Uri uri = new UriBuilder("epic", targetEpicId.ToString("N")).Uri;
        networkManager.StartClient(uri);
    }


    async Task RegisterPlayer()
    {
        Msg msg = Msg.NewRegisterPlayer(server.serverVersion, username);
        Response response = await server.SendMessage(msg, ResponseHeader.PlayerRegistered);

        if (!PlayerId.TryParseBytes(response.Content, out PlayerId playerId))
        {
            Debug.LogError("Failed to parse PlayerId");
        }

        currentPlayerId = playerId;
    }

    async Task CreateRoom()
    {
        Msg msg = Msg.NewCreateRoom(currentPlayerId, currentEpicId);
        Response response = await server.SendMessage(msg, ResponseHeader.RoomCreated);

        if (!RoomId.TryParseBytes(response.Content, out RoomId roomId))
        {
            Debug.LogError("Failed to parse RoomId");
        }

        currentRoomId = roomId;
    }

    async Task GetRoom(RoomId roomId)
    {
        Msg msg = Msg.NewGetRoom(roomId);
        Response response = await server.SendMessage(msg, ResponseHeader.RoomFound);

        targetEpicId = new Guid(response.Content);
    }
}
