using Godot;

namespace Behide.Prefabs;

public partial class SettingsAwareSubViewport : SubViewport
{
    private IDisposable? disposable;

    public override void _EnterTree()
    {
        disposable = GameManager.Settings.Changed.Subscribe(_ => SettingsChanged());
        SettingsChanged();
    }

    public override void _ExitTree() => disposable?.Dispose();

    private void SettingsChanged()
    {
        Scaling3DMode = GetWindow().Scaling3DMode;
        Scaling3DScale = GetWindow().Scaling3DScale;
        Msaa3D = GetWindow().Msaa3D;
        ScreenSpaceAA = GetWindow().ScreenSpaceAA;
        UseTaa = GetWindow().UseTaa;
    }
}
