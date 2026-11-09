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
float fogEnabled;
float3 fogColor;
float fogStart;
float fogEnd;
float fogDensity;
float2 TextureScale;
float4x4 lightViewProjection;
float shadowsEnabled;
float2 shadowMapSize;
float alphaCutoff;
// Same bias constants as ShadowMap.fx (depth is stored as z/w by its DepthPass technique).
static const float modulatedEpsilon = 0.000041200182749889791011810302734375;
static const float maxEpsilon = 0.000023200045689009130001068115234375;

texture baseTexture;
sampler2D textureSampler : register(s0) = sampler_state
{
    Texture = (baseTexture);
        MagFilter = Point;
        MinFilter = Point;
        MipFilter = Point;
        AddressU = Wrap;
        AddressV = Wrap;
};

texture shadowMap;
sampler2D shadowSampler : register(s1) = sampler_state
{
    Texture = (shadowMap);
    MagFilter = Point;
    MinFilter = Point;
    MipFilter = Point;
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
    float4 ShadowPosition : TEXCOORD3;
};

VertexShaderOutput MainVS(in VertexShaderInput input)
{
    VertexShaderOutput output = (VertexShaderOutput) 0;

    output.Position = mul(input.Position, WorldViewProjection);
    output.WorldPosition = mul(input.Position, World);
    output.Normal = mul(input.Normal, InverseTransposeWorld);
    output.ShadowPosition = mul(output.WorldPosition, lightViewProjection);
    output.TextureCoordinates = input.TextureCoordinates * TextureScale;

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

	// CAMBIO
    float4 texelColor = tex2D(textureSampler, input.TextureCoordinates);
    clip(texelColor.a - alphaCutoff);
        
	// Calculate the diffuse light
    float NdotL = saturate(dot(normal, surfaceToLight));
    float upwardFactor = lerp(1.0, upwardLightScale, saturate(normal.y));
    float3 diffuseLight = KDiffuse * diffuseColor * NdotL * lightFactor * upwardFactor;

	// Calculate the specular light
    float NdotH = dot(normal, halfVector);
    float3 specularLight = sign(NdotL) * KSpecular * specularColor
        * pow(saturate(NdotH), shininess) * lightFactor * upwardFactor;

    // Shadow test, mirroring ShadowedPCFPS in ShadowMap.fx (3x3 PCF, slope-scaled bias).
    float3 shadowUv = input.ShadowPosition.xyz / max(input.ShadowPosition.w, 0.0001);
    shadowUv.xy = shadowUv.xy * 0.5 + 0.5;
    shadowUv.y = 1.0 - shadowUv.y;
    float notInShadow = 1.0;
    if (shadowsEnabled > 0.5 && input.ShadowPosition.w > 0.0
        && shadowUv.x >= 0.0 && shadowUv.x <= 1.0
        && shadowUv.y >= 0.0 && shadowUv.y <= 1.0)
    {
        float inclinationBias = max(modulatedEpsilon * (1.0 - dot(normal, normalize(toLight))), maxEpsilon);
        float2 texelSize = 1.0 / shadowMapSize;
        notInShadow = 0.0;
        for (int x = -1; x <= 1; x++)
            for (int y = -1; y <= 1; y++)
            {
                float pcfDepth = tex2D(shadowSampler, shadowUv.xy + float2(x, y) * texelSize).r + inclinationBias;
                notInShadow += step(shadowUv.z, pcfDepth) / 9.0;
            }
    }
    float shadowed = lerp(0.15, 1.0, notInShadow);
    diffuseLight *= shadowed;
    specularLight *= shadowed;
    
    // Final calculation
    float4 finalColor = float4(saturate(ambientColor * KAmbient + diffuseLight) * texelColor.rgb + specularLight, 1.0);
    float fogFactor = saturate((length(input.WorldPosition.xyz - eyePosition) - fogStart) / max(fogEnd - fogStart, 0.001));
    fogFactor = fogEnabled > 0.5 ? fogFactor : 0.0;
    finalColor.rgb = lerp(finalColor.rgb, fogColor, fogFactor);
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
