using Godot;
using System.Reactive;

namespace Behide.Game;

public partial class Settings
{
    private _SceneTree.__0_TabContainer.__1_Graphics.__2_VBox Graphics => nodes.TabContainer.Graphics.VBox;

    /// <summary>
    /// Change game state when graphics settings change
    /// </summary>
    private void GraphicsListenSettings()
    {
        // Glow
        Graphics.Glow.Enabled.Toggled += toggledOn =>
        {
            var env = GetTree().Root.FindChildren("*", nameof(WorldEnvironment), true, false);
            if (env.Count <= 0) return;
            if (env[0] is not WorldEnvironment worldEnvironment) return;
            worldEnvironment.Environment.SetGlowEnabled(toggledOn);
        };

        // SSR
        Graphics.SSR.Enabled.Toggled += toggledOn =>
        {
            var env = GetTree().Root.FindChildren("*", nameof(WorldEnvironment), true, false);
            if (env.Count <= 0) return;
            if (env[0] is not WorldEnvironment worldEnvironment) return;
            worldEnvironment.Environment.SetSsrEnabled(toggledOn);
        };

        // Chromatic Aberration
        Graphics.ChromaticAberration.Enabled.Toggled += GameManager.VisualEffectsLayer.EnableChromaticAberration;

        // Shadows
        Graphics.ShadowsQuality.OptionButton.ItemSelected += selectedIdx =>
        {
            RenderingServer.PositionalSoftShadowFilterSetQuality(selectedIdx switch
            {
                1 => RenderingServer.ShadowQuality.Hard,
                2 => RenderingServer.ShadowQuality.SoftVeryLow,
                3 => RenderingServer.ShadowQuality.SoftLow,
                4 => RenderingServer.ShadowQuality.SoftMedium,
                5 => RenderingServer.ShadowQuality.SoftHigh,
                6 => RenderingServer.ShadowQuality.SoftUltra,
                _ => RenderingServer.ShadowQuality.Hard
            });
            GetTree().CallGroup(
                Groups.ShadowingLights,
                Light3D.MethodName.SetShadow,
                selectedIdx != 0
            );
        };
        GetTree().SceneChanged += () => GetTree().CallGroup(
            Groups.ShadowingLights,
            Light3D.MethodName.SetShadow,
            Graphics.ShadowsQuality.OptionButton.Selected != 0
        );
    }

    /// <summary>
    /// Trigger save when graphics settings changed
    /// </summary>
    private void GraphicsListenSettingsForSaving()
    {
        Graphics.Glow.Enabled.Toggled += _ => Changed.OnNext(Unit.Default);
        Graphics.SSR.Enabled.Toggled += _ => Changed.OnNext(Unit.Default);
        Graphics.ChromaticAberration.Enabled.Toggled += _ => Changed.OnNext(Unit.Default);
        Graphics.ShadowsQuality.OptionButton.ItemSelected += _ => Changed.OnNext(Unit.Default);
    }

    private void GraphicsApplyFromConfig(ConfigFile config)
    {
        var glow = config.GetValue(nameof(Graphics), "glow", true).AsBool();
        var ssr = config.GetValue(nameof(Graphics), "ssr", true).AsBool();
        var chromaticAberration = config.GetValue(nameof(Graphics), "chromatic-aberration", true).AsBool();
        var shadowsQuality = config.GetValue(nameof(Graphics), "shadows-quality", "soft-low").AsString() switch
        {
            "disabled" => 0,
            "hard" => 1,
            "soft-very-low" => 2,
            "soft-low" => 3,
            "soft-medium" => 4,
            "soft-high" => 5,
            "soft-ultra" => 6,
            _ => 3
        };

        Graphics.Glow.Enabled.SetPressed(glow);
        Graphics.ShadowsQuality.OptionButton.Select(shadowsQuality);
        Graphics.SSR.Enabled.SetPressed(ssr);
        Graphics.ChromaticAberration.Enabled.SetPressed(chromaticAberration);

        Graphics.Glow.Enabled.EmitSignal(BaseButton.SignalName.Toggled, glow);
        Graphics.ShadowsQuality.OptionButton.EmitSignal(OptionButton.SignalName.ItemSelected, shadowsQuality);
        Graphics.SSR.Enabled.EmitSignal(BaseButton.SignalName.Toggled, ssr);
        Graphics.ChromaticAberration.Enabled.EmitSignal(BaseButton.SignalName.Toggled, chromaticAberration);
    }

    private void GraphicsApplyToConfig(ConfigFile config)
    {
        config.SetValue(nameof(Graphics), "glow", Graphics.Glow.Enabled.ButtonPressed);
        config.SetValue(nameof(Graphics), "ssr", Graphics.SSR.Enabled.ButtonPressed);
        config.SetValue(nameof(Graphics), "chromatic-aberration", Graphics.ChromaticAberration.Enabled.ButtonPressed);
        config.SetValue(nameof(Graphics), "shadows-quality", Graphics.ShadowsQuality.OptionButton.Selected switch
        {
            0 => "disabled",
            1 => "hard",
            2 => "soft-very-low",
            3 => "soft-low",
            4 => "soft-medium",
            5 => "soft-high",
            6 => "soft-ultra",
            _ => "soft-low"
        });
    }
}
