using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

class CustomTextField
{
    public static void SetPlaceholderText(TextField textField, string placeholder)
    {
        string placeholderClass = TextField.ussClassName + "__placeholder";

        onFocusOut();
        textField.RegisterCallback<FocusInEvent>(evt => onFocusIn());
        textField.RegisterCallback<FocusOutEvent>(evt => onFocusOut());

        void onFocusIn()
        {
            if (textField.ClassListContains(placeholderClass))
            {
                textField.value = string.Empty;
                textField.RemoveFromClassList(placeholderClass);
            }
        }

        void onFocusOut()
        {
            if (string.IsNullOrEmpty(textField.text))
            {
                textField.SetValueWithoutNotify(placeholder);
                textField.AddToClassList(placeholderClass);
            }
        }
    }
}

public class HomeScreen : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private InputActionReference inputAction;

    private bool usernameModalOpened = false;

    private VisualElement home;
    private VisualElement usernameModal;

    void Update()
    {
        VisualElement home = uiDocument.rootVisualElement.Query<VisualElement>("Home");
        Label homeText = home.Query<Label>("Text");

        if (gameManager.connected) homeText.text = $"Press {inputAction.action.GetBindingDisplayString().ToLowerInvariant()} to start";
        else homeText.text = "Connecting...";
    }


    public void OnStart(InputAction.CallbackContext context)
    {
        if (usernameModalOpened
            || !gameManager.connected
            || gameManager.playerRegistered
            || context.phase != InputActionPhase.Performed) return;

        VisualElement home = uiDocument.rootVisualElement.Query<VisualElement>("Home");
        VisualElement usernameModal = uiDocument.rootVisualElement.Query<VisualElement>("UsernameModal");

        if (PlayerPrefs.HasKey("username"))
        {
            string username = PlayerPrefs.GetString("username");
            gameManager.RegisterPlayer(username);
        }
        else
        {
            // Show modal
            home.AddToClassList("hide");
            usernameModal.RemoveFromClassList("hide");
            usernameModalOpened = true;

            // Set placeholder
            TextField textField = usernameModal.Q<TextField>("UsernameTextField");
            string fakeUsername = FakeUsername.getRandom();
            CustomTextField.SetPlaceholderText(textField, fakeUsername + "...");

            // Handle validate
            Button validateButton = usernameModal.Q<Button>("Button");
            validateButton.RegisterCallback<ClickEvent>(_ => SaveUsername(textField.text));
        }
    }

    void SaveUsername(string username)
    {
        VisualElement home = uiDocument.rootVisualElement.Query<VisualElement>("Home");
        VisualElement usernameModal = uiDocument.rootVisualElement.Query<VisualElement>("UsernameModal");

        // Hide modal
        home.RemoveFromClassList("hide");
        usernameModal.AddToClassList("hide");
        usernameModalOpened = false;

        PlayerPrefs.SetString("username", username);
        PlayerPrefs.Save();
    }
}