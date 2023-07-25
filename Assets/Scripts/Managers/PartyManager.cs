#nullable enable
using UnityEngine;
using Mirror;
using BehideServer.Types;
using UnityEngine.SceneManagement;
using System.Threading.Tasks;

public class PartyManager : MonoBehaviour
{
    private ScenesManager scenes = null!;
    private SessionManager session = null!;
    private NetworkManager network = null!;
    private UIManager ui = null!;

    void Start()
    {
        scenes = GameManager.instance.scenes;
        session = GameManager.instance.session;
        network = GameManager.instance.network;
        ui = GameManager.instance.ui;

        network.OnServerNetworkMessage += HandleServerNetworkMessage;
        network.OnClientNetworkMessage += HandleClientNetworkMessage;
        // network.OnServerDisconnected += HandlePlayerDisconnection;
    }

    private void HandleServerNetworkMessage(NetworkConnection connection, BehideNetwork.IBehideNetworkMsg rawMsg)
    {
        if (session.room == null || session.room?.isHost != true) return;
        switch (rawMsg)
        {
            case BehideNetwork.PlayerJoined:
                var msg = (BehideNetwork.PlayerJoined)rawMsg;
                session.room.AddPlayer(connection.connectionId, msg.username);
                if (connection.connectionId != NetworkClient.connection.connectionId)
                    ui.LogInfo($"{msg.username} as joined");
                break;
            default: break;
        }
    }
    private void HandleClientNetworkMessage(BehideNetwork.IBehideNetworkMsg msg)
    {
        switch (msg)
        {
            case BehideNetwork.GameEnded:
                SceneManager.LoadSceneAsync(scenes.homeSceneName);
                break;

            case BehideNetwork.RoomClosed:
                ui.LogInfo("Room closed");

                network.StopClient();
                if (session.room?.isHost == true) network.StopServer();

                session.SetRoom(null);

                if (SceneManager.GetActiveScene().name != scenes.homeSceneName)
                    SceneManager.LoadScene(scenes.homeSceneName);
                else
                    GameObject.Find("UI").GetComponent<Home>().ResetUI();

                break;

            default: break;
        }
    }

    // private void HandlePlayerDisconnection(NetworkConnectionToClient conn) // TODO
    // {
    //     if (session.room?.isHost != true) return;
    //     network.mirrorNetwork.transport.ServerDisconnect(conn.connectionId);
    //     session.room.RemovePlayer(conn.connectionId);
    // }


    public async Task<Result> CreateRoom()
    {
        var resRoomId = await network.CreateRoom();
        if (resRoomId.IsFailure)
        {
            ui.LogInfo("Failed to create room: " + resRoomId.Error);
            return resRoomId;
        }

        session.SetRoom(new Room(resRoomId.Value, true));

        // Add current player to session.room's connectedPlayers
        var joinMessage = new BehideNetwork.PlayerJoined { username = session.username };
        NetworkClient.Send(joinMessage);

        ui.LogInfo("Created room: " + session.room?.id.ToString());
        return Result.Ok();
    }
    public async Task<Result> JoinRoom(string rawRoomId)
    {
        if (session.username == null) return ui.LogFail("Cannot join room: invalid username");
        if (!RoomId.TryParse(rawRoomId, out RoomId targetRoomId)) return ui.LogFail("Cannot join room: invalid roomId");
        ui.LogInfo("Joining room " + targetRoomId.ToString());

        var res = await network.JoinRoom(targetRoomId);
        if (res.IsFailure) return ui.LogFail("Failed to join room: " + res.Error);

        session.SetRoom(new Room(targetRoomId, false));

        // Send info to session.room's host
        var joinMessage = new BehideNetwork.PlayerJoined { username = session.username };
        NetworkClient.Send(joinMessage);

        return Result.Ok();
    }
    public async Task<Result> CloseRoom()
    {
        if (session.room?.isHost != true) return ui.LogFail("Cannot close room: not the host");

        foreach (var player in session.room.connectedPlayers)
        {
            // Excluding local client
            if (player.Key == NetworkClient.connection.connectionId) continue;

            if (!NetworkServer.connections.TryGetValue(player.Key, out NetworkConnectionToClient connection))
            {
                ui.LogError($"Closing room: failed to find connection for player {player.Key}");
                continue;
            }

            connection.Send(new BehideNetwork.RoomClosed());
        }

        // Shutting down to local client
        await System.Threading.Tasks.Task.Run(() => // Awaiting all other clients to disconnect
        {
            while (NetworkServer.connections.Count > 1) { }
        });
        // Closed room an all external clients. Closing local one.
        NetworkClient.Send(new BehideNetwork.RoomClosed());

        return Result.Ok();
    }

    public async Task<Result> StartGame()
    {
        if (session.room?.isHost != true) return ui.LogFail("Cannot start game: not the room host");

        await network.SetGameScene();
        NetworkClient.Ready();

        foreach (var player in session.room.connectedPlayers)
        {
            if (!NetworkServer.connections.TryGetValue(player.Key, out NetworkConnectionToClient connection))
            {
                ui.LogError($"Starting game: failed to find connection for player {player.Key}");
                continue;
            };

            GameObject gameObject = Instantiate(network.GetPlayerPrefab());
            gameObject.name = $"Player connId={player.Key}";

            NetworkServer.AddPlayerForConnection(connection, gameObject);
        }

        return Result.Ok();
    }

    public Result EndGame()
    {
        if (session.room?.isHost != true) return ui.LogFail("Cannot end game: not the room host");
        ui.LogInfo("Ending game");
        NetworkServer.SendToAll(new BehideNetwork.GameEnded());
        return Result.Ok();
    }
}