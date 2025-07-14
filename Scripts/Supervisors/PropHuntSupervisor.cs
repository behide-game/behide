namespace Behide.Game.Supervisors;

using System;
using Godot;
using System.Threading.Tasks;

public partial class PropHuntSupervisor : BasicSupervisor
{
    private Serilog.ILogger log = null!;
    [Export] private Countdown countdown = null!;

    private static readonly TimeSpan countdownDuration = TimeSpan.FromMinutes(5);

    private readonly TaskCompletionSource<int> hunterPeerIdTcs = new();
    private Task<int> HunterPeerId => hunterPeerIdTcs.Task;

    // #region Initialization
    public override void _EnterTree()
    {
        base._EnterTree();
        log = Serilog.Log.ForContext("Tag", "Supervisor/PropHunt");
        // SetUpUI();
    }

    protected override void PlayersReady()
    {
        foreach (var player in GameManager.Room.Players)
        {
            SpawnPlayer(player.Key);
        }
    }

    // private async void SetUpUI()
    // {
    //     // Show if we are the hunter
    //     GetNode<Control>(
    //         await HunterPeerId == Multiplayer.GetUniqueId()
    //             ? isHunterLabelNodePath
    //             : isPropLabelNodePath
    //     ).Show();
    // }
    //
    // private void ChooseHunter()
    // {
    //     // Block if the current player does not have the authority to choose the hunter
    //     if (GetMultiplayerAuthority() != Multiplayer.GetUniqueId()) return;
    //
    //     var players = GameManager.Room.Players;
    //
    //     // Choose the hunter
    //     var idx = new Random().Next(players.Count);
    //     var playerId = players.ElementAt(idx).Key;
    //     Rpc(nameof(RpcSetHunter), playerId);
    // }
    // #endregion
    //
    // #region RPCs
    // [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    // public void RpcSetHunter(int peerId)
    // {
    //     if (!hunterPeerIdTcs.TrySetResult(peerId)) log.Warning("Failed to set hunterPeerId. Strange...");
    //     log.Information("Hunter has been chosen (PeerId: {PeerId})", peerId);
    // }
    // #endregion
}
