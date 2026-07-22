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

    public override void _EnterTree()
    {
        if (!IsMultiplayerAuthority()) return;
        BodyEntered += b =>
        {
            GD.Print($"Body entered {b.GetPath()}");
            Explode();
        };
    }

    private void Explode()
    {
        var bodies = Area.GetOverlappingBodies();
        foreach (var body in bodies)
        {
            if (body is RigidBody3D rigidBody) PushBody(rigidBody);
            if (body is PlayerBody player) HitPlayerRpc(player.GetPath());
        }
        QueueFree();
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
        GetNode<RigidBody3D>(nodePath).SetMultiplayerAuthority(remoteId);
        log.Debug("Set authority of {NodePath} to {RemoteId}", nodePath, remoteId);
    }
}
