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
        hunterChosen += (_, hunter) =>
        {
            if (hunter == Multiplayer.GetUniqueId())
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

            SpawnHunter();
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
        if (playerBody is PlayerHunter) return;
        Spectator.Enable();
    }

    [Rpc(CallLocal = true)]
    public void RpcSetHunter(int peerId)
    {
        hunterPeerId = peerId;
        hunterChosen?.Invoke(null, peerId);
        log.Information("Hunter has been chosen (PeerId: {PeerId})", peerId);
    }

    [Rpc(CallLocal = true)]
    public void RpcGameFinished(bool propsWon, bool timedOut)
    {
        InGameCountdown.TimeElapsed -= GameTimeout;
        gameFinished = true;

        // Change UI
        foreach (var player in room.Players.Values)
        {
            var body = PlayerBodies.Find(body => body.GetMultiplayerAuthority() == player.Value.PeerId);
            var node = playerListItem.Instantiate<PlayerListItem>();
            if (body is null)
            {
                log.Error("Failed to find player body: player = {Player}", player.Value);
                node.SetPlayer(player, _ => "Error");
            }
            else
                node.SetPlayer(player, _ => body.Alive ? "Survived" : "Died");

            if (player.Value.PeerId == hunterPeerId)
                HunterList.AddChild(node);
            else
                PropList.AddChild(node);
        }
        InGame.Hide();
        EndGame.Show();
        if (timedOut) TimedOut.Show();
        if (propsWon) PropsWonLabel.Show();
        else HunterWinLabel.Show();

        // Freeze player bodies
        foreach (var body in PlayerBodies) body.Freeze();
        Spectator.Disable();

        log.Information("Game finished!: {Winner}", propsWon ? "Props won" : "Hunter wins");
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
