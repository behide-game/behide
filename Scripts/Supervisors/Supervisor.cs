using System;
using Godot;
using System.Linq;
using System.Collections.Generic;
using System.Reactive.Subjects;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;

namespace Behide.Game.Supervisors;
using Types;

/// <summary>
/// It just spawns players
/// </summary>
public partial class Supervisor : Node
{
    private const string Tag = "Supervisor/Basic";
    private readonly Serilog.ILogger log = Serilog.Log.ForContext("Tag", Tag);

    [Export] private string openMenuAction = null!;
    [Export] private Node behideObjects = null!;
    private Node behideObjectsParent = null!;
    [Export] protected PlayerSpawner Spawner = null!;

    private readonly Dictionary<int, BehaviorSubject<Player>> players = GameManager.Room.Players;

    public override void _EnterTree()
    {
        // Set authority
        int firstPlayerToJoin;
        try
        {
            firstPlayerToJoin = players.Min(kv => kv.Key);
        }
        catch (Exception)
        {
            log.Error("No player found. This means that the room is empty. Strange...");
            _ = GameManager.Room.LeaveRoom();
            GameManager.instance.SetGameState(GameManager.GameState.Home);
            return;
        }
        SetMultiplayerAuthority(firstPlayerToJoin);

        // Hide behide objects on the authority to prevent bugs
        behideObjects.SetMultiplayerAuthority(firstPlayerToJoin);
        if (IsMultiplayerAuthority())
        {
            behideObjectsParent = behideObjects.GetParent();
            behideObjectsParent.RemoveChild(behideObjects);
        }

        // Wait players to be ready
        Task.Run(async () =>
        {
            var tasks = players.Select(kv => kv.Value
                .Where(p => p.State is PlayerStateInGame)
                .Take(1)
                .ToTask()
            );

            await Task.WhenAll(tasks);
            CallDeferred(nameof(PlayersReady));
            behideObjectsParent.CallDeferred(Node.MethodName.AddChild, behideObjects);
        });

        SetReadyWhenSceneLoaded();
    }

    private void SetReadyWhenSceneLoaded()
    {
        var sceneNode = GetTree().CurrentScene;
        if (sceneNode.IsNodeReady()) GameManager.Room.SetPlayerState(new PlayerStateInGame());
        else sceneNode.Ready += () => GameManager.Room.SetPlayerState(new PlayerStateInGame());
    }

    protected virtual void PlayersReady() {}

    public override void _Input(InputEvent @event)
    {
        if (!@event.IsAction(openMenuAction, true)) return;
        _ = GameManager.Room.LeaveRoom();
        GameManager.instance.SetGameState(GameManager.GameState.Lobby);
    }
}
