using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class Tests
{
    GameManager gameManager;
    string startScreenSceneName = "start screen";

    [OneTimeSetUp]
    public void LoadScene()
    {
        SceneManager.LoadScene(startScreenSceneName);
    }

    [UnityTest]
    public IEnumerator CheckingConnection()
    {
        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();

        yield return Common.waitUntilOrTimeout(20_000, () =>
            gameManager.connectionsManager.connected.behide
            && gameManager.connectionsManager.connected.eos
        );

        Assert.IsTrue(gameManager.connectionsManager.connected.behide, "Should be connected to behide's server");
        Assert.IsTrue(gameManager.connectionsManager.connected.eos, "Should be connected to EOS");
    }
}