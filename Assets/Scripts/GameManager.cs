#nullable enable
using UnityEngine;

[RequireComponent(typeof(ScenesManager))]
[RequireComponent(typeof(UIManager))]
[RequireComponent(typeof(SessionManager))]
[RequireComponent(typeof(NetworkManager))]
[RequireComponent(typeof(PartyManager))]
public class GameManager : MonoBehaviour
{
    static public GameManager instance { get; private set; } = null!;

    [HideInInspector] public ScenesManager scenes = null!;
    [HideInInspector] public UIManager ui = null!;
    [HideInInspector] public SessionManager session = null!;
    [HideInInspector] public NetworkManager network = null!;
    [HideInInspector] public PartyManager party = null!;

    void Awake()
    {
        if (instance != null) { Destroy(this); return; }

        instance = this;
        DontDestroyOnLoad(this);

        scenes = GetComponent<ScenesManager>();
        ui = GetComponent<UIManager>();
        session = GetComponent<SessionManager>();
        network = GetComponent<NetworkManager>();
        party = GetComponent<PartyManager>();
    }
}