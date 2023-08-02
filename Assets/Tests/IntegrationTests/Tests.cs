using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.InputSystem.UI;
using UnityEditor.SceneManagement;

class TestsFixture : IPrebuildSetup
{
    GameManager gameManager;
    string startScreenSceneName = "start screen";
    string startScreenScenePath = "Assets/Scenes/Start screen/Start screen.unity";
    string initialSceneName;

    public void Setup()
    {
        var initialScenePath = EditorSceneManager.GetActiveScene().path;
        var scene = EditorSceneManager.GetSceneByPath(startScreenScenePath);

        EditorSceneManager.OpenScene(startScreenScenePath);
        GameObject.Find("InputManager").GetComponent<InputSystemUIInputModule>().enabled = false;
        EditorSceneManager.SaveOpenScenes();

        EditorSceneManager.OpenScene(initialScenePath);
    }


    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        initialSceneName = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(startScreenSceneName);
    }

    [OneTimeTearDown]
    public void OneTimeTearDown()
    {
        SceneManager.LoadScene(initialSceneName);
    }

    [UnityTest]
    public IEnumerator CheckingConnection()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        yield return Common.waitUntilOrTimeout(25_000, () =>
            gameManager.network.behideConnected()
            && gameManager.network.eosConnected()
        );

        Assert.IsTrue(gameManager.network.behideConnected(), "Should be connected to behide's server");
        Assert.IsTrue(gameManager.network.eosConnected(), "Should be connected to EOS");
        Assert.IsTrue(gameManager.network.epicId.Success, "There should be an epicId");
        Assert.IsTrue(gameManager.network.epicId.Value != null, "There should be an epicId");
    }
}