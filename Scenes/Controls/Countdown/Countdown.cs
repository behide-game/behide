using Godot;
using System;

namespace Behide.UI.Controls;

/// <summary>
/// A network synced countdown
/// </summary>
public abstract partial class Countdown : Node
{
    private DateTimeOffset? endDate;
    private bool timeElapsed;

    public event Action? TimeElapsed;

    protected abstract void Process(bool timeElapsed, TimeSpan remainingTime);
    protected abstract void Reset();

    public override void _Process(double delta)
    {
        if (!endDate.HasValue || timeElapsed) return;

        var now = DateTimeOffset.Now + (GameManager.TimeSync.ClockDelta.Value ?? TimeSpan.Zero);
        var remainingTime = endDate.Value - now;
        timeElapsed = remainingTime < TimeSpan.Zero;

        Process(timeElapsed, remainingTime);
        if (timeElapsed) TimeElapsed?.Invoke();
    }

    public void StartCountdown(double duration)
    {
        if (!IsMultiplayerAuthority()) return;
        var endDate = DateTimeOffset.Now + TimeSpan.FromMilliseconds(duration);
        var endTimestamp = endDate.ToUnixTimeMilliseconds();
        Rpc(nameof(StartCountdownRpc), endTimestamp);
    }

    public void StartCountdown(TimeSpan duration) => StartCountdown(duration.TotalMilliseconds);
    public void StartCountdownDeferred(TimeSpan duration) => CallDeferred(nameof(StartCountdown), duration.TotalMilliseconds);

    public void ResetCountdown()
    {
        if (!IsMultiplayerAuthority()) return;
        Rpc(nameof(ResetCountdownRpc));
    }

    [Rpc(CallLocal = true)]
    private void StartCountdownRpc(long endTimestamp) =>
        endDate = DateTimeOffset.FromUnixTimeMilliseconds(endTimestamp);

    [Rpc(CallLocal = true)]
    private void ResetCountdownRpc()
    {
        timeElapsed = false;
        endDate = null;
        Reset();
    }
}
