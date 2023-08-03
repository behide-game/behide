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
        createGameButton.clickable.clicked += async () =>
        {
            var res = await gameManager.party.CreateRoom();
            if (res.IsFailure) return;

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
        if (showCreateGameUI && gameManager.session.room != null) {
            GUILayout.BeginArea(new Rect(50, 50, 200, 200));
            GUILayout.BeginVertical();

            GUILayout.Label("<b>Room ID</b>: " + (gameManager.session.room.id.ToString() ?? "No room"));

            GUILayout.Label($"Connected players ({gameManager.session.room.connectedPlayers.Count.ToString() ?? "No room"})");
            foreach (var player in gameManager.session.room.connectedPlayers)
            {
                GUILayout.Label($"<b>{player.Key}</b>: {player.Value}");
            }

            if (GUILayout.Button("Start game")) _ = gameManager.party.StartGame();
            if (GUILayout.Button("Close room")) _ = gameManager.party.CloseRoom();

            GUILayout.EndVertical();
            GUILayout.EndArea();
        }

        if (showJoinGameUI && gameManager.session.room == null)
        {
            GUILayout.BeginArea(new Rect(50, 50, 200, 200));
            GUILayout.BeginHorizontal();

            GUIjoinRoomId = GUILayout.TextField(GUIjoinRoomId, 4);
            if (GUILayout.Button("Join")) _ = gameManager.party.JoinRoom(GUIjoinRoomId);

            GUILayout.EndHorizontal();
            GUILayout.EndArea();
        }
        else if (showJoinGameUI && gameManager.session.room != null)
        {
            GUILayout.BeginArea(new Rect(50, 50, 200, 200));
            GUILayout.BeginHorizontal();

            GUILayout.Label("In room " + gameManager.session.room?.id);

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
