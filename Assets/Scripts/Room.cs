#nullable enable

using System.Collections.Generic;
using UnityEngine;
using BehideServer.Types;

public class Room
{
    public RoomId id;
    public bool isHost;
    public Dictionary<int, string> connectedPlayers;

    public Room(RoomId _id, bool _isHost)
    {
        id = _id;
        isHost = _isHost;
        connectedPlayers = new();
    }

    public void AddPlayer(int connectionId, string username)
    {
        if (!connectedPlayers.TryAdd(connectionId, username))
            Debug.LogError($"Failed to add player named \"{username}\" (connectionId: {connectionId}) to connectedPlayers dictionary.");
    }

    public void RemovePlayer(int connectionId)
    {
        if (!connectedPlayers.Remove(connectionId))
            Debug.LogError($"Failed to remove player with connectionId \"{connectionId}\" of connectedPlayers dictionary.");
    }
}