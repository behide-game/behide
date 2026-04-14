using Godot;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide.Game.UI.Lobby;

using Types;

[SceneTree(root: "nodes")]
public partial class Lobby : Control
{
    private readonly ILogger log = Log.CreateLogger("UI/Lobby");
    private readonly CancellationTokenSource nodeAliveCts = new();
    private CancellationToken NodeAliveCt => nodeAliveCts.Token;
    private Room room = null!;

    [Export] private PackedScene playerListItemScene = null!;
    private readonly TimeSpan countdownDuration = TimeSpan.FromSeconds(2); //(10);

    public override void _EnterTree()
    {
        if (GameManager.Room.Room is null)
        {
            log.Error("Not in a room");
            return;
        }

        room = GameManager.Room.Room;

        // Set authority
        UpdateLobbyAuthority();
        room.PlayerLeft.Subscribe(_ => UpdateLobbyAuthority(), NodeAliveCt);
        room.PlayerJoined.Subscribe(_ => UpdateLobbyAuthority(), NodeAliveCt);

        // Update countdown state
        room.PlayerStateChanged.Subscribe(p =>
        {
            if (p.State is not PlayerStateInLobby) return;
            RefreshCountdownState();
        }, NodeAliveCt);
        room.PlayerLeft.Subscribe(_ => RefreshCountdownState(), NodeAliveCt);
        room.PlayerJoined.Subscribe(_ => RefreshCountdownState(), NodeAliveCt);

        // Start game when countdown finished
        Countdown.TimeElapsed += () =>
        {
            if (!IsMultiplayerAuthority()) return;
            CallDeferred(Node.MethodName.Rpc, nameof(StartGameRpc));
        };

        // Set room code in UI
        RoomCode.Text = room.RoomId.ToString();

        // Set players UI
        foreach (var player in room.Players.Values) AddPlayerToUi(player);
        room.PlayerJoined.Subscribe(p => AddPlayerToUi(room.Players[p.PeerId]), NodeAliveCt);

        // Listen room configuration changes
        room.Configuration.Changed.Subscribe(_ => ChangePlayerList(), NodeAliveCt);
        ChangePlayerList();
    }

    public override void _ExitTree()
    {
        nodeAliveCts.Cancel();
        nodeAliveCts.Dispose();
    }

    private void UpdateLobbyAuthority()
    {
        var minPeerId = room.Players.Min(p => p.Key);
        if (minPeerId == GetMultiplayerAuthority()) return;
        SetMultiplayerAuthority(minPeerId);
        Countdown.SetMultiplayerAuthority(minPeerId);
    }

    private void RefreshCountdownState()
    {
        if (!IsMultiplayerAuthority()) return; // Only the lobby authority can start the countdown

        var allReady = room.Players.Values.All(player =>
            player.Value.State is PlayerStateInLobby { IsReady: true }
        );

        if (allReady)
        {
            // No clock delta needed as the time is based on the first player to have joined the room
            Countdown.StartCountdown(countdownDuration);
            log.Debug("Game start planned for {Start}", DateTimeOffset.Now + countdownDuration);
        }
        else
        {
            Countdown.ResetCountdown();
            log.Debug("Game start cancelled");
        }
    }

    [Rpc(CallLocal = true)]
    private void StartGameRpc() =>
        GameManager.SetGameState(GameManager.GameState.Game);
}
