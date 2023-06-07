using System;
using Mirror;

namespace BehideNetwork
{
    public interface IBehideNetworkMsg { }

    public struct PlayerJoined : IBehideNetworkMsg, NetworkMessage
    {
        public string username;
    }

    public struct GameStarted : IBehideNetworkMsg, NetworkMessage { }
    public struct GameEnded : IBehideNetworkMsg, NetworkMessage { }
    public struct RoomClosed : IBehideNetworkMsg, NetworkMessage { }
}

public class BehideNetworkManager : NetworkManager
{
    public event Action OnClientConnected;
    public event Action<NetworkConnectionToClient, BehideNetwork.IBehideNetworkMsg> OnServerNetworkMessage;
    public event Action<BehideNetwork.IBehideNetworkMsg> OnClientNetworkMessage;

    public override void OnStartServer()
    {
        base.OnStartServer();
        NetworkServer.RegisterHandler<BehideNetwork.PlayerJoined>((conn, msg) => OnServerNetworkMessage?.Invoke(conn, msg));
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        NetworkClient.RegisterHandler<BehideNetwork.PlayerJoined>(msg => OnClientNetworkMessage?.Invoke(msg));
        NetworkClient.RegisterHandler<BehideNetwork.GameStarted>(msg => OnClientNetworkMessage?.Invoke(msg));
        NetworkClient.RegisterHandler<BehideNetwork.GameEnded>(msg => OnClientNetworkMessage?.Invoke(msg));
        NetworkClient.RegisterHandler<BehideNetwork.RoomClosed>(msg => OnClientNetworkMessage?.Invoke(msg));
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        OnClientConnected?.Invoke();
    }
}