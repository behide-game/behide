using Godot;

namespace Behide.Game;

public partial class Settings
{
    private _SceneTree.__0_TabContainer.__1_Graphics.__2_VBox Graphics => nodes.TabContainer.Graphics.VBox;

    private void ListenGraphicsSettings()
    {
        // Shadows
        Graphics.Shadows.Enabled.Toggled += toggledOn =>
        {
            GetTree().CallGroup(
                Groups.ShadowingLights,
                Light3D.MethodName.SetShadow,
                toggledOn
            );
        };

        // SSR
        Graphics.SSR.Enabled.Toggled += toggledOn =>
        {
            var env = GetTree().Root.FindChildren("*", nameof(WorldEnvironment), true, false);
            if (env.Count <= 0) return;
            if (env[0] is not WorldEnvironment worldEnvironment) return;
            worldEnvironment.Environment.SetSsrEnabled(toggledOn);
        };

        // Blow
        Graphics.Glow.Enabled.Toggled += toggledOn =>
        {
            var env = GetTree().Root.FindChildren("*", nameof(WorldEnvironment), true, false);
            if (env.Count <= 0) return;
            if (env[0] is not WorldEnvironment worldEnvironment) return;
            worldEnvironment.Environment.SetGlowEnabled(toggledOn);
        };
    }
}
