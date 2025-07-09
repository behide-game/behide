using Godot;
using System;
using Behide;

[GlobalClass]
public partial class Countdown : Node
{
    [Export] private NodePath labelNodePath = null!;
    [Export] private string runningTextFormat = null!;
    [Export] private string beforeRunningText = null!;
    [Export] private string afterRunningText = null!;

    private Label label = null!;
    private DateTimeOffset? endDate;
    private bool timeElapsed;

    public event Action? TimeElapsed;

    public override void _EnterTree()
    {
        label = GetNode<Label>(labelNodePath);
        label.SetText(beforeRunningText);
    }

    public override void _Process(double delta)
    {
        if (!endDate.HasValue || timeElapsed) return;

        var now = DateTimeOffset.Now + (GameManager.Room.ClockDelta.Value ?? TimeSpan.Zero);
        var remainingTime = endDate - now;
        timeElapsed = remainingTime < TimeSpan.Zero;

        if (timeElapsed)
        {
            label.SetText(afterRunningText);
            if (IsMultiplayerAuthority()) TimeElapsed?.Invoke();
        }
        else label.SetText(string.Format(runningTextFormat, remainingTime));
    }

    public void StartCountdown(DateTimeOffset endDate)
    {
        if (!IsMultiplayerAuthority()) return;
        Rpc(nameof(StartCountdownRpc), endDate.ToUnixTimeMilliseconds());
    }

    public void ResetCountdown()
    {
        if (!IsMultiplayerAuthority()) return;
        Rpc(nameof(ResetCountdownRpc));
    }

    [Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void StartCountdownRpc(long endTimestamp) =>
        endDate = DateTimeOffset.FromUnixTimeMilliseconds(endTimestamp);

    [Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ResetCountdownRpc()
    {
        timeElapsed = false;
        endDate = null;
        label.SetText(beforeRunningText);
    }
}
