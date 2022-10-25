using BehideServer.Interop.Udp;
using System;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

[RequireComponent(typeof(NetworkManager))]
public class UdpService : MonoBehaviour
{
    [SerializeField]
    private NetworkManager networkManager;
    private Serilog.ILogger log;

    private UdpClient udp;
    private Thread udpThread;

    private DateTimeOffset pingSendMoment;

    private void Awake()
    {
        networkManager.NetworkManagerReady.AddListener(StartService);
    }

    private void StartService()
    {
        log = networkManager.log;
        udp = new UdpClient(networkManager.localEP);

        udpThread = new Thread(new ThreadStart(UdpReceiveLoop));
        udpThread.Name = "udpThread";
        udpThread.IsBackground = true;
        udpThread.Start();

        byte[] pingMsg = new Msg(MsgHeader.Ping, new byte[0]).toBytes();
        pingSendMoment = DateTimeOffset.Now;

        udp.Send(pingMsg, pingMsg.Length, networkManager.serverEP);
    }

    private void OnDestroy()
    {
        if (udpThread != null)
        {
            udpThread.Abort();
        }
    }

    private void UdpReceiveLoop()
    {
        try
        {
            while (true)
            {
                IPEndPoint remoteEP = new(IPAddress.Any, 29000);
                byte[] receiveBytes = udp.Receive(ref remoteEP);
                Msg msg = Msg.fromBytes(receiveBytes);

                switch (msg.Header)
                {
                    case MsgHeader.Ping:
                        DateTimeOffset now = DateTimeOffset.Now;
                        double difference = Math.Ceiling((now - pingSendMoment).TotalMilliseconds);

                        log.Information("[UDP] Ping {0}ms", difference);
                        break;
                }
            }
        }
        catch (ThreadAbortException) { }
        catch (Exception e)
        {
            log.Error($"Error in udp receive loop: {e}");
        }
        finally
        {
            udp.Close();
        }
    }
}