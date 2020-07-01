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
        Editor m_CachedDefaultVolumeProfileEditor;
        Editor m_CachedLookDevVolumeProfileEditor;
        ReorderableList m_BeforeTransparentCustomPostProcesses;
        ReorderableList m_BeforeTAACustomPostProcesses;
        ReorderableList m_BeforePostProcessCustomPostProcesses;
        ReorderableList m_AfterPostProcessCustomPostProcesses;
        int m_CurrentVolumeProfileInstanceID;
        HDDefaultSettings defaultSettings;

        static DefaultSettingsPanelIMGUI()
        {
            // fix init of selection along what is serialized
            if (k_ExpandedState[Expandable.BakedOrCustomProbeFrameSettings])
                selectedFrameSettings = SelectedFrameSettings.BakedOrCustomReflection;
            else if (k_ExpandedState[Expandable.RealtimeProbeFrameSettings])
                selectedFrameSettings = SelectedFrameSettings.RealtimeReflection;
            else //default value: camera
                selectedFrameSettings = SelectedFrameSettings.Camera;
        }

        internal static DiffusionProfileSettingsListUI diffusionProfileUI = new DiffusionProfileSettingsListUI();

    public enum SelectedFrameSettings
    {
        Camera,
        BakedOrCustomReflection,
        RealtimeReflection
    }

    internal static SelectedFrameSettings selectedFrameSettings;

        public void OnGUI(string searchContext)
        {
            m_ScrollViewPosition = GUILayout.BeginScrollView(m_ScrollViewPosition,EditorStyles.largeLabel);

            if(HDRenderPipeline.currentPipeline == null)
            {
                EditorGUILayout.HelpBox("No HDRP pipeline currently active (see Quality Settings active level).",MessageType.Warning);
            }
            UpdateDefaultSettings();
            Draw_AssetSelection();

            if(defaultSettings != null)
            {
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(Styles.resourceLabel,EditorStyles.boldLabel);
                Draw_Resources();
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(Styles.frameSettingsLabel,EditorStyles.boldLabel);
                Draw_DefaultFrameSettings();
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(Styles.volumeComponentsLabel,EditorStyles.boldLabel);
                Draw_VolumeInspector();
                EditorGUILayout.Space();

                EditorGUILayout.LabelField(Styles.customPostProcessOrderLabel,EditorStyles.boldLabel);
                Draw_CustomPostProcess();
            }
            GUILayout.EndScrollView();
        }

        /// <summary>
        /// Executed when activate is called from the settings provider.
        /// </summary>
        /// <param name="searchContext"></param>
        /// <param name="rootElement"></param>
        public void OnActivate(string searchContext,VisualElement rootElement)
        {
            m_ScrollViewPosition = Vector2.zero;
            UpdateDefaultSettings();
        }

        void UpdateDefaultSettings()
        {
            HDDefaultSettings gfxSettings = HDDefaultSettings.instance;
            if(gfxSettings != defaultSettings)
            {
                defaultSettings = gfxSettings;
                InitializeCustomPostProcessesLists();
            }
        }

        void InitializeCustomPostProcessesLists()
        {
            if(defaultSettings is null)
            {
                Debug.LogError("No HD Default Settings"); //TODJENNY remove in cleanup
                return;
            }
            var ppVolumeTypes = TypeCache.GetTypesDerivedFrom<CustomPostProcessVolumeComponent>();
            var ppVolumeTypeInjectionPoints = new Dictionary<Type,CustomPostProcessInjectionPoint>();
            foreach(var ppVolumeType in ppVolumeTypes)
            {
                if(ppVolumeType.IsAbstract)
                    continue;

                var comp = ScriptableObject.CreateInstance(ppVolumeType) as CustomPostProcessVolumeComponent;
                ppVolumeTypeInjectionPoints[ppVolumeType] = comp.injectionPoint;
                CoreUtils.Destroy(comp);
            }

            InitList(ref m_BeforeTransparentCustomPostProcesses,defaultSettings.beforeTransparentCustomPostProcesses,"After Opaque And Sky",CustomPostProcessInjectionPoint.AfterOpaqueAndSky);
            InitList(ref m_BeforePostProcessCustomPostProcesses,defaultSettings.beforePostProcessCustomPostProcesses,"Before Post Process",CustomPostProcessInjectionPoint.BeforePostProcess);
            InitList(ref m_AfterPostProcessCustomPostProcesses,defaultSettings.afterPostProcessCustomPostProcesses,"After Post Process",CustomPostProcessInjectionPoint.AfterPostProcess);
            InitList(ref m_BeforeTAACustomPostProcesses,defaultSettings.beforeTAACustomPostProcesses,"Before TAA",CustomPostProcessInjectionPoint.BeforeTAA);

            void InitList(ref ReorderableList reorderableList,List<string> customPostProcessTypes,string headerName,CustomPostProcessInjectionPoint injectionPoint)
            {
                // Sanitize the list:
                customPostProcessTypes.RemoveAll(s => Type.GetType(s) == null);

                reorderableList = new ReorderableList(customPostProcessTypes,typeof(string));
                reorderableList.drawHeaderCallback = (rect) =>
                    EditorGUI.LabelField(rect,headerName,EditorStyles.label);
                reorderableList.drawElementCallback = (rect,index,isActive,isFocused) =>
                {
                    rect.height = EditorGUIUtility.singleLineHeight;
                    var elemType = Type.GetType(customPostProcessTypes[index]);
                    EditorGUI.LabelField(rect,elemType.ToString(),EditorStyles.boldLabel);
                };
                reorderableList.onAddCallback = (list) =>
                {
                    var menu = new GenericMenu();

                    foreach(var kp in ppVolumeTypeInjectionPoints)
                    {
                        if(kp.Value == injectionPoint && !customPostProcessTypes.Contains(kp.Key.AssemblyQualifiedName))
                            menu.AddItem(new GUIContent(kp.Key.ToString()),false,() =>
                            {
                                Undo.RegisterCompleteObjectUndo(defaultSettings,$"Added {kp.Key.ToString()} Custom Post Process");
                                customPostProcessTypes.Add(kp.Key.AssemblyQualifiedName);
                            });
                    }

                    if(menu.GetItemCount() == 0)
                        menu.AddDisabledItem(new GUIContent("No Custom Post Process Available"));

                    menu.ShowAsContext();
                    EditorUtility.SetDirty(defaultSettings);
                };
                reorderableList.onRemoveCallback = (list) =>
                {
                    Undo.RegisterCompleteObjectUndo(defaultSettings,$"Removed {list.list[list.index].ToString()} Custom Post Process");
                    customPostProcessTypes.RemoveAt(list.index);
                    EditorUtility.SetDirty(defaultSettings);
                };
                reorderableList.elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
                reorderableList.onReorderCallback = (list) => EditorUtility.SetDirty(defaultSettings);
            }
        }

        void Draw_AssetSelection()
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            EditorGUILayout.BeginHorizontal();
            var newAsset = (HDDefaultSettings)EditorGUILayout.ObjectField(Styles.defaultSettingsAssetLabel,defaultSettings,typeof(RenderPipelineDefaultSettings),false);
            if(newAsset != defaultSettings)
            {
                defaultSettings = newAsset;
                HDDefaultSettings.UpdateGraphicsSettings(newAsset);
            }

            if(GUILayout.Button(EditorGUIUtility.TrTextContent("New","Create a HD Default Settings Asset in your default resource folder (defined in Wizard)"),GUILayout.Width(38),GUILayout.Height(18)))
            {
                HDAssetFactory.CreateHDDefaultSettings();
            }
            EditorGUILayout.EndHorizontal();

            // TODOJENNY:  move shader log level to default settings?
            if(HDRenderPipeline.currentAsset != null)
            {
                EditorGUILayout.Space();
                var serializedObject = new SerializedObject(HDRenderPipeline.currentAsset);
                var serializedHDRPAsset = new SerializedHDRenderPipelineAsset(serializedObject);
                HDRenderPipelineUI.GeneralSection.Draw(serializedHDRPAsset,null);
            }
            EditorGUIUtility.labelWidth = oldWidth;
        }

        void Draw_CustomPostProcess()
        {
            if(m_BeforePostProcessCustomPostProcesses == null)
                InitializeCustomPostProcessesLists();

            m_BeforeTransparentCustomPostProcesses.DoLayoutList();
            m_BeforeTAACustomPostProcesses.DoLayoutList();
            m_BeforePostProcessCustomPostProcesses.DoLayoutList();
            m_AfterPostProcessCustomPostProcesses.DoLayoutList();
        }

        void Draw_Resources()
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            var serializedObject = new SerializedObject(defaultSettings);
            var serialized = new SerializedHDDefaultSettings(serializedObject);

            // HDRenderPipelineUI.GeneralSection.Draw(serializedHDDefaultSettings, null);

            EditorGUILayout.PropertyField(serialized.renderPipelineResources,HDRenderPipelineUI.Styles.GeneralSection.renderPipelineResourcesContent);

            HDRenderPipeline hdrp = (RenderPipelineManager.currentPipeline as HDRenderPipeline);
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

            serializedObject.ApplyModifiedProperties();
            EditorGUIUtility.labelWidth = oldWidth;
        }

        void Draw_VolumeInspector()
        {
            var oldWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = Styles.labelWidth;

            EditorGUILayout.BeginHorizontal();
            var asset = defaultSettings.GetOrCreateDefaultVolumeProfile();
            var newAsset = (VolumeProfile)EditorGUILayout.ObjectField(Styles.defaultVolumeProfileLabel,asset,typeof(VolumeProfile),false);
            if(newAsset == null)
            {
                Debug.Log("Default Volume Profile Asset cannot be null. Rolling back to previous value.");
            }
            else if(newAsset != asset)
            {
                asset = newAsset;
                defaultSettings.defaultVolumeProfile = asset;
                EditorUtility.SetDirty(defaultSettings);
            }

            if(GUILayout.Button(EditorGUIUtility.TrTextContent("New","Create a new Volume Profile for default in your default resource folder (defined in Wizard)"),GUILayout.Width(38),GUILayout.Height(18)))
            {
                DefaultVolumeProfileCreator.CreateAndAssign(DefaultVolumeProfileCreator.Kind.Default,defaultSettings);
            }
            EditorGUILayout.EndHorizontal();

            // The state of the profile can change without the asset reference changing so in this case we need to reset the editor.
            if(m_CurrentVolumeProfileInstanceID != asset.GetInstanceID() && m_CachedDefaultVolumeProfileEditor != null)
            {
                m_CurrentVolumeProfileInstanceID = asset.GetInstanceID();
                m_CachedDefaultVolumeProfileEditor = null;
            }

            Editor.CreateCachedEditor(asset,Type.GetType("UnityEditor.Rendering.VolumeProfileEditor"),ref m_CachedDefaultVolumeProfileEditor);
            EditorGUIUtility.labelWidth -= 18;
            bool oldEnabled = GUI.enabled;
            GUI.enabled = AssetDatabase.IsOpenForEdit(asset);
            m_CachedDefaultVolumeProfileEditor.OnInspectorGUI();
            GUI.enabled = oldEnabled;
            EditorGUIUtility.labelWidth = oldWidth;

            EditorGUILayout.Space();

            EditorGUILayout.BeginHorizontal();
            var lookDevAsset = EditorDefaultSettings.GetOrAssignLookDevVolumeProfile();
            EditorGUIUtility.labelWidth = 221;
            var newLookDevAsset = (VolumeProfile)EditorGUILayout.ObjectField(Styles.lookDevVolumeProfileLabel,lookDevAsset,typeof(VolumeProfile),false);
            if(lookDevAsset == null)
            {
                Debug.Log("LookDev Volume Profile Asset cannot be null. Rolling back to previous value.");
            }
            else if(newLookDevAsset != lookDevAsset)
            {
                defaultSettings.defaultLookDevProfile = newLookDevAsset;
                EditorUtility.SetDirty(defaultSettings);
            }

            if(GUILayout.Button(EditorGUIUtility.TrTextContent("New","Create a new Volume Profile for default in your default resource folder (defined in Wizard)"),GUILayout.Width(38),GUILayout.Height(18)))
            {
                DefaultVolumeProfileCreator.CreateAndAssign(DefaultVolumeProfileCreator.Kind.LookDev,defaultSettings);
            }
            EditorGUILayout.EndHorizontal();

            Editor.CreateCachedEditor(lookDevAsset,Type.GetType("UnityEditor.Rendering.VolumeProfileEditor"),ref m_CachedLookDevVolumeProfileEditor);
            EditorGUIUtility.labelWidth -= 18;
            oldEnabled = GUI.enabled;
            GUI.enabled = AssetDatabase.IsOpenForEdit(asset);
            m_CachedLookDevVolumeProfileEditor.OnInspectorGUI();
            GUI.enabled = oldEnabled;
            EditorGUIUtility.labelWidth = oldWidth;

            if(lookDevAsset.Has<VisualEnvironment>())
                EditorGUILayout.HelpBox("VisualEnvironment is not modifiable and will be overridden by the LookDev",MessageType.Warning);
            if(lookDevAsset.Has<HDRISky>())
                EditorGUILayout.HelpBox("HDRISky is not modifiable and will be overridden by the LookDev",MessageType.Warning);
        }

        void Draw_DefaultFrameSettings()
        {
            var serializedObject = new SerializedObject(defaultSettings);
            var serialized = new SerializedHDDefaultSettings(serializedObject);
            FrameSettingsSection.Draw(serialized,null);
            serializedObject.ApplyModifiedProperties();
        }



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
            GUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(Styles.defaultFrameSettingsContent,EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();
            selectedFrameSettings = (SelectedFrameSettings)EditorGUILayout.EnumPopup(selectedFrameSettings);
            if(EditorGUI.EndChangeCheck())
                ApplyChangedDisplayedFrameSettings(serialized,owner);
            GUILayout.EndHorizontal();
        }

        enum Expandable
        {
            CameraFrameSettings = 1 << 0, //obsolete
            BakedOrCustomProbeFrameSettings = 1 << 1, //obsolete
            RealtimeProbeFrameSettings = 1 << 2, //obsolete
        }

        static readonly ExpandedState<Expandable,HDDefaultSettings> k_ExpandedState = new ExpandedState<Expandable,HDDefaultSettings>(Expandable.CameraFrameSettings, "HDRP");

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
    }

    class DefaultVolumeProfileCreator:ProjectWindowCallback.EndNameEditAction
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
                    settings.defaultVolumeProfile = profile;
                    break;
                case Kind.LookDev:
                    settings.defaultLookDevProfile = profile;
                    break;
            }
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

            var assetCreator = ScriptableObject.CreateInstance<DefaultVolumeProfileCreator>();
            assetCreator.SetKind(kind);
            ProjectWindowUtil.StartNameEditingIfProjectWindowExists(assetCreator.GetInstanceID(),assetCreator,$"Assets/{HDProjectSettings.projectSettingsFolderPath}/{GetDefaultName(kind)}.asset",null,null);
        }
    }
}
