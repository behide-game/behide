using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class UITests : InputTestFixture
{
    string startScreenSceneName = "start screen";
    string homePhysicalButtonKeyboardSpriteName = "Enter_Key_Dark";
    string homePhysicalButtonGamepadSpriteName = "XboxSeriesX_A";
    Keyboard keyboard;
    Mouse mouse;
    Gamepad gamepad;

    public override void Setup()
    {
        keyboard = InputSystem.AddDevice<Keyboard>();
        mouse = InputSystem.AddDevice<Mouse>();
        gamepad = InputSystem.AddDevice<Gamepad>();
        InputSystem.Update();

        SceneManager.LoadScene(startScreenSceneName);
    }

    [UnityTest]
    public IEnumerator PhysicalButtonTest()
    {
        VisualElement uiRoot = GameObject.Find("UI").GetComponent<UIDocument>().rootVisualElement;


        // Awaiting UI to be ready
        Label homeText = uiRoot.Q("Home").Q<Label>("Subtitle");
        VisualElement readyHomeText = uiRoot.Q("Home").Q("ReadySubtitle");

        Func<bool> isUiReady = () =>
            homeText.style.display == DisplayStyle.None
            && readyHomeText.style.display == DisplayStyle.Flex;

        yield return Common.waitUntilOrTimeout(20_000, isUiReady);

        Assert.IsTrue(isUiReady(), "UI should show \"Press _ to start\"");


        // Testing physical button component
        VisualElement physicalButton = uiRoot.Query("PhysicalButton");
        VisualElement physicalButtonImage = physicalButton.Query("Image");

        // Set gamepad as active device
        Press(gamepad.leftTrigger);
        yield return null;

        Assert.IsTrue(physicalButtonImage.style.backgroundImage.value.sprite.name == homePhysicalButtonGamepadSpriteName);

        // Set keyboard as active device
        Press(keyboard.aKey);
        yield return null;

        Assert.IsTrue(physicalButtonImage.style.backgroundImage.value.sprite.name == homePhysicalButtonKeyboardSpriteName);
    }
}