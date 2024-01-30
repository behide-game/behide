namespace Behide;

using Godot;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using Behide.Networking;
using Behide.OnlineServices;

public record Player(int Id, string Username);

public partial class RoomManager : Node3D
{
    private NetworkManager network = null!;

    public readonly List<Player> players = [];
    public event Action<Player>? PlayerRegistered;

    public override void _EnterTree()
    {
        network = GameManager.Network;
    }


    public async Task<Result<RoomId>> CreateRoom()
    {
        switch (await network.StartHost())
        {
            case Result<RoomId>.Error error:
                return new Result<RoomId>.Error($"Failed to create a room: {error.Failure}");

            case Result<RoomId>.Ok roomId:
                RegisterPlayer($"Player: {Multiplayer.GetUniqueId()}");
                return new Result<RoomId>.Ok(roomId.Value);

            default:
                return new Result<RoomId>.Error($"Unexpected error");
        };
    }

    public async void JoinRoom(RoomId roomId)
    {
        // Connect
        // GameManager.Ui.Log($"Connecting...");
        await network.StartClient(roomId);
        RegisterPlayer($"Player: {Multiplayer.GetUniqueId()}");
    }


    public void RegisterPlayer(string username) => Rpc(nameof(RegisterPlayerRpc), username);

    [Rpc(
        MultiplayerApi.RpcMode.AnyPeer,
        CallLocal = true,
        TransferMode = MultiplayerPeer.TransferModeEnum.Reliable
    )]
    private void RegisterPlayerRpc(string username)
    {
        var playerId = Multiplayer.GetRemoteSenderId();
        var player = new Player(playerId, username);

        players.Add(player);
        PlayerRegistered?.Invoke(player);
    }


    // public void SpawnPlayer(long playerId)
    // {
    //     GD.Print($"Spawning {playerId}");
    //     var mainNode = GetNode("/root/multiplayer");

    //     // Create node and set his name
    //     var playerPrefab = GD.Load<PackedScene>("res://Prefabs/player.tscn");
    //     var playerNode = playerPrefab.Instantiate<Node3D>();
    //     playerNode.Name = playerId.ToString();

    //     // Put node in the world
    //     mainNode.AddChild(playerNode, true);
    // }
}
