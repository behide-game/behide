using Godot;

[GlobalClass]

public partial class OutlineEffect : CompositorEffect
{
    public RenderingDevice rd;
    public Rid shader;
    public Rid pipeline;
    public bool textureInitialized = false;
    public Vector2I textureSize;
    public Rid outputTexture;
    public Rid sampler;
    public Texture2Drd outputTexture2D;
    public Texture2Drd OutputTexture2D => outputTexture2D;
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
			if ( shader.IsValid )
			{
				try{
                    rd.FreeRid(shader);
                }
                catch {GD.Print("OUTLINE COMPOSITOR : Failed to free the shader");}
                try{
                    rd.FreeRid(sampler);
                }
                catch {GD.Print("OUTLINE COMPOSITOR : Failed to free the sampler");}
                try{
                    if(textureInitialized) rd.FreeRid(outputTexture);
                }
                catch {GD.Print("OUTLINE COMPOSITOR : Failed to free the output texture");}

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

		var shaderFile  = GD.Load<RDShaderFile>("res://Assets/Shaders/outline_shader.glsl");
		var shaderSpirv = shaderFile.GetSpirV();
        GD.Print("OUTLINE COMPOSITOR : Shader compiled");

		shader = rd.ShaderCreateFromSpirV( shaderSpirv );

		if ( shader.IsValid )
		{
			pipeline = rd.ComputePipelineCreate(shader);
            GD.Print("OUTLINE COMPOSITOR : Pipeline initialized");
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

        outputTexture = rd.TextureCreate(format, new RDTextureView());

        if (outputTexture.IsValid)
        {
            textureInitialized = true;
            textureSize = size;

            if (outputTexture2D == null)
            {
                outputTexture2D = new Texture2Drd();
            }

            outputTexture2D.TextureRdRid = outputTexture;

            GD.Print("OUTLINE COMPOSITOR : Texture initialized");
        }
    }

    public override void _RenderCallback( int callbacktype, RenderData pRenderData)
    {
        var renderSceneBuffers  = ( RenderSceneBuffersRD ) pRenderData.GetRenderSceneBuffers();
        Vector2I size = renderSceneBuffers.GetInternalSize();
        // Initialize texture on pipeline entering and on window resize
        if (!textureInitialized || size != textureSize)
        {
            if (textureInitialized)
            {
                rd.FreeRid(outputTexture);
                outputTexture2D = null;
            }
            _InitializeTexture(size);
        }

        var pushConstant = new float[]{
            size.X,
		    size.Y
        };

        // Distribute load
        Vector3I groups = new Vector3I(
            (size.X + 7) / 8,
            (size.Y + 7) / 8,
            1
        );

        var bytesList = new List<byte>();
        Array.ForEach( pushConstant, c => bytesList.AddRange( BitConverter.GetBytes( c ) ) );
        var pushConstantBytes = bytesList.ToArray();

        // Only 1 view in practice but just in case
        int viewCount = (int) renderSceneBuffers.GetViewCount();
        for ( var i = 0; i < viewCount; i++)
        {
            var view = (uint) i;
            // Get the image streamed by the camera. GetTexture instead of GetColorLayer to ensure MSAA compatibility
            Rid inputImage = renderSceneBuffers.GetTexture("render_buffers", "color");

            // input image is treated as a texture because the base image lacks the TEXTURE_USAGE_STORAGE_BIT flag
            var uniformInputImage  = new RDUniform();
            uniformInputImage.UniformType = RenderingDevice.UniformType.SamplerWithTexture;
            uniformInputImage.Binding = 0;
            uniformInputImage.AddId( sampler );
            uniformInputImage.AddId( inputImage );

            // image to be sent to TextureRect beacuse we can't write on base image (also flag issue)
            var uniformOutputTexture  = new RDUniform();
            uniformOutputTexture.UniformType = RenderingDevice.UniformType.Image;
            uniformOutputTexture.Binding = 1;
            uniformOutputTexture.AddId( outputTexture );

            var uniformSet  = UniformSetCacheRD.GetCache(shader, 0, new Godot.Collections.Array<RDUniform>(){uniformInputImage, uniformOutputTexture});
            if (!uniformSet.IsValid)
            {
                GD.Print("OUTLINE COMPOSITOR : Uniform set failed");
                return;
            }
            var computeList  = rd.ComputeListBegin();
            rd.ComputeListBindComputePipeline(computeList, pipeline);
            rd.ComputeListBindUniformSet(computeList, uniformSet, 0);
            rd.ComputeListSetPushConstant(computeList, pushConstantBytes, (uint) pushConstantBytes.Length );
            rd.ComputeListDispatch(computeList, (uint)groups.X, (uint)groups.Y, (uint)groups.Z);
            rd.ComputeListAddBarrier(computeList);
            rd.ComputeListEnd();
        }
    }
}
