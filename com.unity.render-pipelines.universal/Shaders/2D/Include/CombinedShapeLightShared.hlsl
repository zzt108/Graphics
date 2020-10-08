
#ifndef COMBINED_SHAPE_LIGHT_PASS
#define COMBINED_SHAPE_LIGHT_PASS

#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/Debugging2D.hlsl"

half _HDREmulationScale;
half _UseSceneLighting;
half4 _RendererColor;

void UpdateShapeLight(half4 maskFilter, half4 invertedFilter, half4 mask, half4 shapeLight, half2 blendFactors,
    inout half4 finalModulate, inout half4 finalAdditive)
{
    if (any(maskFilter))
    {
        half4 processedMask = (1 - invertedFilter) * mask + invertedFilter * (1 - mask);
        shapeLight *= dot(processedMask, maskFilter);
    }

    half4 modulate = shapeLight * blendFactors.x;
    half4 additive = shapeLight * blendFactors.y;

    finalModulate += modulate;
    finalAdditive += additive;
}

half4 CalculateFinalColor(half3 initialColor, half3 finalColor, half3x3 tangentMatrixWS, half3 normalTS, half alpha)
{
    #if defined(_DEBUG_SHADER)
    if(_DebugMaterialIndex != DEBUG_MATERIAL_NONE)
    {
        switch(_DebugMaterialIndex)
        {
            case DEBUG_MATERIAL_UNLIT:
            case DEBUG_MATERIAL_DIFFUSE:
                return half4(initialColor, 1);

            case DEBUG_MATERIAL_ALPHA:
                return half4(alpha, alpha, alpha, 1);

            case DEBUG_MATERIAL_NORMAL_WORLD_SPACE:
                half3 normalWS = TransformTangentToWorld(normalTS, tangentMatrixWS);

                return half4((normalWS * 0.5) + 0.5, 1);

            case DEBUG_MATERIAL_NORMAL_TANGENT_SPACE:
                return half4((normalTS * 0.5) + 0.5, 1);

            default:
                return half4(0, 0, 0, 1);
        }
    }
    #endif

    finalColor = lerp(initialColor, finalColor, _UseSceneLighting);
    return half4(finalColor, alpha);
}

half4 CombinedShapeLightShared(half4 color, half4 mask, half2 lightingUV, half3x3 tangentMatrixWS, half3 normalTS)
{
	if (color.a == 0.0)
		discard;

    color = color * _RendererColor; // This is needed for sprite shape

    half4 finalModulate = half4(0, 0, 0, 0);
    half4 finalAdditive = half4(0, 0, 0, 0);
    half hdrEmulationScale = _HDREmulationScale;
    int numShapeLights = 0;

    #if USE_SHAPE_LIGHT_TYPE_0
    half4 shapeLight0 = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, lightingUV);

    UpdateShapeLight(_ShapeLightMaskFilter0, _ShapeLightInvertedFilter0, mask, shapeLight0, _ShapeLightBlendFactors0,
                      finalModulate, finalAdditive);
    numShapeLights++;
    #endif

    #if USE_SHAPE_LIGHT_TYPE_1
    half4 shapeLight1 = SAMPLE_TEXTURE2D(_ShapeLightTexture1, sampler_ShapeLightTexture1, lightingUV);

    UpdateShapeLight(_ShapeLightMaskFilter1, _ShapeLightInvertedFilter1, mask, shapeLight1, _ShapeLightBlendFactors1,
                      finalModulate, finalAdditive);
    numShapeLights++;
    #endif

    #if USE_SHAPE_LIGHT_TYPE_2
    half4 shapeLight2 = SAMPLE_TEXTURE2D(_ShapeLightTexture2, sampler_ShapeLightTexture2, lightingUV);

    UpdateShapeLight(_ShapeLightMaskFilter2, _ShapeLightInvertedFilter2, mask, shapeLight2, _ShapeLightBlendFactors2,
                      finalModulate, finalAdditive);
    numShapeLights++;
    #endif

    #if USE_SHAPE_LIGHT_TYPE_3
    half4 shapeLight3 = SAMPLE_TEXTURE2D(_ShapeLightTexture3, sampler_ShapeLightTexture3, lightingUV);

    UpdateShapeLight(_ShapeLightMaskFilter3, _ShapeLightInvertedFilter3, mask, shapeLight3, _ShapeLightBlendFactors3,
                      finalModulate, finalAdditive);
    numShapeLights++;
    #endif

    if(numShapeLights == 0)
    {
        finalModulate = half4(1, 1, 1, 1);
        hdrEmulationScale = 1;
    }

    #if defined(_DEBUG_SHADER)
    if((_DebugLightingIndex == DEBUG_LIGHTING_LIGHT_ONLY) || (_DebugLightingIndex == DEBUG_LIGHTING_LIGHT_DETAIL))
    {
        color.rgb = half3(0.1, 0.1, 0.1);
        hdrEmulationScale = 1;

        if(_DebugLightingIndex == DEBUG_LIGHTING_LIGHT_ONLY)
        {
            normalTS = half3(0, 0, 1);
        }
    }
    #endif

    half3 litColor = (color.rgb * finalModulate.rgb) + finalAdditive.rgb;
    half3 sceneLightingColor = hdrEmulationScale * litColor;

    return CalculateFinalColor(color.rgb, sceneLightingColor, tangentMatrixWS, normalTS, color.a);
}
#endif
