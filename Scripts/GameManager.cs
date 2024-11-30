#nullable disable
namespace Behide;

using Godot;
using Behide.Log;
using Behide.Networking;

public partial class GameManager : Node3D
{
    public static GameManager instance = null!;

    public static RoomManager Room { get; private set; }
    public static NetworkManager Network { get; private set; }

    public enum GameState { Home, Lobby, Game }
    public static GameState State { get; private set; } = GameState.Home;

    private static readonly PackedScene homeScene = GD.Load<PackedScene>("res://Scenes/Home/Home.tscn");
    private static readonly PackedScene lobbyScene = GD.Load<PackedScene>("res://Scenes/Lobby/Lobby.tscn");
    private static readonly PackedScene gameScene = GD.Load<PackedScene>("res://Scenes/Game/Game.tscn");

    private Serilog.ILogger Log = null!;

    public override void _EnterTree()
    {
        if (instance != null) return;
        instance = this;

        GetTree().AutoAcceptQuit = false;

        Logging.ConfigureLogger();
        Log = Serilog.Log.ForContext("Tag", "GameManager");

        Room = GetNode<RoomManager>("/root/RoomManager");
        Network = GetNode<NetworkManager>("/root/NetworkManager");
    }

    public override void _Notification(int what)
    {
        if (what == NotificationWMCloseRequest)
        {
            Serilog.Log.CloseAndFlush();
            GetTree().Quit();
        }
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
        Log.Verbose("Changed game state to {state}", state);
    }
}
