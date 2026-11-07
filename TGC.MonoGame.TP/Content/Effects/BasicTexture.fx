#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// ROBADO DE SAMPLES
float4x4 World;
float4x4 View;
float4x4 Projection;
float2 TextureScale;
float3 eyePosition;
float fogEnabled;
float3 fogColor;
float fogStart;
float fogEnd;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TextureCoordinate : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TextureCoordinate : TEXCOORD1;
    float3 WorldPosition : TEXCOORD2;
};

texture ModelTexture;
sampler2D textureSampler = sampler_state
{
    Texture = (ModelTexture);
    MagFilter = Point;
    MinFilter = Linear;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;

    float4 worldPosition = mul(input.Position, World);
    float4 viewPosition = mul(worldPosition, View);

	// Project position
    output.Position = mul(viewPosition, Projection);

	// Propagate texture coordinates
    output.TextureCoordinate = input.TextureCoordinate * TextureScale;
    output.WorldPosition = worldPosition.xyz;

    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
	// Obtener el color y alfa del texel
    float4 textureColor = tex2D(textureSampler, input.TextureCoordinate);

    // DESCARTAR PÍXEL SI ES TRANSPARENTE:
    // Si textureColor.a < 0.5, el argumento es negativo y la GPU descarta el píxel
    // sin escribir nada en el Depth Buffer (Z-Buffer).
    clip(textureColor.a - 0.5f);

    float fogFactor = saturate((length(input.WorldPosition - eyePosition) - fogStart) / max(fogEnd - fogStart, 0.001));
    textureColor.rgb = lerp(textureColor.rgb, fogColor, fogEnabled > 0.5 ? fogFactor : 0.0);
    return textureColor;
}

technique BasicDrawing
{
    pass P0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};