#nullable enable
using UnityEngine;
using UnityEngine.SceneManagement;
using Mirror;
using BehideServer.Types;

public class GameManager : MonoBehaviour
{
    [SerializeField] private string gameSceneName = "";
    [SerializeField] private string homeSceneName = "";

    static public GameManager instance { get; private set; } = null!;
    public ConnectionsManager connectionsManager = null!;

    // Game state
    public string? username { get; private set; }
    public Room? room { get; private set; }

    void Awake()
    {
        if (instance != null) { Destroy(this); return; }

        instance = this;
        DontDestroyOnLoad(this);

        connectionsManager.networkManager.OnServerNetworkMessage += HandleServerNetworkMessage;
        connectionsManager.networkManager.OnClientNetworkMessage += HandleClientNetworkMessage;
        connectionsManager.networkManager.OnServerDisconnected += HandlePlayerDisconnection;
    }

    public void SetUsername(string newUsername) => username = newUsername;


    private void HandleServerNetworkMessage(NetworkConnection connection, BehideNetwork.IBehideNetworkMsg rawMsg)
    {
        if (room == null || room?.isHost != true) return;
        switch (rawMsg) {
            case BehideNetwork.PlayerJoined:
                var msg = (BehideNetwork.PlayerJoined)rawMsg;
                room.AddPlayer(connection.connectionId, msg.username);
                break;
            default: break;
        }
    }
    private void HandleClientNetworkMessage(BehideNetwork.IBehideNetworkMsg msg)
    {
        switch (msg) {
            case BehideNetwork.GameEnded:
                SceneManager.LoadSceneAsync(homeSceneName);
                break;

            case BehideNetwork.RoomClosed:
                Debug.Log("Room closed");

                if (room?.isHost == true)
                    connectionsManager.networkManager.StopHost();
                else connectionsManager.networkManager.StopClient();

                room = null;

                if (SceneManager.GetActiveScene().name != homeSceneName)
                    SceneManager.LoadScene(homeSceneName);
                else
                    GameObject.Find("UI").GetComponent<Home>().ResetUI();

                break;

            default: break;
        }
    }

    private void HandlePlayerDisconnection(NetworkConnectionToClient conn)
    {
        if (room?.isHost != true) return;
        connectionsManager.networkManager.transport.ServerDisconnect(conn.connectionId);
        room.RemovePlayer(conn.connectionId);
    }


    public async void CreateRoom()
    {
        Debug.Log("Creating room");

        RoomId roomId = await connectionsManager.CreateRoom();
        room = new Room(roomId, true);

        // Add current player to room's connectedPlayers
        var joinMessage = new BehideNetwork.PlayerJoined { username = username };
        NetworkClient.Send(joinMessage);
    }
    public async void JoinRoom(string rawRoomId)
    {
        if (username == null) return;
        if (!RoomId.TryParse(rawRoomId, out RoomId targetRoomId)) return;
        Debug.Log("Joining room");

        await connectionsManager.JoinRoom(targetRoomId);
        room = new Room(targetRoomId, false);

        // Send info to room's host
        var joinMessage = new BehideNetwork.PlayerJoined { username = username };
        NetworkClient.Send(joinMessage);
    }
    public async void CloseRoom()
    {
        if (room?.isHost != true) return;
        Debug.Log("Closing room");

        int localConnectionId = NetworkClient.connection.connectionId;

        foreach (var player in room.connectedPlayers)
        {
            // Excluding local client
            if (player.Key == localConnectionId) continue;

            if (!NetworkServer.connections.TryGetValue(player.Key, out NetworkConnectionToClient connection)) return;
            connection.Send(new BehideNetwork.RoomClosed());
        }

        // Shutting down to local client
        await System.Threading.Tasks.Task.Run(() => { // Awaiting all other clients to disconnect
            while (NetworkServer.connections.Count > 1) { }
        });
        Debug.Log("Closed room an all external clients. Closing local one.");
        HandleClientNetworkMessage(new BehideNetwork.RoomClosed());
    }

    public void StartGame()
    {
        if (room?.isHost != true) return;

        connectionsManager.networkManager.ServerChangeScene(gameSceneName);
        NetworkManager.loadingSceneAsync.completed += _ =>
        {
            foreach (var player in room.connectedPlayers)
            {
                if (!NetworkServer.connections.TryGetValue(player.Key, out NetworkConnectionToClient connection)) return;

                GameObject gameObject = Instantiate(connectionsManager.networkManager.playerPrefab);
                gameObject.name = $"Player {player.Key}";

                if (!NetworkClient.ready) NetworkClient.Ready();
                NetworkServer.AddPlayerForConnection(connection, gameObject);
            }
        };
    }

    public void EndGame()
    {
        if (room?.isHost != true) return;
        Debug.Log("Ending game");
        NetworkServer.SendToAll(new BehideNetwork.GameEnded());
    }
}