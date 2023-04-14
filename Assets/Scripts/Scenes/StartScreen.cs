using System;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private InputActionReference inputAction;
    [SerializeField] private string homeSceneName;

    private bool usernameModalOpened = false;

    private Func<VisualElement> home;
    private Func<VisualElement> usernameModal;

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
        var validateButton = usernameModal().Q<VisualElement>("ValidateArrow").ElementAt(0) as Button;
        validateButton.clickable.clicked += () => SubmitUsername(textField.text);
    }

    void Update()
    {
        Label homeText = home().Query<Label>("Subtitle");
        string anyKey = inputAction.action.GetBindingDisplayString().ToLowerInvariant();

        if (gameManager.connected) homeText.text = $"Press {anyKey} to start";
        else if ((gameManager.connectError ?? "") != "")
        {
            homeText.text = $"Failed to connect:\n{gameManager.connectError}";
        }
        else homeText.text = "Connecting...";
    }


    public void OnAnyKeyPressed(InputAction.CallbackContext context)
    {
        if (usernameModalOpened
            || !gameManager.connected
            || gameManager.playerRegistered
            || context.phase != InputActionPhase.Performed) return;

        if (PlayerPrefs.HasKey("username"))
        {
            string username = PlayerPrefs.GetString("username");
            gameManager.RegisterPlayer(username);
            SwitchScene();
            return;
        }

        // Show username modal
        home().AddToClassList("hide");
        usernameModal().RemoveFromClassList("hide");
        usernameModalOpened = true;
    }

    private void SubmitUsername(string username)
    {
        // Save username
        PlayerPrefs.SetString("username", username);
        PlayerPrefs.Save();

        gameManager.RegisterPlayer(username);
        SwitchScene();
    }

    private void SwitchScene()
    {
        SceneManager.LoadSceneAsync(homeSceneName);
    }
}