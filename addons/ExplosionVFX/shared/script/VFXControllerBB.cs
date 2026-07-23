using Godot;

[Tool]
public partial class VfxControllerBb : Node3D
{
    private readonly List<ShaderMaterial> materials = [];
    private readonly List<GpuParticles3D> particles = [];
    private AnimationPlayer? anim;

    [Export] public bool OneShot;

    public override void _EnterTree()
    {
        // Set materials
        foreach (var c in GetChildren())
        {
            if (c is GpuParticles3D { MaterialOverride: ShaderMaterial pMat })
                materials.Add(pMat);
            else if (c is MeshInstance3D { MaterialOverride: ShaderMaterial mMat })
                materials.Add(mMat);
        }

        // Set particles
        foreach (var c in GetChildren())
            if (c is GpuParticles3D p) particles.Add(p);

        // Set anim
        anim = GetNodeOrNull<AnimationPlayer>("AnimationPlayer");
    }

    [Export(PropertyHint.Range, "0.0,8.0,0.01")]
    public float SpeedScale
    {
        get;
        set
        {
            field = value;
            foreach (var p in particles)
                p.SpeedScale = field;

            if (anim != null)
                anim.SpeedScale = field;
        }
    } = 1.0f;

    [ExportToolButton("Play")] private Callable PlayButton => Callable.From(Play);
    [ExportToolButton("Stop")] private Callable StopButton => Callable.From(Stop);

    [Signal] public delegate void FinishedEventHandler();
    [Signal] public delegate void StoppedEventHandler();

    public async void Play()
    {
        if (anim == null) return;

        anim.Play("main");
        anim.Seek(0.0);

        await ToSignal(anim, AnimationMixer.SignalName.AnimationFinished);
        EmitSignal(SignalName.Finished);

        if (OneShot) return;
        anim.Advance(0.0);
        Play();
    }

    public void Stop()
    {
        if (anim == null) return;

        anim.Play("stop");
        anim.Stop();
        EmitSignal(SignalName.Stopped);
    }

    public void _RestartParticles()
    {
        foreach (var p in particles) p.Restart();
    }

    protected void SetShaderParam(string key, Variant value)
    {
        foreach (var m in materials) m.SetShaderParameter(key, value);
    }
}
