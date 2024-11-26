namespace Behide.UI.Game;

using System;
using Godot;
using Serilog;
using Behide.Types;

public partial class Game : Node3D
{
    private ILogger Log = null!;

    [Export] private PackedScene playerPrefab = null!;
    [Export] private NodePath playersNodePath = null!;

    public override void _EnterTree()
    {
        Log = Serilog.Log.ForContext("Tag", "Game");

        // Spawn players
        GameManager.Room.PlayerStateChanged.Subscribe(SpawnPlayer);
        foreach (var player in GameManager.Room.players)
        {
            if (player.State is not PlayerStateInGame)
                continue;
            SpawnPlayer(player);
        }
    }

    private void SpawnPlayer(Player player)
    {
        Log.Debug("Spawning {PeerId}", player.PeerId);

        // Create node and set his name
        var playerNode = playerPrefab.Instantiate<Node3D>();
        playerNode.Name = player.PeerId.ToString();

        // Put node in the world / the scene / the tree
        var playersNode = GetNode<Node3D>(playersNodePath);
        playersNode.AddChild(playerNode, true);
    }
}
