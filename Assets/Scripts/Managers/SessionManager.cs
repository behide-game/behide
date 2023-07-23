#nullable enable
using System;
using UnityEngine;

public class SessionManager : MonoBehaviour
{
    public string? username { get; private set; }
    public void SetUsername(string username) => this.username = username;

    public Room? room { get; private set; }
    public void SetRoom(Room? room) => this.room = room;

    public Guid? epicId { get; private set; }
    public void SetEpicId(Guid? epicId) => this.epicId = epicId;
}