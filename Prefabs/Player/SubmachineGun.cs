using Behide.Game;
using Behide.Game.Player;
using Godot;

namespace Behide.Prefabs.Player;

[SceneTree]
public partial class SubmachineGun : Gun
{
    [Export] private AudioStream shootSound = null!;
    [Export] private AudioStream reloadSound = null!;
    private RayCast3D Raycast = null!;
    protected override int magazineSize => 45;
    protected override float fireRate => 10f;
    protected override float reloadTime => 2.5f;

    public override void InitializeNodes(HunterBody hunter)
    {
        Hunter = hunter;
        Hud = _.Hud;
        Raycast = _.RayCast;
        PlayerUsername = _.Hud.Center.PlayerUsername;
        CrosshairHit = _.Hud.Center.Crosshair.CrosshairHit;
        AmmoLabel = _.Hud.RBottom.Ammo;
    }

    public override void InitializeProperties()
    {
        damagePerAmmo = 2;
        totalAmmoCount = 6667;
        ammoCount = magazineSize;
        crosshairHitDuration = 0.3;
    }

    protected override NodePath? Shoot()
    {
        // Update properties
        base.Shoot();

        // Play sounds
        _.AudioStreamPlayer3D.Stream = shootSound;
        _.AudioStreamPlayer3D.Play();

        // Return + crosshair hit effect
        switch (Raycast.GetCollider())
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

    protected override void Reload()
    {
        // Update properties
        base.Reload();

        // Play sounds
        _.AudioStreamPlayer3D.Stream = reloadSound;
        _.AudioStreamPlayer3D.Play();
    }
}
