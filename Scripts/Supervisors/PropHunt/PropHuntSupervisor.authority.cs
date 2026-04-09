using Behide.Game.Player;

namespace Behide.Game.Supervisors;

public partial class PropHuntSupervisor
{
    protected override void PlayersReady()
    {
        base.PlayersReady();
        if (!IsMultiplayerAuthority()) return;

        hunterChosen += (_, hunter) =>
        {
            SpawnProps(hunter);
            PreGameCountdown.StartCountdown(preGameDuration); // Start countdown
        };

        ChooseHunter();
    }

    private void ChooseHunter()
    {
        var idx = new Random().Next(room.Players.Count);
        var playerId = room.Players.ElementAt(idx).Key;
        Rpc(nameof(RpcSetHunter), playerId);
    }

    private void SpawnProps(int hunter)
    {
        foreach (var player in room.Players)
        {
            if (player.Key == hunter) continue;
            Spawner.SpawnPlayer(player.Key, false); // TODO: Add spawn points
        }
    }

    private void SpawnHunter()
    {
        if (!IsMultiplayerAuthority()) return;
        if (hunterPeerId is null)
        {
            log.Error("Cannot spawn the hunter, hunterPeerId is null");
            return;
        }
        Spawner.SpawnPlayer(hunterPeerId.Value, true);
    }

    private void CheckGameEnd(PlayerBody dead)
    {
        if (!IsMultiplayerAuthority()) return;

        if (dead is PlayerHunter)
            Rpc(nameof(RpcGameFinished), true, false);
        else
        {
            var allPropsDead = PlayerBodies.TrueForAll(playerBody =>
                playerBody is PlayerHunter || !playerBody.Alive
            );
            if (allPropsDead) Rpc(nameof(RpcGameFinished), false, false);
        }
    }

    private void GameTimeout()
    {
        if (!IsMultiplayerAuthority()) return;
        Rpc(nameof(RpcGameFinished), true, true);
    }
}
