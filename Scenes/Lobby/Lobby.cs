using Godot;
using Serilog;
using Behide.UI.Controls;
using Log = Behide.Logging.Log;

// ReSharper disable WithExpressionModifiesAllMembers

namespace Behide.Game.UI.Lobby;

using Types;

[SceneTree(root: "nodes")]
public partial class Lobby : Control
{
    private readonly ILogger log = Log.CreateLogger("UI/Lobby");
    private readonly CancellationTokenSource nodeAliveCts = new();
    private CancellationToken NodeAliveCt => nodeAliveCts.Token;
    private Room room = null!;

    private Control PlayerList => nodes.Lobby.Boxes.Players.MarginContainer.VBoxContainer.ScrollContainer.Players;
    private Label ReadyButton => nodes.Lobby.Boxes.Buttons.Ready.MarginContainer.Label;
    private LabelCountdown Countdown => nodes.Countdown;
    private Label RoomCode => nodes.Lobby.Header.Code.Value;

    [Export] private PackedScene playerListItemScene = null!;
    private readonly TimeSpan countdownDuration = TimeSpan.FromSeconds(10);

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
        room.PlayerStateChanged.Subscribe(_ => RefreshCountdownState(), NodeAliveCt);

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
    }

    public override void _ExitTree()
    {
        nodeAliveCts.Cancel();
        nodeAliveCts.Dispose();
    }

    private void AddPlayerToUi(IObservable<Player> player)
    {
        var node = playerListItemScene.Instantiate<PlayerListItem>();
        PlayerList.AddChild(node);
        node.SetPlayer(player);
    }

    private void ReadyButtonPressed()
    {
        var playerState = room.LocalPlayer.Value.State;
        if (playerState is not PlayerStateInLobby state)
        {
            log.Error("Cannot toggle player state: Player not in a lobby state: {}", playerState);
            return;
        }
        room.SetPlayerState(state with { IsReady = !state.IsReady });

        ReadyButton.Text = !state.IsReady ? "Unset ready" : "Set ready"; // TODO: I18n

        RefreshCountdownState();
    }

    private static void QuitButtonPressed()
    {
        _ = GameManager.Room.LeaveRoom();
        GameManager.SetGameState(GameManager.GameState.Home);
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
