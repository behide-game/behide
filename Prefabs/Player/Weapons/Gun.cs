using Godot;

namespace Behide.Prefabs.Weapons;

public abstract partial class Gun : Node3D
{
    public abstract Control Hud { get; }
    public abstract Label PlayerUsernameLabel { get; }
    protected abstract Label AmmoLabel { get; }
    protected abstract TextureRect AmmoIcon { get; }
    protected abstract TextureRect ReloadIcon { get; }

    public abstract int DamagePerAmmo { get; }
    protected abstract int TotalAmmoCount { get; set; }
    protected abstract int MagazineSize { get; }
    protected abstract float ReloadTime { get; }
    protected abstract float FireRate { get; }

    private int AmmoCount
    {
        get;
        set
        {
            field = value;
            AmmoLabel.Text = value + " | " + TotalAmmoCount;
        }
    }

    private double fireCooldownTime;
    private double reloadTimeRemaining;

    private bool CanShoot => fireCooldownTime <= 0 && AmmoCount > 0;
    private bool CanReload => fireCooldownTime <= 0 && reloadTimeRemaining <= 0 && AmmoCount < MagazineSize && TotalAmmoCount > 0;

    public override void _EnterTree() => AmmoCount = MagazineSize;

    public override void _Process(double delta)
    {
        if (fireCooldownTime > 0) fireCooldownTime -= delta;

        if (reloadTimeRemaining <= 0) return;
        if (reloadTimeRemaining - delta <= 0)
        {
            var ammoDiff = MagazineSize - AmmoCount;
            var realAmmoAmountAdded = int.Min(ammoDiff, TotalAmmoCount);
            TotalAmmoCount -= realAmmoAmountAdded;
            AmmoCount += realAmmoAmountAdded;

            AmmoIcon.Show();
            ReloadIcon.Hide();
        }
        ReloadIcon.OffsetTransformRotation += (float)delta*10;
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

    public virtual void SecondaryShoot() {}

    public void Reload()
    {
        if (!CanReload) return;

        reloadTimeRemaining = ReloadTime;
        AmmoIcon.Hide();
        ReloadIcon.Show();
        ReloadIcon.OffsetTransformRotation = 0f;
        AmmoLabel.Text = "Reloading";
        ReloadCore();
    }
}
