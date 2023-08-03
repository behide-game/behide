#nullable enable
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using EpicTransport;
using BehideServer.Types;

public class NetworkManager : MonoBehaviour
{
    public (Result? eos, Result? behide) connected;
    public Result<Guid>? epicId;

    public bool behideConnected() => connected.behide?.Success ?? false;
    public bool eosConnected() => connected.eos?.Success ?? false;

    public event Action<Mirror.NetworkConnectionToClient, BehideNetwork.IBehideNetworkMsg> OnServerNetworkMessage = null!;
    public event Action<BehideNetwork.IBehideNetworkMsg> OnClientNetworkMessage = null!;

    [Header("Behide server")]
    [SerializeField] private string behideServerIp = "82.64.42.87";
    [SerializeField] private int behideServerPort = 6567;
    [SerializeField] private int behideConnectTimeout;
    private BehideServerConnection behideConnection = null!;

    [Header("Epic Online Services")]
    [SerializeField] private int eosConnectTimeout;
    private const string EPIC_SCHEME = "epic";

    private BehideNetworkManager mirrorNetwork = null!;

    void Awake()
    {
        mirrorNetwork = (BehideNetworkManager)Mirror.NetworkManager.singleton;

        mirrorNetwork.OnServerNetworkMessage += (conn, msg) => OnServerNetworkMessage?.Invoke(conn, msg);
        mirrorNetwork.OnClientNetworkMessage += msg => OnClientNetworkMessage?.Invoke(msg);

        ConnectBehideServer();
        ConnectEos();
    }
    void OnDestroy() => behideConnection?.Dispose();


    private async void ConnectBehideServer()
    {
        try
        {
            var tcs = new TaskCompletionSource<bool>();

            var cts = new CancellationTokenSource(behideConnectTimeout);
            cts.Token.Register(() => tcs.TrySetResult(false));

            var ipEndPoint = new IPEndPoint(IPAddress.Parse(behideServerIp), behideServerPort);
            behideConnection = new BehideServerConnection(ipEndPoint);
            behideConnection.OnConnected += (_, _) => tcs.TrySetResult(true);

            await behideConnection.Start();

            if (await tcs.Task)
            {
                var errorOpt = await behideConnection.CheckBehideServerVersion();
                connected.behide = errorOpt != null ? Result.Fail(errorOpt) : Result.Ok();
            }
            else connected.behide = Result.Fail("Behide server connection locally timed out.");

        }
        catch (Exception error)
        {
            connected.behide = Result.Fail(error.Message);
        }

        var ui = GameManager.instance.ui;

        if (connected.behide.Success) ui.LogInfo("Behide server connected");
        else ui.LogError(connected.behide.Error);
    }

    private async void ConnectEos()
    {
        var connectedTcs = new TaskCompletionSource<Epic.OnlineServices.Result>();

        var cts = new CancellationTokenSource(eosConnectTimeout);
        cts.Token.Register(() => connectedTcs.TrySetResult(Epic.OnlineServices.Result.TimedOut));

        var epicSdk = GetComponentInChildren<EOSSDKComponent>();
        epicSdk.OnConnected.AddListener(result => connectedTcs.TrySetResult(result));

        var result = await connectedTcs.Task;

        if (result == Epic.OnlineServices.Result.Success)
        {
            connected.eos = Result.Ok();

            if (Guid.TryParse(EOSSDKComponent.LocalUserProductIdString, out Guid parsedEpicId))
            {
                epicId = Result.Ok(parsedEpicId);
                GameManager.instance.session.SetEpicId(parsedEpicId);
            }
            else epicId = Result.Fail<Guid>("Failed to parse epic id");
        }
        else if (result == Epic.OnlineServices.Result.TimedOut)
        {
            epicId = Result.Fail<Guid>("EOS connection locally timed out.");
            connected.eos = Result.Fail("EOS connection locally timed out.");
        }
        else
        {
            epicId = Result.Fail<Guid>("EOS connection failed: " + result.ToString());
            connected.eos = Result.Fail("EOS connection failed: " + result.ToString());
        }

        var ui = GameManager.instance.ui;

        if (connected.eos.Success) ui.LogInfo("EOS connected");
        else ui.LogError(connected.eos.Error);
    }


    public void StopClient() => mirrorNetwork.StopClient();
    public void StopServer() => mirrorNetwork.StopServer();
    public GameObject GetPlayerPrefab() => mirrorNetwork.playerPrefab;

    private async Task SetScene(string sceneName)
    {
        var tcs = new TaskCompletionSource<bool>();
        mirrorNetwork.ServerChangeScene(sceneName);
        Mirror.NetworkManager.loadingSceneAsync.completed += (_) => tcs.TrySetResult(true);

        await tcs.Task;
    }
    public Task SetGameScene() => SetScene(GameManager.instance.scenes.gameSceneName);
    public Task SetHomeScene() => SetScene(GameManager.instance.scenes.homeSceneName);


    /// <summary>
    /// Create a room on behide's server and start the Mirror host
    /// </summary>
    public async Task<Result<RoomId>> CreateRoom()
    {
        if (epicId == null || epicId.IsFailure) return Result.Fail<RoomId>("invalid epicId");

        Msg msg = Msg.NewCreateRoom(epicId.Value);
        Response response = await behideConnection.SendMessage(msg, ResponseHeader.RoomCreated);

        if (!RoomId.TryParseBytes(response.Content, out RoomId roomId))
            return Result.Fail<RoomId>("failed to parse RoomId");

        mirrorNetwork.StartHost();
        return Result.Ok(roomId);
    }

    /// <summary>
    /// Join a room on behide's server from it's id and connect to the Mirror host
    /// </summary>
    public async Task<Result> JoinRoom(RoomId roomId)
    {
        Response response = await behideConnection.SendMessage(Msg.NewGetRoom(roomId), ResponseHeader.RoomGet);
        if (!GuidHelper.TryParseBytes(response.Content, out Guid roomEpicId)) return Result.Fail("roomId returned by server not parsable");

        Uri uri = new UriBuilder(EPIC_SCHEME, roomEpicId.ToString("N")).Uri;


        TaskCompletionSource<bool> tcs = new();

        Action? handler = null;
        mirrorNetwork.OnClientConnected += handler = () => {
            tcs.TrySetResult(true);
            mirrorNetwork.OnClientConnected -= handler;
        };

        Action? handler1 = null;
        Mirror.Transport.active.OnClientDisconnected += handler1 = () => {
            tcs.TrySetResult(false);
            Mirror.Transport.active.OnClientDisconnected -= handler1;
        };

        mirrorNetwork.StartClient(uri);

        return await tcs.Task ? Result.Ok() : Result.Fail("probably timed out");
    }
}