float4x4 xViewProjection;
texture xNightTexture;
float4 xLightingFactors = float4(1, 1, 1, 1);
float xWarpPhase = 1;
float xWarpAmplitude = 0.016;

sampler NightTextureSampler = sampler_state
{
	texture = <xNightTexture>;
	//Filter=POINT;
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

float4 BlueprintPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex = tex2D(NightTextureSampler, PSIn.TexCoord);
	float alpha = tex.a * PSIn.Colour.a * xLightingFactors.a;
	return float4(PSIn.Colour.r * alpha, 0, PSIn.Colour.b * alpha, alpha);
}

float4 TemperaturePixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex = tex2D(NightTextureSampler, PSIn.TexCoord);
	float alpha = tex.a * PSIn.Colour.a * xLightingFactors.a;
	return float4(PSIn.Colour.r * alpha, PSIn.Colour.g * alpha, PSIn.Colour.b * alpha, alpha);
}

float4 BlueprintWarpPixelShader(VertexToPixel PSIn) : COLOR0
{
	float phase = sin(xWarpPhase + (PSIn.Colour.g * 6.283));
	float warpFactorX = 1 + (phase * xWarpAmplitude);
	float warpFactorY = 1 - (phase * xWarpAmplitude * 0.5);
	float2 tc = float2(0.5 + (warpFactorX * (PSIn.TexCoord.x - 0.5)), 0.5 + (warpFactorY * (PSIn.TexCoord.y - 0.5)));

	float4 tex = tex2D(NightTextureSampler, tc);
	float alpha = tex.a * PSIn.Colour.a * xLightingFactors.a;
	return float4(PSIn.Colour.r * alpha, 0, PSIn.Colour.b * alpha, alpha);
}

technique BlueprintTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShader();
		PixelShader = compile ps_4_0 BlueprintPixelShader();
	}
}

technique TemperatureTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShader();
		PixelShader = compile ps_4_0 TemperaturePixelShader();
	}
}

technique BlueprintWarpTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShader();
		PixelShader = compile ps_4_0 BlueprintWarpPixelShader();
	}
}