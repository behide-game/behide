namespace Behide.Game.Supervisors;

using System;
using Godot;
using Behide.Types;
using System.Linq;

/// <summary>
/// TODO: Write a description
/// </summary>
public partial class PropHuntSupervisor : Node
{
    private Serilog.ILogger Log = null!;

    [Export] private PackedScene playerPrefab = null!;
    [Export] private NodePath spawnerNodePath = null!;
    [Export] private NodePath playersNodePath = null!;
    [Export] private NodePath countdownLabelNodePath = null!;
    [Export] private NodePath isHunterLabelNodePath = null!;
    [Export] private NodePath isPropLabelNodePath = null!;

    private const int countdownDurationInMin = 5;
    private readonly Countdown countdown = new(TimeSpan.FromMinutes(countdownDurationInMin));

    private int? hunterPeerId = null;
    private event Action<int>? HunterChosen;

    #region Initialization
    public override void _EnterTree()
    {
        Log = Serilog.Log.ForContext("Tag", "Supervisor/PropHunt");

        // Set authority to the last player to join
        // The authority is the player who spawns players and choose randomly the hunter
        var lastPlayerToJoin = GameManager.Room.players.MaxBy(player => player.PeerId);
        if (lastPlayerToJoin is null) {
            Log.Error("No player found. This means that the room is empty. Strange...");
            return;
        }

        SetMultiplayerAuthority(lastPlayerToJoin.PeerId);

        SpawnPlayers();
        ChooseHunter();
        SetUpUI();
    }

    public override void _Ready()
    {
        GameManager.Room.SetPlayerState(new PlayerStateInGame());
    }

    private void SpawnPlayers()
    {
        if (GetMultiplayerAuthority() != Multiplayer.GetUniqueId()) return;

        Log.Debug("Spawning players...");
        var players = GameManager.Room.players;

        GameManager.Room.PlayerStateChanged.Subscribe(TrySpawnPlayer);
        foreach (var player in players) TrySpawnPlayer(player);
    }

    private void TrySpawnPlayer(Player player)
    {
        if (player.State is not PlayerStateInGame) {
            Log.Debug("Spawning {PeerId} error: Player is not in the correct state: {PlayerState}", player.PeerId, player.State);
            return;
        }

        // Create node and set his name
        var playerNode = playerPrefab.Instantiate<Node3D>();
        playerNode.Name = player.PeerId.ToString();

        // Put node in the world / the scene / the tree
        var playersNode = GetNode<Node3D>(playersNodePath);
        playersNode.AddChild(playerNode);

        Log.Debug("Spawned {PeerId}", player.PeerId);
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
        Rpc(nameof(RpcSetHunter), players[idx].PeerId);
    }

    /// <summary>
    /// Plug the countdown to the UI.
    /// Show if we are the hunter.
    /// </summary>
    private void SetUpUI()
    {
        // Set up the countdown
        var label = GetNode<Label>(countdownLabelNodePath);
        countdown.Tick += timeLeft => label.Text = timeLeft.ToString(@"mm\:ss");

        // Show if we are the hunter
        if (hunterPeerId is not null) // TODO: Change it when we have a loading screen
        {
            if (hunterPeerId == Multiplayer.GetUniqueId())
                GetNode<Control>(isHunterLabelNodePath).Show();
            else
                GetNode<Control>(isPropLabelNodePath).Show();
        }
        HunterChosen += hunterPeerId =>
        {
            if (hunterPeerId == Multiplayer.GetUniqueId())
                GetNode<Control>(isHunterLabelNodePath).Show();
            else
                GetNode<Control>(isPropLabelNodePath).Show();
        };
    }
    #endregion

    #region RPCs
    [Rpc(MultiplayerApi.RpcMode.Authority, CallLocal = true)]
    public void RpcSetHunter(int peerId)
    {
        hunterPeerId = peerId;
        HunterChosen?.Invoke(peerId);
        Log.Information("Hunter has been chosen (PeerId: {PeerId})", peerId);
    }
    #endregion
}
