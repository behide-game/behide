#nullable enable
using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    private GameManager gameManager = null!;
    [SerializeField] private UIDocument uiDocument = null!;
    [SerializeField] private InputActionReference startInputAction = null!;
    [SerializeField] private InputActionReference validateInputAction = null!;
    [SerializeField] private string homeSceneName = "";

    private bool usernameModalOpened = false;

    private VisualElement home() => uiDocument.rootVisualElement.Query<VisualElement>("Home");
    private VisualElement usernameModal() => uiDocument.rootVisualElement.Query<VisualElement>("UsernameModal");

    void Awake() => gameManager = GameManager.instance;

    async void Start()
    {
        startInputAction.ToInputAction().performed += OnStartPressed;

        // Setup username modal
        if (!PlayerPrefs.HasKey("username")) {
            // Set placeholder
            var textField = usernameModal().Q<TextField>("UsernameTextField");
            var fakeUsername = FakeUsername.getRandom();
            TextFieldUtils.SetPlaceholderText(textField, fakeUsername + "...");

            // Handle validate
            validateInputAction.ToInputAction().performed += _ =>
            {
                if (startInputAction.ToInputAction().triggered) return;
                SubmitUsername(textField.text);
            };
        }

        // Default UI
        var homeText = home().Query<Label>("Subtitle").First();
        var readyHomeText = home().Query("ReadySubtitle").First();

        homeText.text = "Connecting...";

        await Task.Run(() =>
        {
            while (true)
            {
                if (gameManager.network.connected.behide != null && gameManager.network.connected.eos != null)
                {
                    break;
                }
            }
        });
        // await UniTask.WaitUntil(() => gameManager.network.connected.behide != null);
        // await UniTask.WaitUntil(() => gameManager.network.connected.eos != null);

        if (gameManager.network.connected.behide!.Success && gameManager.network.connected.eos!.Success)
        {
            homeText.style.display = DisplayStyle.None;
            readyHomeText.style.display = DisplayStyle.Flex;
        }
        else homeText.text = "Failed to connect";
    }

    // void Update()
    // {
    //     Label homeText = home().Query<Label>("Subtitle");
    //     VisualElement readyHomeText = home().Query("ReadySubtitle");

    //     if (gameManager.connectionsManager.connected.behide && gameManager.connectionsManager.connected.eos)
    //     {
    //         homeText.style.display = DisplayStyle.None;
    //         readyHomeText.style.display = DisplayStyle.Flex;
    //     }
    //     else if ((gameManager.connectionsManager.connectError ?? "") != "")
    //     {
    //         homeText.text = $"Failed to connect";
    //         Label errorText = home().Query<Label>("ErrorMessage");
    //         errorText.text = gameManager.connectionsManager.connectError;
    //     }
    //     else homeText.text = "Connecting...";
    // }


    public void OnStartPressed(InputAction.CallbackContext context)
    {
        if (usernameModalOpened
            || !gameManager.network.behideConnected()
            || !gameManager.network.eosConnected()) return;

        startInputAction.ToInputAction().Disable();

        if (PlayerPrefs.HasKey("username"))
        {
            SwitchScene();
            return;
        }

        // Show username modal
        home().AddToClassList("hide");
        usernameModal().style.display = DisplayStyle.Flex;
        usernameModal().RemoveFromClassList("hide");
        usernameModalOpened = true;
    }

    private void SubmitUsername(string _username)
    {
        if (!usernameModalOpened) return;

        var textField = usernameModal().Q<TextField>("UsernameTextField");
        var username =
            TextFieldUtils.PlaceholderVisible(textField)
            ? _username.Substring(0, _username.Length - 3)
            : _username;

        if (username == String.Empty) return;

        // Save username
        PlayerPrefs.SetString("username", username);
        PlayerPrefs.Save();

        SwitchScene();
    }

    private void SwitchScene()
    {
        string username = PlayerPrefs.GetString("username");
        gameManager.session.SetUsername(username);
        SceneManager.LoadSceneAsync(homeSceneName);
    }
}