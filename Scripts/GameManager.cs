using Behide.Game.Supervisors;

namespace Behide;

using Godot;
using Networking;

public partial class GameManager : Node
{
    public static GameManager instance = null!;

    public static RoomManager Room { get; private set; } = null!;
    public static NetworkManager Network { get; private set; } = null!;
    public static TimeSynchronizer TimeSync { get; private set; } = null!;

    public enum GameState { Home, Lobby, Game }
    public static GameState State { get; private set; } = GameState.Home;

    private static readonly PackedScene homeScene = ResourceLoader.Load<PackedScene>("res://Scenes/Home/Home.tscn");
    private static readonly PackedScene lobbyScene = ResourceLoader.Load<PackedScene>("res://Scenes/Lobby/Lobby.tscn");
    private static readonly PackedScene gameScene = ResourceLoader.Load<PackedScene>("res://Scenes/Game/Game.tscn");

    private Serilog.ILogger log = null!;

    public static Supervisor? Supervisor
    {
        get => State == GameState.Game ? field : null;
        set
        {
            if (State == GameState.Game) field = value;
        }
    }

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
        TimeSync = GetNode<TimeSynchronizer>("/root/TimeSynchronizer");
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
            _ => throw new Exception("Unexpected game state"),
        };

        State = state;
        GetTree().ChangeSceneToPacked(sceneToLoad);
        log.Verbose("Changed game state to {state}", state);
    }
}
