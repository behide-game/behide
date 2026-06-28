using Godot;

namespace Behide.Game;

[SceneTree]
public partial class VisualEffectsLayer : CanvasLayer
{
    public void EnableFpsDisplay(bool enable) => _.FPS.Label.SetVisible(enable);
    public void EnableChromaticAberration(bool enable) => _.ChromaticAberration.SetVisible(enable);

    public override void _Process(double delta) => _.FPS.Label.Text = Math.Round(1 / delta).ToString("0");
}
