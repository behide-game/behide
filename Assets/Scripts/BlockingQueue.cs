using System;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;

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