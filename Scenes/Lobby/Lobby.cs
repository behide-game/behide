namespace Behide.Game.UI.Lobby;

using Godot;
using Behide.Types;
using Behide.OnlineServices.Signaling;
using Behide.UI.Controls;
using Serilog;
using System;
using System.Linq;
using System.Threading;

public partial class Lobby : Control
{
    [Export] private PackedScene playerListItemScene = null!;
    [Export] private NodePath codeInputPath = "";
    [Export] private NodePath playerListPath = "";
    [Export] private NodePath countdownPath = "";
    [Export] private NodePath readyButtonTextPath = "";

    private Control chooseModeControl = null!;
    private Control lobbyControl = null!;

    private ILogger Log = null!;

    private readonly Countdown countdown = new(TimeSpan.FromSeconds(5));
    private CancellationTokenSource eventsCts = new();


    public override void _EnterTree()
    {
        Log = Serilog.Log.ForContext("Tag", "UI/Lobby");

        chooseModeControl = GetNode<Control>("ChooseMode");
        lobbyControl = GetNode<Control>("Lobby");

        chooseModeControl.Show();
        lobbyControl.Hide();

        var countdownLabel = GetNode<Label>(countdownPath);
        var countdownDefaultText = countdownLabel.Text;

        countdown.Tick += timeLeft => countdownLabel.Text = $"Starting in {timeLeft.TotalSeconds}s";
        countdown.Canceled += () => countdownLabel.Text = countdownDefaultText;
        countdown.Finished += () =>
        {
            countdownLabel.Text = "Starting game...";
            GameManager.instance.SetGameState(GameManager.GameState.Game);
            GameManager.Room.SetPlayerState(new PlayerStateInGame());
        };
    }
    public override void _ExitTree()
    {
        eventsCts.Cancel();
        eventsCts.Dispose();
        eventsCts = new CancellationTokenSource();
    }

    private void ShowLobby(RoomId roomId)
    {
        chooseModeControl.Hide();
        lobbyControl.Show();
        GetNode<Label>("Lobby/Header/Code/Value").Text = RoomId.raw(roomId);

        GameManager.Room.players.ForEach(OnPlayerRegistered);
        GameManager.Room.PlayerRegistered.Subscribe(OnPlayerRegistered, eventsCts.Token);
        GameManager.Room.PlayerLeft.Subscribe(OnPlayerLeft, eventsCts.Token);
        GameManager.Room.PlayerStateChanged.Subscribe(OnPlayerStateChanged, eventsCts.Token);
        eventsCts.Token.Register(countdown.Cancel);
    }
    private void HideLobby()
    {
        lobbyControl.Hide();
        chooseModeControl.Show();

        _ExitTree(); // Unsubscribe from events and stop countdown

        var playerControls = GetNode<VBoxContainer>(playerListPath).GetChildren();
        foreach (var playerControl in playerControls) playerControl.QueueFree();

        var readyControl = GetNode<Label>(readyButtonTextPath);
        readyControl.Text = "Not ready";
    }

    private async void HostButtonPressed()
    {
        var res = await GameManager.Room.CreateRoom();

        res.Match(
            success: roomId =>
            {
                Log.Information("Room created with code {RoomId}", RoomId.raw(roomId));
                ShowLobby(roomId);
            },
            failure: Log.Error
        );
    }
    private async void JoinButtonPressed()
    {
        var rawCode = GetNode<LineEdit>(codeInputPath).Text;
        var codeOpt = RoomId.tryParse(rawCode);

        Log.Debug("Join button pressed with code {Code}", rawCode);

        if (codeOpt.HasValue(out var code) == false)
        {
            Log.Error("Invalid room code");
            return;
        }

        var res = await GameManager.Room.JoinRoom(code);

        res.Match(
            success: _ =>
            {
                // TODO: Move logs to RoomManager
                ShowLobby(code);
            },
            failure: Log.Error
        );
    }

    private void StartCountdownIfAllReady()
    {
        var allReady = GameManager.Room.players.All(player =>
            player.State is PlayerStateInLobby playerState && playerState.IsReady
        );
        if (allReady) countdown.Start();
        else countdown.Cancel();
    }

    // Apply the player state to the UI
    private void OnPlayerRegistered(Player player)
    {
        var playerList = GetNode<VBoxContainer>(playerListPath);
        var playerItem = playerListItemScene.Instantiate<PlayerListItem>();

        playerItem.SetPlayer(player);
        playerList.AddChild(playerItem);

        countdown.Cancel(); // Stop countdown because the new player is, by default, not ready
    }
    private void OnPlayerLeft(Player player)
    {
        var playerList = GetNode<VBoxContainer>(playerListPath);
        var playerLabel = playerList
            .GetChildren()
            .Cast<PlayerListItem>()
            .FirstOrDefault(p => p.GetPeerId() == player.PeerId);

        if (playerLabel is null) return;

        playerList.RemoveChild(playerLabel);
        playerLabel.QueueFree();

        StartCountdownIfAllReady();
    }
    private void OnPlayerStateChanged(Player player)
    {
        if (player.State is not PlayerStateInLobby playerState)
        {
            Log.Error("Player state changed to not in lobby");
            return;
        }

        // Update player ready status in the UI
        var playerList = GetNode<VBoxContainer>(playerListPath);
        var playerItem = playerList
            .GetChildren()
            .Cast<PlayerListItem>()
            .FirstOrDefault(p => p.GetPeerId() == player.PeerId);

        if (playerItem is null)
        {
            Log.Error("Player {Username} not found", player.Username);
            return;
        }

        playerItem.SetPlayer(player);

        // Start countdown if all players are ready
        StartCountdownIfAllReady();
    }

    // Handle UI button presses
    private void OnReadyButtonPressed()
    {
        var player = GameManager.Room.localPlayer;
        if (player?.State is not PlayerStateInLobby playerState)
        {
            Log.Error("Player state is not in lobby");
            return;
        }

        var newState = playerState with { IsReady = !playerState.IsReady };
        GameManager.Room.SetPlayerState(newState);

        var readyControl = GetNode<Label>(readyButtonTextPath);
        readyControl.Text = newState.IsReady ? "Ready" : "Not ready";
    }
    private void OnQuitButtonPressed()
    {
        GameManager.Room.LeaveRoom();
        HideLobby();
    }
}
