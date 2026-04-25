using Behide.Game.Player;
using Godot;

namespace Behide.Prefabs.Player;

public abstract partial class Gun : Node3D
{
    protected HunterBody Hunter = null!;
    public Control Hud = null!;
    public Label PlayerUsername = null!;
    protected Control CrosshairHit = null!;
    protected Label AmmoLabel = null!;

    protected abstract int magazineSize { get; }
    protected abstract float reloadTime { get; }
    protected abstract float fireRate { get; }

    protected int ammoCount
    {
        get;
        set
        {
            field = value;
            AmmoLabel.Text = value + " / " + totalAmmoCount;
        }
    }
    protected int totalAmmoCount;
    protected internal int damagePerAmmo;
    protected double crosshairHitDuration;
    protected Tween? crosshairHitTween;

    private bool canShoot => timerFire <= 0 && ammoCount > 0;
    private bool canReload => timerReload <= 0 && ammoCount < magazineSize && totalAmmoCount > 0;

    private double timerFire;
    private double timerReload;

    public abstract void InitializeNodes(HunterBody hunter);
    public abstract void InitializeProperties();

    public void TickGun(double delta)
    {
        if (timerFire > 0) timerFire -= delta;

        if (timerReload <= 0) return;
        if (timerReload - delta <= 0)
        {
            var ammoDiff = magazineSize - ammoCount;
            var realAmmoAmountAdded = int.Min(ammoDiff, totalAmmoCount);
            totalAmmoCount -= realAmmoAmountAdded;
            ammoCount += realAmmoAmountAdded;
        }
        timerReload -= delta;
    }

    public NodePath? TryShoot()
    {
        if (canShoot) return Shoot();
        if (timerFire <= 0 && timerReload <= 0 && ammoCount == 0)
            TryReload();
        return null;
    }

    protected virtual NodePath? Shoot()
    {
        timerFire = 1 / fireRate;
        ammoCount -= 1;
        // TODO: Animations
        return null;
    }

    public void TryReload()
    {
        if (!canReload) return;
        Reload();
    }

    protected virtual void Reload()
    {
        timerReload = reloadTime;
        AmmoLabel.Text = "Reloading";
        // TODO: Animations
    }
}
