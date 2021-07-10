float4x4 xViewProjection;
texture xTexture;
float xGlobalAlpha = 1;

sampler TextureSampler = sampler_state
{
	texture = <xTexture>;
};

struct VertexToPixel
{
	float2 TexCoord           : TEXCOORD0;
	float4 Position			  : POSITION;
	float4 Colour             : COLOR0;
};

VertexToPixel MainVertexShader(float4 inPos : POSITION0, float2 texCoord : TEXCOORD0, float4 vColor : COLOR0)
{
	VertexToPixel Output = (VertexToPixel)0;

	Output.TexCoord = texCoord;
	Output.Position = mul(inPos, xViewProjection);
	Output.Colour = vColor;

	return Output;
}

float4 MainPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex = tex2D(TextureSampler, PSIn.TexCoord);
	float alpha = tex.a * PSIn.Colour.a * xGlobalAlpha;
	return float4(alpha * PSIn.Colour.rgb, alpha);
}

technique MainTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 MainVertexShader();
		PixelShader = compile ps_4_0 MainPixelShader();
	}
}
