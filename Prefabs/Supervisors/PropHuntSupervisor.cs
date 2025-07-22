using System;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Behide.UI.Controls;

namespace Behide.Game.Supervisors;

[SceneTree]
public partial class PropHuntSupervisor : Supervisor
{
    [Export] private Node PlayersNode
    {
        get => Spawner.PlayersNode;
        set => Spawner.PlayersNode = value;
    }
    [Export] private new Node BehideObjects
    {
        get => ((Supervisor)this).BehideObjects;
        set => ((Supervisor)this).BehideObjects = value;
    }

    private AdvancedLabelCountdown preGameCountdown = null!;
    private LabelCountdown inGameCountdown = null!;

    private Control preGameProp = null!;
    private Control preGameHunter = null!;
    private Control inGame = null!;
    private Control endGame = null!;

    private Label isPropLabel = null!;
    private Label isHunterLabel = null!;

    private Label propsWonLabel = null!;
    private Label hunterWonLabel = null!;

    private readonly Serilog.ILogger log = Serilog.Log.ForContext("Tag", "Supervisor/PropHunt");
    private static readonly TimeSpan preGameDuration = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan inGameDuration = TimeSpan.FromMinutes(5);

    private readonly TaskCompletionSource<int> hunterPeerIdTcs = new();
    private Task<int> HunterPeerId => hunterPeerIdTcs.Task;

    public override void _EnterTree()
    {
        base._EnterTree();
        // Load nodes
        preGameCountdown = _.UI.AdvancedLabelCountdown;
        preGameProp = _.UI.Pre_gameProp;
        preGameHunter = _.UI.Pre_gameHunter;

        inGame = _.UI.In_game;
        inGameCountdown = _.UI.In_game.Countdown;
        isPropLabel = _.UI.In_game.IsProp;
        isHunterLabel = _.UI.In_game.IsHunter;

        endGame = _.UI.End_game;
        propsWonLabel = _.UI.End_game.PropsWin;
        hunterWonLabel = _.UI.End_game.HunterWins;

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
            inGameCountdown.StartCountdown(inGameDuration);
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

            preGameCountdown.StartCountdownDeferred(preGameDuration);
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
