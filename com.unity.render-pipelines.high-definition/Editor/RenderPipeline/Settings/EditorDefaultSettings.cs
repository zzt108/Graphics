using System;
using System.IO;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine;

namespace UnityEditor.Rendering.HighDefinition
{
    static class EditorDefaultSettings
    {
        /// <summary>Get the current default VolumeProfile asset. If it is missing, the builtin one is assigned to the current settings.</summary>
        /// <returns>The default VolumeProfile if an HDRenderPipelineAsset is the base SRP asset, null otherwise.</returns>
        internal static VolumeProfile GetOrAssignDefaultVolumeProfile()
        {
            return HDDefaultSettings.instance.GetOrCreateDefaultVolumeProfile();
        }

        /// <summary>Get the current default VolumeProfile asset. If it is missing, the builtin one is assigned to the current settings.</summary>
        /// <param name="hdrpAsset">Asset to check.</param>
        /// <returns>The default VolumeProfile if an HDRenderPipelineAsset is the base SRP asset, null otherwise.</returns>
        internal static VolumeProfile GetOrAssignDefaultVolumeProfile(HDRenderPipelineAsset hdrpAsset)
        {
            return GetOrAssignDefaultVolumeProfile();
        }

        /// <summary>Get the current LookDev VolumeProfile asset. If it is missing, the builtin one is assigned to the current settings.</summary>
        /// <returns>The default VolumeProfile if an HDRenderPipelineAsset is the base SRP asset, null otherwise.</returns>
        internal static VolumeProfile GetOrAssignLookDevVolumeProfile()
        {
            if (HDRenderPipeline.currentAsset == null)
                return null;

            return GetOrAssignLookDevVolumeProfile(HDRenderPipeline.currentAsset);
        }

        /// <summary>Get the current LookDev VolumeProfile asset. If it is missing, the builtin one is assigned to the current settings.</summary>
        /// <param name="hdrpAsset">Asset to check.</param>
        /// <returns>The default VolumeProfile if an HDRenderPipelineAsset is the base SRP asset, null otherwise.</returns>
        internal static VolumeProfile GetOrAssignLookDevVolumeProfile(HDRenderPipelineAsset hdrpAsset)
        {
            if (hdrpAsset.defaultLookDevProfile == null || hdrpAsset.defaultLookDevProfile.Equals(null))
                hdrpAsset.defaultLookDevProfile =
                   HDDefaultSettings.instance.renderPipelineEditorResources.lookDev.defaultLookDevVolumeProfile;

            return hdrpAsset.defaultLookDevProfile;
        }
    }
}
