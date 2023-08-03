using UnityEngine.UIElements;

public class TextFieldUtils
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

    public static bool PlaceholderVisible(TextField textField)
    {
        string placeholderClass = TextField.ussClassName + "__placeholder";
        return textField.ClassListContains(placeholderClass);
    }
}