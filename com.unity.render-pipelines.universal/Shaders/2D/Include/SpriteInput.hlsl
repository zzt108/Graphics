
#include "Packages/com.unity.render-pipelines.universal/Shaders/2D/Include/LightingUtility.hlsl"

TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
half4 _MainTex_ST;

TEXTURE2D(_MaskTex);
SAMPLER(sampler_MaskTex);

#if defined(_NORMALMAP)
TEXTURE2D(_NormalMap);
SAMPLER(sampler_NormalMap);
half4 _NormalMap_ST;
#endif

#if USE_SHAPE_LIGHT_TYPE_0
SHAPE_LIGHT(0)
#endif

#if USE_SHAPE_LIGHT_TYPE_1
SHAPE_LIGHT(1)
#endif

#if USE_SHAPE_LIGHT_TYPE_2
SHAPE_LIGHT(2)
#endif

#if USE_SHAPE_LIGHT_TYPE_3
SHAPE_LIGHT(3)
#endif
