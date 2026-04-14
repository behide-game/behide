using Behide.Game.Player;

namespace Behide.Game.Supervisors;

public partial class PropHuntSupervisor
{
    protected override void PlayersReady()
    {
        base.PlayersReady();
        if (!IsMultiplayerAuthority()) return;

        huntersChose += (_, hunters) =>
        {
            SpawnProps(hunters);
            PreGameCountdown.StartCountdown(preGameDuration); // Start countdown
        };

        ChooseHunters();
    }

    private void ChooseHunters()
    {
        if (room.Configuration.HunterCount > 0)
        {
            var ids = room.Players.Keys.ToArray();
            Random.Shared.Shuffle(ids);
            var hunterIds = ids[..room.Configuration.HunterCount];
            Rpc(nameof(RpcHuntersChose), hunterIds);
        }
        else
            Rpc(nameof(RpcHuntersChose), room.Configuration.Hunters);
    }

    private void SpawnProps(int[] hunters)
    {
        foreach (var player in room.Players)
        {
            if (hunters.Contains(player.Key)) continue;
            Spawner.SpawnPlayer(player.Key, false); // TODO: Add spawn points
        }
    }

    private void SpawnHunters()
    {
        if (!IsMultiplayerAuthority()) return;
        if (hunterPeerIds is null)
        {
            log.Error("Cannot spawn hunters, hunterPeerIds is null");
            return;
        }

        foreach (var hunterPeerId in hunterPeerIds)
            Spawner.SpawnPlayer(hunterPeerId, true);
    }

    private void CheckGameEnd(PlayerBody dead)
    {
        if (!IsMultiplayerAuthority()) return;

        if (dead is PlayerHunter)
        {
            var allHuntersDead = PlayerBodies.TrueForAll(playerBody =>
                playerBody is PlayerProp || !playerBody.Alive
            );
            if (allHuntersDead) Rpc(nameof(RpcGameFinished), true, false);
        }
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
