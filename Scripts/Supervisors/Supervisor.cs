using Godot;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Behide.Game.Player;
using Behide.Logging;

namespace Behide.Game.Supervisors;
using Types;

/// <summary>
/// It just spawns players
/// </summary>
public abstract partial class Supervisor : Node
{
    private readonly Serilog.ILogger log = Log.CreateLogger("Supervisor/Base");

    [Export] public Node BehideObjects = null!;
    private Node behideObjectsParent = null!;
    [Export] protected PlayerSpawner Spawner = null!;

    public Room Room = null!;
    protected readonly List<PlayerBody> PlayerBodies = [];

    public override void _EnterTree()
    {
        GameManager.Supervisor = this;
        if (GameManager.Room.Room is null)
        {
            log.Error("Cannot set up supervisor: Not in a room");
            return;
        }
        Room = GameManager.Room.Room;

        // Set authority
        int firstPlayerToJoin;
        try
        {
            firstPlayerToJoin = Room.Players.Min(kv => kv.Key);
        }
        catch (Exception)
        {
            log.Error("No player found. This means that the room is empty. Strange...");
            _ = GameManager.Room.LeaveRoom();
            GameManager.SetGameState(GameManager.GameState.Home);
            return;
        }
        SetMultiplayerAuthority(firstPlayerToJoin);

        // Hide behide objects on the authority to prevent bugs
        BehideObjects.SetMultiplayerAuthority(firstPlayerToJoin);
        if (IsMultiplayerAuthority())
        {
            behideObjectsParent = BehideObjects.GetParent();
            behideObjectsParent.RemoveChild(BehideObjects);
            BehideObjects.SetOwner(null);
        }

        // Wait players to be ready
        Task.Run(async () =>
        {
            var tasks = Room.Players.Select(kv => kv.Value
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
        if (sceneNode.IsNodeReady()) Room.SetPlayerState(new PlayerStateInGame(true));
        else sceneNode.Ready += () => Room.SetPlayerState(new PlayerStateInGame(true));
    }

    protected virtual void PlayersReady()
    {
        if (!IsMultiplayerAuthority()) return;
        behideObjectsParent.AddChild(BehideObjects);
        BehideObjects.SetOwner(behideObjectsParent);
    }

    public void PlayerSpawned(PlayerBody player) => PlayerBodies.Add(player);

    public virtual void PlayerDied(PlayerBody playerBody) { }

    public virtual void LocalPlayerDied(PlayerBody playerBody) => Room.SetPlayerState(new PlayerStateInGame(false));

    public Player? GetBodyPlayer(PlayerBody playerBody)
    {
        var peerId = playerBody.GetMultiplayerAuthority();
        return Room.Players.TryGetValue(peerId, out var player)
            ? player.Value
            : null;
    }
}
