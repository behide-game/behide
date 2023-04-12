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
    }

    void Update()
    {
        Label homeText = home().Query<Label>("Subtitle");
        string anyKey = inputAction.action.GetBindingDisplayString().ToLowerInvariant();

        if (gameManager.connected) {
            homeText.text = $"Press {anyKey} to start";
        }
        else if (gameManager.connectError != null && gameManager.connectError != "")
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
        }
        else
        {
            // Show modal
            home().AddToClassList("hide");
            usernameModal().RemoveFromClassList("hide");
            usernameModalOpened = true;

            // Set placeholder
            TextField textField = usernameModal().Q<TextField>("UsernameTextField");
            string fakeUsername = FakeUsername.getRandom();
            TextFieldUtils.SetPlaceholderText(textField, fakeUsername + "...");

            // Handle validate
            VisualElement validateButton = usernameModal().Q<VisualElement>("ValidateArrow");
            validateButton.RegisterCallback<ClickEvent>(_ => { SaveUsername(textField.text); SwitchScene(); });
        }
    }

    private void SaveUsername(string username)
    {
        PlayerPrefs.SetString("username", username);
        PlayerPrefs.Save();
    }

    private void SwitchScene() {
        SceneManager.LoadSceneAsync(homeSceneName);
    }
}