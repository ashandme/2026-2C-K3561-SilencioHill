#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define SV_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

texture baseTexture;
sampler2D textureSampler = sampler_state
{
    Texture = (baseTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

float time;
float intensity;
float distortion;

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TextureCoordinates : TEXCOORD0;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput)0;
    output.Position = input.Position;
    output.TextureCoordinates = input.TextureCoordinates;
    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    float2 uv = input.TextureCoordinates;
    uv.x += sin(uv.y * 6.0 + time * 1.5) * 0.004 * saturate(distortion);
    float4 color = tex2D(textureSampler, uv);
    float2 pixel = floor(input.TextureCoordinates * float2(1280.0, 720.0));
    float redNoise = frac(sin(dot(pixel + time * 17.0, float2(12.9898, 78.233))) * 43758.5453);
    float greenNoise = frac(sin(dot(pixel + time * 23.0, float2(39.3468, 11.1351))) * 24634.6345);
    float blueNoise = frac(sin(dot(pixel + time * 31.0, float2(73.156, 41.271))) * 15731.743);
    float3 randomColor = float3(redNoise, greenNoise, blueNoise);
    float scanline = 0.96 + 0.04 * sin(input.TextureCoordinates.y * 1400.0);
    float amount = saturate(intensity);
    color.rgb = lerp(color.rgb, color.rgb * (0.72 + randomColor * 0.56) * scanline, amount);
    return color;
}

technique Noise
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};
