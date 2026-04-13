using System.Reactive;
using System.Reactive.Subjects;
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

    private Control HunterList => nodes.Lobby.Boxes.ScrollContainer.Players.Hunters.PlayerList;
    private Control PropList => nodes.Lobby.Boxes.ScrollContainer.Players.Props.PlayerList;
    private Label RoleButton => nodes.Lobby.Boxes.Buttons.Role.MarginContainer.Label;
    private Label ReadyButton => nodes.Lobby.Boxes.Buttons.Ready.MarginContainer.Label;
    private LabelCountdown Countdown => nodes.Countdown;
    private Label RoomCode => nodes.Lobby.Header.Code.Value.Value;

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
    }

    public override void _ExitTree()
    {
        nodeAliveCts.Cancel();
        nodeAliveCts.Dispose();
    }

    private void AddPlayerToUi(BehaviorSubject<Player> player)
    {
        var node = playerListItemScene.Instantiate<PlayerListItem>();
        node.SetPlayer(player, p => p.State switch
        {
            PlayerStateInLobby isReady => isReady.IsReady ? "Ready" : "Not ready",
            PlayerStateInGame => "In game",
            _ => "Gone"
        });

        room.Configuration.Changed.Subscribe(OnConfigChanged, NodeAliveCt);
        OnConfigChanged(Unit.Default);
        return;

        void OnConfigChanged(Unit _)
        {
            if (room.Configuration.IsHunter(player.Value.PeerId))
            {
                if (node.GetParent() == PropList) PropList.RemoveChild(node);
                HunterList.AddChild(node);
            }
            else
            {
                if (node.GetParent() == HunterList) HunterList.RemoveChild(node);
                PropList.AddChild(node);
            }
        }
    }

    private void RoleButtonPressed()
    {
        var config = room.Configuration;
        var peerId = room.LocalPlayer.Value.PeerId;
        if (config.IsHunter(peerId))
        {
            config.RemoveHunter(peerId);
            RoleButton.Text = "Be hunter";
        }
        else
        {
            config.AddHunter(peerId);
            RoleButton.Text = "Be prop";
        }
    }

    private void ReadyButtonPressed()
    {
        var playerState = room.LocalPlayer.Value.State;
        if (playerState is not PlayerStateInLobby)
            log.Warning("Player not in a lobby state: {State}", playerState);

        var newState =
            playerState is PlayerStateInLobby state
                ? state with { IsReady = !state.IsReady }
                : new PlayerStateInLobby(true);

        room.SetPlayerState(newState);

        ReadyButton.Text = newState.IsReady ? "Unset ready" : "Set ready"; // TODO: I18n
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
