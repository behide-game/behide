using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

[InitializeOnLoad]
public static class TestRunner
{
    static TestRunner()
    {
        EditorApplication.playModeStateChanged += Cleanup;
    }

    private static void Cleanup(PlayModeStateChange stateChange) {
        if (stateChange != PlayModeStateChange.EnteredEditMode) return;
        SetupClass.Cleanup();
    }

    [MenuItem("Test/Run tests (play mode)")]
    public static void RunPlaymodeTests()
    {
        var api = TestRunnerApi.CreateInstance<TestRunnerApi>();
        var filters = new Filter() { testMode = TestMode.PlayMode };
        var settings = new ExecutionSettings(filters);

        api.Execute(settings);
    }
}