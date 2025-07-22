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

[SceneTree]
public partial class Lobby : Control
{
    private Control chooseModeControl = null!;
    private Control lobbyControl = null!;

    // Choose mode
    private LineEdit codeInput = null!;

    // Lobby
    private VBoxContainer playerListNode = null!;
    [Export] private PackedScene playerListItemScene = null!;
    private Label readyButtonLabel = null!;
    private LabelCountdown countdown = null!;

    private ILogger log = null!;
    private readonly TimeSpan countdownDuration = TimeSpan.FromSeconds(1);// TimeSpan.FromSeconds(5);
    private CancellationTokenSource inRoomEventsCts = new();
    private readonly CancellationTokenSource nodeAliveCts = new();

    public override void _EnterTree()
    {
        log = Log.ForContext("Tag", "UI/Lobby");
        chooseModeControl = _.ChooseMode;
        lobbyControl = _.Lobby;
        codeInput = _.ChooseMode.Buttons.VBoxContainer.Code.LineEdit;
        playerListNode = _.Lobby.Boxes.Players.MarginContainer.VBoxContainer.ScrollContainer.Players;
        readyButtonLabel = _.Lobby.Boxes.VBoxContainer.HBoxContainer.Ready.Center.Label;
        countdown = _.Countdown;

        chooseModeControl.Show();
        lobbyControl.Hide();

        // Set authority
        GameManager.Room.PlayerLeft.Subscribe(_ => OnPlayersChanged(), nodeAliveCts.Token);
        GameManager.Room.PlayerRegistered.Subscribe(_ => OnPlayersChanged(), nodeAliveCts.Token);
        void OnPlayersChanged()
        {
            var minPeerId = GameManager.Room.Players.Min(p => p.Key);
            if (minPeerId == GetMultiplayerAuthority()) return;
            SetMultiplayerAuthority(minPeerId);
        }

        // Subscribe to countdown
        countdown.TimeElapsed += () =>
        {
            if (!IsMultiplayerAuthority() || inRoomEventsCts.Token.IsCancellationRequested) return;
            CallDeferred(Node.MethodName.Rpc, nameof(StartGameRpc));
        };
    }

    private void CancelRoomEvents()
    {
        inRoomEventsCts.Cancel();
        inRoomEventsCts.Dispose();
        inRoomEventsCts = new CancellationTokenSource();
    }

    public override void _ExitTree()
    {
        inRoomEventsCts.Cancel();
        inRoomEventsCts.Dispose();
        nodeAliveCts.Cancel();
        nodeAliveCts.Dispose();
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
            countdown.StartCountdown(countdownDuration);
            log.Debug("Game start planned for {Start}", DateTimeOffset.Now + countdownDuration);
        }
        else
        {
            countdown.ResetCountdown();
            log.Debug("Game start cancelled");
        }
    }

    [Rpc(CallLocal = true)]
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
        GameManager.Room.PlayerRegistered.Subscribe(AddPlayerToUi, inRoomEventsCts.Token);

        // eventsCts.Token.Register(countdown.Cancel);
        // TODO: Check if it break something
        // gameStartTime = null;
    }
    private void HideLobby()
    {
        lobbyControl.Hide();
        chooseModeControl.Show();

        CancelRoomEvents(); // Unsubscribe from room events

        foreach (var playerControl in playerListNode.GetChildren()) playerControl.QueueFree();
        readyButtonLabel.Text = "Not ready";
    }

    public async void HostButtonPressed()
    {
        try
        {
            var roomId = await GameManager.Room.CreateRoom();
            log.Information("Room created with code {RoomId}", roomId);
            ShowLobby(roomId);
        }
        catch (Exception e)
        {
            log.Warning("Trying to host game failed: {error}", e.Message); // TODO: Show error to user
        }
    }
    public async void JoinButtonPressed()
    {
        try
        {
            var rawCode = codeInput.Text;
            var code = RoomId.tryParse(rawCode).ToNullable();
            if (code is null)
            {
                log.Error("Invalid room code");
                return;
            }

            await GameManager.Room.JoinRoom(code);
            ShowLobby(code);
        }
        catch (Exception e)
        {
            log.Warning("Trying to join room failed: {error}", e.Message); // TODO: Show error to user
        }
    }

    // Apply the player state to the UI
    private void AddPlayerToUi(BehaviorSubject<Player> player)
    {
        var playerItem = playerListItemScene.Instantiate<PlayerListItem>();
        playerItem.SetPlayer(player.Value);
        playerListNode.AddChild(playerItem);

        // Update the UI accordingly to the player's state
        player.Subscribe(
            UpdatePlayerUi, // Update UI when it's state changes
            () => RemovePlayerFromUi(player.Value.PeerId), // Remove player from UI when he leaves
            inRoomEventsCts.Token
        );

        RefreshCountdownState();
    }
    private void RemovePlayerFromUi(int playerId)
    {
        var playerLabel = playerListNode
            .GetChildren()
            .Cast<PlayerListItem>()
            .FirstOrDefault(p => p.GetPeerId() == playerId);

        if (playerLabel is null) return;

        playerListNode.RemoveChild(playerLabel);
        playerLabel.QueueFree();

        RefreshCountdownState();
    }

    private void UpdatePlayerUi(Player player)
    {
        if (player.State is not PlayerStateInLobby) return;
        var playerItem = playerListNode
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

        readyButtonLabel.Text = newState.IsReady ? "Set not ready" : "Set ready";
    }
    private void OnQuitButtonPressed()
    {
        var _ = GameManager.Room.LeaveRoom();
        HideLobby();
    }
}
