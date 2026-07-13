using Godot;

namespace Behide.Prefabs.Player;

public abstract partial class Gun : Node3D
{
    public abstract Control Hud { get; }
    public abstract Label PlayerUsernameLabel { get; }
    protected abstract Label AmmoLabel { get; }
    protected abstract TextureRect AmmoPicto { get; }
    protected abstract TextureRect ReloadPicto { get; }

    public abstract int DamagePerAmmo { get; }
    protected abstract int MagazineSize { get; }
    protected abstract float ReloadTime { get; }
    protected abstract float FireRate { get; }

    private int AmmoCount
    {
        get;
        set
        {
            field = value;
            AmmoLabel.Text = value + " | " + MagazineSize;
        }
    }

    private double fireCooldownTime;
    private double reloadTimeRemaining;

    private bool CanShoot => fireCooldownTime <= 0 && AmmoCount > 0;
    private bool CanReload => fireCooldownTime <= 0 && reloadTimeRemaining <= 0 && AmmoCount < MagazineSize;

    public override void _EnterTree() => AmmoCount = MagazineSize;

    public override void _Process(double delta)
    {
        if (fireCooldownTime > 0) fireCooldownTime -= delta;

        if (reloadTimeRemaining <= 0) return;
        if (reloadTimeRemaining - delta <= 0)
        {
            AmmoPicto.Show();
            ReloadPicto.Hide();
            AmmoCount = MagazineSize;
        }
        ReloadPicto.OffsetTransformRotation += (float)delta*10;
        reloadTimeRemaining -= delta;
    }

    protected abstract Node3D? ShootCore();
    protected abstract void ReloadCore();

    public Node3D? TryShoot()
    {
        if (CanShoot)
        {
            fireCooldownTime = 1 / FireRate;
            AmmoCount -= 1;
            var shootObject = ShootCore();

            return shootObject;
        }
        if (CanReload) Reload();
        return null;
    }

    public void Reload()
    {
        reloadTimeRemaining = ReloadTime;
        AmmoPicto.Hide();
        ReloadPicto.Show();
        ReloadPicto.OffsetTransformRotation = 0f;
        AmmoLabel.Text = "Reloading";
        ReloadCore();
    }
}
