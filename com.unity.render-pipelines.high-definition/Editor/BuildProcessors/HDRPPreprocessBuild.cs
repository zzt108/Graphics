using UnityEngine;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace UnityEditor.Rendering.HighDefinition
{
    class HDRPPreprocessBuild : IPreprocessBuildWithReport
    {
        public int callbackOrder { get { return 0; } }

        public void OnPreprocessBuild(BuildReport report)
        {
            // Don't execute the preprocess if we are not on a HDRenderPipeline
            HDRenderPipelineAsset hdPipelineAsset = QualitySettings.renderPipeline as HDRenderPipelineAsset;
            if (hdPipelineAsset == null)
            {
                if (!Application.isBatchMode)
                {
                    if (!EditorUtility.DisplayDialog("Build Player",
                                                    "There is no HDRP Asset provided in the selected Quality Level.\nAre you sure you want to continue?\n Build time can be extremely long without it.", "Ok", "Cancel"))
                    {
                        throw new BuildFailedException("Stop build on request.");
                    }
                }
                else
                {
                    Debug.LogWarning("There is no HDRP Asset provided in the selected Quality Level. Build time can be extremely long without it.");
                }

                return;
            }

            // If platform is supported all good
            GraphicsDeviceType  unsupportedGraphicDevice = GraphicsDeviceType.Null;
            if (HDUtils.AreGraphicsAPIsSupported(report.summary.platform, out unsupportedGraphicDevice)
                && HDUtils.IsSupportedBuildTarget(report.summary.platform)
                && HDUtils.IsOperatingSystemSupported(SystemInfo.operatingSystem))
                return;

            unsupportedGraphicDevice = (unsupportedGraphicDevice == GraphicsDeviceType.Null) ? SystemInfo.graphicsDeviceType : unsupportedGraphicDevice;
            string msg = "The platform " + report.summary.platform.ToString() + " with the graphic API " +  unsupportedGraphicDevice + " is not supported with High Definition Render Pipeline";

            // Throw an exception to stop the build
            throw new BuildFailedException(msg);
        }
    }
}
