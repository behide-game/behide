using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Godot;

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

public partial class Settings
{
    public readonly Subject<Unit> Changed = new();

    /// <summary>
    /// Fire Changed when any settings changed
    /// </summary>
    private void SaveConfigOnChanged()
    {
        var change = (double _) => Changed.OnNext(Unit.Default);

        General.HorizontalSensitivity.Changed.Subscribe(change);
        General.VerticalSensitivity.Changed.Subscribe(change);
        General.FOV.Changed.Subscribe(change);
        General.Username.LineEdit.TextChanged += _ => change.Invoke(0.0);

        Changed
            .Throttle(TimeSpan.FromMilliseconds(500))
            .Subscribe(_ => CallThreadSafe(nameof(Save)));
    }

    /// <summary>
    /// Load settings saved and apply them to UI
    /// </summary>
    private void LoadConfig()
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
        General.HorizontalSensitivity.SetValue(hSensi);
        General.VerticalSensitivity.SetValue(vSensi);
        General.FOV.SetValue(fov);

        var username = (string?)config.GetValueOrDefault("User", "username");
        General.Username.LineEdit.Text = username;
    }

    /// <summary>
    /// Save the settings to the file
    /// </summary>
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
