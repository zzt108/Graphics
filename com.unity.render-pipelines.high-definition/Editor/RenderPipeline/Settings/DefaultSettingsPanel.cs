using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering;
using UnityEngine.UIElements;
using UnityEditorInternal;
using System.Linq;

namespace UnityEditor.Rendering.HighDefinition
{
    using CED = CoreEditorDrawer<SerializedHDDefaultSettings>;

    class DefaultSettingsPanelProvider
    {
        static DefaultSettingsPanelIMGUI s_IMGUIImpl = new DefaultSettingsPanelIMGUI();

        [SettingsProvider]
        public static SettingsProvider CreateSettingsProvider()
        {
            return new SettingsProvider("Project/Graphics/HDRP Default Settings",SettingsScope.Project)
            {
                activateHandler = s_IMGUIImpl.OnActivate,
                keywords = SettingsProvider.GetSearchKeywordsFromGUIContentProperties<HDRenderPipelineUI.Styles.GeneralSection>()
                    .Concat(SettingsProvider.GetSearchKeywordsFromGUIContentProperties<DefaultSettingsPanelIMGUI.Styles>())
                    .Concat(OverridableFrameSettingsArea.frameSettingsKeywords).ToArray(),
                guiHandler = s_IMGUIImpl.OnGUI,
            };
        }
    }
    internal class DefaultSettingsPanelIMGUI
    {
        public class Styles
        {
            public const int labelWidth = 220;
            public static readonly GUIContent defaultSettingsAssetLabel = new GUIContent("Default Settings Asset");
            public static readonly GUIContent defaultVolumeProfileLabel = new GUIContent("Default Volume Profile Asset");
            public static readonly GUIContent lookDevVolumeProfileLabel = new GUIContent("LookDev Volume Profile Asset");
            public static readonly GUIContent resourceLabel = new GUIContent("Resources");
            public static readonly GUIContent frameSettingsLabel = new GUIContent("Frame Settings");
            public static readonly GUIContent volumeComponentsLabel = new GUIContent("Volume Components");
            public static readonly GUIContent customPostProcessOrderLabel = new GUIContent("Custom Post Process Orders");
            public static readonly GUIContent defaultFrameSettingsContent = EditorGUIUtility.TrTextContent("Default Frame Settings For");
        }

        Vector2 m_ScrollViewPosition = Vector2.zero;
        public static readonly CED.IDrawer Inspector;

        static DefaultSettingsPanelIMGUI()
        {
            Inspector = CED.Group(
                CED.Group((serialized,owner) => EditorGUILayout.LabelField(Styles.resourceLabel,EditorStyles.boldLabel)),
                ResourcesSection,
                CED.Group((serialized,owner) => EditorGUILayout.LabelField(Styles.frameSettingsLabel,EditorStyles.boldLabel)),
                FrameSettingsSection,
                CED.Group((serialized,owner) => EditorGUILayout.LabelField(Styles.volumeComponentsLabel,EditorStyles.boldLabel)),
                VolumeSection,
                CED.Group((serialized,owner) => EditorGUILayout.LabelField(Styles.customPostProcessOrderLabel,EditorStyles.boldLabel)),
                CustomPostProcessesSection
        );
            // fix init of selection along what is serialized
            if (k_ExpandedState[Expandable.BakedOrCustomProbeFrameSettings])
                selectedFrameSettings = SelectedFrameSettings.BakedOrCustomReflection;
            else if (k_ExpandedState[Expandable.RealtimeProbeFrameSettings])
                selectedFrameSettings = SelectedFrameSettings.RealtimeReflection;
            else //default value: camera
                selectedFrameSettings = SelectedFrameSettings.Camera;
        }

        SerializedHDDefaultSettings serializedSettings;
        HDDefaultSettings settingsSerialized;
        public void OnGUI(string searchContext)
        {
            using(var scrollScope = new EditorGUILayout.ScrollViewScope(m_ScrollViewPosition,EditorStyles.largeLabel))
            {
                if(HDRenderPipeline.currentPipeline == null)
                {
                    EditorGUILayout.HelpBox("No HDRP pipeline currently active (see Quality Settings active level).",MessageType.Warning);
                }
                if((serializedSettings == null) || (settingsSerialized != HDDefaultSettings.instance))
                {
                    settingsSerialized = HDDefaultSettings.instance;
                    var serializedObject = new SerializedObject(HDDefaultSettings.instance);
                    serializedSettings = new SerializedHDDefaultSettings(serializedObject);
                }
                else
                {
                    serializedSettings.serializedObject.Update();
                }
                Draw_AssetSelection(ref serializedSettings,null);

                if(serializedSettings != null)
                {
                    EditorGUILayout.Space();
                    Inspector.Draw(serializedSettings, null);
                }
                serializedSettings.serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// Executed when activate is called from the settings provider.
        /// </summary>
        /// <param name="searchContext"></param>
        /// <param name="rootElement"></param>
        public void OnActivate(string searchContext,VisualElement rootElement)
        {
            m_ScrollViewPosition = Vector2.zero;
        }

        #region Global HDDefaultSettings asset selection
        void Draw_AssetSelection(ref SerializedHDDefaultSettings serialized, Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            var newAsset = (HDDefaultSettings)EditorGUILayout.ObjectField(Styles.defaultSettingsAssetLabel,serialized.serializedObject.targetObject,typeof(HDDefaultSettings),false);
            if(EditorGUI.EndChangeCheck())
            {
                HDDefaultSettings.UpdateGraphicsSettings(newAsset);
                EditorUtility.SetDirty(serialized.serializedObject.targetObject);
            }

            if(GUILayout.Button(EditorGUIUtility.TrTextContent("New","Create a HD Default Settings Asset in your default resource folder (defined in Wizard)"),GUILayout.Width(38),GUILayout.Height(18)))
            {
                HDAssetFactory.CreateHDDefaultSettings();
            }
            EditorGUILayout.EndHorizontal();

            // TODOJENNY:  move shader log level to default settings?
            /*if(HDRenderPipeline.currentAsset != null)
            {
                EditorGUILayout.Space();
                var serializedObject = new SerializedObject(HDRenderPipeline.currentAsset);
                var serializedHDRPAsset = new SerializedHDRenderPipelineAsset(serializedObject);
                HDRenderPipelineUI.GeneralSection.Draw(serializedHDRPAsset,null);
            }*/
            EditorGUIUtility.labelWidth = oldWidth;
        }
        #endregion // Global HDDefaultSettings asset selection

        #region Resources
        
        static readonly CED.IDrawer ResourcesSection = CED.Group(Drawer_ResourcesSection);
        static void Drawer_ResourcesSection(SerializedHDDefaultSettings serialized,Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            EditorGUILayout.PropertyField(serialized.renderPipelineResources,HDRenderPipelineUI.Styles.GeneralSection.renderPipelineResourcesContent);

            HDRenderPipeline hdrp = HDRenderPipeline.currentPipeline;
            if(hdrp != null && hdrp.rayTracingSupported)
                EditorGUILayout.PropertyField(serialized.renderPipelineRayTracingResources,HDRenderPipelineUI.Styles.GeneralSection.renderPipelineRayTracingResourcesContent);

            // Not serialized as editor only datas... Retrieve them in data
            EditorGUI.showMixedValue = serialized.editorResourceHasMultipleDifferentValues;
            EditorGUI.BeginChangeCheck();
            var editorResources = EditorGUILayout.ObjectField(HDRenderPipelineUI.Styles.GeneralSection.renderPipelineEditorResourcesContent,serialized.firstEditorResources,typeof(HDRenderPipelineEditorResources),allowSceneObjects: false) as HDRenderPipelineEditorResources;
            if(EditorGUI.EndChangeCheck())
                serialized.SetEditorResource(editorResources);
            //TODOJENNY check how to do this for default settings
            EditorGUI.showMixedValue = false;
            
            EditorGUIUtility.labelWidth = oldWidth;
        }

        #endregion // Resources

        #region Frame Settings

        public enum SelectedFrameSettings
        {
            Camera,
            BakedOrCustomReflection,
            RealtimeReflection
        }

        internal static SelectedFrameSettings selectedFrameSettings;
        static readonly CED.IDrawer FrameSettingsSection = CED.Group(
            CED.Group(
                (serialized,owner) => EditorGUILayout.BeginVertical("box"),
                Drawer_TitleDefaultFrameSettings
                ),
            CED.Conditional(
                (serialized,owner) => k_ExpandedState[Expandable.CameraFrameSettings],
                CED.Select(
                    (serialized,owner) => serialized.defaultFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                    )
                ),
            CED.Conditional(
                (serialized,owner) => k_ExpandedState[Expandable.BakedOrCustomProbeFrameSettings],
                CED.Select(
                    (serialized,owner) => serialized.defaultBakedOrCustomReflectionFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                    )
                ),
            CED.Conditional(
                (serialized,owner) => k_ExpandedState[Expandable.RealtimeProbeFrameSettings],
                CED.Select(
                    (serialized,owner) => serialized.defaultRealtimeReflectionFrameSettings,
                    FrameSettingsUI.InspectorInnerbox(withOverride: false)
                    )
                ),
            CED.Group((serialized,owner) => EditorGUILayout.EndVertical())
            );


        static void Drawer_TitleDefaultFrameSettings(SerializedHDDefaultSettings serialized,Editor owner)
        {

            using(new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField(Styles.defaultFrameSettingsContent,EditorStyles.boldLabel);
                EditorGUI.BeginChangeCheck();
                selectedFrameSettings = (SelectedFrameSettings)EditorGUILayout.EnumPopup(selectedFrameSettings);
                if(EditorGUI.EndChangeCheck())
                    ApplyChangedDisplayedFrameSettings(serialized,owner);
            }
        }

        enum Expandable
        {
            CameraFrameSettings = 1 << 0, //obsolete
            BakedOrCustomProbeFrameSettings = 1 << 1, //obsolete
            RealtimeProbeFrameSettings = 1 << 2, //obsolete
        }

        static readonly ExpandedState<Expandable,HDDefaultSettings> k_ExpandedState = new ExpandedState<Expandable,HDDefaultSettings>(Expandable.CameraFrameSettings,"HDRP");

        static public void ApplyChangedDisplayedFrameSettings(SerializedHDDefaultSettings serialized,Editor owner)
        {
            k_ExpandedState.SetExpandedAreas(Expandable.CameraFrameSettings | Expandable.BakedOrCustomProbeFrameSettings | Expandable.RealtimeProbeFrameSettings,false);
            switch(selectedFrameSettings)
            {
                case SelectedFrameSettings.Camera:
                    k_ExpandedState.SetExpandedAreas(Expandable.CameraFrameSettings,true);
                    break;
                case SelectedFrameSettings.BakedOrCustomReflection:
                    k_ExpandedState.SetExpandedAreas(Expandable.BakedOrCustomProbeFrameSettings,true);
                    break;
                case SelectedFrameSettings.RealtimeReflection:
                    k_ExpandedState.SetExpandedAreas(Expandable.RealtimeProbeFrameSettings,true);
                    break;
            }
        }

        #endregion // Frame Settings

        #region Custom Post Processes
        
        static readonly CED.IDrawer CustomPostProcessesSection = CED.Group(Drawer_CustomPostProcess);
        static void Drawer_CustomPostProcess(SerializedHDDefaultSettings serialized,Editor owner)
        {
            serialized.uiBeforeTransparentCustomPostProcesses.DoLayoutList();
            serialized.uiBeforeTAACustomPostProcesses.DoLayoutList();
            serialized.uiBeforePostProcessCustomPostProcesses.DoLayoutList();
            serialized.uiAfterPostProcessCustomPostProcesses.DoLayoutList();
        }
        #endregion // Custom Post Processes

        #region Volume Profiles
        static Editor m_CachedDefaultVolumeProfileEditor;
        static Editor m_CachedLookDevVolumeProfileEditor;
        static int m_CurrentVolumeProfileInstanceID;

        static readonly CED.IDrawer VolumeSection = CED.Group(Drawer_VolumeSection);

        static void Drawer_VolumeSection(SerializedHDDefaultSettings serialized,Editor owner)
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            VolumeProfile asset = null;
            var defaultSettings = (serialized.serializedObject.targetObject as HDDefaultSettings);
            using(new EditorGUILayout.HorizontalScope())
            {
                var oldAssetValue = serialized.volumeProfileDefault.objectReferenceValue;
                EditorGUILayout.PropertyField(serialized.volumeProfileDefault,Styles.defaultVolumeProfileLabel);
                asset = serialized.volumeProfileDefault.objectReferenceValue as VolumeProfile;
                if(asset == null && oldAssetValue != null)
                {
                    Debug.Log("Default Volume Profile Asset cannot be null. Rolling back to previous value.");
                    serialized.volumeProfileDefault.objectReferenceValue = oldAssetValue;
                }

                if(GUILayout.Button(EditorGUIUtility.TrTextContent("New","Create a new Volume Profile for default in your default resource folder (defined in Wizard)"),GUILayout.Width(38),GUILayout.Height(18)))
                {
                    VolumeProfileCreator.CreateAndAssign(VolumeProfileCreator.Kind.Default,serialized.serializedObject.targetObject as HDDefaultSettings);
                }
            }

            // The state of the profile can change without the asset reference changing so in this case we need to reset the editor.
            if(m_CurrentVolumeProfileInstanceID != asset.GetInstanceID() && m_CachedDefaultVolumeProfileEditor != null)
            {
                m_CurrentVolumeProfileInstanceID = asset.GetInstanceID();
                m_CachedDefaultVolumeProfileEditor = null;
            }
            if(asset != null)
            {
                Editor.CreateCachedEditor(asset,Type.GetType("UnityEditor.Rendering.VolumeProfileEditor"),ref m_CachedDefaultVolumeProfileEditor);
                EditorGUIUtility.labelWidth -= 18;
                bool oldEnabled = GUI.enabled;
                GUI.enabled = AssetDatabase.IsOpenForEdit(asset);
                m_CachedDefaultVolumeProfileEditor.OnInspectorGUI();
                GUI.enabled = oldEnabled;
                EditorGUIUtility.labelWidth = oldWidth;
            }

            EditorGUILayout.Space();

            VolumeProfile lookDevAsset = null;
            using(new EditorGUILayout.HorizontalScope())
            {
                var oldAssetValue = serialized.volumeProfileLookDev.objectReferenceValue;
                EditorGUILayout.PropertyField(serialized.volumeProfileLookDev,Styles.lookDevVolumeProfileLabel);
                lookDevAsset = serialized.volumeProfileLookDev.objectReferenceValue as VolumeProfile;
                if(lookDevAsset == null && oldAssetValue != null)
                {
                    Debug.Log("LookDev Volume Profile Asset cannot be null. Rolling back to previous value.");
                    serialized.volumeProfileLookDev.objectReferenceValue = oldAssetValue;
                }

                if(GUILayout.Button(EditorGUIUtility.TrTextContent("New","Create a new Volume Profile for default in your default resource folder (defined in Wizard)"),GUILayout.Width(38),GUILayout.Height(18)))
                {
                    VolumeProfileCreator.CreateAndAssign(VolumeProfileCreator.Kind.LookDev,serialized.serializedObject.targetObject as HDDefaultSettings);
                }
            }
            if(lookDevAsset)
            {
                Editor.CreateCachedEditor(lookDevAsset,Type.GetType("UnityEditor.Rendering.VolumeProfileEditor"),ref m_CachedLookDevVolumeProfileEditor);
                EditorGUIUtility.labelWidth -= 18;
                bool oldEnabled = GUI.enabled;
                GUI.enabled = AssetDatabase.IsOpenForEdit(lookDevAsset);
                m_CachedLookDevVolumeProfileEditor.OnInspectorGUI();
                GUI.enabled = oldEnabled;
                EditorGUIUtility.labelWidth = oldWidth;

                if(lookDevAsset.Has<VisualEnvironment>())
                    EditorGUILayout.HelpBox("VisualEnvironment is not modifiable and will be overridden by the LookDev",MessageType.Warning);
                if(lookDevAsset.Has<HDRISky>())
                    EditorGUILayout.HelpBox("HDRISky is not modifiable and will be overridden by the LookDev",MessageType.Warning);
            }
        }

        #endregion // Volume Profiles


    }

    class VolumeProfileCreator:ProjectWindowCallback.EndNameEditAction
    {
        public enum Kind { Default, LookDev }
        Kind m_Kind;

        void SetKind(Kind kind) => m_Kind = kind;

        public override void Action(int instanceId,string pathName,string resourceFile)
        {
            var profile = VolumeProfileFactory.CreateVolumeProfileAtPath(pathName);
            ProjectWindowUtil.ShowCreatedAsset(profile);
            Assign(profile);
        }

        void Assign(VolumeProfile profile)
        {
            switch(m_Kind)
            {
                case Kind.Default:
                    if(settings != null)
                        settings.defaultVolumeProfile = profile;
                    break;
                case Kind.LookDev:
                    if(settings != null)
                        settings.defaultLookDevProfile = profile;
                    break;
            }
            EditorUtility.SetDirty(settings);
        }

        static string GetDefaultName(Kind kind)
        {
            string defaultName;
            switch(kind)
            {
                case Kind.Default:
                    defaultName = "DefaultVolumeSettingsProfile";
                    break;
                case Kind.LookDev:
                    defaultName = "LookDevVolumeSettingsProfile";
                    break;
                default:
                    defaultName = "N/A";
                    break;
            }
            return defaultName;
        }

        static HDDefaultSettings settings;
        public static void CreateAndAssign(Kind kind,HDDefaultSettings defaultSettings)
        {
            settings = defaultSettings;

            var assetCreator = ScriptableObject.CreateInstance<VolumeProfileCreator>();
            assetCreator.SetKind(kind);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(assetCreator.GetInstanceID(),assetCreator,$"Assets/{HDProjectSettings.projectSettingsFolderPath}/{GetDefaultName(kind)}.asset",null,null);
        }
    }
}
