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
