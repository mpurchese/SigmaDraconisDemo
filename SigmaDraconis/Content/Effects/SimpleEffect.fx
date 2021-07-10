float4x4 xViewProjection;
Texture2D xTexture;
float3 xAmbientLight = float3(1, 1, 1);

sampler TextureSampler = sampler_state
{
	texture = <xTexture>;
	Filter=POINT;
};

struct VertexToPixel
{
	float2 TexCoord           : TEXCOORD0;
	float4 Position			  : POSITION;
};

struct VertexToPixelWithColour
{
	float2 TexCoord           : TEXCOORD0;
	float4 Position			  : POSITION;
	float4 Colour             : COLOR0;
};

VertexToPixel SimpleVertexShader(float4 inPos : POSITION0, float2 texCoord : TEXCOORD0)
{
	VertexToPixel Output = (VertexToPixel)0;

	Output.TexCoord = texCoord;
	Output.Position = mul(inPos, xViewProjection);

	return Output;
}

VertexToPixelWithColour SimpleVertexShaderWithColour(float4 inPos : POSITION0, float2 texCoord : TEXCOORD0, float4 vColor : COLOR0)
{
	VertexToPixelWithColour Output = (VertexToPixelWithColour)0;

	Output.TexCoord = texCoord;
	Output.Position = mul(inPos, xViewProjection);
	Output.Colour = vColor;

	return Output;
}

float4 SimplePixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex = tex2D(TextureSampler, PSIn.TexCoord);
	return tex;
}

float4 AlphaPixelShader(VertexToPixelWithColour PSIn) : COLOR0
{
	float4 tex = tex2D(TextureSampler, PSIn.TexCoord);
	return float4(tex.r * PSIn.Colour.a, tex.g * PSIn.Colour.a, tex.b * PSIn.Colour.a, tex.a * PSIn.Colour.a);
}

float4 InverseAlphaPixelShader(VertexToPixelWithColour PSIn) : COLOR0
{
	float4 tex = tex2D(TextureSampler, PSIn.TexCoord);
	//return float4(tex.r, tex.g, tex.b, min(0.9, 1.0 - tex.a));
	return float4(PSIn.Colour.r, PSIn.Colour.g, PSIn.Colour.b, min(PSIn.Colour.a, min(0.9, 1.0 - tex.a)));
}

float4 LitPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex = tex2D(TextureSampler, PSIn.TexCoord);
	float4 lightingColour = float4(xAmbientLight, 1);
	return tex * lightingColour;
}

float4 MonoPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex = tex2D(TextureSampler, PSIn.TexCoord);
	float shade = min(0.5, (tex.r + tex.g + tex.b) / 3.0);
	return float4(shade, shade, shade, tex.a);
}

float4 MonoDarkPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex = tex2D(TextureSampler, PSIn.TexCoord);
	float shade = min(0.5, (tex.r + tex.g + tex.b) / 5.0);
	return float4(shade, shade, shade, tex.a);
}

float4 MonoBrightPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex = tex2D(TextureSampler, PSIn.TexCoord);
	float shade = min(1.0, (tex.r + tex.g + tex.b) / 1.5);
	return float4(shade, shade, shade, tex.a);
}

technique SimpleTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShader();
		PixelShader = compile ps_4_0 SimplePixelShader();
	}
}

technique AlphaTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShaderWithColour();
		PixelShader = compile ps_4_0 AlphaPixelShader();
	}
}

// For terrain lighting
technique InverseAlphaTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShaderWithColour();
		PixelShader = compile ps_4_0 InverseAlphaPixelShader();
	}
}

technique LitTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShader();
		PixelShader = compile ps_4_0 LitPixelShader();
	}
}

technique MonoTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShader();
		PixelShader = compile ps_4_0 MonoPixelShader();
	}
}

technique MonoDarkTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShader();
		PixelShader = compile ps_4_0 MonoDarkPixelShader();
	}
}

technique MonoBrightTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 SimpleVertexShader();
		PixelShader = compile ps_4_0 MonoBrightPixelShader();
	}
}