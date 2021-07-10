float4x4 xViewProjection;
texture xTexture;
float4 xColour;

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

VertexToPixel SimpleVertexShader(float4 inPos : POSITION0, float2 texCoord : TEXCOORD0, float4 vColor : COLOR0)
{
	VertexToPixel Output = (VertexToPixel)0;

	Output.TexCoord = texCoord;
	Output.Position = mul(inPos, xViewProjection);
	Output.Colour = vColor;

	return Output;
}

float4 SimplePixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex = tex2D(TextureSampler, PSIn.TexCoord);
	return float4(PSIn.Colour.r * tex.a * xColour.r, PSIn.Colour.g * tex.a * xColour.g, PSIn.Colour.b * tex.a * xColour.b, PSIn.Colour.a * tex.a * xColour.a);
}

float4 LitPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex = tex2D(TextureSampler, PSIn.TexCoord);

	float brightness = PSIn.Colour.r;
	float lamplight = PSIn.Colour.g;
	float r = brightness * (xColour.r + ((1.0 - xColour.r) * lamplight));
	float g = brightness * (xColour.g + ((1.0 - xColour.g) * lamplight));
	float b = brightness * (xColour.b + ((1.0 - xColour.b) * lamplight));

	return float4(r * tex.a, g * tex.a, b * tex.a, PSIn.Colour.a * tex.a);
}

technique LitTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShader();
		PixelShader = compile ps_4_0 LitPixelShader();
	}
}

technique SimpleTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShader();
		PixelShader = compile ps_4_0 SimplePixelShader();
	}
}