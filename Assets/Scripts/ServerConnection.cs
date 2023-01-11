using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;

using SuperSimpleTcp;
using BehideServer.Types;

public class BlockingQueue<T> : IDisposable
{
    private bool disposed;
    private readonly Queue<T> queue;
    private event EventHandler OnElementEnqueued;

    public BlockingQueue()
    {
        queue = new Queue<T>();
    }

    ~BlockingQueue() => Dispose();

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed) return;
        if (disposing) queue.Clear();
        disposed = true;
    }


    public void Enqueue(T item)
    {
        if (disposed) return;
        queue.Enqueue(item);
        OnElementEnqueued?.Invoke(this, EventArgs.Empty);
    }

    public async Task<T> Dequeue(int timeoutMs = 5000)
    {
        if (disposed) { throw new ObjectDisposedException("BlockingQueue"); }
        if (queue.Count == 0)
        {
            var tcs = new TaskCompletionSource<T>();
            var cts = new CancellationTokenSource(timeoutMs);

            void handler(object _, object _arg)
            {
                if (queue.TryDequeue(out T result)) {
                    tcs.SetResult(result);
                    return;
                };

                tcs.SetException(new Exception("Queue is empty but it should not."));
            }

            void timeoutHandler()
            {
                tcs.SetException(new TimeoutException());
                OnElementEnqueued -= handler;
            }

            OnElementEnqueued += handler;
            cts.Token.Register(timeoutHandler);

            var taskResult = await tcs.Task;
            OnElementEnqueued -= handler;

            return taskResult;
        }

        queue.TryDequeue(out T result);
        return result;
    }
}

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



    async public Task SendMessage(Msg message)
    {
        await tcp.SendAsync(message.ToBytes());
    }

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
    /// Start the internal TCP server and activate event handler.
    /// </summary>
    public void Start()
    {
        tcp = new SimpleTcpClient(endPoint);

        tcp.Events.DataReceived += OnData;
        tcp.Events.Connected += TcpOnConnected;
        tcp.Events.Disconnected += TcpOnDisconnected;

        tcp.Connect();
    }


    void TcpOnConnected(object sender, ConnectionEventArgs e)
    {
        //Debug.Log("[TCP] Connected: " + e.IpPort);
        OnConnected?.Invoke(sender, e);
    }

    void TcpOnDisconnected(object sender, ConnectionEventArgs e)
    {
        //Debug.Log("[TCP] Disconnected: " + e.Reason);
        OnDisconnected.Invoke(sender, e);
    }

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
