using UnityEngine;
using UnityEngine.UIElements;

public class Home : MonoBehaviour
{
    private GameManager gameManager;

    [SerializeField] UIDocument uiDoc;
    private Button createGameButton;
    private Button joinGameButton;

    private bool showCreateGameUI = false;
    private bool showJoinGameUI = false;
    private string GUIjoinRoomId = "";

    void Awake()
    {
        gameManager = GameManager.instance;
    }

    void Start()
    {
        // Create room button
        createGameButton = uiDoc.rootVisualElement.Query<Button>("CreateButton");
        createGameButton.clickable.clicked += () =>
        {
            gameManager.CreateRoom();

            showCreateGameUI = true;
            VisualElement container = uiDoc.rootVisualElement.Query("Container");
            container.style.display = DisplayStyle.None;
        };

        // Join room button
        joinGameButton = uiDoc.rootVisualElement.Query<Button>("JoinButton");
        joinGameButton.clickable.clicked += () =>
        {
            showJoinGameUI = true;
            VisualElement container = uiDoc.rootVisualElement.Query("Container");
            container.style.display = DisplayStyle.None;
        };
    }

    void OnGUI()
    {
        if (showCreateGameUI && gameManager.room != null) {
            GUILayout.BeginArea(new Rect(50, 50, 200, 200));
            GUILayout.BeginVertical();

            GUILayout.Label("<b>Room ID</b>: " + (gameManager.room?.id.ToString() ?? "No room"));

            GUILayout.Label($"Connected players ({gameManager.room?.connectedPlayers.Length.ToString() ?? "No room"})");
            foreach (var player in gameManager.room?.connectedPlayers)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label($"<b>{player.username}</b>");
                GUILayout.Label(player.connectionId.ToString());
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Start game")) gameManager.StartGame();
            if (GUILayout.Button("Close room")) gameManager.CloseRoom();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        if (showJoinGameUI && gameManager.room == null)
        {
            GUILayout.BeginArea(new Rect(50, 50, 200, 200));
            GUILayout.BeginHorizontal();

            GUIjoinRoomId = GUILayout.TextField(GUIjoinRoomId, 4);
            if (GUILayout.Button("Join")) gameManager.JoinRoom(GUIjoinRoomId);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        else if (showJoinGameUI && gameManager.room != null)
        {
            GUILayout.BeginArea(new Rect(50, 50, 200, 200));
            GUILayout.BeginHorizontal();

            GUILayout.Label("In room " + gameManager.room?.id);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
    }

    public void ResetUI()
    {
        showCreateGameUI = false;
        showJoinGameUI = false;

        VisualElement container = uiDoc.rootVisualElement.Query("Container");
        container.style.display = DisplayStyle.Flex;
    }
}
