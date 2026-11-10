#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

// Ghost bloom, four techniques used in order each frame:
//   Occluder  - level geometry drawn black so walls hide the glow (depth only, in practice)
//   BloomPass - the ghost drawn in a solid glow color, faded by fog
//   Blur      - separable 9-tap gaussian, run horizontally then vertically
//   Integrate - scene + blurred glow (additive)

float4x4 WorldViewProjection;

float3 bloomColor;
float fogEnabled;
float fogStart;
float fogEnd;

float2 blurDirection; // texel size * direction (* spacing)
float bloomIntensity;

texture baseTexture;
sampler2D textureSampler = sampler_state
{
    Texture = (baseTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

texture bloomTexture;
sampler2D bloomTextureSampler = sampler_state
{
    Texture = (bloomTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

// --- Model passes (occluders and ghost) ---

struct ModelInput
{
    float4 Position : POSITION0;
};

struct ModelOutput
{
    float4 Position : SV_POSITION;
    float Depth : TEXCOORD0;
};

ModelOutput ModelVS(in ModelInput input)
{
    ModelOutput output = (ModelOutput) 0;
    output.Position = mul(input.Position, WorldViewProjection);
    // Clip-space w is the view-space depth; close enough to the eye distance BlinnPhong fogs with.
    output.Depth = output.Position.w;
    return output;
}

float4 OccluderPS(ModelOutput input) : COLOR
{
    return float4(0, 0, 0, 1);
}

float4 BloomPS(ModelOutput input) : COLOR
{
    float fogFactor = saturate((input.Depth - fogStart) / max(fogEnd - fogStart, 0.001));
    fogFactor = fogEnabled > 0.5 ? fogFactor : 0.0;
    return float4(bloomColor * (1.0 - fogFactor), 1);
}

// --- Full screen passes ---

struct PostProcessInput
{
    float4 Position : POSITION0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct PostProcessOutput
{
    float4 Position : SV_POSITION;
    float2 TextureCoordinates : TEXCOORD0;
};

PostProcessOutput PostProcessVS(in PostProcessInput input)
{
    PostProcessOutput output;
    output.Position = input.Position;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float4 BlurPS(in PostProcessOutput input) : COLOR
{
    float2 uv = input.TextureCoordinates;
    float3 color = tex2D(textureSampler, uv).rgb * 0.2270270270;
    color += tex2D(textureSampler, uv + blurDirection * 1.0).rgb * 0.1945945946;
    color += tex2D(textureSampler, uv - blurDirection * 1.0).rgb * 0.1945945946;
    color += tex2D(textureSampler, uv + blurDirection * 2.0).rgb * 0.1216216216;
    color += tex2D(textureSampler, uv - blurDirection * 2.0).rgb * 0.1216216216;
    color += tex2D(textureSampler, uv + blurDirection * 3.0).rgb * 0.0540540541;
    color += tex2D(textureSampler, uv - blurDirection * 3.0).rgb * 0.0540540541;
    color += tex2D(textureSampler, uv + blurDirection * 4.0).rgb * 0.0162162162;
    color += tex2D(textureSampler, uv - blurDirection * 4.0).rgb * 0.0162162162;
    return float4(color, 1);
}

float4 BloomIntegratePS(in PostProcessOutput input) : COLOR
{
    float3 bloom = tex2D(bloomTextureSampler, input.TextureCoordinates).rgb;
    float3 scene = tex2D(textureSampler, input.TextureCoordinates).rgb;
    return float4(saturate(scene + bloom * bloomIntensity), 1);
}

technique Occluder
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL ModelVS();
        PixelShader = compile PS_SHADERMODEL OccluderPS();
    }
};

technique BloomPass
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL ModelVS();
        PixelShader = compile PS_SHADERMODEL BloomPS();
    }
};

technique Blur
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL PostProcessVS();
        PixelShader = compile PS_SHADERMODEL BlurPS();
    }
};

technique Integrate
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL PostProcessVS();
        PixelShader = compile PS_SHADERMODEL BloomIntegratePS();
    }
};
