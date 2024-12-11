namespace Behide.Game.Supervisors;

using System;
using Godot;
using System.Linq;
using System.Threading.Tasks;

/// <summary>
/// TODO: Write a description
/// </summary>
public partial class PropHuntSupervisor : BasicSupervisor
{
    private Serilog.ILogger Log = null!;

    [Export] private NodePath countdownLabelNodePath = null!;
    [Export] private NodePath isHunterLabelNodePath = null!;
    [Export] private NodePath isPropLabelNodePath = null!;

    private const int countdownDurationInMin = 5;
    private readonly Countdown countdown = new(TimeSpan.FromMinutes(countdownDurationInMin));

    private readonly TaskCompletionSource<int> hunterPeerIdTcs = new();
    private Task<int> HunterPeerId => hunterPeerIdTcs.Task;

    #region Initialization
    public override void _EnterTree()
    {
        base._EnterTree();
        Log = Serilog.Log.ForContext("Tag", "Supervisor/PropHunt");
        SetUpUI();
    }

    public override void AllPlayersSpawned()
    {
        ChooseHunter(); // Need all players to be ready because it uses a RPC
    }

    /// <summary>
    /// Plug the countdown to the UI.
    /// Show if we are the hunter.
    /// </summary>
    private async void SetUpUI()
    {
        // Set up the countdown
        var label = GetNode<Label>(countdownLabelNodePath);
        countdown.Tick += timeLeft => label.Text = timeLeft.ToString(@"mm\:ss");

        // Show if we are the hunter
        GetNode<Control>(
            await HunterPeerId == Multiplayer.GetUniqueId()
                ? isHunterLabelNodePath
                : isPropLabelNodePath
        ).Show();
    }

    /// <summary>
    /// Choose the hunter if we are the designed player to do so.
    /// </summary>
    private void ChooseHunter()
    {
        // Block if the current player does not have the authority to choose the hunter
        if (GetMultiplayerAuthority() != Multiplayer.GetUniqueId()) return;

        var players = GameManager.Room.players;

        // Choose the hunter
        var idx = new Random().Next(players.Count);
        var playerId = players.ElementAt(idx).Key;
        Rpc(nameof(RpcSetHunter), playerId);
    }
    #endregion

    #region RPCs
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcSetHunter(int peerId)
    {
        if (!hunterPeerIdTcs.TrySetResult(peerId)) Log.Warning("Failed to set hunterPeerId. Strange...");
        Log.Information("Hunter has been chosen (PeerId: {PeerId})", peerId);
    }
    #endregion
}
