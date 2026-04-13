using System.Reactive;
using Godot;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Behide.Types;
using Behide.OnlineServices.Signaling;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide;

// public static class MyExtensions
// {
//     extension<T>(IMemoryPackable<T> packable)
//     {
//         private byte[] ToBytes() => MemoryPackSerializer.Serialize((T)packable);
//         private static T? FromBytes(byte[] bytes) => MemoryPackSerializer.Deserialize<T>(bytes);
//
//         public Variant ToVariant() => Variant.CreateFrom(packable.ToBytes().AsSpan());
//         public static T? FromVariant(Variant variant)
//         {
//             var bytes = variant.AsByteArray();
//             return IMemoryPackable<T>.FromBytes(bytes);
//         }
//     }
// }

public partial class RoomConfiguration : Node
{
    public RoomConfiguration() => Name = "Configuration";

    private bool randomHunter;
    private int hunterCount = 1;
    private readonly HashSet<int> hunters = [];
    private readonly Subject<Unit> changed = new();

    public bool RandomHunter { get => randomHunter; set => Rpc(nameof(RpcSetRandomHunter), value); }
    public int HunterCount { get => hunterCount; set => Rpc(nameof(RpcSetHunterCount), value); }
    public IObservable<Unit> Changed => changed;

    public void AddHunter(int peerId) => Rpc(nameof(RpcAddHunter), peerId);
    public void RemoveHunter(int peerId) => Rpc(nameof(RpcRemoveHunter), peerId);

    public bool IsHunter(int peerId) => hunters.Contains(peerId);

    public void SendTo(long peerId)
    {
        RpcId(peerId, nameof(RpcSetRandomHunter), RandomHunter);
        RpcId(peerId, nameof(RpcSetHunterCount), HunterCount);
        foreach (var hunterId in hunters)
            RpcId(peerId, nameof(RpcAddHunter), hunterId);
    }

    // RPCs
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void RpcSetRandomHunter(bool random)
    {
        randomHunter = random;
        changed.OnNext(Unit.Default);
    }

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
    private void RpcSetAll(bool random, int count, int[] hunterIds)
    {
        randomHunter = random;
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
        log.Debug("Room enters tree");
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

        // Sync configuration
        if (Multiplayer.GetUniqueId() == Players.Keys.Min())
            Configuration.SendTo(peerId);
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
        var player = (Player)playerVariant;
        Players.Add(
            player.PeerId,
            new BehaviorSubject<Player>(player)
        );
        playerJoined.OnNext(player);
    }

    /// <summary>Set a player state (the state of the caller)</summary>
    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SetPlayerStateRpc(Variant newStateVariant)
    {
        var playerId = Multiplayer.GetRemoteSenderId();
        var player = Players.GetValueOrDefault(playerId);

        var newState = (PlayerState)newStateVariant;
        log.Debug("[RPC] Player {PlayerId} is now in state {State}", playerId, newState);

        if (player is null)
        {
            log.Warning("[SetPlayerStateRpc]: Player {PlayerId} not found", playerId);
            return;
        }

        var newPlayer = player.Value with { State = newState };
        player.OnNext(newPlayer);
        playerStateChanged.OnNext(newPlayer);
        if (playerId == LocalPlayer.Value.PeerId) LocalPlayer.OnNext(newPlayer);
    }
}
