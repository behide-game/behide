using Behide.Game.Player;
using Godot;

namespace Behide.Game.Supervisors;

public partial class PropHuntSupervisor
{
    public override void _EnterTree()
    {
        base._EnterTree();
        // Load nodes
        preGameCountdown = nodes.UI.AdvancedLabelCountdown;
        preGameProp = nodes.UI.Pre_gameProp;
        preGameHunter = nodes.UI.Pre_gameHunter;

        inGame = nodes.UI.In_game;
        inGameCountdown = nodes.UI.In_game.Countdown;
        isPropLabel = nodes.UI.In_game.IsProp;
        isHunterLabel = nodes.UI.In_game.IsHunter;

        endGame = nodes.UI.End_game;
        propsWonLabel = nodes.UI.End_game.PropsWin;
        hunterWinLabel = nodes.UI.End_game.HunterWins;

        spectator = nodes.Spectator;

        // Show UI
        hunterChosen += (_, hunter) =>
        {
            if (hunter == Multiplayer.GetUniqueId())
            {
                preGameHunter.CallDeferred(CanvasItem.MethodName.Show);
                isHunterLabel.CallDeferred(CanvasItem.MethodName.Show);
            }
            else
            {
                preGameProp.CallDeferred(CanvasItem.MethodName.Show);
                isPropLabel.CallDeferred(CanvasItem.MethodName.Show);
            }
        };

        preGameCountdown.TimeElapsed += () =>
        {
            preGameHunter.Hide();
            preGameProp.Hide();
            inGame.Show();

            SpawnHunter();
            inGameCountdown.StartCountdown(inGameDuration);
        };

        inGameCountdown.TimeElapsed += GameTimeout;
    }


    public override void PlayerDied(PlayerBody playerBody)
    {
        log.Information("{PlayerName} died", playerBody.Name);
        CheckGameEnd(playerBody);
    }

    public override void LocalPlayerDied()
    {
        if (!gameFinished) spectator.Enable();
    }

    [Rpc(CallLocal = true)]
    public void RpcSetHunter(int peerId)
    {
        hunterPeerId = peerId;
        hunterChosen?.Invoke(null, peerId);
        log.Information("Hunter has been chosen (PeerId: {PeerId})", peerId);
    }

    [Rpc(CallLocal = true)]
    public void RpcGameFinished(bool propsWon)
    {
        inGameCountdown.TimeElapsed -= GameTimeout;
        gameFinished = true;

        // Change UI
        inGame.Hide();
        endGame.Show();
        if (propsWon) propsWonLabel.Show();
        else hunterWinLabel.Show();

        // Stop player bodies
        foreach (var body in PlayerBodies) body.Alive = false;

        log.Information("Game finished!: {Winner}", propsWon ? "Props won" : "Hunter wins");
    }
}
