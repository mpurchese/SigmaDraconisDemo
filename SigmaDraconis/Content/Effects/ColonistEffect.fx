float4x4 xViewProjection;
texture xTexture1;
texture xTexture2;
texture xTextureColonistColour;
texture xTexturePackContentColour;
float4 xLightingDirectionFactors;
float xLightingFactorBlueness;
float xAlpha = 1;

sampler TextureSampler1 = sampler_state
{
	texture = <xTexture1>;
	Filter=POINT;
};
sampler TextureSampler2 = sampler_state
{
	texture = <xTexture2>;
	Filter=POINT;
};

sampler TextureSamplerColonistColour = sampler_state
{
	texture = <xTextureColonistColour>;
	Filter=POINT;
};
sampler TextureSamplerPackContentColour = sampler_state
{
	texture = <xTexturePackContentColour>;
	Filter=POINT;
};

struct VertexToPixel
{
	float2 TexCoord0          : TEXCOORD0;
	float2 TexCoord1          : TEXCOORD1;
	float2 TexCoord2          : TEXCOORD2;
	float4 Position			  : POSITION;
	float4 Colour0            : COLOR0;
	float4 Colour1            : COLOR1;
};

VertexToPixel MainVertexShader(float4 inPos : POSITION0, float4 vColor0 : COLOR0, float2 texCoord0 : TEXCOORD0, float2 texCoord1 : TEXCOORD1, float2 texCoord2 : TEXCOORD2)
{
	VertexToPixel Output = (VertexToPixel)0;

	float alpha = vColor0.a * xAlpha;

	float e = xLightingDirectionFactors.b;
	float w = xLightingDirectionFactors.r;
	float t = max(vColor0.r, xLightingDirectionFactors.g);

	float d = max(0, 1 - e - w - t);
	float blueness = d * xLightingFactorBlueness;

	Output.TexCoord0 = texCoord0;
	Output.TexCoord1 = texCoord1;
	Output.TexCoord2 = texCoord2;
	Output.Position = mul(inPos, xViewProjection);
	Output.Colour0 = float4(w, t, e, d);
	Output.Colour1 = float4(0, 0, blueness, alpha);

	return Output;
}

float4 MainPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex1 = tex2D(TextureSampler1, PSIn.TexCoord0);
	float4 tex2 = tex2D(TextureSampler2, PSIn.TexCoord0);
	float4 colonistColour = tex2D(TextureSamplerColonistColour, PSIn.TexCoord1);
	float4 packContentColour = tex2D(TextureSamplerPackContentColour, PSIn.TexCoord2);

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

	// b is the backpack colour
	float outR = (g * 2 * colonistColour.r) + (r * 2 * packContentColour.r) + (b * 0.28);
	float outG = (g * 2 * colonistColour.g) + (r * 2 * packContentColour.g) + (b * 0.22);
	float outB = (g * 2 * colonistColour.b) + (r * 2 * packContentColour.b) + (b * 0.20);

	return float4(outR, outG, outB, a);
}

float4 HeadPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex1 = tex2D(TextureSampler1, PSIn.TexCoord0);
	float4 tex2 = tex2D(TextureSampler2, PSIn.TexCoord0);
	float4 colonistColour = tex2D(TextureSamplerColonistColour, PSIn.TexCoord1);

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

	// b is the backpack colour
	float outR = (g * 2 * colonistColour.r) + (b * 0.28);
	float outG = (g * 2 * colonistColour.g) + (b * 0.22);
	float outB = (g * 2 * colonistColour.b) + (b * 0.20);

	return float4(outR, outG, outB, a);
}

technique MainTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 MainVertexShader();
		PixelShader = compile ps_4_0 MainPixelShader();
	}
}

technique HeadTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 MainVertexShader();
		PixelShader = compile ps_4_0 HeadPixelShader();
	}
}