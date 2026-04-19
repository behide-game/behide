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
    public string? GetUsername()
    {
        var lineEditText = nodes.Username.LineEdit.Text;
        return string.IsNullOrWhiteSpace(lineEditText)
            ? null
            : lineEditText;
    }
    public double HorizontalSensitivity => nodes.HorizontalSensitivity.Value;
    public double VerticalSensitivity => nodes.VerticalSensitivity.Value;
    public double Fov => nodes.FOV.Value;

    private readonly ILogger log = Log.CreateLogger("Settings");
    public readonly Subject<Unit> Changed = new();

    public override void _EnterTree()
    {
        ApplyConfig();

        var change = (double _) => Changed.OnNext(Unit.Default);

        nodes.HorizontalSensitivity.Changed.Subscribe(change);
        nodes.VerticalSensitivity.Changed.Subscribe(change);
        nodes.FOV.Changed.Subscribe(change);
        nodes.Username.LineEdit.TextChanged += _ => change.Invoke(0.0);

        Changed.Throttle(TimeSpan.FromSeconds(2)).Subscribe(_ => CallThreadSafe(nameof(Save)));
    }

    private void ApplyConfig()
    {
        var config = new ConfigFile();
        if (config.Load("user://settings.cfg") != Error.Ok)
        {
            log.Error("Failed to load settings");
            return;
        }

        var hSensi = (double)config.GetValue("Controls", "horizontal_sensitivity", 1);
        var vSensi = (double)config.GetValue("Controls", "vertical_sensitivity", 1);
        var fov = (double)config.GetValue("Controls", "fov", 90);
        nodes.HorizontalSensitivity.SetValue(hSensi);
        nodes.VerticalSensitivity.SetValue(vSensi);
        nodes.FOV.SetValue(fov);

        var username = (string?)config.GetValueOrDefault("User", "username");
        nodes.Username.LineEdit.Text = username;
    }

    private void Save()
    {
        var config = new ConfigFile();
        config.SetValue("Controls", "horizontal_sensitivity", HorizontalSensitivity);
        config.SetValue("Controls", "vertical_sensitivity", VerticalSensitivity);
        config.SetValue("Controls", "fov", Fov);
        config.SetValue("User", "username", GetUsername() ?? string.Empty);

        var err = config.Save("user://settings.cfg");
        if (err == Error.Ok) return;
        log.Error("Failed to save settings");
    }
}
