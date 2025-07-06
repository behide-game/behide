using Godot;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Reactive.Subjects;
using Behide.OnlineServices.Signaling;
using Behide.UI.Controls;
// ReSharper disable WithExpressionModifiesAllMembers

namespace Behide.Game.UI.Lobby;

using Types;

public partial class Lobby : Control
{
    [Export] private PackedScene playerListItemScene = null!;
    [Export] private NodePath codeInputPath = "";
    [Export] private NodePath playerListPath = "";
    [Export] private NodePath countdownPath = "";
    [Export] private NodePath readyButtonTextPath = "";

    private Control chooseModeControl = null!;
    private Control lobbyControl = null!;

    private ILogger log = null!;

    private DateTimeOffset? gameStartTime;
    private CancellationTokenSource eventsCts = new();

    /// <summary>
    /// Return true if the local player is the one who connected the
    /// first to the lobby among the remaining lobby players.
    /// </summary>
    /// <returns></returns>
    private bool IsLobbyAuthority() => GameManager.Room.Players.Min(p => p.Key) == Multiplayer.GetUniqueId();

    public override void _EnterTree()
    {
        log = Log.ForContext("Tag", "UI/Lobby");

        chooseModeControl = GetNode<Control>("ChooseMode");
        lobbyControl = GetNode<Control>("Lobby");

        chooseModeControl.Show();
        lobbyControl.Hide();

        // Init properties for countdown
        countdownLabel = GetNode<Label>(countdownPath);
        countdownDefaultText = countdownLabel.Text;
    }
    public override void _ExitTree()
    {
        eventsCts.Cancel();
        eventsCts.Dispose();
        eventsCts = new CancellationTokenSource();
    }

    #region Countdown and game launch management
    private Label countdownLabel = null!;
    private string countdownDefaultText = null!;

    public override void _Process(double delta)
    {
        if (gameStartTime is null && countdownLabel.Text != countdownDefaultText)
        {
            countdownLabel.Text = countdownDefaultText;
        }

        if (gameStartTime is not { } startTime) return;
        var now = DateTimeOffset.Now + (GameManager.Room.ClockDelta.Value ?? TimeSpan.Zero);
        var remainingTime = startTime - now;
        var timeElapsed = remainingTime < TimeSpan.Zero;
        countdownLabel.Text =
            timeElapsed
                ? "Starting game..."
                : $"Starting in {remainingTime:s\\.ff}s";

        if (timeElapsed && IsLobbyAuthority())
        {
            Rpc(nameof(StartGameRpc));
            gameStartTime = null;
        }
    }

    /// <summary>
    /// Set or disable the countdown based on the player states
    /// </summary>
    private void RefreshCountdownState()
    {
        // Only the lobby authority can start the countdown
        if (!IsLobbyAuthority()) return;

        var allReady = GameManager.Room.Players.Values.All(player =>
            player.Value.State is PlayerStateInLobby playerState && playerState.IsReady
        );

        if (allReady)
            Rpc(
                nameof(SetCountdownRpc),
                (DateTimeOffset.Now + TimeSpan.FromSeconds(5)).ToUnixTimeMilliseconds()
            );
        else Rpc(nameof(ResetCountdownRpc));
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void SetCountdownRpc(long startTime)
    {
        log.Debug("Game start set at {Start}", DateTimeOffset.FromUnixTimeMilliseconds(startTime));
        gameStartTime = DateTimeOffset.FromUnixTimeMilliseconds(startTime);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void ResetCountdownRpc()
    {
        log.Debug("Game start reset");
        gameStartTime = null;
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void StartGameRpc()
    {
        GameManager.instance.SetGameState(GameManager.GameState.Game);
    }
    #endregion

    private void ShowLobby(RoomId roomId)
    {
        chooseModeControl.Hide();
        lobbyControl.Show();
        GetNode<Label>("Lobby/Header/Code/Value").Text = RoomId.raw(roomId);

        // Add players to UI
        foreach (var player in GameManager.Room.Players.Values) AddPlayerToUi(player);
        GameManager.Room.PlayerRegistered.Subscribe(AddPlayerToUi, eventsCts.Token);

        // eventsCts.Token.Register(countdown.Cancel);
        // TODO: Check if it break something
        // gameStartTime = null;
    }
    private void HideLobby()
    {
        lobbyControl.Hide();
        chooseModeControl.Show();

        _ExitTree(); // Unsubscribe from events and stop countdown // TODO

        var playerControls = GetNode<VBoxContainer>(playerListPath).GetChildren();
        foreach (var playerControl in playerControls) playerControl.QueueFree();

        var readyControl = GetNode<Label>(readyButtonTextPath);
        readyControl.Text = "Not ready";
    }

    public async void HostButtonPressed()
    {
        var roomId = await GameManager.Room.CreateRoom();
        log.Information("Room created with code {RoomId}", RoomId.raw(roomId));
        ShowLobby(roomId);
    }
    public async void JoinButtonPressed()
    {
        var rawCode = GetNode<LineEdit>(codeInputPath).Text;
        var code = RoomId.tryParse(rawCode).ToNullable();

        log.Debug("Join button pressed with code {Code}", rawCode);

        if (code is null)
        {
            log.Error("Invalid room code");
            return;
        }

        await GameManager.Room.JoinRoom(code);
        ShowLobby(code);
    }

    // Apply the player state to the UI
    private void AddPlayerToUi(BehaviorSubject<Player> player)
    {
        var playerList = GetNode<VBoxContainer>(playerListPath);
        var playerItem = playerListItemScene.Instantiate<PlayerListItem>();

        playerItem.SetPlayer(player.Value);
        playerList.AddChild(playerItem);

        // Update the UI accordingly to the player's state
        player.Subscribe(
            UpdatePlayerUi,                                // Update UI when it's state changes
            () => RemovePlayerFromUi(player.Value.PeerId), // Remove player from UI when he leaves
            eventsCts.Token
        );

        // Stop countdown if needed
        // because the new player is, by default, not ready
        RefreshCountdownState();
    }
    private void RemovePlayerFromUi(int playerId)
    {
        var playerList = GetNode<VBoxContainer>(playerListPath);
        var playerLabel = playerList
            .GetChildren()
            .Cast<PlayerListItem>()
            .FirstOrDefault(p => p.GetPeerId() == playerId);

        if (playerLabel is null) return;

        playerList.RemoveChild(playerLabel);
        playerLabel.QueueFree();

        RefreshCountdownState();
    }
    private void UpdatePlayerUi(Player player)
    {
        if (player.State is not PlayerStateInLobby) return;

        // Update player ready status in the UI
        var playerList = GetNode<VBoxContainer>(playerListPath);
        var playerItem = playerList
            .GetChildren()
            .Cast<PlayerListItem>()
            .FirstOrDefault(p => p.GetPeerId() == player.PeerId);

        if (playerItem is null)
        {
            log.Error("Player {Username} not found", player.Username);
            return;
        }

        playerItem.SetPlayer(player);

        // Start countdown if all players are ready
        RefreshCountdownState();
    }

    // Handle UI button presses
    private void OnReadyButtonPressed()
    {
        var player = GameManager.Room.LocalPlayer;
        if (player?.Value.State is not PlayerStateInLobby playerState)
        {
            log.Error("Player state is not in lobby");
            return;
        }

        var newState = playerState with { IsReady = !playerState.IsReady };
        GameManager.Room.SetPlayerState(newState);

        var readyControl = GetNode<Label>(readyButtonTextPath);
        readyControl.Text = newState.IsReady ? "Ready" : "Not ready";
    }
    private void OnQuitButtonPressed()
    {
        _ = GameManager.Room.LeaveRoom();
        HideLobby();
    }
}
