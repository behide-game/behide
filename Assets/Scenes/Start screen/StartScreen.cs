#nullable enable

using System;
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

    private Func<VisualElement> home = null!;
    private Func<VisualElement> usernameModal = null!;

    void Awake() => gameManager = GameManager.instance;

    void Start()
    {
        home = () => uiDocument.rootVisualElement.Query<VisualElement>("Home");
        usernameModal = () => uiDocument.rootVisualElement.Query<VisualElement>("UsernameModal");
        startInputAction.ToInputAction().performed += OnStartPressed;

        // Setup username modal
        if (PlayerPrefs.HasKey("username")) return;

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

    void Update()
    {
        Label homeText = home().Query<Label>("Subtitle");
        VisualElement readyHomeText = home().Query("ReadySubtitle");

        if (gameManager.connectionsManager.connected.behide && gameManager.connectionsManager.connected.eos)
        {
            homeText.style.display = DisplayStyle.None;
            readyHomeText.style.display = DisplayStyle.Flex;
        }
        else if ((gameManager.connectionsManager.connectError ?? "") != "")
        {
            homeText.text = $"Failed to connect";
            Label errorText = home().Query<Label>("ErrorMessage");
            errorText.text = gameManager.connectionsManager.connectError;
        }
        else homeText.text = "Connecting...";
    }


    public void OnStartPressed(InputAction.CallbackContext context)
    {
        if (usernameModalOpened
            || !gameManager.connectionsManager.connected.behide
            || !gameManager.connectionsManager.connected.eos) return;

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
        gameManager.SetUsername(username);
        SceneManager.LoadSceneAsync(homeSceneName);
    }
}