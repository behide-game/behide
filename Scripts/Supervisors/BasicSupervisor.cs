namespace Behide.Game.Supervisors;

using Godot;
using Behide.Types;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Threading.Tasks;

/// <summary>
/// It just spawns players
/// </summary>
public partial class BasicSupervisor : Node
{
    private Serilog.ILogger Log = null!;
    public const string tag = "Supervisor/Basic";

    [Export] private PackedScene playerPrefab = null!;
    [Export] private NodePath playersNodePath = null!;

    private readonly Dictionary<int, BehaviorSubject<Player>> players = GameManager.Room.players;

    private MultiplayerSynchronizer? positionSynchronizer = null;

    public override void _EnterTree()
    {
        Log = Serilog.Log.ForContext("Tag", tag);

        // Set authority to the last player to join
        // The authority is the player who spawns players and choose randomly the hunter
        int? lastPlayerToJoin = players.Max(kv => kv.Key);
        if (lastPlayerToJoin is null)
        {
            Log.Error("No player found. This means that the room is empty. Strange...");
            return;
        }

        SetMultiplayerAuthority(lastPlayerToJoin.Value);
    }

    public override async void _Ready()
    {
        // Spawn all players
        var playersNode = GetNode<Node3D>(playersNodePath);
        foreach (var kv in players)
        {
            var player = kv.Value.Value;
            var playerNode = playerPrefab.Instantiate<Node3D>();
            playerNode.Name = player.PeerId.ToString();
            playerNode.Position = new Vector3(0, 0, player.PeerId * 4);

            playersNode.AddChild(playerNode);
        }

        // Retrieve our position synchronizer
        positionSynchronizer =
            playersNode
                .GetNode(Multiplayer.GetUniqueId().ToString())
                .GetNode<MultiplayerSynchronizer>("PositionSynchronizer");

        // Set in game
        // Warning: Info cannot be exchanged with RPC from this object
        //          because it may not be instantiated on all peers
        GameManager.Room.SetPlayerState(new PlayerStateInGame());

        // Show us to the players when they are ready
        var tasksList = GameManager.Room.players.Values.Select(obs =>
            Task.Run(async () =>
            {
                // Wait player to be ready
                var player = await obs
                    .Where(player => player.State is PlayerStateInGame)
                    .Take(1);

                CallDeferred(nameof(SetVisibleFor), player.PeerId);
                Log.Debug("Set visible for {PeerId}", player.PeerId);
            })
        );

        await Task.WhenAll(tasksList);
        AllPlayersSpawned();
    }

    private void SetVisibleFor(int peerId) => positionSynchronizer?.SetVisibilityFor(peerId, true);

    public virtual void AllPlayersSpawned() { }
}
