using System;
using Godot;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Threading.Tasks;

namespace Behide.Game.Supervisors;
using Types;

/// <summary>
/// It just spawns players
/// </summary>
public partial class BasicSupervisor : Node
{
    private Serilog.ILogger log = null!;
    public const string Tag = "Supervisor/Basic";

    [Export] private PackedScene playerPrefab = null!;
    [Export] private NodePath playersNodePath = null!;

    private readonly Dictionary<int, BehaviorSubject<Player>> players = GameManager.Room.Players;

    private MultiplayerSynchronizer? positionSynchronizer;

    public override void _EnterTree()
    {
        log = Serilog.Log.ForContext("Tag", Tag);

        // Set authority to the last player to join
        // The authority is the player who spawns players and choose randomly the hunter
        int lastPlayerToJoin;
        try
        {
            lastPlayerToJoin = players.Max(kv => kv.Key);
        }
        catch (Exception)
        {
            log.Error("No player found. This means that the room is empty. Strange...");
            return;
        }

        SetMultiplayerAuthority(lastPlayerToJoin);
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
        var tasksList = GameManager.Room.Players.Values.Select(playerObs =>
            Task.Run(async () =>
            {
                // Wait player to be ready
                var player = await playerObs
                    .Where(player => player.State is PlayerStateInGame)
                    .Take(1);

                CallDeferred(nameof(SetVisibleFor), player.PeerId);
                log.Debug("Set visible for {PeerId}", player.PeerId);
            })
        );

        await Task.WhenAll(tasksList);
        AllPlayersSpawned();
    }

    private void SetVisibleFor(int peerId) => positionSynchronizer?.SetVisibilityFor(peerId, true);

    protected virtual void AllPlayersSpawned() { }
}
