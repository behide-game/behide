using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ScenesManager : MonoBehaviour
{
    [SerializeField] public string gameSceneName = "";
    [SerializeField] public string homeSceneName = "";
    [HideInInspector] public event Action sceneChanged;

    void Awake()
    {
        SceneManager.activeSceneChanged += (_, __) => sceneChanged.Invoke();
    }

    public bool SceneIsHome() => SceneManager.GetActiveScene().name == homeSceneName;
    public bool SceneIsGame() => SceneManager.GetActiveScene().name == gameSceneName;

    private async Task InternalLoadScene(string sceneName)
    {
        if (SceneManager.GetActiveScene().name != sceneName) {
            var tcs = new TaskCompletionSource<bool>();
            SceneManager.LoadSceneAsync(sceneName).completed += _ => tcs.TrySetResult(true);

            await tcs.Task;
        }
    }

    public Task LoadHomeScene() => InternalLoadScene(homeSceneName);
    public Task LoadGameScene() => InternalLoadScene(gameSceneName);
    async public Task LoadSceneAsync(string sceneName)
    {
        if (sceneName == gameSceneName) await LoadGameScene();
        else
        if (sceneName == homeSceneName) await LoadHomeScene();
    }
}