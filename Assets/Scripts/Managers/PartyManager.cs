#nullable enable
using UnityEngine;
using Mirror;
using BehideServer.Types;
using UnityEngine.SceneManagement;

public class PartyManager : MonoBehaviour
{
    private string homeSceneName = "home";
    private string gameSceneName = "game";
    private SessionManager session = null!;
    private NetworkManager network = null!;

    void Start()
    {
        session = GameManager.instance.session;
        network = GameManager.instance.network;

        network.mirrorNetwork.OnServerNetworkMessage += HandleServerNetworkMessage;
        network.mirrorNetwork.OnClientNetworkMessage += HandleClientNetworkMessage;
        network.mirrorNetwork.OnServerDisconnected += HandlePlayerDisconnection;
    }

    private void HandleServerNetworkMessage(NetworkConnection connection, BehideNetwork.IBehideNetworkMsg rawMsg)
    {
        if (session.room == null || session.room?.isHost != true) return;
        switch (rawMsg) {
            case BehideNetwork.PlayerJoined:
                var msg = (BehideNetwork.PlayerJoined)rawMsg;
                session.room.AddPlayer(connection.connectionId, msg.username);
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
                Debug.Log("Session.room closed");

                if (session.room?.isHost == true)
                    network.mirrorNetwork.StopHost();
                else network.mirrorNetwork.StopClient();

                session.SetRoom(null);

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
        if (session.room?.isHost != true) return;
        network.mirrorNetwork.transport.ServerDisconnect(conn.connectionId);
        session.room.RemovePlayer(conn.connectionId);
    }


    public async void CreateRoom()
    {
        Debug.Log("Creating session.room");

        RoomId roomId = await network.CreateRoom();
        session.SetRoom(new Room(roomId, true));

        // Add current player to session.room's connectedPlayers
        var joinMessage = new BehideNetwork.PlayerJoined { username = session.username };
        NetworkClient.Send(joinMessage);
    }
    public async void JoinRoom(string rawRoomId)
    {
        if (session.username == null) return;
        if (!RoomId.TryParse(rawRoomId, out RoomId targetRoomId)) return;
        Debug.Log("Joining session.room");

        await network.JoinRoom(targetRoomId);
        session.SetRoom(new Room(targetRoomId, false));

        // Send info to session.room's host
        var joinMessage = new BehideNetwork.PlayerJoined { username = session.username };
        NetworkClient.Send(joinMessage);
    }
    public async void CloseRoom()
    {
        if (session.room?.isHost != true) return;
        Debug.Log("Closing session.room");

        int localConnectionId = NetworkClient.connection.connectionId;

        foreach (var player in session.room.connectedPlayers)
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
        Debug.Log("Closed session.room an all external clients. Closing local one.");
        HandleClientNetworkMessage(new BehideNetwork.RoomClosed());
    }

    public void StartGame()
    {
        if (session.room?.isHost != true) return;

        network.mirrorNetwork.ServerChangeScene(gameSceneName);
        Mirror.NetworkManager.loadingSceneAsync.completed += _ =>
        {
            foreach (var player in session.room.connectedPlayers)
            {
                if (!NetworkServer.connections.TryGetValue(player.Key, out NetworkConnectionToClient connection)) return;

                GameObject gameObject = Instantiate(network.mirrorNetwork.playerPrefab);
                gameObject.name = $"Player {player.Key}";

                if (!NetworkClient.ready) NetworkClient.Ready();
                NetworkServer.AddPlayerForConnection(connection, gameObject);
            }
        };
    }

    public void EndGame()
    {
        if (session.room?.isHost != true) return;
        Debug.Log("Ending game");
        NetworkServer.SendToAll(new BehideNetwork.GameEnded());
    }
}