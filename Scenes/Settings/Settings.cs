using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Godot;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide.Game;

[SceneTree(root: "nodes")]
public partial class Settings : Control
{
    private readonly ILogger log = Log.CreateLogger("Settings");

    public override void _Ready()
    {
        VideoListenSettings();
        GraphicsListenSettings();

        LoadConfig();
        SaveConfigOnChanged();

        GeneralListenSettingsForSaving();
        ControlsListenSettingsForSaving();
        VideoListenSettingsForSaving();
        GraphicsListenSettingsForSaving();
    }

    public readonly Subject<Unit> Changed = new();

    /// <summary>
    /// Fire Changed when any settings changed
    /// </summary>
    private void SaveConfigOnChanged()
    {
        Changed
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(_ => CallThreadSafe(nameof(Save)));
    }

    /// <summary>
    /// Load settings saved and apply
    /// </summary>
    private void LoadConfig()
    {
        var config = new ConfigFile();
        if (config.Load("user://settings.cfg") != Error.Ok)
        {
            log.Error("Failed to load settings");
            return;
        }

        GeneralApplyFromConfig(config);
        ControlsApplyFromConfig(config);
        VideoApplyFromConfig(config);
        GraphicsApplyFromConfig(config);
    }

    /// <summary>
    /// Save the settings to the file
    /// </summary>
    private void Save()
    {
        var config = new ConfigFile();

        GeneralApplyToConfig(config);
        ControlsApplyToConfig(config);
        VideoApplyToConfig(config);
        GraphicsApplyToConfig(config);

        var err = config.Save("user://settings.cfg");
        if (err == Error.Ok) return;
        log.Error("Failed to save settings");
    }
}
