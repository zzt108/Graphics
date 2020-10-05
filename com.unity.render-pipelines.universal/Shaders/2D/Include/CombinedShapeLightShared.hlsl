
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

half4 CalculateFinalColor(half3 initialColor, half3 finalColor, half alpha)
{
    #if defined(_DEBUG_SHADER)
    if(_DebugMaterialIndex != DEBUG_LIGHTING_NONE)
    {
        switch(_DebugMaterialIndex)
        {
            case DEBUG_MATERIAL_UNLIT:
            case DEBUG_MATERIAL_DIFFUSE:
                return half4(initialColor, 1);

            case DEBUG_MATERIAL_ALPHA:
                return half4(alpha, alpha, alpha, 1);

            default:
                return half4(0, 0, 0, 1);
        }
    }
    #endif

    finalColor = lerp(initialColor, finalColor, _UseSceneLighting);
    return half4(finalColor, alpha);
}

half4 CombinedShapeLightShared(half4 color, half4 mask, half2 lightingUV)
{
	if (color.a == 0.0)
		discard;

    color = color * _RendererColor; // This is needed for sprite shape

#if !USE_SHAPE_LIGHT_TYPE_0 && !USE_SHAPE_LIGHT_TYPE_1 && !USE_SHAPE_LIGHT_TYPE_2 && ! USE_SHAPE_LIGHT_TYPE_3
    half3 sceneLightingColor = color;
#else
    half4 finalModulate = half4(0, 0, 0, 0);
    half4 finalAdditive = half4(0, 0, 0, 0);

    #if USE_SHAPE_LIGHT_TYPE_0
    half4 shapeLight0 = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, lightingUV);

    UpdateShapeLight(_ShapeLightMaskFilter0, _ShapeLightInvertedFilter0, mask, shapeLight0, _ShapeLightBlendFactors0,
                      finalModulate, finalAdditive);
    #endif

    #if USE_SHAPE_LIGHT_TYPE_1
    half4 shapeLight1 = SAMPLE_TEXTURE2D(_ShapeLightTexture1, sampler_ShapeLightTexture1, lightingUV);

    UpdateShapeLight(_ShapeLightMaskFilter1, _ShapeLightInvertedFilter1, mask, shapeLight1, _ShapeLightBlendFactors1,
                      finalModulate, finalAdditive);
    #endif

    #if USE_SHAPE_LIGHT_TYPE_2
    half4 shapeLight2 = SAMPLE_TEXTURE2D(_ShapeLightTexture2, sampler_ShapeLightTexture2, lightingUV);

    UpdateShapeLight(_ShapeLightMaskFilter2, _ShapeLightInvertedFilter2, mask, shapeLight2, _ShapeLightBlendFactors2,
                      finalModulate, finalAdditive);
    #endif

    #if USE_SHAPE_LIGHT_TYPE_3
    half4 shapeLight3 = SAMPLE_TEXTURE2D(_ShapeLightTexture3, sampler_ShapeLightTexture3, lightingUV);

    UpdateShapeLight(_ShapeLightMaskFilter3, _ShapeLightInvertedFilter3, mask, shapeLight3, _ShapeLightBlendFactors3,
                      finalModulate, finalAdditive);
    #endif

    half3 sceneLightingColor = _HDREmulationScale * (color.rgb * finalModulate + finalAdditive);
#endif

    return CalculateFinalColor(color.rgb, sceneLightingColor, color.a);
}
#endif
