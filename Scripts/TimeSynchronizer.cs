using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Behide.Types;
using Godot;

namespace Behide;

// Algorithm based on https://en.wikipedia.org/wiki/Network_Time_Protocol#Clock_synchronization_algorithm
/// <summary>
/// Generate clock delta to synchronize clocks between peers.
/// The peer with the lowest peer id is the reference.
/// </summary>
public partial class TimeSynchronizer : Node
{
    private Serilog.ILogger log = null!;
    private CancellationToken? timeSyncCancellationToken;
    private Subject<long> clockDeltaMeasurements = new();

    public BehaviorSubject<TimeSpan?> ClockDelta = new(null);

    public override void _EnterTree()
    {
        log = Serilog.Log.ForContext("Tag", "TimeSync");
    }

    public void Start(BehaviorSubject<Player> localPlayer)
    {
        // Cancel time synchronization when we quit the room
        var cts = new CancellationTokenSource();
        localPlayer.Subscribe(_ => { }, onCompleted: cts.Cancel, token: cts.Token);
        timeSyncCancellationToken = cts.Token;

        // Determine reference player
        if (GameManager.Room.Players.Count == 0)
        {
            log.Error("Failed to start time synchronization, no players in room");
            return;
        }
        if (GameManager.Room.Players.Count == 1)
        {
            log.Information("Stopping time synchronization because we are now the time reference");
            return;
        }

        var referencePlayer = GameManager.Room.Players.MinBy(kv => kv.Key).Value;
        referencePlayer.Subscribe(
            _ => { },
            onCompleted: () =>
            {
                cts.Cancel();
                Start(localPlayer);
            },
            token: cts.Token
        );

        // Start
        CallDeferred(nameof(StartTimeSync), referencePlayer.Value.PeerId);
    }

    private void StartTimeSync(int referencePeerId)
    {
        if (!timeSyncCancellationToken.HasValue)
        {
            log.Error("Time synchronization should have a cancellation token set.");
            return;
        }
        var ct = timeSyncCancellationToken.Value;

        // Reset observables
        clockDeltaMeasurements = new Subject<long>();
        ClockDelta = new BehaviorSubject<TimeSpan?>(null);

        // Log when clock delta is updated
        ClockDelta.Subscribe(
            delta =>
            {
                if (delta is not null) log.Debug("[Time Sync] Clock delta changed: {Delta}", delta);
            },
            ct
        );

        // `clockDeltaMeasurements` are the latencies measured. They are independent.
        // When a new delta is emitted we determine the average clock delta with the reference.
        // We discard the deltas that differ by more than 1 standard deviation from the median.
        // The purpose is to eliminate the packets that were retransmitted by tcp.
        var windowLength = 9;
        var deltaWindow = new List<long>(windowLength);
        clockDeltaMeasurements
            .Select(newValue =>
            {
                if (deltaWindow.Count >= windowLength) deltaWindow.RemoveAt(0);
                deltaWindow.Add(newValue);
                return deltaWindow.ToArray();
            })
            .Select(deltas =>
            {
                if (deltas.Length == 0) return (TimeSpan?)null;

                // Calculating standard deviation
                var average = deltas.Average();
                var variance = deltas
                    .Select(delta => Math.Pow(delta - average, 2))
                    .Average();
                var standardDeviation = Math.Sqrt(variance);

                // Finding median
                Array.Sort(deltas);
                var (q, r) = Math.DivRem(deltas.Length, 2);
                var median =
                    r == 1
                    ? deltas[q]
                    : (deltas[q] + deltas[q - 1]) / 2;

                // Filtering deltas
                var min = median - standardDeviation;
                var max = median + standardDeviation;
                var averageDelta = deltas
                    .Where(delta => min <= delta && delta <= max)
                    .Average();

                return TimeSpan.FromMilliseconds(averageDelta);
            })
            .Subscribe(avgDelta => ClockDelta.OnNext(avgDelta), ct);

        _ = Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                CallDeferred(nameof(PollTime), referencePeerId);
                try { await Task.Delay(1000, ct); }
                catch { /* ignored */ }
            }
        }, ct);
    }

    private void PollTime(int referencePeerId)
    {
        var time = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        RpcId(referencePeerId, nameof(PollTimeRpc), time);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void PollTimeRpc(long requestTime)
    {
        var now = DateTimeOffset.Now + (ClockDelta.Value ?? TimeSpan.Zero);
        var nowMs = now.ToUnixTimeMilliseconds();
        RpcId(Multiplayer.GetRemoteSenderId(), nameof(AnswerRpc), requestTime, nowMs);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void AnswerRpc(long requestTime, long receivedTime)
    {
        var currentTime = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        var clockDelta = (receivedTime - requestTime + receivedTime - currentTime) / 2;
        clockDeltaMeasurements.OnNext(clockDelta);
    }
}
