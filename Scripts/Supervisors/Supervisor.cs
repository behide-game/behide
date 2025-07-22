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
    private readonly Serilog.ILogger log = Serilog.Log.ForContext("Tag", "Supervisor/Base");

    [Export] public Node BehideObjects = null!;
    private Node behideObjectsParent = null!;
    [Export] protected PlayerSpawner Spawner = null!;

    protected readonly Dictionary<int, BehaviorSubject<Player>> Players = GameManager.Room.Players;

    public override void _EnterTree()
    {
        // Set authority
        int firstPlayerToJoin;
        try
        {
            firstPlayerToJoin = Players.Min(kv => kv.Key);
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
        BehideObjects.SetMultiplayerAuthority(firstPlayerToJoin);
        if (IsMultiplayerAuthority())
        {
            behideObjectsParent = BehideObjects.GetParent();
            behideObjectsParent.RemoveChild(BehideObjects);
        }

        // Wait players to be ready
        Task.Run(async () =>
        {
            var tasks = Players.Select(kv => kv.Value
                .Where(p => p.State is PlayerStateInGame)
                .Take(1)
                .ToTask()
            );

            await Task.WhenAll(tasks);
            CallDeferred(nameof(PlayersReady));
        });

        SetReadyWhenSceneLoaded();
    }

    private void SetReadyWhenSceneLoaded()
    {
        var sceneNode = GetTree().CurrentScene;
        if (sceneNode.IsNodeReady()) GameManager.Room.SetPlayerState(new PlayerStateInGame());
        else sceneNode.Ready += () => GameManager.Room.SetPlayerState(new PlayerStateInGame());
    }

    protected virtual void PlayersReady()
    {
        if (IsMultiplayerAuthority()) behideObjectsParent.AddChild(BehideObjects);
    }

    public override void _Input(InputEvent @event)
    {
        if (!@event.IsAction(InputActions.OpenMenu, true)) return;
        _ = GameManager.Room.LeaveRoom();
        GameManager.instance.SetGameState(GameManager.GameState.Lobby);
    }
}
