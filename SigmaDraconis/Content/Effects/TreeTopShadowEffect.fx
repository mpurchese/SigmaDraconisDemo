float4x4 xViewProjection;
texture xTexture;
float xWarpPhase = 1;
float xWarpAmplitude = 0.016;
float3 xSunVector;

sampler TextureSampler = sampler_state
{
	texture = <xTexture>;
	Filter=POINT;
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
	Output.Colour = vColor;
	Output.Position = mul(outPos, xViewProjection);

	return Output;
}

float4 Main(VertexToPixel PSIn) : COLOR0
{
	float phase = sin(xWarpPhase + (PSIn.Colour.g * 6.283));
	float warpFactorX = 1 + (phase * xWarpAmplitude);
	float warpFactorY = 1 - (phase * xWarpAmplitude * 0.5);
	float2 tc = float2(0.5 + (warpFactorX * (PSIn.TexCoord.x - 0.5)), 0.5 + (warpFactorY * (PSIn.TexCoord.y - 0.5)));

	float4 tex = tex2D(TextureSampler, tc);
	return float4(0, 0, 0, tex.a * PSIn.Colour.a);
}

technique Technique1
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShader();
		PixelShader = compile ps_4_0 Main();
	}
}
