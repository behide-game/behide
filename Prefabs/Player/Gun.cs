using Godot;

namespace Behide.Prefabs.Player;

public abstract partial class Gun : Node3D
{
    public abstract Control Hud { get; }
    public abstract Label PlayerUsernameLabel { get; }
    protected abstract Label AmmoLabel { get; }

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
            AmmoLabel.Text = value + " / " + TotalAmmoCount;
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
        }
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
        AmmoLabel.Text = "Reloading";
        ReloadCore();
    }
}
