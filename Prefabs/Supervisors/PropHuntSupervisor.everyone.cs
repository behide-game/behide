using Behide.Game.Player;
using Godot;

namespace Behide.Game.Supervisors;

public partial class PropHuntSupervisor
{
    public override void _EnterTree()
    {
        base._EnterTree();
        // Load nodes
        preGameCountdown = _.UI.AdvancedLabelCountdown;
        preGameProp = _.UI.Pre_gameProp;
        preGameHunter = _.UI.Pre_gameHunter;

        inGame = _.UI.In_game;
        inGameCountdown = _.UI.In_game.Countdown;
        isPropLabel = _.UI.In_game.IsProp;
        isHunterLabel = _.UI.In_game.IsHunter;

        endGame = _.UI.End_game;
        propsWonLabel = _.UI.End_game.PropsWin;
        hunterWinLabel = _.UI.End_game.HunterWins;

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
        inGame.Hide();
        endGame.Show();
        if (propsWon) propsWonLabel.Show();
        else hunterWinLabel.Show();

        log.Information("Game finished!: {Winner}", propsWon ? "Props won" : "Hunter wins");
    }
}
