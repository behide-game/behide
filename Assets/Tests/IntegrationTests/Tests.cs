using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

class TestsFixture
{
    GameManager gameManager;
    string startScreenSceneName = "start screen";

    [OneTimeSetUp]
    public void OneTimeSetup()
    {
        SceneManager.LoadScene(startScreenSceneName);
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