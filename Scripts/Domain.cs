using System;
using System.Diagnostics.CodeAnalysis;
using MemoryPack;

namespace Behide.Types;

[MemoryPackable]
public partial record Player
{
    public required int PeerId { get; init; }
    public required string Username { get; init; }
    public required PlayerState State { get; init; }

    [SetsRequiredMembers]
    public Player(int peerId, string username, PlayerState state)
    {
        PeerId = peerId;
        Username = username;
        State = state;
    }

    private byte[] ToBytes() => MemoryPackSerializer.Serialize(this);
    private static Player? FromBytes(byte[] bytes) => MemoryPackSerializer.Deserialize<Player>(bytes);

    // Convertible to Godot.Variant for networking
    private Godot.Variant ToVariant() => Godot.Variant.CreateFrom(ToBytes().AsSpan());
    private static Player? FromVariant(Godot.Variant variant)
    {
        var bytes = variant.AsByteArray();
        if (bytes is null) return null;

        return FromBytes(bytes);
    }

    // Simplify the conversion from/to Godot.Variant
    public static implicit operator Godot.Variant(Player player) => player.ToVariant();
    public static implicit operator Player(Godot.Variant variant) => FromVariant(variant)!;
}

[MemoryPackable]
[MemoryPackUnion(0, typeof(PlayerStateInLobby))]
[MemoryPackUnion(1, typeof(PlayerStateInGame))]
public abstract partial record PlayerState
{
    private byte[] ToBytes() => MemoryPackSerializer.Serialize(this);
    private static PlayerState? FromBytes(byte[] bytes) => MemoryPackSerializer.Deserialize<PlayerState>(bytes);

    // Convertible to Godot.Variant for networking
    private Godot.Variant ToVariant() => Godot.Variant.CreateFrom(ToBytes().AsSpan());
    private static PlayerState? FromVariant(Godot.Variant variant)
    {
        var bytes = variant.AsByteArray();
        if (bytes is null) return null;

        return FromBytes(bytes);
    }

    // Simplify the conversion from/to Godot.Variant
    public static implicit operator Godot.Variant(PlayerState state) => state.ToVariant();
    public static implicit operator PlayerState(Godot.Variant variant) => FromVariant(variant)!;
}

[MemoryPackable]
public partial record PlayerStateInLobby(bool IsReady) : PlayerState;
[MemoryPackable]
public partial record PlayerStateInGame() : PlayerState;
