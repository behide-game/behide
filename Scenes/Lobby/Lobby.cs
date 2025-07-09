using Godot;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Reactive.Subjects;
using System.Threading.Tasks;
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
    [Export] private NodePath readyButtonTextPath = "";
    [Export] private NodePath countdownPath = "";

    private Countdown countdown = null!;
    private Control chooseModeControl = null!;
    private Control lobbyControl = null!;

    private ILogger log = null!;
    private CancellationTokenSource eventsCts = new();

    public override void _EnterTree()
    {
        log = Log.ForContext("Tag", "UI/Lobby");

        chooseModeControl = GetNode<Control>("ChooseMode");
        chooseModeControl.Show();

        lobbyControl = GetNode<Control>("Lobby");
        lobbyControl.Hide();

        // Set authority
        GameManager.Room.PlayerLeft.Subscribe(
            _ =>
            {
                var minPeerId = GameManager.Room.Players.Min(p => p.Key);
                if (minPeerId == GetMultiplayerAuthority()) return;
                SetMultiplayerAuthority(minPeerId);
            },
            eventsCts.Token
        );

        // Subscribe to countdown
        countdown = GetNode<Countdown>(countdownPath);
        countdown.TimeElapsed += () =>
        {
            if (!IsMultiplayerAuthority() || eventsCts.Token.IsCancellationRequested) return;
            CallDeferred(Node.MethodName.Rpc, nameof(StartGameRpc));
        };
    }

    public override void _ExitTree()
    {
        eventsCts.Cancel();
        eventsCts.Dispose();
        eventsCts = new CancellationTokenSource();
    }

    private void RefreshCountdownState()
    {
        if (!IsMultiplayerAuthority()) return; // Only the lobby authority can start the countdown

        var allReady = GameManager.Room.Players.Values.All(player =>
            player.Value.State is PlayerStateInLobby { IsReady: true }
        );

        if (allReady)
        {
            // No clock delta needed as the time is based on the first player to have joined the room
            var startTime = DateTimeOffset.Now + TimeSpan.FromSeconds(5);
            countdown.StartCountdown(startTime);
            log.Debug("Game start planned for {Start}", startTime);
        }
        else
        {
            countdown.ResetCountdown();
            log.Debug("Game start cancelled");
        }
    }

    [Rpc(CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
    private void StartGameRpc()
    {
        GameManager.instance.SetGameState(GameManager.GameState.Game);
    }

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

        _ExitTree(); // Unsubscribe from events (including countdown) // TODO

        var playerControls = GetNode<VBoxContainer>(playerListPath).GetChildren();
        foreach (var playerControl in playerControls) playerControl.QueueFree();

        var readyControl = GetNode<Label>(readyButtonTextPath);
        readyControl.Text = "Not ready";
    }

    public async void HostButtonPressed()
    {
        try
        {
            var roomId = await GameManager.Room.CreateRoom();
            log.Information("Room created with code {RoomId}", roomId);
            ShowLobby(roomId);
        }
        catch (Exception) { /* ignore */ }
    }
    public async void JoinButtonPressed()
    {
        try
        {
            var rawCode = GetNode<LineEdit>(codeInputPath).Text;
            var code = RoomId.tryParse(rawCode).ToNullable();
            if (code is null)
            {
                log.Error("Invalid room code");
                return;
            }

            await GameManager.Room.JoinRoom(code);
            ShowLobby(code);
        }
        catch (Exception) { /* ignore */ }
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
            UpdatePlayerUi, // Update UI when it's state changes
            () => RemovePlayerFromUi(player.Value.PeerId), // Remove player from UI when he leaves
            eventsCts.Token
        );

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
        readyControl.Text = newState.IsReady ? "Set not ready" : "Set ready";
    }
    private void OnQuitButtonPressed()
    {
        _ = GameManager.Room.LeaveRoom();
        HideLobby();
    }
}
