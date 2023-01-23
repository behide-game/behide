using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Mirror;
using BehideServer.Types;
using EpicTransport;
using UnityEngine.Events;

public class ConnectionsManager : MonoBehaviour
{
    private const string EPIC_SCHEME = "epic";

    [HideInInspector]
    public UnityEvent OnConnected;

    [Header("Behide server")]
    public string ip;
    public int port;

    [Header("Epic")]
    [SerializeField] private int epicConnectionTimeout = 5000;
    private Task<bool> epicConnected;
    private Guid epicId;

    private ServerConnection server;
    private NetworkManager networkManager;

    private string GUItextInput;

    async void Awake()
    {
        DontDestroyOnLoad(this);

        networkManager = GetComponentInChildren<NetworkManager>();
        var epicSdk = GetComponentInChildren<EOSSDKComponent>();

        var epicConnectedTcs = new TaskCompletionSource<bool>();
        var epicConnectedCts = new CancellationTokenSource(epicConnectionTimeout);
        epicConnected = epicConnectedTcs.Task;

        epicConnectedCts.Token.Register(() => epicConnectedTcs.SetResult(false));
        epicSdk.OnConnected.AddListener(() => epicConnectedTcs.SetResult(true));

        if (await epicConnected)
        {
            if (!Guid.TryParse(EOSSDKComponent.LocalUserProductIdString, out epicId)) Debug.LogError("Failed to parse epicId");

            // Create connection with BehideServer
            IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            server = new ServerConnection(ipEndPoint);
            server.OnConnected += (_, _) => OnConnected.Invoke();
            server.Start();
        }
        else
        {
            Debug.LogError("Failed to start EpicTransport");
            epicSdk.enabled = false;
        }
    }

    void OnDestroy()
    {
        server?.Dispose();
    }


    public async Task<PlayerId> RegisterPlayer(string username)
    {
        Msg msg = Msg.NewRegisterPlayer(server.serverVersion, username);
        Response response = await server.SendMessage(msg, ResponseHeader.PlayerRegistered);
        if (!PlayerId.TryParseBytes(response.Content, out PlayerId playerId)) throw new Exception("Failed to parse PlayerId");

        return playerId;
    }

    public async Task<RoomId> CreateRoom(PlayerId playerId)
    {
        Msg msg = Msg.NewCreateRoom(playerId, epicId);
        Response response = await server.SendMessage(msg, ResponseHeader.RoomCreated);
        if (!RoomId.TryParseBytes(response.Content, out RoomId roomId)) throw new Exception("Failed to parse RoomId");

        networkManager.StartHost();
        return roomId;
    }

    public async Task JoinRoom(RoomId roomId)
    {
        Response response = await server.SendMessage(Msg.NewJoinRoom(roomId), ResponseHeader.RoomJoined);
        if (!Room.TryParse(response.Content, out Room joinedRoom)) throw new Exception("Failed to parse the joined room.");

        Uri uri = new UriBuilder(EPIC_SCHEME, joinedRoom.EpicId.ToString("N")).Uri;
        networkManager.StartClient(uri);
    }
}