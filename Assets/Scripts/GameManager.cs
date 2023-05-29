#nullable enable
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using BehideServer.Types;

public class GameManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "";
    [SerializeField] private string homeSceneName = "";

    static public GameManager instance { get; private set; } = null!;
    public ConnectionsManager connectionManager = null!;

    // Game state
    public string? username { get; private set; }
    public (RoomId id, bool isHost, (int connectionId, string username)[] connectedPlayers)? room { get; private set; }

    void Awake()
    {
        if (instance != null) { Destroy(this); return; }

        instance = this;
        DontDestroyOnLoad(this);
    }


    public void SetUsername(string newUsername) => username = newUsername;

    public void PlayerJoined(NetworkConnectionToClient connection, string username)
    {
        if (room == null || room?.isHost != true) return;

        var newPlayerArray = room.Value.connectedPlayers.Append((connection.connectionId, username)).ToArray();
        room = (room.Value.id, room.Value.isHost, newPlayerArray);
    }

    public async void CreateRoom()
    {
        Debug.Log("Creating room");

        var roomId = await connectionManager.CreateRoom();
        room = (roomId, true, new (int, string)[] { });
    }
    public async void JoinRoom(string rawRoomId)
    {
        if (username == null) return;
        if (!RoomId.TryParse(rawRoomId, out RoomId targetRoomId)) return;
        Debug.Log("Joining room");

        await connectionManager.JoinRoom(targetRoomId);
        room = (targetRoomId, false, new (int, string)[] { });
    }

    public void StartGame()
    {
        if (room?.isHost != true) return;

        connectionManager.networkManager.ServerChangeScene(gameSceneName);

        NetworkManager.loadingSceneAsync.completed += _ =>
        {
            foreach (var player in room.Value.connectedPlayers)
            {
                if (!NetworkServer.connections.TryGetValue(player.connectionId, out NetworkConnectionToClient connection)) return;

                GameObject gameObject = Instantiate(connectionManager.networkManager.playerPrefab);
                gameObject.name = $"Player {player.connectionId}";

                if (!NetworkClient.ready) NetworkClient.Ready();
                NetworkServer.AddPlayerForConnection(connection, gameObject);
            }
        };
    }

    public void EndGame()
    {
        if (room?.isHost != true) return;
        Debug.Log("Ending game");

        connectionManager.networkManager.ServerChangeScene(homeSceneName);
        NetworkServer.SendToAll(new BehideNetworkManager.GameEndedMessage());
    }

    public void CloseRoom()
    {
        if (room?.isHost != true) return;
        Debug.Log("Closing room");

        int localConnectionId = NetworkClient.connection.connectionId;

        foreach (var player in room.Value.connectedPlayers)
        {
            // Excluding local client
            if (player.connectionId == localConnectionId) continue;

            if (!NetworkServer.connections.TryGetValue(player.connectionId, out NetworkConnectionToClient connection)) return;
            connection.Send(new BehideNetworkManager.RoomClosedMessage());
        }

        // Sending to local client
        System.Threading.Tasks.Task.Run(() => {
            while (true)
            {
                if (NetworkServer.connections.Count > 1) continue;

                Debug.Log("Closed room an all external clients. Closing local one.");

                if (NetworkServer.connections.TryGetValue(localConnectionId, out NetworkConnectionToClient localConnection) && localConnection != null)
                    localConnection.Send(new BehideNetworkManager.RoomClosedMessage());

                return;
            }
        });
    }

    public void RoomClosed()
    {
        Debug.Log("Room closed");

        if (room?.isHost == true)
            connectionManager.networkManager.StopHost();
        else
            connectionManager.networkManager.StopClient();

        room = null;

        if (SceneManager.GetActiveScene().name != homeSceneName)
            SceneManager.LoadScene(homeSceneName);
        else
            GameObject.Find("UI").GetComponent<Home>().ResetUI();
    }
}