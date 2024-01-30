#nullable disable
namespace Behide;

using Godot;
using Behide.Networking;
using Behide.Game.UI;

public partial class GameManager : Node3D
{
    public static GameManager instance = null!;

    public static UIManager Ui { get; private set; }
    public static RoomManager Room { get; private set; }
    public static NetworkManager Network { get; private set; }

    public enum GameState { Home, Lobby, Game }
    public static GameState State { get; private set; } = GameState.Home;

    private static readonly PackedScene homeScene = GD.Load<PackedScene>("res://Scenes/Home/Home.tscn");
    private static readonly PackedScene lobbyScene = GD.Load<PackedScene>("res://Scenes/Lobby/Lobby.tscn");
    private static readonly PackedScene gameScene = GD.Load<PackedScene>("res://Scenes/Game/Game.tscn");

    public override void _EnterTree()
    {
        if (instance != null) return;
        instance = this;

        Room = GetNode<RoomManager>("/root/RoomManager");
        Network = GetNode<NetworkManager>("/root/NetworkManager");
        // Ui = GetNode<UIManager>("/root/multiplayer/Managers/UIManager");
    }


    public void SetGameState(GameState state)
    {
        var sceneToLoad = state switch
        {
            GameState.Home => homeScene,
            GameState.Lobby => lobbyScene,
            GameState.Game => gameScene,
            _ => throw new System.Exception("Unexpected game state"),
        };

        State = state;
        GetTree().ChangeSceneToPacked(sceneToLoad);
    }
}
