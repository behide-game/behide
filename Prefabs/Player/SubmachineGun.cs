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
    protected override TextureRect AmmoIcon => _.Hud.RBottom.AmmoIcon;
    protected override TextureRect ReloadIcon => _.Hud.RBottom.ReloadIcon;

    private Control HitMark => _.Hud.Center.Crosshair.CrosshairHit;
    private RayCast3D Raycast => _.RayCast;

    [Export] private AudioStream shootSound = null!;
    [Export] private AudioStream reloadSound = null!;

    private double crosshairHitDuration = 0.3;
    private Tween? crosshairHitTween;

    public override void _EnterTree()
    {
        base._EnterTree();
        if (IsMultiplayerAuthority()) SetZClipScale();
    }

    private void SetZClipScale()
    {
        var descendants = _.Model.FindChildren("*", nameof(MeshInstance3D), owned: false);
        foreach (var descendant in descendants)
        {
            if (descendant is not MeshInstance3D mesh) continue;
            for (var i = 0; i < mesh.GetSurfaceOverrideMaterialCount(); i++)
            {
                var material = mesh.GetActiveMaterial(i);
                if (material is not StandardMaterial3D standardMaterial) continue;

                var newMaterial = (StandardMaterial3D)standardMaterial.Duplicate();
                newMaterial.UseZClipScale = true;
                newMaterial.ZClipScale = 0.01f;
                mesh.SetSurfaceOverrideMaterial(i, newMaterial);
            }
        }
    }

    protected override Node3D? ShootCore()
    {
        // Play sounds
        PlaySoundRpc(true);

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

    protected override void ReloadCore() => PlaySoundRpc(false);

    [Rpc(CallLocal =  true)]
    private void PlaySound(bool isShootSound)
    {
        _.AudioStreamPlayer3D.Stream = isShootSound ? shootSound : reloadSound;
        _.AudioStreamPlayer3D.Play();
    }
}
