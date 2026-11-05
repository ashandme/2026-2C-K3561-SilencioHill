#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

float4x4 WorldViewProjection;
float4x4 World;
float4x4 InverseTransposeWorld;

float3 ambientColor; // Light's Ambient Color
float3 diffuseColor; // Light's Diffuse Color
float3 specularColor; // Light's Specular Color
float KAmbient;
float KDiffuse;
float KSpecular;
float shininess;
float3 lightPosition;
float3 lightDirection; // Direction from a surface toward a directional light
float lightIsDirectional;
float lightIsSpotlight;
float spotInnerCosine;
float spotOuterCosine;
float attenuationConstant;
float attenuationLinear;
float attenuationQuadratic;
float upwardLightScale;
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
    // Base vectors
    float3 normal = normalize(input.Normal.xyz);
    float3 toLight = lightPosition - input.WorldPosition.xyz;
    float lightDistance = length(toLight);
    float3 surfaceToLight = lightIsDirectional > 0.5
        ? normalize(lightDirection)
        : normalize(toLight);
    float lightFactor = 1.0;

    if (lightIsDirectional <= 0.5)
    {
        lightFactor = 1.0 / max(
            attenuationConstant
                + attenuationLinear * lightDistance
                + attenuationQuadratic * lightDistance * lightDistance,
            0.001);

        if (lightIsSpotlight > 0.5)
        {
            float3 lightToSurface = -surfaceToLight;
            float spotCosine = dot(lightToSurface, normalize(lightDirection));
            float spotFactor = saturate(
                (spotCosine - spotOuterCosine)
                / max(spotInnerCosine - spotOuterCosine, 0.001));
            lightFactor *= spotFactor;
        }
    }

    float3 viewDirection = normalize(eyePosition - input.WorldPosition.xyz);
    float3 halfVector = normalize(surfaceToLight + viewDirection);

	// Get the texture texel
    float4 texelColor = tex2D(textureSampler, input.TextureCoordinates);

	// Calculate the diffuse light
    float NdotL = saturate(dot(normal, surfaceToLight));
    float upwardFactor = lerp(1.0, upwardLightScale, saturate(normal.y));
    float3 diffuseLight = KDiffuse * diffuseColor * NdotL * lightFactor * upwardFactor;

	// Calculate the specular light
    float NdotH = dot(normal, halfVector);
    float3 specularLight = sign(NdotL) * KSpecular * specularColor
        * pow(saturate(NdotH), shininess) * lightFactor * upwardFactor;

    // Final calculation
    float4 finalColor = float4(saturate(ambientColor * KAmbient + diffuseLight) * texelColor.rgb + specularLight, 1.0);
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
