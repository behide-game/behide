using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class LaunchGameTests : InputTestFixture
{
    GameManager gameManager;

    public override void Setup()
    {
        SceneManager.LoadScene("start screen", LoadSceneMode.Additive);
    }

    public override void TearDown()
    {
        SceneManager.UnloadSceneAsync("start screen");
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