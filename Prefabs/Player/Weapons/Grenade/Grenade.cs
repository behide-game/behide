using Behide.Game;
using Behide.Game.Player;
using Godot;
using Serilog;
using Log = Behide.Logging.Log;

namespace Behide.Prefabs.Weapons;

[SceneTree("grenade.tscn", root: "nodes")]
public partial class Grenade : RigidBody3D
{
    private readonly ILogger log = Log.CreateLogger(nameof(Grenade));

    private Area3D Area => nodes.Area3D;
    [Export] private float blowForce = 10f;
    [Export] private int damage = 50;

    private VFXExplosionBB explosion = null!;
    private bool exploded;

    public override void _EnterTree()
    {
        // Configure explosion
        var vfx = nodes.VFX;
        if (vfx is not VFXExplosionBB vfxExplosion)
        {
            log.Error("VFX are not of type VFXExplosionBB");
            return;
        }

        explosion = vfxExplosion;
        explosion.OneShot = true;
        explosion.Finished += QueueFree;

        // Authority trigger explosion
        if (!IsMultiplayerAuthority()) return;
        BodyEntered += _ =>
        {
            if (exploded) return;
            exploded = true;
            ExplodeRpc();
        };

        // If spawning the grenade in a wall BodyEntered is not triggered
        if (GetContactCount() > 0) ExplodeRpc();
    }

    [Rpc(CallLocal = true)]
    private void Explode()
    {
        Freeze = true;
        nodes.MeshInstance3D.Visible = false;

        // Start animation
        explosion.Play();

        // Treat bodies
        if (!IsMultiplayerAuthority()) return;
        var bodies = Area.GetOverlappingBodies();
        foreach (var body in bodies)
        {
            if (body is RigidBody3D rigidBody) PushBody(rigidBody);
            if (body is PlayerBody player) HitPlayerRpc(player.GetPath());
        }
    }

    private void PushBody(RigidBody3D body)
    {
        SetObjectAuthorityRpc(body.GetPath());
        var dir = body.GlobalPosition - GlobalPosition;
        body.ApplyCentralImpulse(dir * blowForce);
    }

    [Rpc(CallLocal = true)]
    private void HitPlayer(NodePath playerNodePath)
    {
        var playerBody = GetNodeOrNull<PlayerBody>(playerNodePath);
        if (playerBody is null)
        {
            log.Error("Failed to get player body from path: {NodePath}", playerNodePath);
            return;
        }

        playerBody.DecreaseHealth(
            Multiplayer.GetRemoteSenderId(),
            damage
        );
    }

    [Rpc(CallLocal = true)]
    private void SetObjectAuthority(NodePath nodePath)
    {
        var remoteId = Multiplayer.GetRemoteSenderId();
        var obj = GetNodeOrNull<BehideObject>(nodePath);
        if (obj is null) return;

        obj.SetMultiplayerAuthority(remoteId);
        log.Debug("Set authority of {NodePath} to {RemoteId}", nodePath, remoteId);
    }
}
