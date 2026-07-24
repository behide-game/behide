using Godot;

[GlobalClass]

public partial class OutlineEffect : CompositorEffect
{
    [Export] public int outlineWidth = 3;
    public RenderingDevice rd;
    public Rid shader_horizontal;
    public Rid shader_vertical;
    public Rid pipeline_horizontal;
    public Rid pipeline_vertical;
    public bool texturesInitialized = false;
    public Vector2I textureSize;
    public Rid textureA;
    public Rid textureB;
    public Rid sampler;
    public Texture2Drd textureArd;
    public Texture2Drd textureBrd;
    public Texture2Drd OutputTexture2D => textureBrd;
    public int width;
    public int height;

    public OutlineEffect()
    {
        EffectCallbackType = EffectCallbackTypeEnum.PostTransparent;
        AccessResolvedColor = true;
        RenderingServer.CallOnRenderThread(Callable.From(_InitializeCompute));
    }

    public override void _Notification( int what )
	{
		if ( what == NotificationPredelete )
		{
			if ( shader_horizontal.IsValid )
			{
				try{
                    rd.FreeRid(shader_horizontal);
                }
                catch {GD.Print("OUTLINE COMPOSITOR : Failed to free the horizontal shader");}
            }
            if ( shader_vertical.IsValid )
			{
				try{
                    rd.FreeRid(shader_vertical);
                }
                catch {GD.Print("OUTLINE COMPOSITOR : Failed to free the vertical shader");}
            }
            if ( sampler.IsValid)
            {
                try{
                    rd.FreeRid(sampler);
                }
                catch {GD.Print("OUTLINE COMPOSITOR : Failed to free the sampler");}
            }
            if (texturesInitialized)
            {
                try{
                    if(texturesInitialized)
                    {
                        rd.FreeRid(textureA);
                        rd.FreeRid(textureB);
                    }
                }
                catch {GD.Print("OUTLINE COMPOSITOR : Failed to free textures");}
            }
		}
	}

    // Initialize when the rendering device is initialized
    public void _InitializeCompute()
	{
		rd = RenderingServer.GetRenderingDevice();

		if ( rd == null )
		{
            GD.Print("OUTLINE COMPOSITOR : Failed to get rendering device");
			return;
		}

		var shaderHFile  = GD.Load<RDShaderFile>("res://Assets/Shaders/outline_shader_horizontal.glsl");
		var shaderHSpirv = shaderHFile.GetSpirV();
        GD.Print("OUTLINE COMPOSITOR : Shader horizontal compiled");

        var shaderVFile  = GD.Load<RDShaderFile>("res://Assets/Shaders/outline_shader_vertical.glsl");
		var shaderVSpirv = shaderVFile.GetSpirV();
        GD.Print("OUTLINE COMPOSITOR : Shader vertical compiled");

		shader_horizontal = rd.ShaderCreateFromSpirV( shaderHSpirv );
        shader_vertical = rd.ShaderCreateFromSpirV( shaderVSpirv );

		if ( shader_horizontal.IsValid )
		{
			pipeline_horizontal = rd.ComputePipelineCreate(shader_horizontal);
            GD.Print("OUTLINE COMPOSITOR : Pipeline horizontal initialized");
		}
        if ( shader_vertical.IsValid )
		{
			pipeline_vertical = rd.ComputePipelineCreate(shader_vertical);
            GD.Print("OUTLINE COMPOSITOR : Pipeline vertical initialized");
		}
        var samplerState = new RDSamplerState();
        sampler = rd.SamplerCreate(samplerState);
        if(sampler.IsValid) GD.Print("OUTLINE COMPOSITOR : Sampler created");
	}

    // Initialize texture when the size is known
    public void _InitializeTexture(Vector2I size)
    {
        RDTextureFormat format = new();
        format.Width = (uint)size.X;
        format.Height = (uint)size.Y;
        format.ArrayLayers = 1;
        format.Mipmaps = 1;
        format.Format = RenderingDevice.DataFormat.R16G16B16A16Sfloat;
        format.UsageBits =
            RenderingDevice.TextureUsageBits.StorageBit |
            RenderingDevice.TextureUsageBits.SamplingBit |
            RenderingDevice.TextureUsageBits.CanCopyFromBit |
            RenderingDevice.TextureUsageBits.CanCopyToBit |
            RenderingDevice.TextureUsageBits.ColorAttachmentBit;

        textureA = rd.TextureCreate(format, new RDTextureView());
        textureB = rd.TextureCreate(format, new RDTextureView());

        if (textureA.IsValid && textureB.IsValid)
        {
            texturesInitialized = true;
            textureSize = size;

            if (textureArd == null) textureArd = new Texture2Drd();
            if (textureBrd == null) textureBrd = new Texture2Drd();

            textureArd.TextureRdRid = textureA;
            textureBrd.TextureRdRid = textureB;

            GD.Print("OUTLINE COMPOSITOR : Texture initialized");
        }
    }

    public override void _RenderCallback( int callbacktype, RenderData pRenderData)
    {
        var renderSceneBuffers  = ( RenderSceneBuffersRD ) pRenderData.GetRenderSceneBuffers();
        Vector2I size = renderSceneBuffers.GetInternalSize();
        // Initialize texture on pipeline entering and on window resize
        if (!texturesInitialized || size != textureSize)
        {
            if (texturesInitialized)
            {
                rd.FreeRid(textureA);
                rd.FreeRid(textureB);
                textureArd = null;
                textureBrd = null;
            }
            _InitializeTexture(size);
        }

        // Distribute load
        Vector3I groups = new Vector3I(
            (size.X + 7) / 8,
            (size.Y + 7) / 8,
            1
        );

        var bytesList = new List<byte>();
        bytesList.AddRange( BitConverter.GetBytes( (float)size.X ) );
        bytesList.AddRange( BitConverter.GetBytes( (float)size.Y ) );
        bytesList.AddRange( BitConverter.GetBytes( outlineWidth ) );
        var pushConstantBytes = bytesList.ToArray();

        // Only 1 view in practice but just in case
        int viewCount = (int) renderSceneBuffers.GetViewCount();
        for ( var i = 0; i < viewCount; i++)
        {
            var view = (uint) i;

            //- HORIZONTAL PASS -//
            // Get the image streamed by the camera. GetTexture instead of GetColorLayer to ensure MSAA compatibility
            Rid inputImage = renderSceneBuffers.GetTexture("render_buffers", "color");

            // input image is treated as a texture because the base image lacks the TEXTURE_USAGE_STORAGE_BIT flag
            var uniformInputImage  = new RDUniform();
            uniformInputImage.UniformType = RenderingDevice.UniformType.SamplerWithTexture;
            uniformInputImage.Binding = 0;
            uniformInputImage.AddId( sampler );
            uniformInputImage.AddId( inputImage );

            // image to be sent to TextureRect beacuse we can't write on base image (also flag issue)
            var uniformtexture  = new RDUniform();
            uniformtexture.UniformType = RenderingDevice.UniformType.Image;
            uniformtexture.Binding = 1;
            uniformtexture.AddId( textureA );

            var uniformSet  = UniformSetCacheRD.GetCache(shader_horizontal, 0, new Godot.Collections.Array<RDUniform>(){uniformInputImage, uniformtexture});
            if (!uniformSet.IsValid)
            {
                GD.Print("OUTLINE COMPOSITOR : Uniform set failed (Horizontal pass)");
                return;
            }
            var computeList  = rd.ComputeListBegin();
            rd.ComputeListBindComputePipeline(computeList, pipeline_horizontal);
            rd.ComputeListBindUniformSet(computeList, uniformSet, 0);
            rd.ComputeListSetPushConstant(computeList, pushConstantBytes, (uint) pushConstantBytes.Length );
            rd.ComputeListDispatch(computeList, (uint)groups.X, (uint)groups.Y, (uint)groups.Z);
            rd.ComputeListEnd();

            //- VERTICAL PASS -//
            uniformInputImage  = new RDUniform();
            uniformInputImage.UniformType = RenderingDevice.UniformType.Image;
            uniformInputImage.Binding = 0;
            uniformInputImage.AddId( textureA );

            uniformtexture = new RDUniform();
            uniformtexture.UniformType = RenderingDevice.UniformType.Image;
            uniformtexture.Binding = 1;
            uniformtexture.AddId( textureB );

            uniformSet  = UniformSetCacheRD.GetCache(shader_vertical, 0, new Godot.Collections.Array<RDUniform>(){uniformInputImage, uniformtexture});
            if (!uniformSet.IsValid)
            {
                GD.Print("OUTLINE COMPOSITOR : Uniform set failed  (Vertical pass)");
                return;
            }
            computeList  = rd.ComputeListBegin();
            rd.ComputeListBindComputePipeline(computeList, pipeline_vertical);
            rd.ComputeListBindUniformSet(computeList, uniformSet, 0);
            rd.ComputeListSetPushConstant(computeList, pushConstantBytes, (uint) pushConstantBytes.Length );
            rd.ComputeListDispatch(computeList, (uint)groups.X, (uint)groups.Y, (uint)groups.Z);
            rd.ComputeListAddBarrier(computeList);
            rd.ComputeListEnd();
        }
    }
}
