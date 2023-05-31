#nullable enable
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Mirror;
using BehideServer.Types;
using EpicTransport;

public class ConnectionsManager : MonoBehaviour
{
    private const string EPIC_SCHEME = "epic";

    public NetworkManager networkManager = null!;

    [Header("Behide server")]
    [SerializeField] private string ip = "82.64.42.87";
    [SerializeField] private int port = 6567;
    private BehideServerConnection behideConnection = null!;

    [Header("Epic")]
    [SerializeField] private int epicConnectionTimeout = 5000;
    private Guid? epicId;

    [HideInInspector] public (bool behide, bool eos) connected = (false, false);
    [HideInInspector] public string? connectError = null;


    async void Awake()
    {
        DontDestroyOnLoad(this);

        // Connect to Behide's server
        _ = Task.Run(async () =>
        {
            try
            {
                IPEndPoint ipEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);
                behideConnection = new BehideServerConnection(ipEndPoint);
                behideConnection.OnConnected += (_, _) => CheckBehideServerVersion();
                await behideConnection.Start();
            }
            catch (Exception error)
            {
                connectError = error.Message;
                Debug.LogError($"Failed to connect Behide's server: {error.Message}");
            }
        });


        // Connect to EOS
        var epicSdk = GetComponentInChildren<EOSSDKComponent>();

        var epicConnectedCts = new CancellationTokenSource(epicConnectionTimeout);
        var epicConnectedTcs = new TaskCompletionSource<bool>();
        var epicConnected = epicConnectedTcs.Task;

        epicConnectedCts.Token.Register(() => epicConnectedTcs.TrySetResult(false));
        epicSdk.OnConnected.AddListener(() => epicConnectedTcs.TrySetResult(true));

        if (!await epicConnected) // When failed to connect
        {
            connectError = "Failed to start EpicTransport";
            Debug.LogError(connectError);
            epicSdk.enabled = false;
            return;
        }

        if (!Guid.TryParse(EOSSDKComponent.LocalUserProductIdString, out Guid newEpicId)) Debug.LogError("Failed to parse epicId");
        epicId = newEpicId;
        connected = (connected.behide, true);
    }

    void OnDestroy() => behideConnection?.Dispose();

    private async void CheckBehideServerVersion()
    {
        Msg msg = Msg.NewCheckServerVersion(BehideServer.Version.GetVersion());

        try
        {
            Response response = await behideConnection.SendMessage(msg, ResponseHeader.CorrectServerVersion);
        }
        catch (Exception error)
        {
            connectError = error.Message;
        }

        connected = (true, connected.eos);
    }


    /// <summary>
    /// Create a room on behide's server and start the peer to peer host
    /// </summary>
    public async Task<RoomId> CreateRoom()
    {
        if (epicId == null) throw new Exception("epicId is null. Is it connected ?");

        Msg msg = Msg.NewCreateRoom((Guid)epicId);
        Response response = await behideConnection.SendMessage(msg, ResponseHeader.RoomCreated);
        if (!RoomId.TryParseBytes(response.Content, out RoomId roomId)) throw new Exception("Failed to parse RoomId");

        networkManager.StartHost();
        return roomId;
    }

    /// <summary>
    /// Join a room on behide's server from it's id and connect to the host in peer to peer
    /// </summary>
    public async Task JoinRoom(RoomId roomId)
    {
        Response response = await behideConnection.SendMessage(Msg.NewGetRoom(roomId), ResponseHeader.RoomGet);
        if (!GuidHelper.TryParseBytes(response.Content, out Guid roomEpicId)) throw new Exception("Failed to parse the joined room.");

        Uri uri = new UriBuilder(EPIC_SCHEME, roomEpicId.ToString("N")).Uri;
        networkManager.StartClient(uri);
    }
}