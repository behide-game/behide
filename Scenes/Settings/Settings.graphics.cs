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

        // Shadows
        Graphics.Shadows.Enabled.Toggled += toggledOn => GetTree().CallGroup(
            Groups.ShadowingLights,
            Light3D.MethodName.SetShadow,
            toggledOn
        );
        GetTree().SceneChanged += () => GetTree().CallGroup(
            Groups.ShadowingLights,
            Light3D.MethodName.SetShadow,
            Graphics.Shadows.Enabled.ButtonPressed
        );

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
    }

    /// <summary>
    /// Trigger save when graphics settings changed
    /// </summary>
    private void GraphicsListenSettingsForSaving()
    {
        Graphics.Glow.Enabled.Toggled += _ => Changed.OnNext(Unit.Default);
        Graphics.Shadows.Enabled.Toggled += _ => Changed.OnNext(Unit.Default);
        Graphics.SSR.Enabled.Toggled += _ => Changed.OnNext(Unit.Default);
        Graphics.ChromaticAberration.Enabled.Toggled += _ => Changed.OnNext(Unit.Default);
    }

    private void GraphicsApplyFromConfig(ConfigFile config)
    {
        var glow = config.GetValue(nameof(Graphics), "glow", true).AsBool();
        var shadows = config.GetValue(nameof(Graphics), "shadows", true).AsBool();
        var ssr = config.GetValue(nameof(Graphics), "ssr", true).AsBool();
        var chromaticAberration = config.GetValue(nameof(Graphics), "chromatic-aberration", true).AsBool();

        Graphics.Glow.Enabled.SetPressed(glow);
        Graphics.Shadows.Enabled.SetPressed(shadows);
        Graphics.SSR.Enabled.SetPressed(ssr);
        Graphics.ChromaticAberration.Enabled.SetPressed(chromaticAberration);
    }

    private void GraphicsApplyToConfig(ConfigFile config)
    {
        config.SetValue(nameof(Graphics), "glow", Graphics.Glow.Enabled.ButtonPressed);
        config.SetValue(nameof(Graphics), "shadows", Graphics.Shadows.Enabled.ButtonPressed);
        config.SetValue(nameof(Graphics), "ssr", Graphics.SSR.Enabled.ButtonPressed);
        config.SetValue(nameof(Graphics), "chromatic-aberration", Graphics.ChromaticAberration.Enabled.ButtonPressed);
    }
}
