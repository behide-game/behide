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
        HuntersChose += (_, hunters) =>
        {
            if (hunters.Contains(Multiplayer.GetUniqueId()))
            {
                PreGameHunter.CallDeferred(CanvasItem.MethodName.Show);
                IsHunterContainer.CallDeferred(CanvasItem.MethodName.Show);
            }
            else
            {
                if (GameManager.PauseMenu.Visible) GameManager.PauseMenu.MouseModeBefore = Input.MouseModeEnum.Captured;
                else Input.MouseMode = Input.MouseModeEnum.Captured;

                PreGameProp.CallDeferred(CanvasItem.MethodName.Show);
                IsPropContainer.CallDeferred(CanvasItem.MethodName.Show);
            }
        };

        PreGameCountdown.TimeElapsed += () =>
        {
            PreGameHunter.Hide();
            PreGameProp.Hide();
            InGame.Show();

            SpawnHunters();
            InGameCountdown.StartCountdown(inGameDuration);

            if (hunterPeerIds is null)
                log.Error("Cannot determine if we are hunter, hunterPeerIds were null");
            else if (hunterPeerIds.Contains(Multiplayer.GetUniqueId()))
            {
                if (GameManager.PauseMenu.Visible) GameManager.PauseMenu.MouseModeBefore = Input.MouseModeEnum.Captured;
                else Input.MouseMode = Input.MouseModeEnum.Captured;
            }
        };

        InGameCountdown.TimeElapsed += GameTimeout;
    }


    public override void PlayerDied(int killerPlayerId, int deadPlayerId)
    {
        // Retrieve players info
        if (!Room.Players.TryGetValue(killerPlayerId, out var killerObs))
        { log.Error("Failed to determine killer: id = {PlayerId}", killerPlayerId); return; }
        if (!Room.Players.TryGetValue(deadPlayerId, out var victimObs))
        { log.Error("Failed to determine victim: id = {PlayerId}", deadPlayerId); return; }

        var killer = killerObs.Value;
        var victim = victimObs.Value;
        log.Information("{KillerName} killed {VictimName}", killer.Username, victim.Username);

        // Add kill field entry
        var killFieldElement = killFieldItem.Instantiate<KillFieldItem>();
        if (killer.PeerId == victim.PeerId)
            killFieldElement.SetKilledThemself(victim);
        else
            killFieldElement.SetKillerAndKilled(killer, victim);

        nodes.UI.KillField.Elements.AddChild(killFieldElement);

        // Update game state
        CheckGameEnd(victim.PeerId);
    }

    public override void LocalPlayerDied(PlayerBody playerBody)
    {
        base.LocalPlayerDied(playerBody);
        if (gameFinished) return;
        Spectator.Enable();
    }

    [Rpc(CallLocal = true)]
    public void RpcHuntersChose(int[] peerIds)
    {
        hunterPeerIds = peerIds;
        HuntersChose?.Invoke(null, peerIds);
        log.Information("Hunters were chose (PeerIds: {PeerIds})", peerIds);
    }

    [Rpc(CallLocal = true)]
    public void RpcGameFinished(bool propsWon, bool timedOut)
    {
        InGameCountdown.TimeElapsed -= GameTimeout;
        gameFinished = true;
        Input.MouseMode = Input.MouseModeEnum.Visible;
        GameManager.PauseMenu.MouseModeBefore = Input.MouseModeEnum.Visible;

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
        foreach (var player in Room.Players.Values)
        {
            var body = PlayerBodies.Find(body => body.GetMultiplayerAuthority() == player.Value.PeerId);
            var node = playerListItem.Instantiate<PlayerListItem>();

            node.SetPlayerName(player.Value.Username);
            if (body is null)
                log.Error("Failed to find player body: player = {Player}", player.Value);
            else
                node.SetAlive(body.Alive);

            if (hunterPeerIds.Contains(player.Value.PeerId))
                HunterList.AddChild(node);
            else
                PropList.AddChild(node);
        }
        InGame.Hide();
        EndGame.Show();
        nodes.UI.TabMenu.QueueFree();
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
        Room.SetPlayerState(new PlayerStateInLobby(false));
        GameManager.SetGameState(GameManager.GameState.Lobby);
    }

    private void UiQuit()
    {
        if (!gameFinished) return;
        _ = GameManager.Room.LeaveRoom();
        GameManager.SetGameState(GameManager.GameState.Home);
    }
}
