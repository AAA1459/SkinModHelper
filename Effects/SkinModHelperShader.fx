
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
	   
	   // Convert z into integers to make sure it's remainders not confusing x.
	   int z = color.b * 15.0;
	   float x = color.r;
	   float xz = (x + z) / 16.0;
	   float y = color.g;
	   
	   color = SAMPLE_TEXTURE(colorgrade, float2(xz, y)) * spriteColor;
	   
	   // don't forgot to multiply back in the alpha
	   color = color * pixel.a;
	}
	return color;
}

//-----------------------------------------------------------------------------
// Techniques.
//-----------------------------------------------------------------------------

technique ColorGrade
{
    pass
    {
        PixelShader = compile ps_2_0 PS_Colorgrade();
    }
}