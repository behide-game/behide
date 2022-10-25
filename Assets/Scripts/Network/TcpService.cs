using BehideServer.Interop.Tcp;
using SuperSimpleTcp;
using System;
using System.Threading;
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(NetworkManager))]
public class TcpService : MonoBehaviour
{
    [SerializeField]
    private NetworkManager networkManager;
    private Serilog.ILogger log;
    //public UnityEvent connectedEvent;

    private SimpleTcpClient tcp;
    private Thread tcpThread;

    private DateTimeOffset pingSendMoment;

    private void Awake()
    {
        networkManager.NetworkManagerReady.AddListener(StartService);
    }

    private void StartService()
    {
        log = networkManager.log;
        tcpThread = new Thread(new ThreadStart(InitTcp));
        tcpThread.Name = "tcpThread";
        tcpThread.IsBackground = true;
        tcpThread.Start();
    }

    private void InitTcp()
    {
        tcp = new SimpleTcpClient(networkManager.serverEP);
        tcp.Connect();

        tcp.Events.DataReceived += OnDataReceived;

        pingSendMoment = DateTimeOffset.Now;
        byte[] bytes = new Msg(MsgHeader.Ping, new byte[0]).toBytes();
        tcp.Send(bytes);
    }

    private void OnDataReceived(object sender, DataReceivedEventArgs e)
    {
        Msg msg = Msg.fromBytes(e.Data);
        switch (msg.Header)
        {
            case MsgHeader.Ping:
                DateTimeOffset now = DateTimeOffset.Now;
                double difference = Math.Ceiling((now - pingSendMoment).TotalMilliseconds);

                log.Information("[TCP] Ping {0}ms", difference);
                break;
        }
    }

    private void OnDestroy()
    {
        if (tcp != null)
        {
            tcp.Disconnect();
            tcpThread.Abort();
        }
    }
}