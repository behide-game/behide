namespace Behide.UI.Game;

using Godot;
using Serilog;


public partial class Game : Node3D
{
    private ILogger Log = null!;

    [Export] private PackedScene playerPrefab = null!;
    [Export] private NodePath playersNodePath = null!;

    public override void _EnterTree()
    {
        Log = Serilog.Log.ForContext("Tag", "Game");
    }

    public override void _Ready()
    {
        // Spawn player
        var playerId = Multiplayer.GetUniqueId();

        Log.Debug($"Spawning {playerId}");

        // Create node and set his name
        var playerNode = playerPrefab.Instantiate<Node3D>();
        playerNode.Name = playerId.ToString();

        // Put node in the world
        var playersNode = GetNode<Node3D>(playersNodePath);
        playersNode.AddChild(playerNode, true);
    }
}
