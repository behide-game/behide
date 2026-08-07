using Godot;

namespace Behide.Game.Player;

[SceneTree("prop.tscn", traverseInstancedScenes: true)]
public partial class PropBody : PlayerBody
{
    private Node3D currentVisualNode = null!;
    private Node3D currentOutlineNode = null!;
    private CollisionShape3D[] collisionNodes = null!;

    [Export] private float maxKickForce = 17f;
    [Export] private ShaderMaterial maskMaterial = null!;

    [ExportGroup("Camera adjust transition")]
    [Export] private double cameraAdjustDuration = 0.4;
    [Export] private Tween.TransitionType cameraAdjustTransitionType = Tween.TransitionType.Bounce;
    [Export] private Tween.EaseType cameraAdjustEaseType = Tween.EaseType.Out;

    private Vector3 initialCameraPosition = Vector3.Zero;
    private Tween? cameraAdjustTween;

    private const float speed = 1.55f;
    private const float slowSpeed = 0.3f;

    protected override Node3D CameraDisk => _.CameraDisk;
    protected override Camera3D Camera => _.CameraDisk.SpringArm3D.Camera;
    private TextureRect TextureRect => _.TextureRect;
    private Camera3D OutlineCamera => _.SubViewport.OutlineCamera;
    private SubViewport SubViewport => _.SubViewport;
    private OutlineEffect OutlineCompositorEffect => GD.Load<OutlineEffect>("res://Assets/Shaders/OutlineCompositorEffect.tres");
    private OutlineEffect? effect;
    protected override RayCast3D RayCast => _.CameraDisk.SpringArm3D.Camera.RayCast;
    protected override HealthBar HealthBar => _.HUD.BottomLeft.Health.HealthBar;
    protected override Label PlayerUsername => _.HUD.Center.PlayerUsername;
    public override MultiplayerSynchronizer PositionSynchronizer => _.PositionSynchronizer;

    public override void _EnterTree()
    {
        currentOutlineNode = _.MeshInstance3DOutline;
        ((MeshInstance3D)currentOutlineNode).SetSurfaceOverrideMaterial(0, maskMaterial);
        base._EnterTree();
        currentVisualNode = _.MeshInstance3D;

        collisionNodes = [_.CollisionShape3D];
        initialCameraPosition = CameraDisk.Position;

        ShowLockedLogo(false);
        AdjustProperties();

        if (!IsMultiplayerAuthority())
        {
            return;
        }

        MoveSpeed = speed;

        var effects = OutlineCamera.Compositor.CompositorEffects;
        effects.Add(OutlineCompositorEffect);
        OutlineCamera.Compositor.SetCompositorEffects(effects);
        effect = (OutlineEffect)OutlineCamera.GetCompositor().GetCompositorEffects()[^1];

        effect.Enabled = true;

        OutlineCamera.MakeCurrent();
        #if !DEBUG
        currentOutlineNode.Hide();
        #endif
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        if (effect != null && effect.OutputTexture2D != null)
        {
            TextureRect.Texture = effect.OutputTexture2D;
        }
    }

    protected override void SetHudsVisibility(bool value) => _.HUD.Get().SetVisible(value);

    public override void _UnhandledInput(InputEvent rawEvent)
    {
        base._UnhandledInput(rawEvent);
        if (!IsMultiplayerAuthority()) return;
        if (!Alive) return;
        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        // Morph
        if (rawEvent.IsActionPressed(InputActions.Morph) && FocusedObject is BehideObject focusedBehideObject)
        {
            GetWindow().SetInputAsHandled();
            MorphRpc(focusedBehideObject.GetPath());
        }
    }

    public override void _UnhandledKeyInput(InputEvent rawEvent)
    {
        if (!IsMultiplayerAuthority()) return;
        if (!Alive) return;
        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        // Rotation locking
        if (rawEvent.IsActionPressed(InputActions.Lock))
        {
            RotationLocked = !RotationLocked;
            ShowLockedLogo(RotationLocked);
            GetWindow().SetInputAsHandled();
        }

        // Kick
        if (rawEvent.IsActionPressed(InputActions.Kick) && FocusedObject is BehideObject objectToKick)
        {
            GetWindow().SetInputAsHandled();

            var kickForce =
                Input.IsActionPressed(InputActions.Slow)
                    ? maxKickForce / 5
                    : maxKickForce;

            var distanceFade = (float)Math.Exp(-GlobalPosition.DistanceTo(FocusedPoint) / 3f);
            var finalForce = kickForce * distanceFade;

            var direction = -Camera.GlobalBasis.Z;
            direction.Y = Math.Max(0, direction.Y);
            direction = direction.Normalized();

            SetObjectAuthorityRpc(objectToKick.GetPath());
            objectToKick.ApplyImpulse(
                finalForce * objectToKick.Mass * direction,
                FocusedPoint - objectToKick.GlobalPosition
            );
        }

        // Toggle outline
        if (rawEvent.IsAction(InputActions.ToggleOutline))
        {
            TextureRect.Visible = rawEvent.IsPressed();
            GetWindow().SetInputAsHandled();
        }

        // Adjust speed
        if (rawEvent.IsAction(InputActions.Slow))
        {
            MoveSpeed = rawEvent.IsPressed() ? slowSpeed : speed;
            GetWindow().SetInputAsHandled();
        }
    }

    [Rpc(CallLocal = true)]
    private void Morph(NodePath behideObjectPath)
    {
        if (GetNode(behideObjectPath) is not BehideObject behideObject) return;
        if (behideObject.VisualNode.Duplicate() is not Node3D newVisualNode) return;

        // Remove current visual node, outline node and collision nodes
        currentVisualNode.QueueFree();
        RemoveChild(currentVisualNode);
        currentOutlineNode.QueueFree();
        RemoveChild(currentOutlineNode);
        foreach (var collisionNode in collisionNodes)
        {
            collisionNode.QueueFree();
            RemoveChild(collisionNode);
        }

        // Set new visual node
        var initialVisualNodePos = newVisualNode.Position;
        currentVisualNode = newVisualNode;
        newVisualNode.Position = Vector3.Zero;
        currentOutlineNode = (Node3D)newVisualNode.Duplicate();
        AddChild(currentVisualNode);
        if (currentOutlineNode is MeshInstance3D currentOutlineMesh)
        {
            currentOutlineMesh.Name = "OutlineMesh";
            currentOutlineMesh.SetLayerMaskValue(2, false);
            currentOutlineMesh.SetLayerMaskValue(4, true);
            for (var i = 0; i < currentOutlineMesh.Mesh.GetSurfaceCount(); i++)
                currentOutlineMesh.SetSurfaceOverrideMaterial(i, maskMaterial);

            AddChild(currentOutlineMesh);
            currentOutlineNode = currentOutlineMesh;
        }
        else
        {
            currentOutlineNode.Name = "OutlineGroup";
            foreach (var meshCandidate in currentOutlineNode.FindChildren("", "MeshInstance3D", true, false))
            {
                if (meshCandidate is not MeshInstance3D mesh)
                {
                    meshCandidate.QueueFree();
                    continue;
                }

                mesh.SetLayerMaskValue(1, false);
                mesh.SetLayerMaskValue(2, false);
                mesh.SetLayerMaskValue(3, false);
                mesh.SetLayerMaskValue(4, true);
                for (var i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
                    mesh.SetSurfaceOverrideMaterial(i, maskMaterial);
            }

            AddChild(currentOutlineNode);
        }
#if DEBUG

#else
            if(IsMultiplayerAuthority()) currentOutlineNode.Hide();
#endif


        // Set new collision shapes
        var newCollisionNodes = new List<CollisionShape3D>(behideObject.CollisionNodes.Length);
        foreach (var collisionNode in behideObject.CollisionNodes)
        {
            var newNode = collisionNode.Duplicate();
            if (newNode is not CollisionShape3D newCollisionNode)
            {
                newNode.QueueFree();
                continue;
            }

            newCollisionNode.Position -= initialVisualNodePos;
            AddChild(newCollisionNode);
            newCollisionNodes.Add(newCollisionNode);
        }

        collisionNodes = newCollisionNodes.ToArray();

        // Set mass
        Mass = behideObject.Mass;

        // Set health and camera position
        AdjustProperties();
    }

    private void AdjustProperties()
    {
        var aabb = currentVisualNode is MeshInstance3D meshInstance3D
            ? meshInstance3D.GetAabb()
            : currentVisualNode
                .FindChildren("*", nameof(MeshInstance3D))
                .Select(mi3D => ((MeshInstance3D)mi3D).GetAabb())
                .Aggregate(new Aabb(), (current, aabb) => current.Merge(aabb));

        // Adjust health
        var volume = aabb.Size.X * aabb.Size.Y * aabb.Size.Z;
        var maxHealth = Math.Log(Math.Round(25 * volume), 1.096);
        MaxHealth = Math.Max(1, (int)maxHealth);

        // Adjust camera position
        var newPosition = initialCameraPosition + aabb.GetCenter();
        var newScale = Math.Max(0.1f, Mathf.Sqrt((aabb.Size.X + aabb.Size.Y + aabb.Size.Z) / 3f));
        var newScaleVector = new Vector3(newScale, newScale, newScale);

        cameraAdjustTween?.Kill();
        cameraAdjustTween = CreateTween();
        cameraAdjustTween.SetTrans(cameraAdjustTransitionType);
        cameraAdjustTween.SetEase(cameraAdjustEaseType);

        cameraAdjustTween.TweenProperty(CameraDisk, "position", newPosition, cameraAdjustDuration);
        cameraAdjustTween.Parallel().TweenProperty(CameraDisk, "scale", newScaleVector, cameraAdjustDuration);
    }

    private void ShowLockedLogo(bool show) => _.HUD.BottomLeft.RotationLockedLabel.SetVisible(show);

    protected override void PlayerPropertiesChanged(Types.Player player)
    {
        var color = player.Color;
        if (currentOutlineNode is MeshInstance3D currentOutlineMesh)
            for (var i = 0; i < currentOutlineMesh.GetSurfaceOverrideMaterialCount(); i++)
                ((ShaderMaterial)currentOutlineMesh.GetSurfaceOverrideMaterial(i)).SetShaderParameter("colorID", color);
        else
        {
            currentOutlineNode.Name = "OutlineGroup";
            foreach (var meshCandidate in currentOutlineNode.FindChildren("", nameof(MeshInstance3D), true, false))
            {
                if (meshCandidate is not MeshInstance3D mesh)
                {
                    meshCandidate.QueueFree();
                    continue;
                }

                for (var i = 0; i < mesh.GetSurfaceOverrideMaterialCount(); i++)
                    ((ShaderMaterial)mesh.GetSurfaceOverrideMaterial(i)).SetShaderParameter("colorID", color);
            }
        }
    }
}
