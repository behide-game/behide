using System.Linq;
using Godot;

namespace Behide.Game;

[Tool]
public partial class BehideObject : RigidBody3D
{
    [ExportToolButton("Auto fill")] private Callable AutoFillButton => Callable.From(AutoFill);
    [Export] public Node3D VisualNode = null!;
    [Export] public CollisionShape3D[] CollisionNodes = null!;
    [Export] public new float Mass
    {
        get => ((RigidBody3D)this).Mass;
        set => ((RigidBody3D)this).Mass = value;
    }

    private void AutoFill()
    {
        var rootNode = GetTree().GetEditedSceneRoot();
        rootNode.Name = System.IO.Path.GetFileNameWithoutExtension(GetSceneFilePath());

        // Find CollisionShape3D nodes
        CollisionNodes = FindChildren("*", nameof(CollisionShape3D))
            .Cast<CollisionShape3D>()
            .ToArray();

        // Find visual node
        var potentialVisualNode = GetChildren().SingleOrDefault(node => node is Node3D and not CollisionShape3D);
        if (potentialVisualNode is not Node3D newVisualNode)
        {
            GD.PrintErr("Could not find visual node for behide object.");
            return;
        }
        newVisualNode.SetName(rootNode.Name);

        VisualNode = newVisualNode;

        // Ensure MultiplayerSynchronizer is present and configured
        foreach (var node in FindChildren("*", nameof(MultiplayerSynchronizer), false)) RemoveChild(node);

        var replicationConfig = new SceneReplicationConfig();
        replicationConfig.AddProperty(".:position");
        replicationConfig.AddProperty(".:rotation");
        replicationConfig.AddProperty(".:linear_velocity");
        replicationConfig.AddProperty(".:angular_velocity");
        replicationConfig.PropertySetReplicationMode(".:position", SceneReplicationConfig.ReplicationMode.OnChange);
        replicationConfig.PropertySetReplicationMode(".:rotation", SceneReplicationConfig.ReplicationMode.OnChange);
        replicationConfig.PropertySetReplicationMode(".:linear_velocity", SceneReplicationConfig.ReplicationMode.OnChange);
        replicationConfig.PropertySetReplicationMode(".:angular_velocity", SceneReplicationConfig.ReplicationMode.OnChange);

        var synchronizer = new MultiplayerSynchronizer();
        AddChild(synchronizer);
        synchronizer.Name = nameof(MultiplayerSynchronizer);
        synchronizer.Owner = rootNode;
        synchronizer.ReplicationConfig = replicationConfig;
    }
}
