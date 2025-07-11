namespace Behide;

using Godot;
using Networking;

public partial class GameManager : Node
{
    public static GameManager instance = null!;

    public static RoomManager Room { get; private set; } = null!;
    public static NetworkManager Network { get; private set; } = null!;

    public enum GameState { Home, Lobby, Game }
    public static GameState State { get; private set; } = GameState.Home;

    private static readonly PackedScene HomeScene = ResourceLoader.Load<PackedScene>("res://Scenes/Home/Home.tscn");
    private static readonly PackedScene LobbyScene = ResourceLoader.Load<PackedScene>("res://Scenes/Lobby/Lobby.tscn");
    private static readonly PackedScene GameScene = ResourceLoader.Load<PackedScene>("res://Scenes/Game/Game.tscn");

    private Serilog.ILogger log = null!;

    public override void _EnterTree()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (instance is not null) return;
        instance = this;

        GetTree().AutoAcceptQuit = false;

        Logging.Logging.ConfigureLogger();
        log = Serilog.Log.ForContext("Tag", "GameManager");

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
            GameState.Home => HomeScene,
            GameState.Lobby => LobbyScene,
            GameState.Game => GameScene,
            _ => throw new System.Exception("Unexpected game state"),
        };

        State = state;
        GetTree().ChangeSceneToPacked(sceneToLoad);
        log.Verbose("Changed game state to {state}", state);
    }
}
