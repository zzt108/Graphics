
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/SpriteInput.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/CombinedShapeLightShared.hlsl"
#if defined(_NORMALMAP)
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/NormalsRenderingShared.hlsl"
#endif

struct Attributes
{
    float3 positionOS : POSITION;
    float4 color      : COLOR;
    float2 uv         : TEXCOORD0;
    #if defined(_NORMALMAP)
    float4 tangentOS  : TANGENT;
    #endif

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4  positionCS  : SV_POSITION;
    half4   color       : COLOR;
    float2	uv          : TEXCOORD0;
    half2	lightingUV  : TEXCOORD1;
    #if defined(_NORMALMAP)
    half3   normalWS    : TEXCOORD2;
    half3   tangentWS	: TEXCOORD3;
    half3   bitangentWS	: TEXCOORD4;
    #endif

    UNITY_VERTEX_OUTPUT_STEREO
};

////////////////////////////////////////////////////////////////////////////////
// Combined shape light...
////////////////////////////////////////////////////////////////////////////////
Varyings CombinedShapeLightVertex(Attributes v)
{
    Varyings o = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(v);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.positionCS = TransformObjectToHClip(v.positionOS);
    o.uv = TRANSFORM_TEX(v.uv, _MainTex);
    float4 clipVertex = o.positionCS / o.positionCS.w;
    o.lightingUV = ComputeScreenPos(clipVertex).xy;
    o.color = v.color;

    #if defined(_NORMALMAP)
    o.normalWS = TransformObjectToWorldDir(float3(0, 0, -1));
    // o.tangentWS = TransformObjectToWorldDir(attributes.tangentOS.xyz);
    // o.bitangentWS = cross(o.normalWS, o.tangentWS) * attributes.tangentOS.w;
    #endif

    return o;
}

half4 CombinedShapeLightFragment(Varyings i) : SV_Target
{
    half4 main = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, i.uv);
    #if defined(_NORMALMAP)
    half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));
    half3x3 tangentMatrixWS = half3x3(i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz);
    #else
    half3 normalTS = half3(0, 0, 1);
    half3x3 tangentMatrixWS = half3x3(half3(1, 0, 0), half3(0, 1, 0), half3(0, 0, 1));
    #endif

    return CombinedShapeLightShared(main, mask, i.lightingUV, tangentMatrixWS, normalTS);
}

////////////////////////////////////////////////////////////////////////////////
// Normals...
////////////////////////////////////////////////////////////////////////////////
#if defined(_NORMALMAP)
Varyings NormalsRenderingVertex(Attributes attributes)
{
    Varyings o = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(attributes);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.positionCS = TransformObjectToHClip(attributes.positionOS);
    o.uv = TRANSFORM_TEX(attributes.uv, _NormalMap);
    o.uv = attributes.uv;
    o.color = attributes.color;
    o.normalWS = TransformObjectToWorldDir(float3(0, 0, -1));
    o.tangentWS = TransformObjectToWorldDir(attributes.tangentOS.xyz);
    o.bitangentWS = cross(o.normalWS, o.tangentWS) * attributes.tangentOS.w;
    return o;
}

half4 NormalsRenderingFragment(Varyings i) : SV_Target
{
    half4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    half3 normalTS = UnpackNormal(SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, i.uv));
    return NormalsRenderingShared(mainTex, normalTS, i.tangentWS.xyz, i.bitangentWS.xyz, i.normalWS.xyz);
}
#endif

////////////////////////////////////////////////////////////////////////////////
// Unlit...
////////////////////////////////////////////////////////////////////////////////
Varyings UnlitVertex(Attributes attributes)
{
    Varyings o = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(attributes);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

    o.positionCS = TransformObjectToHClip(attributes.positionOS);
    o.uv = TRANSFORM_TEX(attributes.uv, _MainTex);
    o.uv = attributes.uv;
    o.color = attributes.color;
    return o;
}

float4 UnlitFragment(Varyings i) : SV_Target
{
    float4 mainTex = i.color * SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);
    return mainTex;
}
