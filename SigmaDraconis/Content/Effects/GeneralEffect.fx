float4x4 xViewProjection;
texture xTexture1;
texture xTexture2;
float4 xLightingDirectionFactors;
float xLightingFactorBlueness;
float xAlpha = 1;
float xWarpPhase = 1;
float xWarpAmplitude = 0.016;

sampler TextureSampler1 = sampler_state
{
	texture = <xTexture1>;
	Filter=POINT;
};
sampler TextureSampler1a = sampler_state
{
	texture = <xTexture1>;
	Filter=LINEAR;
};
sampler TextureSampler2 = sampler_state
{
	texture = <xTexture2>;
	Filter=POINT;
};

struct VertexToPixel
{
	float2 TexCoord           : TEXCOORD0;
	float4 Position			  : POSITION;
	float4 Colour0            : COLOR0;
	float4 Colour1            : COLOR1;
};

VertexToPixel MainVertexShader(float4 inPos : POSITION0, float4 vColor0 : COLOR0, float2 texCoord : TEXCOORD0)
{
	VertexToPixel Output = (VertexToPixel)0;

	float alpha = vColor0.a * xAlpha;

	float e = xLightingDirectionFactors.b;
	float w = xLightingDirectionFactors.r;
	float t = max(vColor0.r, xLightingDirectionFactors.g);

	float d1 = max(0, 1 - (2 * (e + w + t)));
	float d2 = max(0, 1 - e - w - t);
	float blueness = d1 * xLightingFactorBlueness * vColor0.b;

	Output.TexCoord = texCoord;
	Output.Position = mul(inPos, xViewProjection);
	Output.Colour0 = float4(w, t, e, d2);
	Output.Colour1 = float4(0, vColor0.g, blueness, alpha);

	return Output;
}

float4 MainPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex1 = tex2D(TextureSampler1, PSIn.TexCoord);
	float4 tex2 = tex2D(TextureSampler2, PSIn.TexCoord);

	float w = PSIn.Colour0.r;
	float t = PSIn.Colour0.g;
	float e = PSIn.Colour0.b;
	float d = PSIn.Colour0.a;

	float adjR = 1 - (PSIn.Colour1.b * 0.3);
	float adjB = 1 + (PSIn.Colour1.b * 0.4);

	float br = ((w * tex2.r) + (t * tex2.g) + (e * tex2.b) + (d * tex2.a)) * PSIn.Colour1.a;

	float r = br * tex1.r * adjR;
	float g = br * tex1.g;
	float b = br * tex1.b * adjB;
	float a = tex1.a * PSIn.Colour1.a;

	return float4(r, g, b, a);
}

float4 StackingAreaPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex1 = tex2D(TextureSampler1, PSIn.TexCoord);
	float4 tex2 = tex2D(TextureSampler2, PSIn.TexCoord);

	float w = PSIn.Colour0.r;
	float t = PSIn.Colour0.g;
	float e = PSIn.Colour0.b;
	float d = PSIn.Colour0.a;

	float br = ((w * tex2.r) + (t * tex2.g) + (e * tex2.b) + (d * tex2.a)) * PSIn.Colour1.a;

	float r = br * tex1.r;
	float g = br * tex1.g;
	float b = br * tex1.b;
	float a = tex1.a * PSIn.Colour1.a;

	return float4(r, g, b, a);
}

float4 LinearFilterPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex1 = tex2D(TextureSampler1a, PSIn.TexCoord);
	float4 tex2 = tex2D(TextureSampler2, PSIn.TexCoord);

	float w = PSIn.Colour0.r;
	float t = PSIn.Colour0.g;
	float e = PSIn.Colour0.b;
	float d = PSIn.Colour0.a;

	float adjR = 1 - (PSIn.Colour1.b * 0.3);
	float adjB = 1 + (PSIn.Colour1.b * 0.4);

	float br = ((w * tex2.r) + (t * tex2.g) + (e * tex2.b) + (d * tex2.a)) * PSIn.Colour1.a;

	float r = br * tex1.r * adjR;
	float g = br * tex1.g;
	float b = br * tex1.b * adjB;
	float a = tex1.a * PSIn.Colour1.a;

	return float4(r, g, b, a);
}

float4 TerrainPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex1 = tex2D(TextureSampler1, PSIn.TexCoord);
	float4 tex2 = tex2D(TextureSampler2, PSIn.TexCoord);

	float w = PSIn.Colour0.r;
	float t = PSIn.Colour0.g;
	float e = PSIn.Colour0.b;
	float d = PSIn.Colour0.a;

	float adjR = 1 - (PSIn.Colour1.b * 0.3);
	float adjB = 1 + (PSIn.Colour1.b * 0.4);
	
	float depth = max(1.0 - tex2.a, 0);
	float br = ((w * tex2.r) + (t * tex2.g) + (e * tex2.b)) * (1 - depth) * PSIn.Colour1.a;

	float r = (br * tex1.r * adjR) + (depth * 0.2) * tex1.a;
	float g = (br * tex1.g) + (depth * 0.15) * tex1.a;
	float b = (br * tex1.b * adjB) + (depth * 0.1) * tex1.a;
	float a = tex1.a * PSIn.Colour1.a;

	return float4(r, g, b, a);
}

float4 WarpPixelShader(VertexToPixel PSIn) : COLOR0
{
	float phase = sin(xWarpPhase + (PSIn.Colour1.g * 6.283));
	float warpFactorX = 1 + (phase * xWarpAmplitude);
	float warpFactorY = 1 - (phase * xWarpAmplitude * 0.5);
	float2 tc = float2(0.5 + (warpFactorX * (PSIn.TexCoord.x - 0.5)), 0.5 + (warpFactorY * (PSIn.TexCoord.y - 0.5)));

	float4 tex1 = tex2D(TextureSampler1, tc);
	float4 tex2 = tex2D(TextureSampler2, tc);

	float w = PSIn.Colour0.r;
	float t = PSIn.Colour0.g;
	float e = PSIn.Colour0.b;
	float d = PSIn.Colour0.a;

	float adjR = 1 - (PSIn.Colour1.b * 0.3);
	float adjB = 1 + (PSIn.Colour1.b * 0.4);

	float br = (w * tex2.r) + (t * tex2.g) + (e * tex2.b) + (d * tex2.a);

	float r = br * tex1.r * adjR * PSIn.Colour1.a;
	float g = br * tex1.g * PSIn.Colour1.a;
	float b = br * tex1.b * adjB * PSIn.Colour1.a;
	float a = tex1.a * PSIn.Colour1.a;

	return float4(r, g, b, a);
}

technique MainTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 MainVertexShader();
		PixelShader = compile ps_4_0 MainPixelShader();
	}
}

technique StackingAreaTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 MainVertexShader();
		PixelShader = compile ps_4_0 StackingAreaPixelShader();
	}
}

technique LinearFilterTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 MainVertexShader();
		PixelShader = compile ps_4_0 LinearFilterPixelShader();
	}
}

technique TerrainTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 MainVertexShader();
		PixelShader = compile ps_4_0 TerrainPixelShader();
	}
}

technique WarpTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 MainVertexShader();
		PixelShader = compile ps_4_0 WarpPixelShader();
	}
}