using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Behide.UI.Controls;

namespace Behide.Game.Supervisors;

public partial class PropHuntSupervisor : Supervisor
{
    [Export] private Node3D playersNode = null!;

    [ExportGroup("Countdowns")]
    [Export] private AdvancedLabelCountdown preGameCountdown = null!;
    [Export] private LabelCountdown inGameCountdown = null!;

    [ExportGroup("UI nodes")]
    [ExportSubgroup("Game parts")]
    [Export] private Control preGameProp = null!;
    [Export] private Control preGameHunter = null!;
    [Export] private Control inGame = null!;
    [Export] private Control endGame = null!;

    [ExportSubgroup("In-game")]
    [Export] private Label isPropLabel = null!;
    [Export] private Label isHunterLabel = null!;

    [ExportSubgroup("End-game")]
    [Export] private Label propsWonLabel = null!;
    [Export] private Label hunterWonLabel = null!;

    private readonly Serilog.ILogger log = Serilog.Log.ForContext("Tag", "Supervisor/PropHunt");
    private static readonly TimeSpan PreGameDuration = TimeSpan.FromMinutes(0.01);
    private static readonly TimeSpan InGameDuration = TimeSpan.FromMinutes(5);

    private readonly TaskCompletionSource<int> hunterPeerIdTcs = new();
    private Task<int> HunterPeerId => hunterPeerIdTcs.Task;

    public override void _EnterTree()
    {
        base._EnterTree();
        Spawner.PlayersNode = playersNode;

        // Show UI
        var localPeerId = Multiplayer.GetUniqueId();
        Task.Run(async () =>
        {
            if (await HunterPeerId == localPeerId)
            {
                preGameHunter.CallDeferred(CanvasItem.MethodName.Show);
                isHunterLabel.CallDeferred(CanvasItem.MethodName.Show);
            }
            else
            {
                preGameProp.CallDeferred(CanvasItem.MethodName.Show);
                isPropLabel.CallDeferred(CanvasItem.MethodName.Show);
            }
        });

        preGameCountdown.TimeElapsed += () =>
        {
            preGameHunter.Hide();
            preGameProp.Hide();
            inGame.Show();

            if (!IsMultiplayerAuthority()) return;
            Spawner.SpawnPlayer(HunterPeerId.Result);
            inGameCountdown.StartCountdown(InGameDuration);
        };

        inGameCountdown.TimeElapsed += () =>
        {
            inGame.Hide();
            endGame.Show();
            propsWonLabel.Show(); // TODO: Determine who won
        };
    }

    protected override void PlayersReady()
    {
        base.PlayersReady();
        ChooseHunter();

        // Start countdown
        if (IsMultiplayerAuthority()) Task.Run(async () =>
        {
            await HunterPeerId;
            foreach (var player in GameManager.Room.Players)
            {
                if (player.Key == await HunterPeerId) continue;
                Spawner.CallDeferred(nameof(Spawner.SpawnPlayer), player.Key); // TODO: Add spawn points
            }

            preGameCountdown.StartCountdownDeferred(PreGameDuration);
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
