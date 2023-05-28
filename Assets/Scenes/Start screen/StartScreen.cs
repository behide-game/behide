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
    [SerializeField] private InputActionReference inputAction = null!;
    [SerializeField] private string homeSceneName = "";

    private bool usernameModalOpened = false;

    private Func<VisualElement> home = null!;
    private Func<VisualElement> usernameModal = null!;

    void Awake() => gameManager = GameManager.instance;

    void Start()
    {
        home = () => uiDocument.rootVisualElement.Query<VisualElement>("Home");
        usernameModal = () => uiDocument.rootVisualElement.Query<VisualElement>("UsernameModal");
        inputAction.ToInputAction().performed += OnAnyKeyPressed;

        // Setup username modal
        if (PlayerPrefs.HasKey("username")) return;

        // Set placeholder
        var textField = usernameModal().Q<TextField>("UsernameTextField");
        var fakeUsername = FakeUsername.getRandom();
        TextFieldUtils.SetPlaceholderText(textField, fakeUsername + "...");

        // Handle validate
        var validateButton = (Button)usernameModal().Q("ValidateArrow").ElementAt(0);
        validateButton.clickable.clicked += () => SubmitUsername(textField.text);
    }

    void Update()
    {
        Label homeText = home().Query<Label>("Subtitle");
        string key = inputAction.action.GetBindingDisplayString().ToLowerInvariant();

        if (gameManager.connections.connected.behide) homeText.text = $"Press {key} to start";
        else if ((gameManager.connections.connectError ?? "") != "")
        {
            homeText.text = $"Failed to connect";
            Label errorText = home().Query<Label>("ErrorMessage");
            errorText.text = gameManager.connections.connectError;
        }
        else homeText.text = "Connecting...";
    }


    public void OnAnyKeyPressed(InputAction.CallbackContext context)
    {
        if (usernameModalOpened
            || !gameManager.connections.connected.behide
            || context.phase != InputActionPhase.Performed) return;

        if (PlayerPrefs.HasKey("username"))
        {
            SwitchScene();
            return;
        }

        // Show username modal
        home().AddToClassList("hide");
        usernameModal().RemoveFromClassList("hide");
        usernameModalOpened = true;
    }

    private void SubmitUsername(string _username)
    {
        var textField = usernameModal().Q<TextField>("UsernameTextField");
        var username =
            TextFieldUtils.PlaceholderVisible(textField)
            ? _username.Substring(0, _username.Length - 3)
            : _username;

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