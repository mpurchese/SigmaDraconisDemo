float4x4 xViewProjection;
texture xTexture;
float3 xSunVector;

sampler TextureSampler = sampler_state
{
	texture = <xTexture>;
	Filter = Anisotropic;
	MaxAnisotropy = 8;
};

struct VertexToPixel
{
	float2 TexCoord           : TEXCOORD0;
	float4 Position			  : POSITION;
	float4 Colour             : COLOR0;
};

VertexToPixel SimpleVertexShader(float4 inPos : POSITION0, float2 texCoord : TEXCOORD0, float4 vColor : COLOR0)
{
	VertexToPixel Output = (VertexToPixel)0;

	// Project shadows
	float ty = tan(xSunVector.y);
    float sx = sin(xSunVector.x) / ty;
    float sy = 0.5 * sin(xSunVector.z) / ty;
	float4 outPos = float4(inPos.x - (inPos.z * sx), inPos.y + (inPos.z * sy), 0, 1); 

	Output.TexCoord = texCoord;
	Output.Position = mul(outPos, xViewProjection);
	Output.Colour = vColor;

	return Output;
}

VertexToPixel PoleVertexShader(float4 inPos : POSITION0, float2 texCoord : TEXCOORD0, float4 vColor : COLOR0)
{
	VertexToPixel Output = (VertexToPixel)0;

	// Use vColor.r for the pole width
	float w = (vColor.r - 0.5) * 50;

	// Normalise sun XY
	float n = sqrt((xSunVector.x * xSunVector.x) + (xSunVector.y * xSunVector.y));
	float xn = xSunVector.x * n;
	float yn = xSunVector.y * n;

	// Project shadows
	float ty = tan(xSunVector.y);
    float sx = sin(xSunVector.x) / ty;
    float sy = 0.5 * sin(xSunVector.z) / ty;
	float4 outPos = float4(inPos.x + (w * yn) - (inPos.z * sx), inPos.y + (inPos.z * sy) - (w * xn * 0.5), 0, 1); 

	Output.TexCoord = texCoord;
	Output.Position = mul(outPos, xViewProjection);
	Output.Colour = vColor;

	return Output;
}

float4 SimplePixelShader(VertexToPixel PSIn) : COLOR0
{
	return float4(0, 0, 0, PSIn.Colour.a);
}

float4 TexturedPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex = tex2D(TextureSampler, PSIn.TexCoord);
	return float4(0, 0, 0, tex.a * PSIn.Colour.a);
}

technique SimpleShadowTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShader();
		PixelShader = compile ps_4_0 SimplePixelShader();
	}
}

technique PoleShadowTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 PoleVertexShader();
		PixelShader = compile ps_4_0 SimplePixelShader();
	}
}

technique TreeTrunkShadowTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 PoleVertexShader();
		PixelShader = compile ps_4_0 TexturedPixelShader();
	}
}

technique TexturedShadowTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShader();
		PixelShader = compile ps_4_0 TexturedPixelShader();
	}
}

