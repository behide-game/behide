using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

public class PhysicalButtonTests : InputTestFixture
{
    Keyboard keyboard;
    Gamepad gamepad;
    string keyboardSpriteName = "Mouse_Left_Key_Dark";
    string gamepadSpriteName = "XboxSeriesX_RT";

    public override void Setup()
    {
        SceneManager.LoadScene("Tests scene", LoadSceneMode.Additive);
        keyboard = InputSystem.AddDevice<Keyboard>();
        gamepad = InputSystem.AddDevice<Gamepad>();
    }

    public override void TearDown()
    {
        SceneManager.UnloadSceneAsync("Tests scene");
    }

    [UnityTest]
    public IEnumerator ChangeDeviceTest()
    {
        VisualElement uiRoot = GameObject.Find("UI").GetComponent<UIDocument>().rootVisualElement;

        Func<VisualElement> physicalButton = () => uiRoot.Query("PhysicalButton");
        Func<VisualElement> physicalButtonImage = () => physicalButton().Query("Image");
        Func<string> imageToCheck = () => physicalButtonImage().style.backgroundImage.value.sprite.name;

        var tests = new InputDevice[] { keyboard, gamepad, gamepad, keyboard, gamepad, keyboard, keyboard };
        var i = 0;

        foreach (var device in tests) {
            Debug.Log($"Iteration {i}");

            var key = device is Keyboard ? (device as Keyboard).aKey : (device as Gamepad).aButton;
            var sprite = device is Keyboard ? keyboardSpriteName : gamepadSpriteName;

            PressAndRelease(key);
            InputSystem.Update();
            yield return null; // Not necessary. It allows to see that device changed when debugging.

            Assert.AreEqual(sprite, imageToCheck());
            i++;
        }
    }
}