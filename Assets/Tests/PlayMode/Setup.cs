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
    static string[] scenesToEdit = new string[]
    {
        "Assets/Scenes/Start screen/Start screen.unity",
        "Assets/Tests/PlayMode/Scene/Tests scene.unity"
    };

    public void Setup()
    {
#if UNITY_EDITOR
        PlayerPrefs.SetString("username", "Test username");

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

    static public void Cleanup()
    {
#if UNITY_EDITOR
        System.Threading.Tasks.Task.Delay(5000).Wait();
        var initialScenePath = EditorSceneManager.GetActiveScene().path;

        PlayerPrefs.DeleteKey("username");
        foreach (var scenePath in scenesToEdit)
        {
            var scene = EditorSceneManager.OpenScene(scenePath);

            try
            {
                GameObject.Find("InputManager")
                    .GetComponent<InputSystemUIInputModule>()
                    .enabled = true;
            }
            catch (Exception) { }

            EditorSceneManager.SaveScene(scene);
        }

        EditorSceneManager.OpenScene(initialScenePath);
#endif
    }

    [UnityTest]
    public IEnumerator Test()
    {
        yield return null;
    }
}