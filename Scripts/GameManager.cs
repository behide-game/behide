using Behide.Game;
using Behide.Game.Supervisors;
using Behide.Game.UI.PauseMenu;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide;

using Godot;

public partial class GameManager : Node
{
    private static GameManager instance = null!;

    public static RoomManager Room { get; private set; } = null!;
    public static TimeSynchronizer TimeSync { get; private set; } = null!;
    public static PauseMenu PauseMenu { get; private set; } = null!;
    public static Settings Settings { get; private set; } = null!;

    public enum GameState { Home, Lobby, Game }
    public static GameState State { get; private set; } = GameState.Home;

    private static readonly PackedScene HomeScene = ResourceLoader.Load<PackedScene>("res://Scenes/Home/Home.tscn");
    private static readonly PackedScene LobbyScene = ResourceLoader.Load<PackedScene>("res://Scenes/Lobby/Lobby.tscn");
    private static readonly PackedScene GameScene = ResourceLoader.Load<PackedScene>("res://Scenes/Restaurant/Restaurant.tscn");

    private readonly ILogger log = Log.CreateLogger("GameManager");

    public static Supervisor? Supervisor
    {
        get => State == GameState.Game ? field : null;
        set { if (State == GameState.Game) field = value; }
    }

    public override void _EnterTree()
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (instance is not null) return;
        instance = this;

        GetTree().AutoAcceptQuit = false;

        Room = GetNode<RoomManager>("/root/RoomManager");
        TimeSync = GetNode<TimeSynchronizer>("/root/TimeSynchronizer");
        PauseMenu = GetNode<PauseMenu>("/root/PauseMenu");
        Settings = PauseMenu.Settings;
    }

    public override void _Notification(int what)
    {
        if (what != NotificationWMCloseRequest) return;
        Log.CloseAndFlush();
        GetTree().Quit();
    }


    public static void SetGameState(GameState state)
    {
        var sceneToLoad = state switch
        {
            GameState.Home => HomeScene,
            GameState.Lobby => LobbyScene,
            GameState.Game => GameScene,
            _ => throw new Exception("Unexpected game state"),
        };

        if (PauseMenu.Visible) PauseMenu.Hide();

        State = state;
        instance.GetTree().ChangeSceneToPacked(sceneToLoad);
        instance.log.Verbose("Changed game state to {state}", state);
    }
}
