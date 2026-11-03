#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 View;
float4x4 Projection;
float4 Tint;
float Brightness;
texture SkyTexture;
sampler2D SkySampler = sampler_state
{
    Texture = (SkyTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float3 Direction : TEXCOORD0;
};

VertexShaderOutput GradientVS(VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.Direction = input.Position.xyz;
    output.Position = mul(float4(input.Position.xyz * 10000.0, 1.0), View);
    output.Position = mul(output.Position, Projection);
    output.Position.z = output.Position.w;
    return output;
}

float4 GradientPS(VertexShaderOutput input) : COLOR0
{
    float3 direction = normalize(input.Direction);
    float v = 1.0 - saturate(direction.y);
    float4 color = tex2D(SkySampler, float2(0.5, v));
    return color * Tint * Brightness;
}

technique Gradient
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL GradientVS();
        PixelShader = compile PS_SHADERMODEL GradientPS();
    }
}
