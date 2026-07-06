using Godot;

namespace Behide.Game;

[SceneTree]
public partial class VisualEffectsLayer : CanvasLayer
{
    public void EnableFpsDisplay(bool enable) => _.FPS.Label.SetVisible(enable);
    public void EnableChromaticAberration(bool enable) => _.ChromaticAberration.SetVisible(enable);

    private const int deltaCount = 60;
    private double[] deltas = new double[deltaCount];
    private int deltaIdx;
    public override void _Process(double delta)
    {
        deltas[deltaIdx++] = delta;
        deltaIdx %= deltas.Length;
    }

    public override void _PhysicsProcess(double delta) =>
        _.FPS.Label.Text = Math.Round(deltaCount / deltas.Sum()).ToString("0");
}
