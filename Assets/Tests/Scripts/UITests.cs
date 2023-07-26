using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class UITests : InputTestFixture
{
    Keyboard keyboard;
    string keyboardSpriteName = "Mouse_Left_Key_Dark";

    public override void Setup()
    {
        keyboard = InputSystem.AddDevice<Keyboard>();
        SceneManager.LoadScene("Tests scene");
    }

    [UnityTest]
    public IEnumerator PhysicalButtonTest()
    {
        VisualElement uiRoot = GameObject.Find("UI").GetComponent<UIDocument>().rootVisualElement;

        Func<VisualElement> physicalButton = () => uiRoot.Query("PhysicalButton");
        Func<VisualElement> physicalButtonImage = () => physicalButton().Query("Image");
        Func<string> imageToCheck = () => physicalButtonImage().style.backgroundImage.value.sprite.name;

        // Test keyboard
        PressAndRelease(keyboard.aKey);
        InputSystem.Update();
        yield return null;
        Assert.AreEqual(keyboardSpriteName, imageToCheck());
    }
}