using System;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

using SuperSimpleTcp;
using BehideServer.Types;

public class ServerConnection : IDisposable
{
    private bool disposed;

    private SimpleTcpClient tcp;
    private IPEndPoint endPoint;
    public string serverVersion;

    public event EventHandler OnConnected;
    public event EventHandler OnDisconnected;
    public event EventHandler<Response> ResponseReceived;

    public BlockingQueue<Response> ResponseQueue;
    public Task<Response> GetLastResponse()
    {
        return ResponseQueue.Dequeue(1000);
    }


    public ServerConnection(IPEndPoint newEndPoint)
    {
        serverVersion = BehideServer.Version.GetVersion();
        endPoint = newEndPoint;
        ResponseQueue = new BlockingQueue<Response>();
    }

    ~ServerConnection() => Dispose();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;
        if (disposing) { tcp.Dispose(); ResponseQueue.Dispose(); };

        disposed = true;
        Debug.Log("Server connection shut down.");
    }



    async public Task SendMessage(Msg message) { Debug.Log("Sending message: " + message); await tcp.SendAsync(message.ToBytes()); }

    async public Task<Response> SendMessage(Msg message, ResponseHeader expectedResponseHeader)
    {
        _ = SendMessage(message);
        Response response = await ResponseQueue.Dequeue();

        if (response.Header != expectedResponseHeader)
        {
            string errorMsg = $"Response don't match the expected header: Expected: {expectedResponseHeader}; Actual: {response.Header}";
            Debug.LogError(errorMsg);
            throw new Exception(errorMsg);
        }

        return response;
    }


    /// <summary>
    /// Start the internal TCP server and activate event handlers.
    /// </summary>
    async public Task Start()
    {
        tcp = new SimpleTcpClient(endPoint);

        tcp.Events.DataReceived += OnData;
        tcp.Events.Connected += TcpOnConnected;
        tcp.Events.Disconnected += TcpOnDisconnected;

        await Task.Run(() => tcp.Connect());
    }


    void TcpOnConnected(object sender, ConnectionEventArgs e) => OnConnected?.Invoke(sender, e);
    void TcpOnDisconnected(object sender, ConnectionEventArgs e) => OnDisconnected.Invoke(sender, e);

    void OnData(object sender, DataReceivedEventArgs e)
    {
        if (!Response.TryParse(e.Data.ToArray(), out Response response))
        {
            Debug.LogError($"[TCP] Failed to parse response");
            return;
        }

        ResponseQueue.Enqueue(response);
        ResponseReceived.Invoke(sender, response);
    }
}
