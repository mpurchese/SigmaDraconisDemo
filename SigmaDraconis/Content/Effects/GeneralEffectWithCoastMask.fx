float4x4 xViewProjection;
texture xTexture1;
texture xTexture2;
texture xTextureMask;
float4 xLightingDirectionFactors;
float xLightingFactorBlueness;
float xAlpha = 1;

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
sampler TextureSampler3 = sampler_state
{
	texture = <xTextureMask>;
	Filter=POINT;
};

struct VertexToPixel
{
	float2 TexCoord0          : TEXCOORD0;
	float2 TexCoord1          : TEXCOORD1;
	float4 Position			  : POSITION;
	float4 Colour0            : COLOR0;
	float4 Colour1            : COLOR1;
};

VertexToPixel MainVertexShader(float4 inPos : POSITION0, float4 vColor0 : COLOR0, float2 texCoord0 : TEXCOORD0, float2 texCoord1 : TEXCOORD1)
{
	VertexToPixel Output = (VertexToPixel)0;

	float alpha = vColor0.a * xAlpha;

	float e = xLightingDirectionFactors.b;
	float w = xLightingDirectionFactors.r;
	float t = max(vColor0.r, xLightingDirectionFactors.g);

	float d1 = max(0, 1 - (2 * (e + w + t)));
	float d2 = max(0, 1 - e - w - t);
	float blueness = d1 * xLightingFactorBlueness * vColor0.b;

	Output.TexCoord0 = texCoord0;
	Output.TexCoord1 = texCoord1;
	Output.Position = mul(inPos, xViewProjection);
	Output.Colour0 = float4(w, t, e, d2);
	Output.Colour1 = float4(0, vColor0.g, blueness, alpha);

	return Output;
}

float4 MainPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex1 = tex2D(TextureSampler1, PSIn.TexCoord0);
	float4 tex2 = tex2D(TextureSampler2, PSIn.TexCoord0);
	float4 tex3 = tex2D(TextureSampler3, PSIn.TexCoord1);

	float w = PSIn.Colour0.r;
	float t = PSIn.Colour0.g;
	float e = PSIn.Colour0.b;
	float d = PSIn.Colour0.a;

	float adjR = 1 - (PSIn.Colour1.b * 0.3);
	float adjB = 1 + (PSIn.Colour1.b * 0.4);

	float br = ((w * min(tex2.r, tex3.r)) + (t * min(tex2.g, tex3.g)) + (e * min(tex2.b, tex3.b)) + (d * min(tex2.a, tex3.a))) * PSIn.Colour1.a;

	float r = br * tex1.r * adjR * tex3.a;
	float g = br * tex1.g * tex3.a;
	float b = br * tex1.b * adjB * tex3.a;
	float a = tex1.a * tex3.a * PSIn.Colour1.a;

	return float4(r, g, b, a);
}

float4 LinearFilterPixelShader(VertexToPixel PSIn) : COLOR0
{
	float4 tex1 = tex2D(TextureSampler1a, PSIn.TexCoord0);
	float4 tex2 = tex2D(TextureSampler2, PSIn.TexCoord0);
	float4 tex3 = tex2D(TextureSampler3, PSIn.TexCoord1);

	float w = PSIn.Colour0.r;
	float t = PSIn.Colour0.g;
	float e = PSIn.Colour0.b;
	float d = PSIn.Colour0.a;

	float adjR = 1 - (PSIn.Colour1.b * 0.3);
	float adjB = 1 + (PSIn.Colour1.b * 0.4);

	float br = ((w * min(tex2.r, tex3.r)) + (t * min(tex2.g, tex3.g)) + (e * min(tex2.b, tex3.b)) + (d * min(tex2.a, tex3.a))) * PSIn.Colour1.a;

	float r = br * tex1.r * adjR * tex3.a;
	float g = br * tex1.g * tex3.a;
	float b = br * tex1.b * adjB * tex3.a;
	float a = tex1.a * tex3.a * PSIn.Colour1.a;

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

technique LinearFilterTechnique
{
	pass Pass1
	{
		VertexShader = compile vs_4_0 MainVertexShader();
		PixelShader = compile ps_4_0 LinearFilterPixelShader();
	}
}