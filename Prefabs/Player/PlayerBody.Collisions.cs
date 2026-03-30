using Godot;

namespace Behide.Game.Player;

public abstract partial class PlayerBody
{
    private void PropagateCollision()
    {
        for (var i = 0; i < GetSlideCollisionCount(); i++)
        {
            var collision = GetSlideCollision(i);
            if (collision.GetCollider() is not RigidBody3D rb) continue;
            if (!rb.IsMultiplayerAuthority()) Rpc(nameof(SetObjectAuthority), rb.GetPath());

            var pushDirection = -collision.GetNormal();
            pushDirection.Y = 0; // Remove verticality
            var velocityDiff = Mathf.Max(0, Velocity.Dot(pushDirection) - rb.LinearVelocity.Dot(pushDirection));
            var massRatio = Mass / rb.Mass;
            var massContribution = massRatio >= 1 ? rb.Mass : Mass;
            var impulse = pushDirection * velocityDiff * massContribution * pushCoefficient;
            rb.ApplyImpulse(impulse, collision.GetPosition() - rb.GlobalPosition);
        }
    }

    [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true)]
    private void SetObjectAuthority(NodePath nodePath)
    {
        var remoteId = Multiplayer.GetRemoteSenderId();
        GetNode<RigidBody3D>(nodePath).SetMultiplayerAuthority(remoteId);
        log.Debug("Set authority of {NodePath} to {RemoteId}", nodePath, remoteId);
    }
}
