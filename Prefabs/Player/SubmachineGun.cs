using Behide.Game;
using Behide.Game.Player;
using Godot;

namespace Behide.Prefabs.Player;

[SceneTree]
public partial class SubmachineGun : Gun
{
    public override int DamagePerAmmo => 2;
    protected override int TotalAmmoCount { get; set; } = 6667;
    protected override int MagazineSize => 45;
    protected override float ReloadTime => 2.5f;
    protected override float FireRate => 10f;

    public override Control Hud => _.Hud;
    public override Label PlayerUsernameLabel => _.Hud.Center.PlayerUsername;
    protected override Label AmmoLabel => _.Hud.RBottom.Ammo;

    private Control HitMark => _.Hud.Center.Crosshair.CrosshairHit;
    private RayCast3D Raycast => _.RayCast;

    [Export] private AudioStream shootSound = null!;
    [Export] private AudioStream reloadSound = null!;

    private double crosshairHitDuration = 0.3;
    private Tween? crosshairHitTween;

    protected override Node3D? ShootCore()
    {
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
                crosshairHitTween.TweenProperty(HitMark, "modulate", new Color(0xFFFFFF00), crosshairHitDuration);
                HitMark.Modulate = new Color(0xFFFFFFFF);
                return player;
            case BehideObject behideObject:
                return behideObject;
            default:
                return null;
        }
    }

    protected override void ReloadCore()
    {
        // Play sounds
        _.AudioStreamPlayer3D.Stream = reloadSound;
        _.AudioStreamPlayer3D.Play();
    }
}
