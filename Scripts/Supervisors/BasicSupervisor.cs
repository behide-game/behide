namespace Behide.Game.Supervisors;

using Godot;
using Behide.Types;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

/// <summary>
/// TODO: Write a description
/// </summary>
public partial class BasicSupervisor : Node
{
    private Serilog.ILogger Log = null!;

    [Export] private PackedScene playerPrefab = null!;
    [Export] private NodePath playersNodePath = null!;

    private readonly List<Player> players = GameManager.Room.players;

    #region Initialization
    public override void _EnterTree()
    {
        Log = Serilog.Log.ForContext("Tag", "Supervisor/Basic");

        // Set authority to the last player to join
        // The authority is the player who spawns players and choose randomly the hunter
        var lastPlayerToJoin = players.MaxBy(player => player.PeerId);
        if (lastPlayerToJoin is null)
        {
            Log.Error("No player found. This means that the room is empty. Strange...");
            return;
        }

        SetMultiplayerAuthority(lastPlayerToJoin.PeerId);
    }

    public override async void _Ready()
    {
        // Spawn all players
        var playersNode = GetNode<Node3D>(playersNodePath);
        foreach (var player in players)
        {
            var playerNode = playerPrefab.Instantiate<Node3D>();
            playerNode.Name = player.PeerId.ToString();
            playerNode.Position = new Vector3(0, 0, player.PeerId * 4);

            playersNode.AddChild(playerNode);
        }

        // Set in game
        CallDeferred(nameof(SetInGameState));

        // Wait for all players to be in game
        await Task.Run(() =>
        {
            while (!players.TrueForAll(player => player.State is PlayerStateInGame))
            {
                Log.Debug("Waiting for players to be in game...");
            }
        });
        Log.Debug("Players are all ready ! Showing them...");

        // Showing us
        var synchronizer =
            playersNode
                .GetNode(Multiplayer.GetUniqueId().ToString())
                .GetNode<MultiplayerSynchronizer>("PositionSynchronizer");
        synchronizer.SetVisibilityFor(0, true);
        Log.Debug("Showed up");
    }

    private static void SetInGameState() => GameManager.Room.SetPlayerState(new PlayerStateInGame());
    #endregion
}
