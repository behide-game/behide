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
            gameManager.network.behideConnected()
            && gameManager.network.eosConnected()
        );

        Assert.IsTrue(gameManager.network.behideConnected(), "Should be connected to behide's server");
        Assert.IsTrue(gameManager.network.eosConnected(), "Should be connected to EOS");
    }
}