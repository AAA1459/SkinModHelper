// ShaderCompiler SkinModHelperShader.fx SkinModHelperShader.cso
// Compilation tool URL: https://github.com/lordseanington/ShaderCompiler/releases/


#define DECLARE_TEXTURE(Name, index) \
    texture Name: register(t##index); \
    sampler Name##Sampler: register(s##index) // Creates a texture at a given texture index

#define SAMPLE_TEXTURE(Name, texCoord) tex2D(Name##Sampler, texCoord) // Samples the texture and returns a color

DECLARE_TEXTURE(sprite, 0); //Declares "text" as the screen to be postprocessed
DECLARE_TEXTURE(colorgrade, 1);

float4 haircolor;
int maskMode;


float4 DoCG_NoAlpha(float4 pixel : COLOR0) : COLOR0
{
    // unmultiply the alpha before the colorgrade, which is the whole damn reason this shader exists.
	float4 color = pixel * (1.0 / max(pixel.a, 1/256.0));
	   
	// Combine int conversion and +0.5 for rounding.
	int x = color.r * 15.0 + 0.5;
	int z = color.b * 15.0 + 0.5;
	int y = color.g * 15.0 + 0.5;
	   
	// extract the coordinates and lock them with +0.5, make sure it doesn't mix with adjacent colors.
	float Y = (y + 0.5) / 16.0;
	float XZ = (x + (z * 16) + 0.5) / 256.0;
	
	// don't forgot to multiply back in the alpha
	return SAMPLE_TEXTURE(colorgrade, float2(XZ, Y)) * pixel.a;
}
float4 MixHair(float4 pixel : COLOR0) : COLOR0
{
    float f = pixel.r + pixel.g + pixel.b;
	if (pixel.r * 3 == f && maskMode == 4) {
	   return pixel * haircolor;
	} 
    if ((f == pixel[maskMode])) {
	   return float4(f,f,f,pixel.a) * haircolor;
    }
	
    return pixel;
}



float4 PS_NoColorgrade(float4 inPosition : SV_Position, float4 spriteColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    return MixHair(SAMPLE_TEXTURE(sprite, uv)) * spriteColor;
}

float4 PS_Colorgrade(float4 inPosition : SV_Position, float4 spriteColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 pixel = SAMPLE_TEXTURE(sprite, uv);
	return MixHair(DoCG_NoAlpha(pixel)) * spriteColor;
}
float4 PS_ColorgradeAftColored(float4 inPosition : SV_Position, float4 spriteColor : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 pixel = MixHair(SAMPLE_TEXTURE(sprite, uv)) * spriteColor;
	return DoCG_NoAlpha(pixel);
}

//-----------------------------------------------------------------------------
// Techniques.
//-----------------------------------------------------------------------------

technique NoColorGrade
{
    pass
    {
        PixelShader = compile ps_2_0 PS_NoColorgrade();
    }
}
technique ColorGrade
{
    pass
    {
        PixelShader = compile ps_2_0 PS_Colorgrade();
    }
}
technique ColorGradeAftColored
{
    pass
    {
        PixelShader = compile ps_2_0 PS_ColorgradeAftColored();
    }
}