namespace Behide.Game.UI.Lobby;

using Types;

public partial class Lobby
{
    private void DecreaseHunterCount() => room.Configuration.HunterCount -= 1;
    private void IncreaseHunterCount()
    {
        if (room.Configuration.HunterCount >= room.Players.Count) return;
        room.Configuration.HunterCount += 1;
    }


    private void RoleButtonPressed()
    {
        var config = room.Configuration;
        var peerId = room.LocalPlayer.Value.PeerId;
        if (config.IsHunter(peerId))
            config.RemoveHunter(peerId);
        else
            config.AddHunter(peerId);

        UpdateRoleButton();
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
}
