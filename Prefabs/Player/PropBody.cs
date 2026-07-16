using System.Diagnostics;
using Behide.UI.Controls;
using Godot;
using Serilog.Debugging;

namespace Behide.Game.Player;

[SceneTree("prop.tscn", traverseInstancedScenes:true)]
public partial class PropBody : PlayerBody
{
    private Node3D currentVisualNode = null!;
    private Node3D currentOutlineNode = null!;
    private CollisionShape3D[] collisionNodes = null!;

    [Export] private float maxKickForce = 17f;

    [ExportGroup("Camera adjust transition")]
    [Export] private double cameraAdjustDuration = 0.4;
    [Export] private Tween.TransitionType cameraAdjustTransitionType = Tween.TransitionType.Bounce;
    [Export] private Tween.EaseType cameraAdjustEaseType = Tween.EaseType.Out;

    private Vector3 initialCameraPosition = Vector3.Zero;
    private Tween? cameraAdjustTween;

    private const float speed = 1.55f;
    private const float slowSpeed = 0.3f;
    private bool isOutlineVisible = false;

    protected override Node3D CameraDisk => _.CameraDisk;
    protected override Camera3D Camera => _.CameraDisk.SpringArm3D.Camera;
    protected Camera3D OutlineCamera => _.SubViewport.OutlineCamera;
    protected SubViewport SubViewport => _.SubViewport;
    protected SubViewport HorizontalViewport => _.HorizontalViewport;
    protected SubViewport VerticalViewport => _.SubViewportContainer.VerticalViewport;
    protected ColorRect ColorRectHorizontal => _.HorizontalViewport.ColorRect;
    protected ColorRect ColorRectVertical => _.SubViewportContainer.VerticalViewport.ColorRect;
    private Shader MaskShader => GD.Load<Shader>(GetSceneFilePath().GetBaseDir().PathJoin("mask.gdshader"));
    protected ShaderMaterial MaskMaterial = null!;
    protected ShaderMaterial HorizontalMaterial => (ShaderMaterial)ColorRectHorizontal.GetMaterial();
    protected ShaderMaterial VerticalMaterial => (ShaderMaterial)ColorRectVertical.GetMaterial();
    protected override RayCast3D RayCast => _.CameraDisk.SpringArm3D.Camera.RayCast;
    protected override BezelContainer HealthBar => _.HUD.BottomLeft.Health.Border.Mask.HealthBar;
    protected override Label HealthLabel => _.HUD.BottomLeft.Health.Border.HealthLabel;
    protected override Label PlayerUsername => _.HUD.Center.PlayerUsername;
    public override MultiplayerSynchronizer PositionSynchronizer => _.PositionSynchronizer;

    public override void _EnterTree()
    {
        base._EnterTree();
        currentVisualNode = _.MeshInstance3D;
        currentOutlineNode = _.MeshInstance3DOutline;

        MaskMaterial = new ShaderMaterial();
        MaskMaterial.SetShader(MaskShader);
        var room = GameManager.Room.Room;
        var color = new Color(1f, 0.8f, 0f, 1f);
        if(room?.Players.TryGetValue(GetMultiplayerAuthority(), out var playerObservable) ?? false)
        {
            color = playerObservable.Value.Color;
        }
        MaskMaterial.SetShaderParameter("colorID", color);

        ((MeshInstance3D)currentOutlineNode).SetSurfaceOverrideMaterial(0, MaskMaterial);

        collisionNodes = [_.CollisionShape3D];
        initialCameraPosition = CameraDisk.Position;

        ShowLockedLogo(false);
        AdjustProperties();

        if(!IsMultiplayerAuthority())
        {
            return;
        }
        OutlineCamera.MakeCurrent();
        #if DEBUG

        #else
            currentOutlineNode.Hide();
        #endif
        if(isOutlineVisible)
        {
            ColorRectVertical.Show();
        }

        HorizontalMaterial.SetShaderParameter("input_texture", SubViewport.GetTexture());
        VerticalMaterial.SetShaderParameter("input_texture", HorizontalViewport.GetTexture());

        GetWindow().SizeChanged += ResizeViewports;
        ResizeViewports();
        UpdateViewportSettingsOnChange();
        UpdateViewportSettings();
    }

    public override void _Process(double delta)
    {
        base._Process(delta);
        UpdateViewportSettings();
        VerticalViewport.Scaling3DScale = 0.5f;
    }

    public override void _PhysicsProcess(double delta)
    {
        base._PhysicsProcess(delta);
        // done in physics process to avoid synchronisation lag between cameras
        OutlineCamera.GlobalTransform = Camera.GlobalTransform;
        OutlineCamera.Fov = Camera.Fov;
        OutlineCamera.Size = Camera.Size;
        OutlineCamera.KeepAspect = Camera.KeepAspect;
    }

    private void UpdateViewportSettingsOnChange()
    {
        GameManager.Settings.ViewportSettingsChanged.Subscribe(_ => UpdateViewportSettings());
    }

    private void UpdateViewportSettings()
    {
        SubViewport.Scaling3DMode = GetWindow().Scaling3DMode;
        SubViewport.Scaling3DScale = GetWindow().Scaling3DScale;
        SubViewport.Msaa3D = GetWindow().Msaa3D;
        SubViewport.ScreenSpaceAA = GetWindow().ScreenSpaceAA;
        SubViewport.UseTaa = GetWindow().UseTaa;
    }

    private void ResizeViewports()
    {
        SubViewport.Size = GetWindow().Size;
        HorizontalViewport.Size = GetWindow().Size;
    }

    protected override void SetHudsVisibility(bool value) => _.HUD.Get().SetVisible(value);

    public override void _UnhandledInput(InputEvent rawEvent)
    {
        base._UnhandledInput(rawEvent);
        if (!IsMultiplayerAuthority()) return;
        if (!Alive) return;
        if (Input.MouseMode != Input.MouseModeEnum.Captured) return;

        // Morph
        if (Input.IsActionJustPressed(InputActions.Morph) && FocusedObject is BehideObject focusedBehideObject)
            Rpc(nameof(Morph), focusedBehideObject.GetPath());

        // Kick
        if (Input.IsActionJustPressed(InputActions.Kick) && FocusedObject is BehideObject objectToKick)
        {
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
        if(Input.IsActionJustPressed(InputActions.ToggleOutline))
        {
            if(isOutlineVisible)
            {
                ColorRectVertical.Hide();
            }
            else
            {
                ColorRectVertical.Show();
            }
            isOutlineVisible = !isOutlineVisible;
        }

        // Adjust speed
        MoveSpeed =
            Input.IsActionPressed(InputActions.Slow)
                ? slowSpeed
                : speed;
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
        if(currentOutlineNode is MeshInstance3D currentOutlineMesh)
        {
            currentOutlineMesh.Name = "OutlineMesh";
            currentOutlineMesh.SetLayerMaskValue(2, false);
            currentOutlineMesh.SetLayerMaskValue(4, true);
            for (int i = 0; i < currentOutlineMesh.Mesh.GetSurfaceCount(); i++)
            {
                currentOutlineMesh.SetSurfaceOverrideMaterial(i, MaskMaterial);
            }
            AddChild(currentOutlineMesh);
            currentOutlineNode = currentOutlineMesh;
        }
        else
        {
            currentOutlineNode.Name = "OutlineGroup";
            foreach (var meshCandidate in currentOutlineNode.FindChildren("", "MeshInstance3D", true, false))
            {
                if(meshCandidate is not MeshInstance3D mesh)
                {
                    meshCandidate.QueueFree();
                    continue;
                }
                mesh.SetLayerMaskValue(1, false);
                mesh.SetLayerMaskValue(2, false);
                mesh.SetLayerMaskValue(3, false);
                mesh.SetLayerMaskValue(4, true);
                for (int i = 0; i < mesh.Mesh.GetSurfaceCount(); i++)
                {
                    mesh.SetSurfaceOverrideMaterial(i, MaskMaterial);
                }
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

    public void ShowLockedLogo(bool show) => _.HUD.BottomLeft.RotationLockedLabel.SetVisible(show);
}
