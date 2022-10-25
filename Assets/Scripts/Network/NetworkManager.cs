using Serilog;
using Serilog.Sinks.Unity3D;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.Events;

public class NetworkManager : MonoBehaviour
{
    [SerializeField]
    private string serverAddress;
    [SerializeField]
    private int serverPort;
    [SerializeField]
    private int localPort;

    public IPEndPoint localEP;
    public IPEndPoint serverEP;

    public Serilog.Core.Logger log;

    public UnityEvent NetworkManagerReady;

    private void Awake()
    {
        serverEP = new IPEndPoint(IPAddress.Parse(serverAddress), serverPort);

        var addresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList.Where(ipAddress => ipAddress.AddressFamily == AddressFamily.InterNetwork);
        localEP = new IPEndPoint(addresses.ElementAt(0), localPort);

        log = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Unity3D()
            .CreateLogger();

        NetworkManagerReady.Invoke();
    }
}