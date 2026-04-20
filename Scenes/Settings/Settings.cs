using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Godot;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide.Game;

internal static class ConfigFileExtensions
{
    extension(ConfigFile config)
    {
        public Variant GetValueOrDefault(string section, string key)
        {
            try
            {
                return config.GetValue(section, key);
            }
            catch (Exception)
            {
                return default;
            }
        }
    }
}

[SceneTree(root: "nodes")]
public partial class Settings : VBoxContainer
{
    public double HorizontalSensitivity => nodes.HorizontalSensitivity.Value;
    public double VerticalSensitivity => nodes.VerticalSensitivity.Value;
    public string? GetUsername()
    {
        var lineEditText = nodes.Username.LineEdit.Text;
        return string.IsNullOrWhiteSpace(lineEditText)
            ? null
            : lineEditText;
    }

    private readonly ILogger log = Log.CreateLogger("Settings");
    private readonly Subject<Unit> changed = new();

    public override void _EnterTree()
    {
        ApplyConfig();
        nodes.HorizontalSensitivity.Changed.Subscribe(_ => changed.OnNext(Unit.Default));
        nodes.VerticalSensitivity.Changed.Subscribe(_ => changed.OnNext(Unit.Default));
        nodes.Username.LineEdit.TextChanged += _ => changed.OnNext(Unit.Default);

        changed.Throttle(TimeSpan.FromSeconds(2)).Subscribe(_ => CallThreadSafe(nameof(Save)));
    }

    private void ApplyConfig()
    {
        var config = new ConfigFile();
        if (config.Load("user://settings.cfg") != Error.Ok)
        {
            log.Error("Failed to load settings");
            return;
        }

        var hSensi = config.GetValueOrDefault("Controls", "horizontal_sensitivity");
        var vSensi = config.GetValueOrDefault("Controls", "vertical_sensitivity");
        nodes.HorizontalSensitivity.SetValue((double)hSensi);
        nodes.VerticalSensitivity.SetValue((double)vSensi);

        var username = (string)config.GetValueOrDefault("User", "username");
        nodes.Username.LineEdit.Text = username;
    }

    private void Save()
    {
        var config = new ConfigFile();
        config.SetValue("Controls", "horizontal_sensitivity", HorizontalSensitivity);
        config.SetValue("Controls", "vertical_sensitivity", VerticalSensitivity);
        if (GetUsername() is { } username) config.SetValue("User", "username", username);

        var err = config.Save("user://settings.cfg");
        if (err == Error.Ok) return;
        log.Error("Failed to save settings");
    }
}
