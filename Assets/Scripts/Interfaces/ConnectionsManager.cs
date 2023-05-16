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

    [SerializeField] private NetworkManager networkManager = null!;

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
                behideConnection.OnConnected += (_, _) => connected = (true, connected.eos);
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

        epicConnectedCts.Token.Register(() => epicConnectedTcs.SetResult(false));
        epicSdk.OnConnected.AddListener(() => epicConnectedTcs.SetResult(true));

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


    public async Task<PlayerId> RegisterPlayer(string username)
    {
        Msg msg = Msg.NewRegisterPlayer(behideConnection.serverVersion, username);
        Response response = await behideConnection.SendMessage(msg, ResponseHeader.PlayerRegistered);
        if (!PlayerId.TryParseBytes(response.Content, out PlayerId playerId)) throw new Exception("Failed to parse PlayerId");

        return playerId;
    }

    public async Task<RoomId> CreateRoom(PlayerId playerId)
    {
        if (epicId == null) throw new Exception("epicId is null. Is it connected ?");

        Msg msg = Msg.NewCreateRoom(playerId, (Guid)epicId);
        Response response = await behideConnection.SendMessage(msg, ResponseHeader.RoomCreated);
        if (!RoomId.TryParseBytes(response.Content, out RoomId roomId)) throw new Exception("Failed to parse RoomId");

        networkManager.StartHost();
        return roomId;
    }

    public async Task JoinRoom(RoomId roomId)
    {
        Response response = await behideConnection.SendMessage(Msg.NewJoinRoom(roomId), ResponseHeader.RoomJoined);
        if (!Room.TryParse(response.Content, out Room joinedRoom)) throw new Exception("Failed to parse the joined room.");

        Uri uri = new UriBuilder(EPIC_SCHEME, joinedRoom.EpicId.ToString("N")).Uri;
        networkManager.StartClient(uri);
    }
}