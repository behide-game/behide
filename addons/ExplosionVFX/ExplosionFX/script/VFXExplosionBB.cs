using Godot;

[Tool]
public partial class VFXExplosionBB : VfxControllerBb
{
    private AudioStreamPlayer3D? AudioStreamPlayer
    {
        get
        {
            if (field != null)
                return field;

            var result = GetNodeOrNull<AudioStreamPlayer3D>("AudioStreamPlayer3D");

            if (!Engine.IsEditorHint())
                field = result;

            return result;
        }
    }

    private GpuParticles3D? Smoke
    {
        get
        {
            if (field != null)
                return field;

            var result = GetNodeOrNull<GpuParticles3D>("Smoke");

            if (!Engine.IsEditorHint())
                field = result;

            return result;
        }
    }

    // public VFXOmniLightBB Light
    // {
    //     get
    //     {
    //         if (field != null)
    //             return field;
    //
    //         var result = GetNodeOrNull<VFXOmniLightBB>("VFXOmniLightBB");
    //
    //         if (!Engine.IsEditorHint())
    //             field = result;
    //
    //         return result;
    //     }
    // }

    // --- Color ---

    [ExportGroup("Color")]
    [Export]
    public Color PrimaryColor
    {
        get;
        set
        {
            field = value;
            SetShaderParam("primaryfield", field);
        }
    }

    [Export]
    public Color SecondaryColor
    {
        get;
        set
        {
            field = value;
            SetShaderParam("secondaryfield", field);
        }
    }

    [Export]
    public Color TertiaryColor
    {
        get;
        set
        {
            field = value;
            SetShaderParam("tertiaryfield", field);
        }
    }

    [Export]
    public float Emission
    {
        get;
        set
        {
            field = value;
            SetShaderParam("emission", field);
        }
    } = 1.0f;

    // --- Light ---

    // [ExportGroup("Light")]
    // [Export]
    // public Color LightColor
    // {
    //     get => field;
    //     set
    //     {
    //         field = value;
    //         if (Light != null) Light.LightColor = field;
    //     }
    // }
    //
    // [Export]
    // public float LightEnergy
    // {
    //     get => field;
    //     set
    //     {
    //         field = value;
    //         if (Light != null) Light.VfxLightEnergy = field;
    //     }
    // } = 0.5f;
    //
    // [Export]
    // public float LightIndirectEnergy
    // {
    //     get => field;
    //     set
    //     {
    //         field = value;
    //         if (Light != null) Light.VfxLightIndirectEnergy = field;
    //     }
    // } = 1.0f;
    //
    // [Export]
    // public float LightVolumetricFogEnergy
    // {
    //     get => field;
    //     set
    //     {
    //         field = value;
    //         if (Light != null) Light.VfxLightVolumetricFogEnergy = field;
    //     }
    // } = 1.0f;

    // --- Shape ---

    [ExportGroup("Shape")]
    [Export]
    public Texture2D? NoiseTexture
    {
        get;
        set
        {
            field = value;
            SetShaderParam("noisefield", field ?? new Variant());
        }
    }

    [Export]
    public Vector2 NoiseScale
    {
        get;
        set
        {
            field = value;
            SetShaderParam("noisefield", field);
        }
    } = new(1.0f, 1.0f);

    [Export]
    public Vector2 NoiseScroll
    {
        get;
        set
        {
            field = value;
            SetShaderParam("noisefield", field);
        }
    } = new(0.0f, 0.2f);

    [Export(PropertyHint.ExpEasing, "inout")]
    public float NoiseShape
    {
        get;
        set
        {
            field = value;
            SetShaderParam("noisefield", field);
        }
    } = 1.0f;

    // --- Smoke ---

    [ExportGroup("Smoke")]
    [Export]
    public Texture2D? SmokeTexture
    {
        get;
        set
        {
            field = value;
            SetShaderParam("smokefield", field ?? new Variant());

            if (field is NoiseTexture2D noiseTex)
            {
                var normalTexture = (NoiseTexture2D)noiseTex.Duplicate();
                normalTexture.AsNormalMap = true;
                normalTexture.BumpStrength = 32.0f;
                SetShaderParam("normalfield", normalTexture);
            }
        }
    }

    [Export]
    public int SmokeAmount
    {
        get;
        set
        {
            field = value;
            Smoke?.Amount = field;
        }
    } = 40;

    [Export]
    public bool UseSteppedShading
    {
        get;
        set
        {
            field = value;
            SetShaderParam("usefield", field);
        }
    } = true;

    [Export(PropertyHint.Range, "2.0,12.0,0.01")]
    public float ShadowSteps
    {
        get;
        set
        {
            field = value;
            SetShaderParam("shadowfield", field);
        }
    } = 3.0f;

    // --- Audio ---

    [ExportGroup("Audio")]
    [Export]
    public AudioStream? AudioStream
    {
        get;
        set
        {
            field = value;
            AudioStreamPlayer?.Stream = field;
        }
    }

    [Export]
    public AudioStreamPlayer3D.AttenuationModelEnum AttenuationModel
    {
        get;
        set
        {
            field = value;
            AudioStreamPlayer?.AttenuationModel = field;
        }
    }

    [Export(PropertyHint.Range, "-80.0,80.0,0.01,suffix:dB")]
    public float VolumeDb
    {
        get;
        set
        {
            field = value;
            AudioStreamPlayer?.VolumeDb = field;
        }
    }

    [Export(PropertyHint.Range, "0.1,100.0,0.01")]
    public float UnitSize
    {
        get;
        set
        {
            field = value;
            AudioStreamPlayer?.UnitSize = field;
        }
    } = 10.0f;

    [Export(PropertyHint.Range, "-24.0,6.0,0.01,suffix:dB")]
    public float MaxDb
    {
        get;
        set
        {
            field = value;
            AudioStreamPlayer?.MaxDb = field;
        }
    } = 3.0f;

    [Export(PropertyHint.Range, "0.01,4.0,0.01")]
    public float PitchScale
    {
        get;
        set
        {
            field = value;
            AudioStreamPlayer?.PitchScale = field;
        }
    } = 1.0f;

    [Export]
    public bool StreamPaused
    {
        get;
        set
        {
            field = value;
            AudioStreamPlayer?.StreamPaused = field;
        }
    }

    [Export(PropertyHint.Range, "0.0,2000.0,0.01,suffix:m")]
    public float AudioMaxDistance
    {
        get;
        set
        {
            field = value;
            AudioStreamPlayer?.MaxDistance = field;
        }
    } = 3.0f;

    [Export]
    public int MaxPolyphony
    {
        get;
        set
        {
            field = value;
            AudioStreamPlayer?.MaxPolyphony = field;
        }
    } = 1;

    [Export(PropertyHint.Range, "0.0,3.0,0.01")]
    public float PanningStrength
    {
        get;
        set
        {
            field = value;
            AudioStreamPlayer?.PanningStrength = field;
        }
    } = 1.0f;

    [Export]
    public StringName Bus
    {
        get;
        set
        {
            field = value;
            AudioStreamPlayer?.Bus = field;
        }
    } = "Master";
}
