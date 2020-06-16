using System;
using UnityEngine.Serialization;
using System.Collections.Generic; //needed for list of Custom Post Processes injections

namespace UnityEngine.Rendering.HighDefinition
{
    public partial class HDRenderPipelineAsset : IVersionable<HDRenderPipelineAsset.Version>
    {
        enum Version
        {
            None,
            First,
            UpgradeFrameSettingsToStruct,
            AddAfterPostProcessFrameSetting,
            AddFrameSettingSpecularLighting = 5, // Not used anymore - don't removed the number
            AddReflectionSettings,
            AddPostProcessFrameSettings,
            AddRayTracingFrameSettings,
            AddFrameSettingDirectSpecularLighting,
            AddCustomPostprocessAndCustomPass,
            ScalableSettingsRefactor,
            ShadowFilteringVeryHighQualityRemoval,
            SeparateColorGradingAndTonemappingFrameSettings,
            ReplaceTextureArraysByAtlasForCookieAndPlanar,
            AddedAdaptiveSSS,
            RemoveCookieCubeAtlasToOctahedral2D,
            DefaultSettingsAsAnAsset
        }

        static readonly MigrationDescription<Version, HDRenderPipelineAsset> k_Migration = MigrationDescription.New(
            /* TODOJENNY - once i know which settings need to be moved to the Default Settings
            MigrationStep.New(Version.UpgradeFrameSettingsToStruct,(HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettingsOverrideMask unusedMaskForDefault = new FrameSettingsOverrideMask();
                if(data.m_ObsoleteFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteFrameSettings,ref data.m_RenderingPathDefaultCameraFrameSettings,ref unusedMaskForDefault);
                if(data.m_ObsoleteBakedOrCustomReflectionFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteBakedOrCustomReflectionFrameSettings,ref data.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings,ref unusedMaskForDefault);
                if(data.m_ObsoleteRealtimeReflectionFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteRealtimeReflectionFrameSettings,ref data.m_RenderingPathDefaultRealtimeReflectionFrameSettings,ref unusedMaskForDefault);
#pragma warning restore 618
            }), 
            MigrationStep.New(Version.AddAfterPostProcessFrameSetting, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToAfterPostprocess(ref data.m_RenderingPathDefaultCameraFrameSettings);
            }),
            MigrationStep.New(Version.AddReflectionSettings, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToDefaultReflectionSettings(ref data.m_RenderingPathDefaultCameraFrameSettings);
                FrameSettings.MigrateToNoReflectionSettings(ref data.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings);
                FrameSettings.MigrateToNoReflectionRealtimeSettings(ref data.m_RenderingPathDefaultRealtimeReflectionFrameSettings);
            }),
            MigrationStep.New(Version.AddPostProcessFrameSettings, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToPostProcess(ref data.m_RenderingPathDefaultCameraFrameSettings);
            }),
            MigrationStep.New(Version.AddRayTracingFrameSettings, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToRayTracing(ref data.m_RenderingPathDefaultCameraFrameSettings);
            }),
            MigrationStep.New(Version.AddFrameSettingDirectSpecularLighting, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToDirectSpecularLighting(ref data.m_RenderingPathDefaultCameraFrameSettings);
                FrameSettings.MigrateToNoDirectSpecularLighting(ref data.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings);
                FrameSettings.MigrateToDirectSpecularLighting(ref data.m_RenderingPathDefaultRealtimeReflectionFrameSettings);
            }),
            MigrationStep.New(Version.AddCustomPostprocessAndCustomPass, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToCustomPostprocessAndCustomPass(ref data.m_RenderingPathDefaultCameraFrameSettings);
            }),*/
            MigrationStep.New(Version.ScalableSettingsRefactor, (HDRenderPipelineAsset data) =>
            {
                ref var shadowInit = ref data.m_RenderPipelineSettings.hdShadowInitParams;
                shadowInit.shadowResolutionArea.schemaId = ScalableSettingSchemaId.With4Levels;
                shadowInit.shadowResolutionDirectional.schemaId = ScalableSettingSchemaId.With4Levels;
                shadowInit.shadowResolutionPunctual.schemaId = ScalableSettingSchemaId.With4Levels;
            }),
            MigrationStep.New(Version.ShadowFilteringVeryHighQualityRemoval, (HDRenderPipelineAsset data) =>
            {
                ref var shadowInit = ref data.m_RenderPipelineSettings.hdShadowInitParams;
                shadowInit.shadowFilteringQuality = shadowInit.shadowFilteringQuality > HDShadowFilteringQuality.High ? HDShadowFilteringQuality.High : shadowInit.shadowFilteringQuality;
            }),/*
            MigrationStep.New(Version.SeparateColorGradingAndTonemappingFrameSettings, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToSeparateColorGradingAndTonemapping(ref data.m_RenderingPathDefaultCameraFrameSettings);
            }),*/
            MigrationStep.New(Version.ReplaceTextureArraysByAtlasForCookieAndPlanar, (HDRenderPipelineAsset data) =>
            {
                ref var lightLoopSettings = ref data.m_RenderPipelineSettings.lightLoopSettings;

#pragma warning disable 618 // Type or member is obsolete
                float cookieAtlasSize = Mathf.Sqrt((int)lightLoopSettings.cookieAtlasSize * (int)lightLoopSettings.cookieAtlasSize * lightLoopSettings.cookieTexArraySize);
                float planarSize = Mathf.Sqrt((int)lightLoopSettings.planarReflectionAtlasSize * (int)lightLoopSettings.planarReflectionAtlasSize * lightLoopSettings.maxPlanarReflectionOnScreen);
#pragma warning restore 618

                // The atlas only supports power of two sizes
                cookieAtlasSize = (float)Mathf.NextPowerOfTwo((int)cookieAtlasSize);
                planarSize = (float)Mathf.NextPowerOfTwo((int)planarSize);

                // Clamp to avoid too large atlases
                cookieAtlasSize = Mathf.Clamp(cookieAtlasSize, (int)CookieAtlasResolution.CookieResolution256, (int)CookieAtlasResolution.CookieResolution8192);
                planarSize = Mathf.Clamp(planarSize, (int)PlanarReflectionAtlasResolution.PlanarReflectionResolution256, (int)PlanarReflectionAtlasResolution.PlanarReflectionResolution8192);

                lightLoopSettings.cookieAtlasSize = (CookieAtlasResolution)cookieAtlasSize;
                lightLoopSettings.planarReflectionAtlasSize = (PlanarReflectionAtlasResolution)planarSize;
            }),
            MigrationStep.New(Version.AddedAdaptiveSSS, (HDRenderPipelineAsset data) =>
            {
            #pragma warning disable 618 // Type or member is obsolete
                bool previouslyHighQuality = data.m_RenderPipelineSettings.m_ObsoleteincreaseSssSampleCount;
            #pragma warning restore 618
                /*
                FrameSettings.MigrateSubsurfaceParams(ref data.m_RenderingPathDefaultCameraFrameSettings,                  previouslyHighQuality);
                FrameSettings.MigrateSubsurfaceParams(ref data.m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings, previouslyHighQuality);
                FrameSettings.MigrateSubsurfaceParams(ref data.m_RenderingPathDefaultRealtimeReflectionFrameSettings,      previouslyHighQuality);
                */
            }),
            MigrationStep.New(Version.RemoveCookieCubeAtlasToOctahedral2D, (HDRenderPipelineAsset data) =>
            {
                ref var lightLoopSettings = ref data.m_RenderPipelineSettings.lightLoopSettings;

#pragma warning disable 618 // Type or member is obsolete
                float cookieAtlasSize = Mathf.Sqrt((int)lightLoopSettings.cookieAtlasSize * (int)lightLoopSettings.cookieAtlasSize * lightLoopSettings.cookieTexArraySize);
                float planarSize = Mathf.Sqrt((int)lightLoopSettings.planarReflectionAtlasSize * (int)lightLoopSettings.planarReflectionAtlasSize * lightLoopSettings.maxPlanarReflectionOnScreen);
#pragma warning restore 618

                if(cookieAtlasSize > 128f && planarSize <= 1024f)
                {
                    Debug.LogWarning("HDRP Internally change the storage of Cube Cookie to Octahedral Projection inside the Planar Reflection Atlas. It is recommended that you increase the size of the Planar Projection Atlas if the cookies no longer fit.");
                }
            }),
            MigrationStep.New(Version.DefaultSettingsAsAnAsset,(HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete

                // 2/ it acted as the definition of the Default Settings - now migrated to its own asset
                if(data == GraphicsSettings.defaultRenderPipeline)
                {
                    HDDefaultSettings.Ensure();
                    HDDefaultSettings.instance.defaultVolumeProfile = data.m_ObsoleteDefaultVolumeProfile;
                    data.m_ObsoleteDefaultVolumeProfile = null;

                    HDDefaultSettings.instance.defaultLookDevProfile = data.m_ObsoleteDefaultLookDevProfile;
                    data.m_ObsoleteDefaultLookDevProfile = null;
                    /*
                    TODOJENNY
                    HDDefaultSettings.instance.GetDefaultFrameSettings(FrameSettingsRenderType.Camera) = data.GetDefaultFrameSettings(FrameSettingsRenderType.Camera);
                    HDDefaultSettings.instance.GetDefaultFrameSettings(FrameSettingsRenderType.CustomOrBakedReflection) = data.GetDefaultFrameSettings(FrameSettingsRenderType.CustomOrBakedReflection);
                    HDDefaultSettings.instance.GetDefaultFrameSettings(FrameSettingsRenderType.RealtimeReflection) = data.GetDefaultFrameSettings(FrameSettingsRenderType.RealtimeReflection);
                   
                    */
                    HDDefaultSettings.instance.renderPipelineResources = data.m_ObsoleteRenderPipelineResources;
                    data.m_ObsoleteRenderPipelineResources = null;

                    HDDefaultSettings.instance.renderPipelineEditorResources = data.m_ObsoleteRenderPipelineEditorResources;
                    data.m_ObsoleteRenderPipelineEditorResources = null;

                    HDDefaultSettings.instance.renderPipelineRayTracingResources = data.m_ObsoleteRenderPipelineRayTracingResources;
                    data.m_ObsoleteRenderPipelineRayTracingResources = null;

                    HDDefaultSettings.instance.beforeTransparentCustomPostProcesses = data.m_ObsoleteBeforeTransparentCustomPostProcesses;
                    data.m_ObsoleteBeforeTransparentCustomPostProcesses = null;

                    HDDefaultSettings.instance.beforePostProcessCustomPostProcesses = data.m_ObsoleteBeforePostProcessCustomPostProcesses;
                    data.m_ObsoleteBeforePostProcessCustomPostProcesses = null;

                    HDDefaultSettings.instance.afterPostProcessCustomPostProcesses = data.m_ObsoleteAfterPostProcessCustomPostProcesses;
                    data.m_ObsoleteAfterPostProcessCustomPostProcesses = null;

                    HDDefaultSettings.instance.beforeTAACustomPostProcesses = data.m_ObsoleteBeforeTAACustomPostProcesses;
                    data.m_ObsoleteBeforeTAACustomPostProcesses = null;
                }
#pragma warning restore 618
            })
        );

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        void OnEnable() => k_Migration.Migrate(this);

#pragma warning disable 618 // Type or member is obsolete
        [SerializeField]
        [FormerlySerializedAs("serializedFrameSettings"), FormerlySerializedAs("m_FrameSettings"), Obsolete("For data migration")]
        ObsoleteFrameSettings m_ObsoleteFrameSettings;
        [SerializeField]
        [FormerlySerializedAs("m_BakedOrCustomReflectionFrameSettings"), Obsolete("For data migration")]
        ObsoleteFrameSettings m_ObsoleteBakedOrCustomReflectionFrameSettings;
        [SerializeField]
        [FormerlySerializedAs("m_RealtimeReflectionFrameSettings"), Obsolete("For data migration")]
        ObsoleteFrameSettings m_ObsoleteRealtimeReflectionFrameSettings;
        [FormerlySerializedAs("defaultVolumeProfile"), Obsolete("For data migration")]
        VolumeProfile m_ObsoleteDefaultVolumeProfile;
        [FormerlySerializedAs("defaultLookDevProfile"), Obsolete("For data migration")]
        VolumeProfile m_ObsoleteDefaultLookDevProfile;
        [FormerlySerializedAs("renderPipelineResources"), Obsolete("For data migration")]
        RenderPipelineResources m_ObsoleteRenderPipelineResources;
        [FormerlySerializedAs("renderPipelineEditorResources"), Obsolete("For data migration")]
        HDRenderPipelineEditorResources m_ObsoleteRenderPipelineEditorResources;
        [FormerlySerializedAs("renderPipelineRayTracingResources"), Obsolete("For data migration")]
        HDRenderPipelineRayTracingResources m_ObsoleteRenderPipelineRayTracingResources;
        [FormerlySerializedAs("beforeTransparentCustomPostProcesses"), Obsolete("For data migration")]
        List<string> m_ObsoleteBeforeTransparentCustomPostProcesses;
        [FormerlySerializedAs("beforePostProcessCustomPostProcesses"), Obsolete("For data migration")]
        List<string> m_ObsoleteBeforePostProcessCustomPostProcesses;
        [FormerlySerializedAs("afterPostProcessCustomPostProcesses"), Obsolete("For data migration")]
        List<string> m_ObsoleteAfterPostProcessCustomPostProcesses;
        [FormerlySerializedAs("beforeTAACustomPostProcesses"), Obsolete("For data migration")]
        List<string> m_ObsoleteBeforeTAACustomPostProcesses;
        /*
                [FormerlySerializedAs(""), Obsolete("For data migration")]
                ObsoleteFrameSettings m_Obsolete;
        */
        // TODOJENNY
#pragma warning restore 618

    }
}
