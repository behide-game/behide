using System;
using UnityEngine;
using Mirror;

public class RoomManager : MonoBehaviour
{
    private NetworkRoomManager manager;
    [SerializeField]
    private string serverUri;

    void Awake()
    {
        manager = GetComponent<NetworkRoomManager>();

        string[] args = Environment.GetCommandLineArgs();
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--launch-as-server")
            {
                manager.StartServer();
                Console.WriteLine("Server started");
            }
        }
    }

    public void ConnectServer()
    {
        Uri.TryCreate(serverUri, UriKind.Absolute, out Uri uri);

        if (uri == null)
        {
            Debug.Log("Incorrect uri !");
        } else
        {
            manager.StartClient(uri);
        }
    }
}
