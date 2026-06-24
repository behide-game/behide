using System.Reactive;
using Godot;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Behide.Types;
using Behide.OnlineServices.Signaling;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide;

public partial class RoomConfiguration : Node
{
    public RoomConfiguration() => Name = "Configuration";

    /// <summary>
    /// 0 when hunters are not chose randomly
    /// </summary>
    private int hunterCount;
    public int HunterCount
    {
        get => hunterCount;
        set
        {
            if (value < 0) return;
            if (hunterCount == value) return;
            Rpc(nameof(RpcSetHunterCount), value);
        }
    }

    private readonly HashSet<int> hunters = [];
    public int[] Hunters => hunters.ToArray();

    private GameManager.GameMap map = GameManager.GameMap.Restaurant;
    public GameManager.GameMap Map
    {
        get => map;
        set
        {
            if (map == value) return;
            Rpc(nameof(RpcSetMap), GameManager.Maps.IndexOf(value));
        }
    }

    private readonly Subject<Unit> changed = new();
    public IObservable<Unit> Changed => changed;
    public override void _ExitTree() => changed.OnCompleted();

    public void AddHunter(int peerId) => Rpc(nameof(RpcAddHunter), peerId);
    public void RemoveHunter(int peerId) => Rpc(nameof(RpcRemoveHunter), peerId);

    public bool IsHunter(int peerId) => hunters.Contains(peerId);

    public void SendTo(long peerId) => RpcId(peerId, nameof(RpcSetAll), HunterCount, hunters.ToArray());

    // RPCs
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcSetHunterCount(int count)
    {
        hunterCount = count;
        changed.OnNext(Unit.Default);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcAddHunter(int peerId)
    {
        hunters.Add(peerId);
        changed.OnNext(Unit.Default);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcRemoveHunter(int peerId)
    {
        hunters.Remove(peerId);
        changed.OnNext(Unit.Default);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcSetMap(int mapIdx)
    {
        map = GameManager.Maps[mapIdx];
        changed.OnNext(Unit.Default);
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer)]
    private void RpcSetAll(int count, int[] hunterIds)
    {
        hunterCount = count;
        foreach (var id in hunterIds) hunters.Add(id);
        changed.OnNext(Unit.Default);
    }
}

public partial class Room : Node
{
    public readonly RoomId RoomId;
    public readonly BehaviorSubject<Player> LocalPlayer;
    public readonly Dictionary<int, BehaviorSubject<Player>> Players = [];
    public readonly RoomConfiguration Configuration = new();

    // Events
    private readonly Subject<Player> playerJoined = new();
    private readonly Subject<Player> playerLeft = new();
    private readonly Subject<Player> playerStateChanged = new();
    private readonly Subject<Unit> roomLeft = new();
    public IObservable<Player> PlayerJoined => playerJoined.AsObservable();
    public IObservable<Player> PlayerLeft => playerLeft.AsObservable();
    public IObservable<Player> PlayerStateChanged => playerStateChanged.AsObservable();
    public IObservable<Unit> RoomLeft => roomLeft.AsObservable();

    private readonly ILogger log = Log.CreateLogger("Room");

    public Room(RoomId roomId, Player player)
    {
        RoomId = roomId;
        LocalPlayer = new BehaviorSubject<Player>(player);
        Name = "Room";
        AddChild(Configuration);
    }

    public Room()
    {
        RoomId = null!;
        throw new Exception("Room should not be instantiated with the parameterless constructor");
    }

    public override void _EnterTree()
    {
        Multiplayer.PeerConnected += MultiplayerOnPeerConnected;
        Multiplayer.PeerDisconnected += MultiplayerOnPeerDisconnected;

        // Register with already connected players
        Rpc(nameof(RegisterPlayerRpc), LocalPlayer.Value);
        foreach (var _ in Multiplayer.GetPeers())
            log.Debug("Registering us with already connected peer: {Username}", LocalPlayer.Value.Username);
    }

    private void MultiplayerOnPeerConnected(long peerId)
    {
        log.Debug("New peer connected, registering us with him: {Username}", LocalPlayer.Value.Username);
        RpcId(peerId, nameof(RegisterPlayerRpc), LocalPlayer.Value);
    }

    private void MultiplayerOnPeerDisconnected(long peerId)
    {
        log.Debug("Player {PeerId} left the room", peerId);
        var playerObservable = Players.GetValueOrDefault((int)peerId);
        if (playerObservable is null) return;

        Players.Remove((int)peerId);
        playerLeft.OnNext(playerObservable.Value);
        playerObservable.OnCompleted();
    }

    public void Leave()
    {
        roomLeft.OnNext(Unit.Default);
        roomLeft.OnCompleted();
        roomLeft.Dispose();

        foreach (var player in Players.Values) player.OnCompleted();
        Players.Clear();
        LocalPlayer.OnCompleted();
        playerJoined.OnCompleted();
        playerLeft.OnCompleted();
        playerStateChanged.OnCompleted();

        LocalPlayer.Dispose();
        playerJoined.Dispose();
        playerLeft.Dispose();
        playerStateChanged.Dispose();
        roomLeft.Dispose();
    }

    /// <summary>Update the player state locally and on the other peers</summary>
    public void SetPlayerState(PlayerState newState) => Rpc(nameof(SetPlayerStateRpc), newState);

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RegisterPlayerRpc(Variant playerVariant)
    {
        var player = Player.FromVariant(playerVariant);
        if (player is null)
        {
            log.Error("[RegisterPlayerRpc] Failed to deserialize variant from {PlayerId}", Multiplayer.GetRemoteSenderId());
            return;
        }

        Players.Add(
            player.PeerId,
            new BehaviorSubject<Player>(player)
        );
        playerJoined.OnNext(player);

        // Sync configuration
        if (Multiplayer.GetUniqueId() != player.PeerId
            && Multiplayer.GetUniqueId() == Players.Keys.Min())
            Configuration.SendTo(player.PeerId);
    }

    /// <summary>Set a player state (the state of the caller)</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SetPlayerStateRpc(Variant newStateVariant)
    {
        var playerId = Multiplayer.GetRemoteSenderId();
        var player = Players.GetValueOrDefault(playerId);
        if (player is null)
        {
            log.Warning("[SetPlayerStateRpc]: Player {PlayerId} not found", playerId);
            return;
        }

        var newState = PlayerState.FromVariant(newStateVariant);
        if (newState is null)
        {
            log.Error("[SetPlayerStateRpc] Failed to deserialize variant from {PlayerId}", playerId);
            return;
        }

        log.Debug("[RPC] Player {PlayerId} is now in state {State}", playerId, newState);

        var newPlayer = player.Value with { State = newState };
        player.OnNext(newPlayer);
        playerStateChanged.OnNext(newPlayer);
        if (playerId == LocalPlayer.Value.PeerId) LocalPlayer.OnNext(newPlayer);
    }
}
