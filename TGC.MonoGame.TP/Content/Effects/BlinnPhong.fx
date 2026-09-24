#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif
// ROBADO DE SAMPLES
float4x4 WorldViewProjection;
float4x4 World;
float4x4 InverseTransposeWorld;

// Support multiple lights (fixed-size array)
static const int NUM_LIGHTS = 4;

// Per-light colors
float3 lightAmbient[NUM_LIGHTS];
float3 lightDiffuse[NUM_LIGHTS];
float3 lightSpecular[NUM_LIGHTS];
float3 lightPosition[NUM_LIGHTS];
int lightCount;

// Material coefficients (single material for the object)
float KAmbient;
float KDiffuse;
float KSpecular;
float shininess;
float3 eyePosition; // Camera position

texture baseTexture;
sampler2D textureSampler = sampler_state
{
    Texture = (baseTexture);
    MagFilter = Linear;
    MinFilter = Linear;
    AddressU = Clamp;
    AddressV = Clamp;
};

struct VertexShaderInput
{
    float4 Position : POSITION0;
    float4 Normal : NORMAL;
    float2 TextureCoordinates : TEXCOORD0;
};

struct VertexShaderOutput
{
    float4 Position : SV_POSITION;
    float2 TextureCoordinates : TEXCOORD0;
    float4 WorldPosition : TEXCOORD1;
    float4 Normal : TEXCOORD2;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;

    output.Position = mul(input.Position, WorldViewProjection);
    output.WorldPosition = mul(input.Position, World);
    output.Normal = mul(input.Normal, InverseTransposeWorld);
    output.TextureCoordinates = input.TextureCoordinates;

    return output;
}

float4 MainPS(VertexShaderOutput input) : COLOR
{
    // Get the texture texel
    float4 texelColor = tex2D(textureSampler, input.TextureCoordinates);

    float3 totalAmbient = 0;
    float3 totalDiffuse = 0;
    float3 totalSpecular = 0;

    float3 viewDirection = normalize(eyePosition - input.WorldPosition.xyz);

    // Accumulate contribution from each active light
    for (int i = 0; i < lightCount; i++)
    {
        float3 lp = lightPosition[i];
        float3 lightDir = normalize(lp - input.WorldPosition.xyz);
        float3 halfVector = normalize(lightDir + viewDirection);

        float NdotL = saturate(dot(input.Normal.xyz, lightDir));
        totalAmbient += lightAmbient[i] * KAmbient;
        totalDiffuse += KDiffuse * lightDiffuse[i] * NdotL;

        float NdotH = dot(input.Normal.xyz, halfVector);
        totalSpecular += sign(NdotL) * KSpecular * lightSpecular[i] * pow(saturate(NdotH), shininess);
    }

    // Final calculation
    float3 lighting = saturate(totalAmbient + totalDiffuse) * texelColor.rgb + totalSpecular;
    float4 finalColor = float4(lighting, texelColor.a);
    return finalColor;

}

technique BasicColorDrawing
{
    pass Pass0
    {
        VertexShader = compile VS_SHADERMODEL MainVS();
        PixelShader = compile PS_SHADERMODEL MainPS();
    }
};