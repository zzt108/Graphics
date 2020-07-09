using System;
using UnityEngine.Serialization;
using UnityEditor;
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
            /* TODOJENNY - once i know which settings need to be moved to the Default Settings */
            MigrationStep.New(Version.UpgradeFrameSettingsToStruct,(HDRenderPipelineAsset data) =>
            {
#pragma warning disable 618 // Type or member is obsolete
                FrameSettingsOverrideMask unusedMaskForDefault = new FrameSettingsOverrideMask();
                if(data.m_ObsoleteFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteFrameSettings,ref data.m_FrameSettingsMovedToDefaultSettings,ref unusedMaskForDefault);
                if(data.m_ObsoleteBakedOrCustomReflectionFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteBakedOrCustomReflectionFrameSettings,ref data.m_BakedOrCustomReflectionFrameSettingsMovedToDefaultSettings,ref unusedMaskForDefault);
                if(data.m_ObsoleteRealtimeReflectionFrameSettings != null)
                    FrameSettings.MigrateFromClassVersion(ref data.m_ObsoleteRealtimeReflectionFrameSettings,ref data.m_RealtimeReflectionFrameSettingsMovedToDefaultSettings,ref unusedMaskForDefault);
#pragma warning restore 618
            }), 
            MigrationStep.New(Version.AddAfterPostProcessFrameSetting, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToAfterPostprocess(ref data.m_FrameSettingsMovedToDefaultSettings);
            }),
            MigrationStep.New(Version.AddReflectionSettings, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToDefaultReflectionSettings(ref data.m_FrameSettingsMovedToDefaultSettings);
                FrameSettings.MigrateToNoReflectionSettings(ref data.m_BakedOrCustomReflectionFrameSettingsMovedToDefaultSettings);
                FrameSettings.MigrateToNoReflectionRealtimeSettings(ref data.m_RealtimeReflectionFrameSettingsMovedToDefaultSettings);
            }),
            MigrationStep.New(Version.AddPostProcessFrameSettings, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToPostProcess(ref data.m_FrameSettingsMovedToDefaultSettings);
            }),
            MigrationStep.New(Version.AddRayTracingFrameSettings, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToRayTracing(ref data.m_FrameSettingsMovedToDefaultSettings);
            }),
            MigrationStep.New(Version.AddFrameSettingDirectSpecularLighting, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToDirectSpecularLighting(ref data.m_FrameSettingsMovedToDefaultSettings);
                FrameSettings.MigrateToNoDirectSpecularLighting(ref data.m_BakedOrCustomReflectionFrameSettingsMovedToDefaultSettings);
                FrameSettings.MigrateToDirectSpecularLighting(ref data.m_RealtimeReflectionFrameSettingsMovedToDefaultSettings);
            }),
            MigrationStep.New(Version.AddCustomPostprocessAndCustomPass, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToCustomPostprocessAndCustomPass(ref data.m_FrameSettingsMovedToDefaultSettings);
            }),
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
            }),
            MigrationStep.New(Version.SeparateColorGradingAndTonemappingFrameSettings, (HDRenderPipelineAsset data) =>
            {
                FrameSettings.MigrateToSeparateColorGradingAndTonemapping(ref data.m_FrameSettingsMovedToDefaultSettings);
            }),
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
                FrameSettings.MigrateSubsurfaceParams(ref data.m_FrameSettingsMovedToDefaultSettings,                  previouslyHighQuality);
                FrameSettings.MigrateSubsurfaceParams(ref data.m_BakedOrCustomReflectionFrameSettingsMovedToDefaultSettings, previouslyHighQuality);
                FrameSettings.MigrateSubsurfaceParams(ref data.m_RealtimeReflectionFrameSettingsMovedToDefaultSettings,      previouslyHighQuality);
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
                // 2/ it acted as the definition of the Default Settings - now migrated to its own asset
                if(data == GraphicsSettings.defaultRenderPipeline)
                {
#pragma warning disable 618 // Type or member is obsolete
                    HDDefaultSettings defaultSettings = HDDefaultSettings.MigrateFromHDRPAsset(data, true);
#pragma warning restore 618
                    HDDefaultSettings.UpdateGraphicsSettings(defaultSettings);
                    EditorUtility.SetDirty(defaultSettings);
                }
            })
        );

        [SerializeField]
        Version m_Version = MigrationDescription.LastVersion<Version>();
        Version IVersionable<Version>.version { get => m_Version; set => m_Version = value; }

        void OnEnable() => k_Migration.Migrate(this);

#pragma warning disable 618 // Type or member is obsolete
        [SerializeField]
        [FormerlySerializedAs("serializedFrameSettings"), FormerlySerializedAs("m_FrameSettings"),Obsolete("For data migration")]
        ObsoleteFrameSettings m_ObsoleteFrameSettings;
        [SerializeField]
        [FormerlySerializedAs("m_BakedOrCustomReflectionFrameSettings"), Obsolete("For data migration")]
        ObsoleteFrameSettings m_ObsoleteBakedOrCustomReflectionFrameSettings;
        [SerializeField]
        [FormerlySerializedAs("m_RealtimeReflectionFrameSettings"), Obsolete("For data migration")]
        ObsoleteFrameSettings m_ObsoleteRealtimeReflectionFrameSettings;

        #region Settings Moved from the HDRP Asset to HDDefaultSettings
        [SerializeField]
        [FormerlySerializedAs("defaultVolumeProfile"), Obsolete("For data migration")]
        internal VolumeProfile m_ObsoleteDefaultVolumeProfile;
        [SerializeField]
        [FormerlySerializedAs("defaultLookDevProfile"), Obsolete("For data migration")]
        internal VolumeProfile m_ObsoleteDefaultLookDevProfile;

        [SerializeField]
        [FormerlySerializedAs("m_RenderingPathDefaultCameraFrameSettings"), Obsolete("For data migration")]
        internal FrameSettings m_FrameSettingsMovedToDefaultSettings;
        [SerializeField]
        [FormerlySerializedAs("m_RenderingPathDefaultBakedOrCustomReflectionFrameSettings"), Obsolete("For data migration")]
        internal FrameSettings m_BakedOrCustomReflectionFrameSettingsMovedToDefaultSettings;
        [SerializeField]
        [FormerlySerializedAs("m_RenderingPathDefaultRealtimeReflectionFrameSettings"), Obsolete("For data migration")]
        internal FrameSettings m_RealtimeReflectionFrameSettingsMovedToDefaultSettings;

        [SerializeField]
        [FormerlySerializedAs("renderPipelineResources"), Obsolete("For data migration")]
        internal RenderPipelineResources m_ObsoleteRenderPipelineResources;
        [SerializeField]
        [FormerlySerializedAs("renderPipelineEditorResources"), Obsolete("For data migration")]
        internal HDRenderPipelineEditorResources m_ObsoleteRenderPipelineEditorResources;
        [SerializeField]
        [FormerlySerializedAs("renderPipelineRayTracingResources"), Obsolete("For data migration")]
        internal HDRenderPipelineRayTracingResources m_ObsoleteRenderPipelineRayTracingResources;

        [SerializeField]
        [FormerlySerializedAs("beforeTransparentCustomPostProcesses"), Obsolete("For data migration")]
        internal List<string> m_ObsoleteBeforeTransparentCustomPostProcesses;
        [SerializeField]
        [FormerlySerializedAs("beforePostProcessCustomPostProcesses"), Obsolete("For data migration")]
        internal List<string> m_ObsoleteBeforePostProcessCustomPostProcesses;
        [SerializeField]
        [FormerlySerializedAs("afterPostProcessCustomPostProcesses"), Obsolete("For data migration")]
        internal List<string> m_ObsoleteAfterPostProcessCustomPostProcesses;
        [SerializeField]
        [FormerlySerializedAs("beforeTAACustomPostProcesses"), Obsolete("For data migration")]
        internal List<string> m_ObsoleteBeforeTAACustomPostProcesses;
        #endregion

#pragma warning restore 618

    }
}
