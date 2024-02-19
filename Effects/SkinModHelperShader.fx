
// Compilation tool URL: https://github.com/lordseanington/ShaderCompiler/releases/

#define DECLARE_TEXTURE(Name, index) \
    texture Name: register(t##index); \
    sampler Name##Sampler: register(s##index) // Creates a texture at a given texture index

#define SAMPLE_TEXTURE(Name, texCoord) tex2D(Name##Sampler, texCoord) // Samples the texture and returns a color

DECLARE_TEXTURE(sprite, 0); //Declares "text" as the screen to be postprocessed
DECLARE_TEXTURE(colorgrade, 1);


float4 PS_Colorgrade(float4 inPosition : SV_Position, float4 spriteColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    // get sprite color
    float4 pixel = SAMPLE_TEXTURE(sprite, uv);
    
	float4 color = float4(0,0,0,0);
	if (pixel.a > 0.0) {
	   // unmultiply the alpha before the colorgrade, which is the whole damn reason this shader exists.
	   color = pixel * (1.0 / pixel.a);
	   
	   int x = int(color.r * 255.0) / 17;
	   int z = int(color.b * 255.0) / 17;
	   int y = int(color.g * 255.0) / 17;
	   
	   // extract the coordinates and lock them with +0.5.
	   float Y = (y + 0.5) / 16.0;
	   float XZ = (x + (z * 16) + 0.5) / 256.0;
	   
	   color = SAMPLE_TEXTURE(colorgrade, float2(XZ, Y));
	   
	   // don't forgot to multiply back in the alpha
	   color = color * pixel.a * spriteColor;
	}
	return color;
}

//-----------------------------------------------------------------------------
// Techniques.
//-----------------------------------------------------------------------------

technique ColorGradeSingle
{
    pass
    {
        PixelShader = compile ps_2_0 PS_Colorgrade();
    }
}