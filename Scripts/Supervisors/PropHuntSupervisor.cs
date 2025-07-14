using System;
using System.Linq;
using System.Threading.Tasks;

namespace Behide.Game.Supervisors;

using Godot;

public partial class PropHuntSupervisor : Supervisor
{
    [Export] private Countdown countdown = null!;
    [Export] private Label isHunterLabel = null!;
    [Export] private Label isPropLabel = null!;

    private readonly Serilog.ILogger log = Serilog.Log.ForContext("Tag", "Supervisor/PropHunt");
    private static readonly TimeSpan CountdownDuration = TimeSpan.FromMinutes(5);

    private readonly TaskCompletionSource<int> hunterPeerIdTcs = new();
    private Task<int> HunterPeerId => hunterPeerIdTcs.Task;

    public override void _EnterTree()
    {
        base._EnterTree();

        // Show if we are the hunter
        var localPeerId = Multiplayer.GetUniqueId();
        Task.Run(async () =>
        {
            var label = await HunterPeerId == localPeerId
                ? isHunterLabel
                : isPropLabel;
            label.CallDeferred(CanvasItem.MethodName.Show);
        });
    }

    protected override void PlayersReady()
    {
        base.PlayersReady();
        foreach (var player in GameManager.Room.Players) Spawner.SpawnPlayer(player.Key);
        ChooseHunter();

        if (IsMultiplayerAuthority()) Task.Run(async () =>
        {
            await HunterPeerId;
            countdown.StartCountdownDeferred(DateTimeOffset.Now + CountdownDuration);
        });
    }

    private void ChooseHunter()
    {
        if (!IsMultiplayerAuthority()) return;
        var idx = new Random().Next(Players.Count);
        var playerId = Players.ElementAt(idx).Key;
        Rpc(nameof(RpcSetHunter), playerId);
    }

    [Rpc(CallLocal = true)]
    public void RpcSetHunter(int peerId)
    {
        if (!hunterPeerIdTcs.TrySetResult(peerId)) log.Warning("Failed to set hunterPeerId. The hunter has been chose multiple times ?");
        log.Information("Hunter has been chosen (PeerId: {PeerId})", peerId);
    }
}
