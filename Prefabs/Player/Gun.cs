using Behide.Game;
using Behide.Game.Player;
using Behide.Game.Supervisors;
using Godot;

namespace Behide.Prefabs.Player;

public abstract partial class Gun : Node3D
{
    protected HunterBody Hunter = null!;
    public Control Hud = null!;
    protected RayCast3D Raycast = null!;
    protected Label PlayerUsername = null!;
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

    private bool canShoot => timerFire <= 0 && ammoCount > 0;
    private bool canReload => timerReload <= 0 && ammoCount < magazineSize && totalAmmoCount > 0;

    private GodotObject? focusedObject;
    private Tween? crosshairHitTween;
    private double timerFire;
    private double timerReload;

    public abstract void InitializeNodes(HunterBody hunter);
    public abstract void InitializeProperties();

    public override void _Process(double delta)
    {
        focusedObject = Raycast.GetCollider();

        // Set player name in HUD
        if (focusedObject is HunterBody hunterBody)
        {
            var owner = Hunter.Supervisor.GetBodyPlayer(hunterBody);
            PlayerUsername.Text = owner?.Username;
        }
        else
            PlayerUsername.Text = string.Empty;

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
        switch (focusedObject)
        {
            case PropBody player:
                crosshairHitTween?.Kill();
                crosshairHitTween = CreateTween();
                crosshairHitTween.SetEase(Tween.EaseType.In);
                crosshairHitTween.TweenProperty(CrosshairHit, "modulate", new Color(0xFFFFFF00), crosshairHitDuration);
                CrosshairHit.Modulate = new Color(0xFFFFFFFF);
                return player.GetPath();
            case BehideObject behideObject:
                return behideObject.GetPath();
            default:
                return null;
        }
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
        // + Animations
    }
}
