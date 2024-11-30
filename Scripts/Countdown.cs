namespace Behide.Game;

using System;
using System.Threading;
using System.Threading.Tasks;

public class Countdown(TimeSpan duration)
{
    private TimeSpan timeLeft;
    private bool isRunning = false;
    private CancellationTokenSource? cts;

    public event Action<TimeSpan>? Tick;
    public event Action? Canceled;
    public event Action? Finished;

    public async void Start()
    {
        if (isRunning) return;

        timeLeft = duration;
        isRunning = true;
        cts = new CancellationTokenSource();
        cts.Token.Register(() => Canceled?.Invoke());

        Tick?.Invoke(timeLeft);

        while (timeLeft > TimeSpan.Zero && !cts.IsCancellationRequested)
        {
            var timeToElapse = timeLeft.TotalSeconds > 1 ? TimeSpan.FromSeconds(1) : timeLeft;

            try
            {
                await Task.Delay(timeToElapse, cts.Token);
            }
            catch (TaskCanceledException) { continue; }

            timeLeft -= timeToElapse;
            Tick?.Invoke(timeLeft);
        }

        if (!cts.IsCancellationRequested)
        {
            Tick?.Invoke(timeLeft);
            Finished?.Invoke();
        }
    }

    public void Cancel()
    {
        if (!isRunning) return;

        cts?.Cancel();
        isRunning = false;
        timeLeft = duration;
    }
}
