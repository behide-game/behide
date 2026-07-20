using Godot;

namespace Behide.Prefabs;

public partial class SettingsAwareCamera3D : Camera3D
{
    private IDisposable? disposable;

    public override void _EnterTree()
    {
        disposable = GameManager.Settings.Changed.Subscribe(_ => SettingsChanged());
        SettingsChanged();
    }

    public override void _ExitTree() => disposable?.Dispose();

    private void SettingsChanged() =>
        Fov = (float)GameManager.Settings.Fov;
}
