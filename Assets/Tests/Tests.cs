using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

public class Tests
{
    GameManager gameManager;
    string startScreenSceneName = "start screen";

    [SetUp]
    public async void SetupScene()
    {
        TaskCompletionSource<bool> tcs = new ();
        SceneManager.LoadSceneAsync(startScreenSceneName).completed += _ => tcs.SetResult(true);
        await tcs.Task;

        gameManager = GameObject.Find("GameManager").GetComponent<GameManager>();
    }

    [UnityTest]
    public IEnumerator CheckingConnection()
    {
        yield return new WaitForSeconds(5f);

        Assert.IsTrue(gameManager.connectionManager.connected.behide, "Should be connected to behide's server");
        Assert.IsTrue(gameManager.connectionManager.connected.eos,    "Should be connected to EOS");
    }
}
