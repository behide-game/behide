using Behide.Game.Player;
using Behide.Types;
using Behide.UI.Controls;
using Godot;

namespace Behide.Game.Supervisors;

public partial class PropHuntSupervisor
{
    public override void _EnterTree()
    {
        base._EnterTree();

        // Show UI
        huntersChose += (_, hunters) =>
        {
            if (hunters.Contains(Multiplayer.GetUniqueId()))
            {
                PreGameHunter.CallDeferred(CanvasItem.MethodName.Show);
                IsHunterLabel.CallDeferred(CanvasItem.MethodName.Show);
            }
            else
            {
                PreGameProp.CallDeferred(CanvasItem.MethodName.Show);
                IsPropLabel.CallDeferred(CanvasItem.MethodName.Show);
            }
        };

        PreGameCountdown.TimeElapsed += () =>
        {
            PreGameHunter.Hide();
            PreGameProp.Hide();
            InGame.Show();

            SpawnHunters();
            InGameCountdown.StartCountdown(inGameDuration);
        };

        InGameCountdown.TimeElapsed += GameTimeout;
    }


    public override void PlayerDied(PlayerBody playerBody)
    {
        log.Information("{PlayerName} died", playerBody.Name);
        CheckGameEnd(playerBody);
    }

    public override void LocalPlayerDied(PlayerBody playerBody)
    {
        if (gameFinished) return;
        Spectator.Enable();
    }

    [Rpc(CallLocal = true)]
    public void RpcHuntersChose(int[] peerIds)
    {
        hunterPeerIds = peerIds;
        huntersChose?.Invoke(null, peerIds);
        log.Information("Hunters were chose (PeerIds: {PeerIds})", peerIds);
    }

    [Rpc(CallLocal = true)]
    public void RpcGameFinished(bool propsWon, bool timedOut)
    {
        InGameCountdown.TimeElapsed -= GameTimeout;
        gameFinished = true;

        // Change UI
        ShowEndGameUi(propsWon, timedOut);

        // Release mouse
        foreach (var body in PlayerBodies) body.Freeze();
        Spectator.Disable();

        // Destroy synchronizers to prevent errors
        DestroySynchronizers(PlayersNode);
        DestroySynchronizers(BehideObjects);

        log.Information("Game finished!: {Winner}", propsWon ? "Props won" : "Hunter wins");
    }

    private void ShowEndGameUi(bool propsWon, bool timedOut)
    {
        foreach (var player in room.Players.Values)
        {
            var body = PlayerBodies.Find(body => body.GetMultiplayerAuthority() == player.Value.PeerId);
            var node = playerListItem.Instantiate<PlayerListItem>();

            node.SetPlayerName(player.Value.Username);
            if (body is null)
            {
                log.Error("Failed to find player body: player = {Player}", player.Value);
                node.SetStatus("Error");
            }
            else
                node.SetStatus(body.Alive ? "Survived" : "Died");

            if (hunterPeerIds.Contains(player.Value.PeerId))
                HunterList.AddChild(node);
            else
                PropList.AddChild(node);
        }
        InGame.Hide();
        EndGame.Show();
        if (timedOut) TimedOut.Show();
        if (propsWon) PropsWonLabel.Show();
        else HunterWinLabel.Show();
    }

    private static void DestroySynchronizers(Node parent)
    {
        if (parent is MultiplayerSynchronizer)
        {
            parent.Free();
            return;
        }

        foreach (var node in parent.GetChildren())
            DestroySynchronizers(node);
    }

    private void UiRestart()
    {
        if (!gameFinished) return;
        room.SetPlayerState(new PlayerStateInLobby(false));
        GameManager.SetGameState(GameManager.GameState.Lobby);
    }

    private void UiQuit()
    {
        if (!gameFinished) return;
        _ = GameManager.Room.LeaveRoom();
        GameManager.SetGameState(GameManager.GameState.Home);
    }
}
