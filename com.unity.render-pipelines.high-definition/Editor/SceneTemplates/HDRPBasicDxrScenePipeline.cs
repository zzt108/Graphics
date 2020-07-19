#if UNITY_2020_2_OR_NEWER
using UnityEditor.SceneTemplate;
#endif
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.SceneManagement;

namespace UnityEditor.Rendering.HighDefinition
{
#if UNITY_2020_2_OR_NEWER
    class HDRPBasicDxrScenePipeline : ISceneTemplatePipeline
    {
        void ISceneTemplatePipeline.AfterTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, Scene scene, bool isAdditive, string sceneName)
        { }

        void ISceneTemplatePipeline.BeforeTemplateInstantiation(SceneTemplateAsset sceneTemplateAsset, bool isAdditive, string sceneName)
        { }

        bool ISceneTemplatePipeline.IsValidTemplateForInstantiation(SceneTemplateAsset sceneTemplateAsset)
        {
            var hdrpAsset = HDRenderPipeline.defaultAsset;
            if (hdrpAsset == null)
                return false;
            return hdrpAsset.currentPlatformRenderPipelineSettings.supportRayTracing;
        }
    }
#endif
}
