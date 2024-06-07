namespace Behide.Game.UI.Lobby;

using Godot;
using Behide.Types;
using Behide.OnlineServices.Signaling;
using Behide.UI.Controls;
using Serilog;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

public class Countdown(int durationInSec)
{
    private readonly int initialDuration = durationInSec;
    private int timeLeft = 0;
    private bool isRunning = false;
    private CancellationTokenSource? cts;

    public event Action<int>? Tick;
    public event Action? Canceled;
    public event Action? Finished;

    public async void Start()
    {
        if (isRunning) return;

        timeLeft = initialDuration;
        isRunning = true;
        cts = new CancellationTokenSource();
        cts.Token.Register(() => Canceled?.Invoke());

        while (timeLeft > 0 && !cts.Token.IsCancellationRequested)
        {
            Tick?.Invoke(timeLeft);
            try
            {
                await Task.Delay(1000).WaitAsync(cts.Token);
            }
            catch (TaskCanceledException)
            {
                break; // Use a try-catch block to hide errors in the console
            }
            timeLeft--;
        }

        if (!cts.Token.IsCancellationRequested)
            Finished?.Invoke();
    }

    public void Stop()
    {
        if (!isRunning) return;

        cts?.Cancel();
        isRunning = false;
        timeLeft = initialDuration;
    }
}

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

    private readonly Countdown countdown = new(5);


    public override void _EnterTree()
    {
        Log = Serilog.Log.ForContext("Tag", "UI/Lobby");

        chooseModeControl = GetNode<Control>("ChooseMode");
        lobbyControl = GetNode<Control>("Lobby");

        chooseModeControl.Visible = true;
        lobbyControl.Visible = false;

        var countdownLabel = GetNode<Label>(countdownPath);
        var countdownDefaultText = countdownLabel.Text;

        countdown.Tick += timeLeft => countdownLabel.Text = $"Starting in {timeLeft}s";
        countdown.Canceled += () => countdownLabel.Text = countdownDefaultText;
        countdown.Finished += () =>
        {
            countdownLabel.Text = "Starting game...";
            GameManager.instance.SetGameState(GameManager.GameState.Game);
            GameManager.Room.SetPlayerState(new PlayerStateInGame());
        };
    }

    private void ShowLobby(RoomId roomId)
    {
        chooseModeControl.Visible = false;
        lobbyControl.Visible = true;
        GetNode<Label>("Lobby/Header/Code/Value").Text = RoomId.raw(roomId);

        GameManager.Room.players.ForEach(OnPlayerRegistered);
        GameManager.Room.PlayerRegistered += OnPlayerRegistered;
        GameManager.Room.PlayerLeft += OnPlayerLeft;
        GameManager.Room.PlayerStateChanged += OnPlayerStateChanged;
    }

    private void HideLobby()
    {
        lobbyControl.Visible = false;
        chooseModeControl.Visible = true;

        GameManager.Room.PlayerRegistered -= OnPlayerRegistered;
        GameManager.Room.PlayerLeft -= OnPlayerLeft;
        GameManager.Room.PlayerStateChanged -= OnPlayerStateChanged;

        countdown.Stop();

        var playerControls = GetNode<VBoxContainer>(playerListPath).GetChildren();
        foreach (var playerControl in playerControls) playerControl.QueueFree();

        var readyControl = GetNode<Button>(readyButtonTextPath);
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
        GD.Print("Join button pressed with code {Code}", rawCode);

        if (codeOpt.HasValue(out var code) == false)
        {
            Log.Error("Invalid room code");
            return;
        }

        var res = await GameManager.Room.JoinRoom(code);

        res.Match(
            success: _ =>
            {
                Log.Information("Joined room"); // Todo: Move logs to RoomManager
                ShowLobby(code);
            },
            failure: Log.Error
        );
    }


    private void OnPlayerRegistered(Player player)
    {
        var playerList = GetNode<VBoxContainer>(playerListPath);
        var playerItem = playerListItemScene.Instantiate<PlayerListItem>();

        playerItem.SetPlayer(player);
        playerList.AddChild(playerItem);
    }

    private void OnPlayerLeft(Player player)
    {
        var playerList = GetNode<VBoxContainer>(playerListPath);
        var playerLabel = playerList
            .GetChildren()
            .Cast<PlayerListItem>()
            .FirstOrDefault(p => p.GetUsername() == player.Username);

        playerList.RemoveChild(playerLabel);
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
            .FirstOrDefault(p => p.GetUsername() == player.Username);

        if (playerItem is null)
        {
            Log.Error("Player {Username} not found", player.Username);
            return;
        }

        playerItem.SetPlayer(player);

        // Start countdown if all players are ready
        if (playerState.IsReady == true)
        {
            var allReady = GameManager.Room.players.All(player =>
                player.State is PlayerStateInLobby playerState && playerState.IsReady
            );
            if (allReady)
                countdown.Start();
        }
        else
            countdown.Stop();
    }


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
