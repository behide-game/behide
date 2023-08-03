#nullable enable
using System;
using System.Net;
using System.Threading.Tasks;
using UnityEngine;

using SuperSimpleTcp;
using BehideServer.Types;

public class BehideResponseNotExcepted : Exception
{
    public BehideResponseNotExcepted() { }
    public BehideResponseNotExcepted(string message) : base(message) { }
    public BehideResponseNotExcepted(string message, Exception inner) : base(message, inner) { }
}

public class BehideServerConnection : IDisposable
{
    private bool disposed;

    private SimpleTcpClient tcp;
    private IPEndPoint endPoint;
    public string serverVersion;

    public event EventHandler OnConnected = null!;
    public event EventHandler OnDisconnected = null!;
    public event EventHandler<Response> ResponseReceived = null!;

    public BlockingQueue<Response> ResponseQueue;
    public Task<Response> GetLastResponse() => ResponseQueue.Dequeue(1000);


    public BehideServerConnection(IPEndPoint newEndPoint)
    {
        serverVersion = BehideServer.Version.GetVersion();
        endPoint = newEndPoint;
        ResponseQueue = new BlockingQueue<Response>();
        tcp = new SimpleTcpClient(endPoint);
    }

    ~BehideServerConnection() => Dispose();

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



    async public Task SendMessage(Msg message) => await tcp.SendAsync(message.ToBytes());

    async public Task<Response> SendMessage(Msg message, ResponseHeader expectedResponseHeader) // TODO: use result
    {
        _ = SendMessage(message);
        Response response = await ResponseQueue.Dequeue();

        if (response.Header != expectedResponseHeader)
        {
            string errorMsg = $"Response don't match the expected header: Expected: {expectedResponseHeader}; Actual: {response.Header}";
            throw new BehideResponseNotExcepted(errorMsg);
        }

        return response;
    }


    /// <summary>
    /// Start the internal TCP server and activate event handlers.
    /// </summary>
    async public Task Start()
    {
        tcp.Events.DataReceived += OnData;
        tcp.Events.Connected += TcpOnConnected;
        tcp.Events.Disconnected += TcpOnDisconnected;

        await Task.Run(() => tcp.Connect());
    }


    void TcpOnConnected(object sender, ConnectionEventArgs e) {
        Debug.Log("Connected to Behide server");
        OnConnected.Invoke(sender, e);
    }
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


    public async Task<string?> CheckBehideServerVersion()
    {
        Msg msg = Msg.NewCheckServerVersion(BehideServer.Version.GetVersion());

        try
        {
            Response response = await SendMessage(msg, ResponseHeader.CorrectServerVersion);
            return null;
        }
        catch (Exception error)
        {
            return error.Message;
        }
    }
}