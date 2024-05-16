namespace Behide.Game.UI.Lobby;

using Godot;
using Behide.Types;
using Behide.OnlineServices.Signaling;
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
            await Task.Delay(1000, cts.Token);
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

        var countdownLabel = GetNode<Label>("Lobby/Header/Countdown");
        var countdownDefaultText = countdownLabel.Text;

        countdown.Tick += timeLeft => countdownLabel.Text = $"Starting in {timeLeft}s";
        countdown.Canceled += () => countdownLabel.Text = countdownDefaultText;
        countdown.Finished += () => countdownLabel.Text = "Finished";
    }

    private void ShowLobby(RoomId roomId)
    {
        chooseModeControl.Visible = false;
        lobbyControl.Visible = true;
        GetNode<Label>("Lobby/Header/Code/Value").Text = RoomId.raw(roomId);

        GameManager.Room.players.ForEach(OnPlayerRegistered);
        GameManager.Room.PlayerRegistered += OnPlayerRegistered;
        GameManager.Room.PlayerLeft += OnPlayerLeft;
        GameManager.Room.PlayerReady += OnPlayerReady;
    }

    private void HideLobby()
    {
        lobbyControl.Visible = false;
        chooseModeControl.Visible = true;

        GameManager.Room.PlayerRegistered -= OnPlayerRegistered;
        GameManager.Room.PlayerLeft -= OnPlayerLeft;
        GameManager.Room.PlayerReady -= OnPlayerReady;

        countdown.Stop();

        var playerControls = GetNode<VBoxContainer>("Lobby/Boxes/Players/VerticalAligner/ScrollContainer/Players").GetChildren();
        foreach (var playerControl in playerControls) playerControl.QueueFree();
    }


    private async void HostButtonPressed()
    {
        var res = await GameManager.Room.CreateRoom();

        res.Match(
            success: roomId => {
                Log.Information("Room created with code {RoomId}", RoomId.raw(roomId));
                ShowLobby(roomId);
            },
            failure: Log.Error
        );
    }

    private async void JoinButtonPressed()
    {
        var rawCode = GetNode<LineEdit>("ChooseMode/Buttons/Join/LineEdit").Text;
        var codeOpt = RoomId.tryParse(rawCode);

        if (codeOpt.HasValue(out var code) == false)
        {
            Log.Error("Invalid room code");
            return;
        }

        var res = await GameManager.Room.JoinRoom(code);

        res.Match(
            success: _ => {
                Log.Information("Joined room"); // Todo: Move logs to RoomManager
                ShowLobby(code);
            },
            failure: Log.Error
        );
    }


    private void OnPlayerRegistered(Behide.Player player)
    {
        var playerList = GetNode<VBoxContainer>("Lobby/Boxes/Players/VerticalAligner/ScrollContainer/Players"); // TODO: Put this kind of thing in a variable editable in the Godot editor
        var playerLabel = new Label { Name = player.Username.Replace(":", "_"), Text = $"{player.Username}: Not ready" }; // TODO: Find a solution for Replace(":", "_")
        GD.Print(player.Username);
        playerList.AddChild(playerLabel);
    }

    private void OnPlayerLeft(Behide.Player player)
    {
        var playerList = GetNode<VBoxContainer>("Lobby/Boxes/Players/VerticalAligner/ScrollContainer/Players");
        var playerLabel = playerList.GetChildren().FirstOrDefault(child => ((Label)child).Text == player.Username);
        playerList.RemoveChild(playerLabel);
    }

    private void OnPlayerReady(Behide.Player player, bool ready)
    {
        var playerList = GetNode<VBoxContainer>("Lobby/Boxes/Players/VerticalAligner/ScrollContainer/Players").GetChildren();
        var playerLabel = (Label?)playerList.FirstOrDefault(child => {
            GD.Print(child.Name);
            return child.Name == player.Username.Replace(":", "_");
        });

        if (playerLabel is null)
        {
            Log.Error("Player {Username} not found", player.Username);
            return;
        }

        playerLabel.Text = $"{player.Username}: {(ready ? "Ready" : "Not ready")}";

        if (ready == true)
        {
            var allReady = GameManager.Room.players.All(player => player.Ready);
            if (allReady)
                countdown.Start();
        }
        else
            countdown.Stop();
    }


    private void OnReadyButtonPressed()
    {
        GameManager.Room.ToggleReady();

        var readyControl = GetNode<Button>("Lobby/Boxes/VBoxContainer/HBoxContainer/Ready");
        readyControl.Text = GameManager.Room.localPlayer?.Ready ?? false ? "Ready" : "Not ready";
    }

    private void OnQuitButtonPressed()
    {
        GameManager.Room.LeaveRoom();
        HideLobby();
    }
}
