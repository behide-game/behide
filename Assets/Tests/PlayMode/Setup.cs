using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem.UI;
using UnityEngine.TestTools;
#if UNITY_EDITOR
using UnityEditor.SceneManagement;
#endif

public class SetupClass : IPrebuildSetup
{
    string[] scenesToEdit = new string[]
    {
        "Assets/Scenes/Start screen/Start screen.unity",
        "Assets/Tests/PlayMode/Scene/Tests scene.unity"
    };

    public void Setup()
    {
#if UNITY_EDITOR
        foreach (var scenePath in scenesToEdit)
        {
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);

            try
            {
                GameObject.Find("InputManager")
                    .GetComponent<InputSystemUIInputModule>()
                    .enabled = false;
            }
            catch (Exception) { }

            EditorSceneManager.SaveScene(scene);
            EditorSceneManager.CloseScene(scene, true);
        }
#endif
    }

    [UnityTest]
    public IEnumerator Test()
    {
        yield return null;
    }
}