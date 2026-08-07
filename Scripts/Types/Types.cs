using System.Diagnostics.CodeAnalysis;
using MemoryPack;
using Godot;

namespace Behide.Types;

[MemoryPackable]
[method: SetsRequiredMembers]
public partial record Player(int PeerId, string Username, Color Color, PlayerState State)
{
    public required int PeerId { get; init; } = PeerId;
    public required string Username { get; init; } = Username;
    public required Color Color { get; init; } = Color;
    public required PlayerState State { get; init; } = State;
}

[MemoryPackable]
[MemoryPackUnion(0, typeof(PlayerStateInLobby))]
[MemoryPackUnion(1, typeof(PlayerStateInGame))]
public abstract partial record PlayerState;

[MemoryPackable]
public partial record PlayerStateInLobby(bool IsReady) : PlayerState;
[MemoryPackable]
public partial record PlayerStateInGame(bool Alive) : PlayerState;
