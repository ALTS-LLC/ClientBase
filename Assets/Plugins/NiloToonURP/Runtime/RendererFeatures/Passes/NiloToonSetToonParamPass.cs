using System;
using System.Reflection;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace NiloToon.NiloToonURP
{
    public class NiloToonSetToonParamPass : ScriptableRenderPass
    {
        // renderer feature's public GUI settings
        [Serializable]
        public class Settings
        {
            [Header("Receive URP Shadow map")]

            [Tooltip( "- Turning ON allows NiloToon Characters to receive the URP shadow map, just like any Lit shader material.\n" +
                      "- Turning OFF disables NiloToon Characters from receiving the URP shadow map, which can improve performance.\n\n" +
                      "The default value is OFF because it generally looks better (visually more stable).\n" +
                      "This is due to potential uncertainties in the user's URP shadow map's resolution and quality.\n\n" +
                      "- If the URP shadow resolution is low, setting NiloToon Characters to not receive the URP shadow map is usually a better choice than receiving a low resolution, jagged URP shadow map.\n\n" +
                      "- However, if you are using a high-quality/high-resolution URP shadow map (e.g. 4 cascade + 4096 size + 10 Shadow Range), then you can turn this ON if it looks good.\n\n" +
                      "Default: OFF")]
            [OverrideDisplayName("Enable?")]
            public bool ShouldReceiveURPShadows = false;

            [Tooltip("Lower this value will fadeout URP shadow map for NiloToon Characters\n\n" +
                     "Default: 1 (apply 100% URP shadow map)")]
            [RangeOverrideDisplayName("     Intensity",0,1)]
            public float URPShadowIntensity = 1;
           
            [Tooltip("Applying extra 'X' meters depth bias offset on top of URP's depth bias for NiloToon characters.\n" +
                     "You can increase it for artistic reasons.\n\n" +
                     "For example,\n" +
                     "set to 2(meters) will force all NiloToon characters to only receive URP shadow map casted by far objects(e.g. tree and buildings) and not receiving URP shadow map that was cast by the character himself(self shadow).\n\n" +
                     "Default: 0 meter")]
            [RangeOverrideDisplayName("     DepthBias Extra",0,2)]
            public float URPShadowDepthBias = 0;

            [Tooltip("Apply X% URP normal bias for NiloToon characters.\n\n" +
                "Same as URP's normalBias:\n" +
                "- reduce it can avoid shadow holes when receiving URP shadow map, but shadow acne will appear\n" +
                "- increase it can add more normal bias for removing shadow acne, but shadow caster polygon will become smaller(e.g. very thin finger), and shadow holes may appear.\n\n" +
                "You can reset it to 1(default) if you are not sure about what value is good.\n\n" +
                "Default: 1 (apply 100% normal bias)")]
            [RangeOverrideDisplayName("     NormalBias Multiplier",0,2)]
            public float URPShadowNormalBiasMultiplier = 1;

            //--------------------------------------------------------------------------
            [Header("DepthTex Rim Light and Shadow")]

            [Tooltip("- turn ON will produce a more stable width 2D depth tex rim light and shadow (turn ON will auto request URP's depth texture, which is slower!)\n" +
                     "- turn OFF to fallback to classic fresnel(dot(N,V)) rim light, which will improve performance if you are targeting slow mobile\n\n" +
                "Default: ON")]
            [OverrideDisplayName("Enable?")]
            public bool EnableDepthTextureRimLigthAndShadow = true;

            [Tooltip("Controls the depth texture(screen space) rim light and shadow's width multiplier. You can edit it for artistic reason.\n\n" +
                     "Default: 1")]
            [RangeOverrideDisplayName("     Width",0,10)]
            public float DepthTextureRimLightAndShadowWidthMultiplier = 1;

            [Tooltip("How easy is rim light occluded by the character himself? You can increase it for artistic reason.\n" +
                     "When the value is high enough(e.g. 0.5 meter), rim light will be blocked by the character himself and only appear on the character silhouette edge.\n\n" +
                     "Default: 0(meter)")]
            [RangeOverrideDisplayName("     Rim Light Self Occlude",0,2)]
            public float DepthTexRimLightDepthDiffThresholdOffset = 0;

            //--------------------------------------------------------------------------
            [Header("Fix semi-transparent(but queue <2500) material")]

            [Tooltip("Enable this if you are in the following situation:\n\n" +
                "- a material(not limited to NiloToon) is using semi-transparent rendering(alpha blending with skybox) + render queue <= 2500\n" +
                "- and your camera is using Skybox Background Type\n\n" +
                "Which will produce random uncleared color on these semi-transparent material since the background color is unknown and not cleared," +
                "in this situation you should enable this toggle, which will draw an extra skybox first before any material draw, providing a correct background Skybox color for these kind of opaque queue + semi-transparent material.\n\n" +
                "*If you are very sure you don't need it, turn it off to improve performance.\n\n" +
                "Default: ON")]
            [OverrideDisplayName("Redraw Skybox before Opaque?")]
            public bool EnableSkyboxDrawBeforeOpaque = true;

            //--------------------------------------------------------------------------
            [Header("Debug GPU performance")]

            [Tooltip("Force NiloToonCharacter shader becomes an Unlit shader, used for debug vertex+fragment shader cost of NiloToonCharacter shader.\n\n" +
                     "Default: OFF")]
            [OverrideDisplayName("Min Char shader")]
            public bool ForceMinimumShader = false;

            [Tooltip("Force NiloToonEnvironment shader becomes a simple diffuse shader, used for debug vertex+fragment shader cost of NiloToonEnvironment shader.\n\n" +
                     "Default: OFF")]
            [OverrideDisplayName("Min Envi shader")]
            public bool ForceMinimumEnviShader = false;

            [Tooltip("Force disable Outline, used for debug the CPU&GPU cost of rendering 'Classic Outline pass'.\n\n" +
                     "Default: OFF")]
            [OverrideDisplayName("Remove outline")]
            public bool ForceNoOutline = false;
        }

        // [Debug shading API]
        // set by NiloToon's debug window only, not exposed in renderer feature's GUI
        public static bool EnableShadingDebug = false;
        public static ShadingDebugCase shadingDebugCase = ShadingDebugCase.JustBaseMap;
        public static bool ForceMinimumShader = false;

        public enum ShadingDebugCase
        {
            JustBaseMap = 0,
            Albedo_Alpha = 1,
            White = 2,
            Occlusion = 3,
            Emission = 4,
            NormalWS = 5,
            BaseMapUV = 6,
            VertexColorR = 7,
            VertexColorG = 8,
            VertexColorB = 9,
            VertexColorA = 10,
            VertexColorRGB = 11,
            Specular = 12,
            UV8 = 13,
            SimpleNdotL = 14,
            IsFace = 15,
            IsSkin = 16,
            Alpha = 17,
            NormalVS = 18,
            AverageURPShadow = 19,
            SH = 20,
            UV1 = 21,
            UV2 = 22,
            UV3 = 23,
            UV4 = 24,
            FogFactor = 25,
            ZOffset = 26,
            viewDirWS = 27,
            normalTS = 28,
            Smoothness = 29,
            Facing = 30,
            CharacterBound2DRectUV = 31,
            MatCapUV = 32,
        }

        // singleton
        public static NiloToonSetToonParamPass Instance => _instance;
        static NiloToonSetToonParamPass _instance;

        // setting
        public Settings Setting => this.allSettings.MiscSettings;
        NiloToonRendererFeatureSettings allSettings;

        // private field and public access
        bool isDeferredRendering;
        PropertyInfo actualRenderingMode_PropertyInfo;
        PropertyInfo useDepthPriming_PropertyInfo;
        Func<UniversalRenderer, RenderingMode> actualRenderingMode_Getter;
        Func<UniversalRenderer, bool> useDepthPriming_Getter;

        // profiling
        ProfilingSampler m_ProfilingSampler;

        // all Shader.PropertyToID
        static readonly int _GlobalReceiveURPShadowAmount_SID = Shader.PropertyToID("_GlobalReceiveShadowMappingAmount");
        static readonly int _GlobalReceiveSelfShadowMappingPosOffset_SID = Shader.PropertyToID("_GlobalReceiveSelfShadowMappingPosOffset");
        static readonly int _GlobalToonShaderNormalBiasMultiplier_SID = Shader.PropertyToID("_GlobalToonShaderNormalBiasMultiplier");
        static readonly int _GlobalMainLightURPShadowAsDirectResultTintColor_SID = Shader.PropertyToID("_GlobalMainLightURPShadowAsDirectResultTintColor");
        static readonly int _GlobalNiloToonReceiveURPShadowblurriness_SID = Shader.PropertyToID("_GlobalNiloToonReceiveURPShadowblurriness");
        static readonly int _GlobalURPShadowAsDirectLightTintIgnoreMaterialURPUsageSetting_SID = Shader.PropertyToID("_GlobalURPShadowAsDirectLightTintIgnoreMaterialURPUsageSetting");

        static readonly int _GlobalEnableDepthTextureRimLigthAndShadow_SID = Shader.PropertyToID("_GlobalEnableDepthTextureRimLigthAndShadow");
        
        static readonly int _GlobalToonShadeDebugCase_SID = Shader.PropertyToID("_GlobalToonShadeDebugCase");
        static readonly int _GlobalOcclusionStrength_SID = Shader.PropertyToID("_GlobalOcclusionStrength");

        static readonly int _GlobalIndirectLightMultiplier_SID = Shader.PropertyToID("_GlobalIndirectLightMultiplier");
        static readonly int _GlobalIndirectLightMinColor_SID = Shader.PropertyToID("_GlobalIndirectLightMinColor");
        static readonly int _GlobalIndirectLightMaxColor_SID = Shader.PropertyToID("_GlobalIndirectLightMaxColor");

        static readonly int _GlobalMainDirectionalLightMultiplier_SID = Shader.PropertyToID("_GlobalMainDirectionalLightMultiplier");
        static readonly int _GlobalMainDirectionalLightMaxContribution_SID = Shader.PropertyToID("_GlobalMainDirectionalLightMaxContribution");

        static readonly int _GlobalAdditionalLightMultiplier_SID = Shader.PropertyToID("_GlobalAdditionalLightMultiplier");
        static readonly int _GlobalAdditionalLightMultiplierForFaceArea_SID = Shader.PropertyToID("_GlobalAdditionalLightMultiplierForFaceArea");
        static readonly int _GlobalAdditionalLightMultiplierForOutlineArea_SID = Shader.PropertyToID("_GlobalAdditionalLightMultiplierForOutlineArea");
        static readonly int _GlobalAdditionalLightApplyRimMask_SID = Shader.PropertyToID("_GlobalAdditionalLightApplyRimMask");
        static readonly int _GlobalAdditionalLightRimMaskPower_SID = Shader.PropertyToID("_GlobalAdditionalLightRimMaskPower");
        static readonly int _GlobalAdditionalLightRimMaskSoftness_SID = Shader.PropertyToID("_GlobalAdditionalLightRimMaskSoftness");
        static readonly int _GlobalAdditionalLightMaxContribution_SID = Shader.PropertyToID("_GlobalAdditionalLightMaxContribution");

        static readonly int _GlobalCinematic3DRimMaskEnabled_SID = Shader.PropertyToID("_GlobalCinematic3DRimMaskEnabled");
        static readonly int _GlobalCinematic3DRimMaskStrength_ClassicStyle_SID = Shader.PropertyToID("_GlobalCinematic3DRimMaskStrength_ClassicStyle");
        static readonly int _GlobalCinematic3DRimMaskStrength_DynamicStyle_SID = Shader.PropertyToID("_GlobalCinematic3DRimMaskStrength_DynamicStyle");
        static readonly int _GlobalCinematic3DRimMaskSharpness_DynamicStyle_SID = Shader.PropertyToID("_GlobalCinematic3DRimMaskSharpness_DynamicStyle");
        static readonly int _GlobalCinematic3DRimMaskStrength_StableStyle_SID = Shader.PropertyToID("_GlobalCinematic3DRimMaskStrength_StableStyle");
        static readonly int _GlobalCinematic3DRimMaskSharpness_StableStyle_SID = Shader.PropertyToID("_GlobalCinematic3DRimMaskSharpness_StableStyle");
        static readonly int _GlobalCinematic2DRimMaskStrength_SID = Shader.PropertyToID("_GlobalCinematic2DRimMaskStrength");
        static readonly int _GlobalCinematicRimTintBaseMap_SID = Shader.PropertyToID("_GlobalCinematicRimTintBaseMap");
        
        static readonly int _GlobalAdditionalLightInjectIntoMainLightColor_Strength_SID = Shader.PropertyToID("_GlobalAdditionalLightInjectIntoMainLightColor_Strength");
        static readonly int _GlobalAdditionalLightInjectIntoMainLightDirection_Strength_SID = Shader.PropertyToID("_GlobalAdditionalLightInjectIntoMainLightDirection_Strength");
        static readonly int _GlobalAdditionalLightInjectIntoMainLightColor_AllowCloseLightOverBright_SID = Shader.PropertyToID("_GlobalAdditionalLightInjectIntoMainLightColor_AllowCloseLightOverBright");
        static readonly int _GlobalAdditionalLightInjectIntoMainLightColor_Desaturate_SID = Shader.PropertyToID("_GlobalAdditionalLightInjectIntoMainLightColor_Desaturate");
        
        static readonly int _GlobalSpecularTintColor_SID = Shader.PropertyToID("_GlobalSpecularTintColor");
        static readonly int _GlobalSpecularInShadowMinIntensity_SID = Shader.PropertyToID("_GlobalSpecularMinIntensity");
        static readonly int _GlobalSpecularReactToLightDirectionChange_SID = Shader.PropertyToID("_GlobalSpecularReactToLightDirectionChange");

        static readonly int _CurrentCameraFOV_SID = Shader.PropertyToID("_CurrentCameraFOV");
        static readonly int _GlobalAspectFix_SID = Shader.PropertyToID("_GlobalAspectFix");
        static readonly int _GlobalFOVorOrthoSizeFix_SID = Shader.PropertyToID("_GlobalFOVorOrthoSizeFix");
        static readonly int _GlobalDepthTexRimLightAndShadowWidthMultiplier_SID = Shader.PropertyToID("_GlobalDepthTexRimLightAndShadowWidthMultiplier");
        static readonly int _GlobalDepthTexRimLightDepthDiffThresholdOffset_SID = Shader.PropertyToID("_GlobalDepthTexRimLightDepthDiffThresholdOffset");
        static readonly int _GlobalUserOverriddenMainLightDirVS_SID = Shader.PropertyToID("_GlobalUserOverriddenMainLightDirVS");
        static readonly int _GlobalUserOverriddenMainLightDirWS_SID = Shader.PropertyToID("_GlobalUserOverriddenMainLightDirWS");
        static readonly int _GlobalUserOverriddenMainLightColor_SID = Shader.PropertyToID("_GlobalUserOverriddenMainLightColor");
        static readonly int _GlobalUserOverriddenFinalMainLightDirWSParam_SID = Shader.PropertyToID("_GlobalUserOverriddenFinalMainLightDirWSParam");
        static readonly int _GlobalUserOverriddenFinalMainLightColorParam_SID = Shader.PropertyToID("_GlobalUserOverriddenFinalMainLightColorParam");
        static readonly int _GlobalVolumeBaseColorTintColor_SID = Shader.PropertyToID("_GlobalVolumeBaseColorTintColor");
        static readonly int _GlobalVolumeMulColor_SID = Shader.PropertyToID("_GlobalVolumeMulColor");
        static readonly int _GlobalVolumeLerpColor_SID = Shader.PropertyToID("_GlobalVolumeLerpColor");
        static readonly int _GlobalRimLightMultiplier_SID = Shader.PropertyToID("_GlobalRimLightMultiplier");
        static readonly int _GlobalRimLightMultiplierForOutlineArea_SID = Shader.PropertyToID("_GlobalRimLightMultiplierForOutlineArea");
        static readonly int _GlobalDepthTexRimLightCameraDistanceFadeoutStartDistance_SID = Shader.PropertyToID("_GlobalDepthTexRimLightCameraDistanceFadeoutStartDistance");
        static readonly int _GlobalDepthTexRimLightCameraDistanceFadeoutEndDistance_SID = Shader.PropertyToID("_GlobalDepthTexRimLightCameraDistanceFadeoutEndDistance");
        static readonly int _GlobalCharacterOverallShadowTintColor_SID = Shader.PropertyToID("_GlobalCharacterOverallShadowTintColor");
        static readonly int _GlobalCharacterOverallShadowStrength_SID = Shader.PropertyToID("_GlobalCharacterOverallShadowStrength");

        static readonly int _NiloToonGlobalEnviGITintColor_SID = Shader.PropertyToID("_NiloToonGlobalEnviGITintColor");
        static readonly int _NiloToonGlobalEnviGIAddColor_SID = Shader.PropertyToID("_NiloToonGlobalEnviGIAddColor");
        static readonly int _NiloToonGlobalEnviGIOverride_SID = Shader.PropertyToID("_NiloToonGlobalEnviGIOverride");
        static readonly int _NiloToonGlobalEnviAlbedoOverrideColor_SID = Shader.PropertyToID("_NiloToonGlobalEnviAlbedoOverrideColor");
        static readonly int _NiloToonGlobalEnviMinimumShader_SID = Shader.PropertyToID("_NiloToonGlobalEnviMinimumShader");
        static readonly int _NiloToonGlobalEnviShadowBorderTintColor_SID = Shader.PropertyToID("_NiloToonGlobalEnviShadowBorderTintColor");
        static readonly int _NiloToonGlobalEnviSurfaceColorResultOverrideColor_SID = Shader.PropertyToID("_NiloToonGlobalEnviSurfaceColorResultOverrideColor");

        static readonly int _NiloToonGlobalAllowUnityCameraDepthTextureWriteOutlineExtrudedPosition_SID = Shader.PropertyToID("_NiloToonGlobalAllowUnityCameraDepthTextureWriteOutlineExtrudedPosition");

        static readonly int _NiloToonGlobalPerCharFaceForwardDirWSArray_SID = Shader.PropertyToID("_NiloToonGlobalPerCharFaceForwardDirWSArray");
        static readonly int _NiloToonGlobalPerCharFaceUpwardDirWSArray_SID = Shader.PropertyToID("_NiloToonGlobalPerCharFaceUpwardDirWSArray");
        static readonly int _NiloToonGlobalPerCharBoundCenterPosWSArray_SID = Shader.PropertyToID("_NiloToonGlobalPerCharBoundCenterPosWSArray");
        static readonly int _NiloToonGlobalPerCharHeadBonePosWSArray_SID = Shader.PropertyToID("_NiloToonGlobalPerCharHeadBonePosWSArray");
        
        static readonly int _NiloToonGlobalPerCharMainDirectionalLightTintColorArray_SID = Shader.PropertyToID("_NiloToonGlobalPerCharMainDirectionalLightTintColorArray");
        static readonly int _NiloToonGlobalPerCharMainDirectionalLightAddColorArray_SID = Shader.PropertyToID("_NiloToonGlobalPerCharMainDirectionalLightAddColorArray");
        
        static readonly int _NiloToonGlobalPerUnityLightDataArray_SID = Shader.PropertyToID("_NiloToonGlobalPerUnityLightDataArray");
        private static readonly int _NiloToonGlobalPerUnityLightDataArray2_SID = Shader.PropertyToID("_NiloToonGlobalPerUnityLightDataArray2");
        
        // Constructor(will not call every frame)
        // *Be careful when calling VolumeManager in constructor, since VolumeManager can be not yet ready to use.
        public NiloToonSetToonParamPass(NiloToonRendererFeatureSettings allSettings)
        {
            this.allSettings = allSettings;
            _instance = this;
            m_ProfilingSampler = new ProfilingSampler(typeof(NiloToonSetToonParamPass).Name);

            // when user is editing NiloToon all in one renderer feature UI's value, URP will recreate this class's instance again and again each frame,
            // so make sure all private fields of this class are re-assign with correct value on instance recreate
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureInputs(renderingData.cameraData.renderer);
        }

        public void ConfigureInputs(ScriptableRenderer renderer)
        {
            var input = GetScriptableRenderPassInput(renderer);

            ConfigureInput(input);
        }

        private ScriptableRenderPassInput GetScriptableRenderPassInput(ScriptableRenderer renderer)
        {
            ScriptableRenderPassInput input = ScriptableRenderPassInput.None;
            
            // call per frame and cache result
            isDeferredRendering = IsDeferredRendering(renderer);

            if (GetEnableDepthTextureRimLigthAndShadow(Setting))
            {
                // we automatically turn on _CameraDepthTexture if needed
                // so user don't need to turn on depth texture in their Universal Render Pipeline Asset manually.
                input |= ScriptableRenderPassInput.Depth;

                // although NiloToon don't need to reply on ScriptableRenderPassInput.Normal in any situations,
                // similar to URP12 SSAO's code, SSAO will request ScriptableRenderPassInput.Normal, here we request Normal also, this will force URP to do a _CameraDepthTexture prepass in deferred rendering,
                // which is needed by NiloToon's depth tex rim light and shadow
                // else depth tex rim light will render as complete white in deferred rendering
                if (isDeferredRendering)
                {
                    input |= ScriptableRenderPassInput.Normal;
                }
            }

            return input;
        }

        private bool IsDeferredRendering(ScriptableRenderer renderer)
        {
            // [find out is Deferred Rendering or not]
            // we want to get UniversalRenderer.cs's actualRenderingMode, but it is internal,
            // so we use reflection to get it
            string actualRenderingMode_PropertyName = "actualRenderingMode";
#if UNITY_2022_1_OR_NEWER
                // in Unity2022.1, URP renamed URP12's actualRenderingMode to URP13's renderingModeActual
                actualRenderingMode_PropertyName = "renderingModeActual";
#endif

            if (actualRenderingMode_PropertyInfo == null)
            {
                // cache PropertyInfo to reduce GC alloc
                actualRenderingMode_PropertyInfo = renderer.GetType().GetProperty(actualRenderingMode_PropertyName, BindingFlags.NonPublic | BindingFlags.Instance);
            }
            if (actualRenderingMode_Getter == null)
            {
                // cache Delegate to reduce GC alloc
                MethodInfo getterMethod = actualRenderingMode_PropertyInfo.GetGetMethod(true);
                actualRenderingMode_Getter = (Func<UniversalRenderer, RenderingMode>)Delegate.CreateDelegate(typeof(Func<UniversalRenderer, RenderingMode>), getterMethod);
            }
            // PropertyInfo.GetValue(...) is reflection, which will have GC allocate per frame,
            // but currently we don't have a way to avoid GC and at the same time ensure correct "actualRenderingMode" per frame,
            // since it is not safe the cache the result of GetValue(...) as user may change it anytime
            RenderingMode actualRenderingMode = actualRenderingMode_Getter(renderer as UniversalRenderer);
            //RenderingMode actualRenderingMode = (RenderingMode)actualRenderingMode_PropertyInfo.GetValue(renderer); // this also works and it is much more simpler, but will GC alloc more

            bool isDeferredRendering = actualRenderingMode == RenderingMode.Deferred;
            return isDeferredRendering;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            setParam(context, renderingData);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            NiloToonPlanarReflectionHelper.EndPlanarReflectionCameraRender(ref cmd);
        }

        private static bool GetEnableDepthTextureRimLigthAndShadow(Settings settings)
        {
            // VolumeManager null check, since this method can be called by RendererPass's constructor, which is not safe to call VolumeManager
            var performanceSettingVolume = VolumeManager.instance?.stack?.GetComponent<NiloToonRenderingPerformanceControlVolume>();
            if(!performanceSettingVolume) return false;

            if (performanceSettingVolume.overrideEnableDepthTextureRimLigthAndShadow.overrideState)
            {
                // If overridden in volume, use the overridden settings from the volume.
                return performanceSettingVolume.overrideEnableDepthTextureRimLigthAndShadow.value;
            }
            else
            {
                // if not overridden in volume, use NiloToon all in one renderer feature's setting
                return settings.EnableDepthTextureRimLigthAndShadow;
            }
        }

        private PassData passData;
        
        private void setParam(ScriptableRenderContext context, RenderingData renderingData)
        {
            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            {
                /*
                Note : should always ExecuteCommandBuffer at least once before using
                ScriptableRenderContext functions (e.g. DrawRenderers) even if you 
                don't queue any commands! This makes sure the frame debugger displays 
                everything under the correct title.
                */
                // https://www.cyanilux.com/tutorials/custom-renderer-features/?fbclid=IwAR27j2f3VVo0IIYDa32Dh76G9KPYzwb8j1J5LllpSnLXJiGf_UHrQ_lDtKg
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                if (passData == null)
                {
                    passData = new PassData();
                }
                passData.camera = renderingData.cameraData.camera;
                passData.additionalLightsCount = renderingData.lightData.additionalLightsCount;
                passData.mainLightIndex = renderingData.lightData.mainLightIndex;
                passData.Setting = Setting;
                passData.visibleLights = renderingData.lightData.visibleLights;
                passData.renderer = renderingData.cameraData.renderer;
                passData.needCorrectDepthTextureDepthWrite = GetNeedCorrectDepthTextureDepthWrite(renderingData.cameraData.renderer);
                
                FillInPassDataArray(passData);
                
                cmd = ExecutePassShared(cmd, passData);

                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }
            
            // must write these line after using{} finished, to ensure profiler and frame debugger display correctness
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        public class PassData
        {
            public Camera camera;
            public int additionalLightsCount;
            public int mainLightIndex;
            public Settings Setting;
            public NativeArray<VisibleLight> visibleLights;
            public ScriptableRenderer renderer;
            public bool needCorrectDepthTextureDepthWrite;
            
            public Vector4[] perCharFaceForwardDirWSArray;
            public Vector4[] perCharFaceUpwardDirWSArray;
            public Vector4[] perCharBoundCenterPosWSArray;
            public Vector4[] perCharPerspectiveRemovalCenterPosWSArray;
            
            public Vector4[] finalPerCharMainDirectionalLightTintColorArray;
            public Vector4[] finalPerCharMainDirectionalLightAddColorArray;
                
            public Vector4[] perUnityVisibleLightNiloToonDataArray;
            public Vector4[] perUnityVisibleLightNiloToonDataArray2;
        }

        public bool GetNeedCorrectDepthTextureDepthWrite(ScriptableRenderer inputRenderer)
        {
            bool isForwardDepthPrimingModeActive = false;

            // URP added deferred rendering & forward depth priming in 2021.3LTS (URP12)
            // NiloToon's character shader will support these options in the following code.
            // If deferred rendering or forward depth priming is active,
            // we need to force NiloToon's character shader's _CameraDepthTexture's write 100% equals character's body depth(not outline's extrude depth)
            // else _CameraDepthTexture's value(outline's depth) will block _CameraColorTexture's body rendering due to depth priming ZTest 
            UniversalRenderer renderer = inputRenderer as UniversalRenderer;

            // [find out is "Forward Rendering + depthPrimingMode active" or not]
            if (!isDeferredRendering)
            {
                // we want to get UniversalRenderer.cs's useDepthPriming, but it is internal,
                // so we use reflection to get it
                if (useDepthPriming_PropertyInfo == null)
                {
                    useDepthPriming_PropertyInfo = renderer.GetType().GetProperty("useDepthPriming", BindingFlags.NonPublic | BindingFlags.Instance);
                }
                if (useDepthPriming_Getter == null)
                {
                    // cache Delegate to reduce GC alloc
                    MethodInfo getterMethod = useDepthPriming_PropertyInfo.GetGetMethod(true);
                    useDepthPriming_Getter = (Func<UniversalRenderer, bool>)Delegate.CreateDelegate(typeof(Func<UniversalRenderer, bool>), getterMethod);
                }
                isForwardDepthPrimingModeActive = useDepthPriming_Getter(renderer);
                //isForwardDepthPrimingModeActive = (bool)useDepthPriming_PropertyInfo.GetValue(renderer);
            }
            bool needCorrectDepthTextureDepthWrite = isDeferredRendering || isForwardDepthPrimingModeActive;
            
            if(!isDeferredRendering && isForwardDepthPrimingModeActive)
            {
                Debug.LogWarning("[NiloToon]Forward/Forward+ together with DepthPriming will make face pixel's rendering rejected, you need to set (DepthPrimingMode = Disable) when using NiloToon_Character's 'IsFace?' feature.");
            }
            
            return needCorrectDepthTextureDepthWrite;
        }
#if UNITY_6000_0_OR_NEWER
        static void ExecutePass(PassData passData, UnsafeGraphContext context)
        {
            // Convert UnsafeCommandBuffer to a regular CommandBuffer, our SetParamStaticShared use the regular CommandBuffer
            CommandBuffer unsafeCommandBuffer = CommandBufferHelpers.GetNativeCommandBuffer(context.cmd);
            
            ExecutePassShared(unsafeCommandBuffer, passData);
        }
#endif
        private static CommandBuffer ExecutePassShared(CommandBuffer cmd, PassData passData)
        {
            var Setting = passData.Setting;
            
            // these volumes will not be null, even if Volume doesn't exist in scene
            var charRenderingControlVolume = VolumeManager.instance.stack.GetComponent<NiloToonCharRenderingControlVolume>();
            var cinematicAdditionalLightVolume = VolumeManager.instance.stack.GetComponent<NiloToonCinematicRimLightVolume>();
            var additionalLightStyleVolume = VolumeManager.instance.stack.GetComponent<NiloToonAdditionalLightStyleVolume>();
            var shadowControlVolume = VolumeManager.instance.stack.GetComponent<NiloToonShadowControlVolume>();
            var environmentControlVolume = VolumeManager.instance.stack.GetComponent<NiloToonEnvironmentControlVolume>();
            var bloomVolume = VolumeManager.instance.stack.GetComponent<NiloToonBloomVolume>();
                
            //////////////////////////////////////////////////////////////////////////////////////////////////
            // special process for any possible planar reflection camera
            //////////////////////////////////////////////////////////////////////////////////////////////////  
            if (NiloToonPlanarReflectionHelper.IsPlanarReflectionCamera(passData.camera))
            {
                NiloToonPlanarReflectionHelper.BeginPlanarReflectionCameraRender(ref cmd);
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // additional light keyword on/off
            //////////////////////////////////////////////////////////////////////////////////////////////////
            // see URP's ForwardLights.cs -> SetUp(), where URP enable additional light keywords
            // here we combine URP's per vertex / per pixel keywords into 1 keyword, to reduce multi_compile shader variant count, since we always need vertex light only by design
            CoreUtils.SetKeyword(cmd, "_NILOTOON_ADDITIONAL_LIGHTS", passData.additionalLightsCount > 0);

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // URP shadow related global settings
            //////////////////////////////////////////////////////////////////////////////////////////////////
            // [findout main light's shadow is enabled or not]
            // see URP's MainLightShadowCasterPass.cs -> RenderMainLightCascadeShadowmap(), where URP enable main light shadow keyword
            int shadowLightIndex = passData.mainLightIndex;
            bool mainLightEnabledShadow = shadowLightIndex != -1;

            // volume override URP shadow related params
            bool receiveURPShadow = shadowControlVolume.receiveURPShadow.overrideState ? shadowControlVolume.receiveURPShadow.value : Setting.ShouldReceiveURPShadows;
            float URPShadowIntensity = shadowControlVolume.URPShadowIntensity.overrideState ? shadowControlVolume.URPShadowIntensity.value : Setting.URPShadowIntensity;
            Color URPShadowAsDirectLightTintColor = shadowControlVolume.URPShadowAsDirectLightTintColor.value;
            URPShadowAsDirectLightTintColor *= shadowControlVolume.URPShadowAsDirectLightMultiplier.value;
            float ReceiveURPShadowblurriness = shadowControlVolume.URPShadowblurriness.value * 0.5f; // in shader, we want 0 ~ 0.5, not 0 ~ 1
            float URPShadowAsDirectLightTintIgnoreMaterialURPUsageSetting = shadowControlVolume.URPShadowAsDirectLightTintIgnoreMaterialURPUsageSetting.value;

            // here we combine URP's main light shadow keyword and our ShouldReceiveURPShadows keyword into 1, to reduce the number of multi_compile shader variant 
            CoreUtils.SetKeyword(cmd, "_NILOTOON_RECEIVE_URP_SHADOWMAPPING", receiveURPShadow && mainLightEnabledShadow);
            cmd.SetGlobalFloat(_GlobalReceiveURPShadowAmount_SID, URPShadowIntensity);
            cmd.SetGlobalFloat(_GlobalToonShaderNormalBiasMultiplier_SID, Setting.URPShadowNormalBiasMultiplier);
            cmd.SetGlobalFloat(_GlobalReceiveSelfShadowMappingPosOffset_SID, Setting.URPShadowDepthBias);
            cmd.SetGlobalColor(_GlobalMainLightURPShadowAsDirectResultTintColor_SID, URPShadowAsDirectLightTintColor);
            cmd.SetGlobalFloat(_GlobalNiloToonReceiveURPShadowblurriness_SID, ReceiveURPShadowblurriness);
            cmd.SetGlobalFloat(_GlobalURPShadowAsDirectLightTintIgnoreMaterialURPUsageSetting_SID, URPShadowAsDirectLightTintIgnoreMaterialURPUsageSetting);

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // RT(screen) aspect
            //////////////////////////////////////////////////////////////////////////////////////////////////
            cmd.SetGlobalVector(_GlobalAspectFix_SID, new Vector2((float)passData.camera.pixelHeight / (float)passData.camera.pixelWidth, 1f));

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // camera fov
            //////////////////////////////////////////////////////////////////////////////////////////////////
            Camera camera = passData.camera;

            // we need to calculate fov here, because if camera is using "Physical Camera", camera.fieldOfView is not correct, while camera.projectionMatrix is always correct
            float cameraFieldOfView = 2 * Mathf.Atan(1f / camera.projectionMatrix.m11) * 180 / Mathf.PI;
            cmd.SetGlobalFloat(_GlobalFOVorOrthoSizeFix_SID, 1f / (camera.orthographic ? camera.orthographicSize * 100f : cameraFieldOfView));
            cmd.SetGlobalFloat(_CurrentCameraFOV_SID, cameraFieldOfView);

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // main light shader data override
            //////////////////////////////////////////////////////////////////////////////////////////////////

            // init with values when no light information(Light/NiloToonCharacterMainLightOverrider/NiloToonCharRenderingControlVolume) exist
            Vector3 finalMainLightShaderDirWS = Vector3.up;
            Color finalMainLightColor = Color.black;

            // [0.fill with URP's main light first]
            int mainLightIndex = passData.mainLightIndex;
                
            // Note: when mainLight doesn't exist, mainLightIndex will be -1
            bool isURPMainLightExist = mainLightIndex != -1;
            if (isURPMainLightExist)
            {
                // mainLight exist, get it's direction and color
                VisibleLight mainLight = passData.visibleLights[mainLightIndex];
                finalMainLightShaderDirWS = -mainLight.light.transform.forward;
                finalMainLightColor = mainLight.finalColor;
                    
                // [1.Light modifier]
                float finalContributeIntoMainLightColor = 1;
                float finalContributeIntoMainLightDirection = 1;
                float finalContributeIntoMainLightColorApplyDesaturation = 0;
                    
                int activeLightModifierScriptCount = NiloToonLightSourceModifier.AllNiloToonLightModifiers.Count;
                for (int lightModifierIndex = 0; lightModifierIndex < activeLightModifierScriptCount; lightModifierIndex++)
                {
                    NiloToonLightSourceModifier script = NiloToonLightSourceModifier.AllNiloToonLightModifiers[lightModifierIndex];

                    if (!script) continue;

                    if (script.AffectsLight(mainLight.light))
                    {
                        finalContributeIntoMainLightColor *= script.contributeToMainLightColor;
                        finalContributeIntoMainLightDirection *= script.contributeToMainLightDirection;
                        finalContributeIntoMainLightColorApplyDesaturation = Mathf.Lerp(finalContributeIntoMainLightColorApplyDesaturation, 1, script.applyDesaturateWhenContributeToMainLightColor);
                    }
                }

                finalMainLightColor *= finalContributeIntoMainLightColor;
                finalMainLightShaderDirWS *= finalContributeIntoMainLightDirection;
                finalMainLightColor = Color.Lerp(finalMainLightColor, finalMainLightColor.grayscale * Color.white, finalContributeIntoMainLightColorApplyDesaturation);
            }
                
            // [2.NiloToonCharacterMainLightOverrider override (before volume override)]
            {
                NiloToonCharacterMainLightOverrider mainLightOverrider = 
                    NiloToonCharacterMainLightOverrider.GetHighestPriorityOverrider(NiloToonCharacterMainLightOverrider.OverrideTiming.BeforeVolumeOverride);
                if (mainLightOverrider)
                {
                    if(mainLightOverrider.overrideDirection)
                    {
                        finalMainLightShaderDirWS = mainLightOverrider.shaderLightDirectionWS;
                    }
                    if (mainLightOverrider.overrideColorAndIntensity)
                    {
                        finalMainLightColor = mainLightOverrider.finalColor;
                    }
                }
            }
                
            // [3.NiloToonCharRenderingControlVolume override]
            Vector3 finalMainLightDirVS = camera.worldToCameraMatrix.MultiplyVector(finalMainLightShaderDirWS);

            Matrix4x4 volumeOverrideRotationMatrixVS = Matrix4x4.Rotate(Quaternion.Euler(new Vector3(charRenderingControlVolume.overridedLightUpDownAngle.value, charRenderingControlVolume.overridedLightLRAngle.value, 0)));
            Vector3 volumeOverridedLightDirVS = volumeOverrideRotationMatrixVS.MultiplyVector(Vector3.forward);
            Vector3 volumeOverridedLightDirWS = camera.cameraToWorldMatrix.MultiplyVector(volumeOverridedLightDirVS);

            float volumeOverrideIntensity = charRenderingControlVolume.overrideLightDirectionIntensity.value;
            finalMainLightShaderDirWS = Vector3.Slerp(finalMainLightShaderDirWS, volumeOverridedLightDirWS, volumeOverrideIntensity).normalized;
            finalMainLightDirVS = Vector3.Slerp(finalMainLightDirVS, volumeOverridedLightDirVS, volumeOverrideIntensity).normalized;

            finalMainLightColor = Color.Lerp(finalMainLightColor, charRenderingControlVolume.overridedLightColor.value, charRenderingControlVolume.lightColorOverrideStrength.value);

            // [4.volume - desaturate, then add]
            finalMainLightColor = Color.Lerp(finalMainLightColor, Color.white * finalMainLightColor.grayscale,charRenderingControlVolume.desaturateLightColor.value);
            finalMainLightColor += charRenderingControlVolume.addMainLightColor.value;

            // [5.NiloToonCharacterMainLightOverrider override (after volume override)]
            {
                NiloToonCharacterMainLightOverrider mainLightOverrider = 
                    NiloToonCharacterMainLightOverrider.GetHighestPriorityOverrider(NiloToonCharacterMainLightOverrider.OverrideTiming.AfterVolumeOverride);
                if (mainLightOverrider)
                {
                    if (mainLightOverrider.overrideDirection)
                    {
                        finalMainLightShaderDirWS = mainLightOverrider.shaderLightDirectionWS;
                    }

                    if (mainLightOverrider.overrideColorAndIntensity)
                    {
                        finalMainLightColor = mainLightOverrider.finalColor;
                    }
                }
            }
                
            // [6. set value to shader]
            cmd.SetGlobalVector(_GlobalUserOverriddenMainLightDirWS_SID, finalMainLightShaderDirWS);
            cmd.SetGlobalVector(_GlobalUserOverriddenMainLightDirVS_SID, finalMainLightDirVS);
            cmd.SetGlobalVector("_GlobalUserOverridedMainLightDirWS", finalMainLightShaderDirWS); // only to supprt 0.13.8 shader
            cmd.SetGlobalVector("_GlobalUserOverridedMainLightDirVS", finalMainLightDirVS); // only to supprt 0.13.8 shader
            cmd.SetGlobalColor(_GlobalUserOverriddenMainLightColor_SID, finalMainLightColor);

            // [7. final (post additional light injection) light direction + color override]
            {
                NiloToonCharacterMainLightOverrider mainLightOverrider = 
                    NiloToonCharacterMainLightOverrider.GetHighestPriorityOverrider(NiloToonCharacterMainLightOverrider.OverrideTiming.AfterEverything);
                    
                Vector4 finalMainLightDirWSParamVector = Vector4.zero;
                Color finalMainLightColorParamVector = Vector4.zero;
                if (mainLightOverrider)
                {
                    if (mainLightOverrider.overrideTiming == NiloToonCharacterMainLightOverrider.OverrideTiming.AfterEverything)
                    {
                        if(mainLightOverrider.overrideDirection)
                        {
                            var dir = mainLightOverrider.shaderLightDirectionWS;
                            finalMainLightDirWSParamVector.x = dir.x;
                            finalMainLightDirWSParamVector.y = dir.y;
                            finalMainLightDirWSParamVector.z = dir.z;
                            finalMainLightDirWSParamVector.w = 1;
                        }
                        if (mainLightOverrider.overrideColorAndIntensity)
                        {
                            finalMainLightColorParamVector = mainLightOverrider.finalColor;
                            finalMainLightColorParamVector.a = 1;
                        }
                    }
                }
                    
                cmd.SetGlobalVector(_GlobalUserOverriddenFinalMainLightDirWSParam_SID, finalMainLightDirWSParamVector);
                cmd.SetGlobalVector(_GlobalUserOverriddenFinalMainLightColorParam_SID, finalMainLightColorParamVector);
            }

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // 2D depth texture rim light (set global uniform)
            //////////////////////////////////////////////////////////////////////////////////////////////////
            cmd.SetGlobalFloat(_GlobalDepthTexRimLightAndShadowWidthMultiplier_SID, Setting.DepthTextureRimLightAndShadowWidthMultiplier * charRenderingControlVolume.depthTextureRimLightAndShadowWidthMultiplier.value * 1.25f); // 1.25 to make default == 1 on user's side GUI
            cmd.SetGlobalFloat(_GlobalDepthTexRimLightDepthDiffThresholdOffset_SID, Setting.DepthTexRimLightDepthDiffThresholdOffset + charRenderingControlVolume.depthTexRimLightDepthDiffThresholdOffset.value);

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // 2D depth texture rim light and shadow (let NiloToonPerCharacterRenderController knows enableDepthTextureRimLigthAndShadow's value)
            //////////////////////////////////////////////////////////////////////////////////////////////////
            cmd.SetGlobalFloat(_GlobalEnableDepthTextureRimLigthAndShadow_SID, GetEnableDepthTextureRimLigthAndShadow(Setting) ? 1 : 0);
                
            //////////////////////////////////////////////////////////////////////////////////////////////////
            // debug case
            //////////////////////////////////////////////////////////////////////////////////////////////////
            cmd.SetGlobalFloat(_GlobalToonShadeDebugCase_SID, (int)shadingDebugCase);
            CoreUtils.SetKeyword(cmd, "_NILOTOON_DEBUG_SHADING", EnableShadingDebug);

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // force minium shader
            //////////////////////////////////////////////////////////////////////////////////////////////////
            CoreUtils.SetKeyword(cmd, "_NILOTOON_FORCE_MINIMUM_SHADER", Setting.ForceMinimumShader || ForceMinimumShader);

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // rendering path related
            //////////////////////////////////////////////////////////////////////////////////////////////////
            
            cmd.SetGlobalFloat(_NiloToonGlobalAllowUnityCameraDepthTextureWriteOutlineExtrudedPosition_SID, passData.needCorrectDepthTextureDepthWrite ? 0 : 1);
                
            //////////////////////////////////////////////////////////////////////////////////////////////////
            // Resolve LightModifier(s) for Main Light's rim multiplier
            //////////////////////////////////////////////////////////////////////////////////////////////////
            float mainLightRimMultiplier = 1;
            if (mainLightIndex != -1)
            {
                // mainLight exist, get it's direction and color
                VisibleLight mainLight = passData.visibleLights[mainLightIndex];
                int activeLightModifierScriptCount = NiloToonLightSourceModifier.AllNiloToonLightModifiers.Count;

                for (int lightModifierIndex = 0; lightModifierIndex < activeLightModifierScriptCount; lightModifierIndex++)
                {
                    NiloToonLightSourceModifier script = NiloToonLightSourceModifier.AllNiloToonLightModifiers[lightModifierIndex];

                    if (!script) continue;

                    if (script.AffectsLight(mainLight.light))
                    {
                        mainLightRimMultiplier *= script.contributeToAdditiveOrRimLightIntensity;
                    }
                }
            }
            //////////////////////////////////////////////////////////////////////////////////////////////////
            // Note:
            // don't need to check null for Volume,
            // because Volume will not be null even when no volume exists in scene
            //////////////////////////////////////////////////////////////////////////////////////////////////

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // global volume (NiloToonCharRenderingControlVolume)
            //////////////////////////////////////////////////////////////////////////////////////////////////

            var c = charRenderingControlVolume;
            Color lerpColorResult;
            lerpColorResult = c.charLerpColor.value;
            lerpColorResult.a = c.charLerpColorUsage.value;
            cmd.SetGlobalColor(_GlobalVolumeLerpColor_SID, lerpColorResult);

            cmd.SetGlobalColor(_GlobalVolumeBaseColorTintColor_SID, c.charBaseColorMultiply.value * c.charBaseColorTintColor.value * bloomVolume.characterBaseColorBrightness.value);
            cmd.SetGlobalColor(_GlobalVolumeMulColor_SID, c.charMulColor.value * c.charMulColorIntensity.value * bloomVolume.characterBrightness.value);
            cmd.SetGlobalFloat(_GlobalOcclusionStrength_SID, c.charOcclusionUsage.value);
            cmd.SetGlobalFloat(_GlobalIndirectLightMultiplier_SID, c.charIndirectLightMultiplier.value);
            cmd.SetGlobalColor(_GlobalIndirectLightMinColor_SID, c.charIndirectLightMinColor.value);
            cmd.SetGlobalColor(_GlobalIndirectLightMaxColor_SID,c.charIndirectLightMaxColor.value);
            cmd.SetGlobalColor(_GlobalMainDirectionalLightMultiplier_SID, c.mainDirectionalLightIntensityMultiplier.value * c.mainDirectionalLightIntensityMultiplierColor.value);
            cmd.SetGlobalColor(_GlobalMainDirectionalLightMaxContribution_SID, c.mainDirectionalLightMaxContribution.value * c.mainDirectionalLightMaxContributionColor.value);
            cmd.SetGlobalColor(_GlobalAdditionalLightMultiplier_SID, c.additionalLightIntensityMultiplier.value * c.additionalLightIntensityMultiplierColor.value * cinematicAdditionalLightVolume.lightIntensityMultiplier.value);
            cmd.SetGlobalColor(_GlobalAdditionalLightMultiplierForFaceArea_SID, c.additionalLightIntensityMultiplierForFaceArea.value * c.additionalLightIntensityMultiplierColorForFaceArea.value);
            cmd.SetGlobalColor(_GlobalAdditionalLightMultiplierForOutlineArea_SID, c.additionalLightIntensityMultiplierForOutlineArea.value * c.additionalLightIntensityMultiplierColorForOutlineArea.value);
            cmd.SetGlobalFloat(_GlobalAdditionalLightApplyRimMask_SID, c.additionalLightApplyRimMask.value);
            cmd.SetGlobalFloat(_GlobalAdditionalLightRimMaskPower_SID, c.additionalLightRimMaskPower.value);
            cmd.SetGlobalFloat(_GlobalAdditionalLightRimMaskSoftness_SID, c.additionalLightRimMaskSoftness.value * 0.5f);
            cmd.SetGlobalColor(_GlobalAdditionalLightMaxContribution_SID, c.additionalLightMaxContribution.value * c.additionalLightMaxContributionColor.value + Color.white * 100 * (cinematicAdditionalLightVolume.strengthRimMask3D_DynmaicStyle.value+ cinematicAdditionalLightVolume.strengthRimMask3D_StableStyle.value));
            cmd.SetGlobalColor(_GlobalRimLightMultiplier_SID, c.charRimLightMultiplier.value * c.charRimLightTintColor.value * mainLightRimMultiplier);
            cmd.SetGlobalColor(_GlobalRimLightMultiplierForOutlineArea_SID, c.charRimLightMultiplierForOutlineArea.value * c.charRimLightTintColorForOutlineArea.value);
            cmd.SetGlobalFloat(_GlobalDepthTexRimLightCameraDistanceFadeoutStartDistance_SID, c.charRimLightCameraDistanceFadeoutStartDistance.value);
            cmd.SetGlobalFloat(_GlobalDepthTexRimLightCameraDistanceFadeoutEndDistance_SID, c.charRimLightCameraDistanceFadeoutEndDistance.value);
            cmd.SetGlobalColor(_GlobalSpecularTintColor_SID, c.specularIntensityMultiplier.value * c.specularTintColor.value);
            cmd.SetGlobalFloat(_GlobalSpecularInShadowMinIntensity_SID, c.specularInShadowMinIntensity.value);
            cmd.SetGlobalFloat(_GlobalSpecularReactToLightDirectionChange_SID, c.specularReactToLightDirectionChange.value ? 1 : 0);
            cmd.SetGlobalColor(_GlobalCharacterOverallShadowTintColor_SID, c.characterOverallShadowTintColor.value * shadowControlVolume.characterOverallShadowTintColor.value);
            cmd.SetGlobalFloat(_GlobalCharacterOverallShadowStrength_SID, c.characterOverallShadowStrength.value * shadowControlVolume.characterOverallShadowStrength.value);

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // global volume (NiloToonCinematicAdditionalLightVolume)
            //////////////////////////////////////////////////////////////////////////////////////////////////
            float finalCinematic3DStrength_ClassicStyle = Mathf.Pow(cinematicAdditionalLightVolume.strengthRimMask3D_ClassicStyle.value, 0.1f);
            float finalCinematic3DStrength_DynamicStyle = Mathf.Pow(cinematicAdditionalLightVolume.strengthRimMask3D_DynmaicStyle.value, 0.1f);
            float finalCinematic3DStrength_StableStyle = Mathf.Pow(cinematicAdditionalLightVolume.strengthRimMask3D_StableStyle.value, 0.1f);
            float finalCinematic2DStrength = cinematicAdditionalLightVolume.strengthRimMask2D.value;
                
            float finalCinematic3DSharpness_DynamicStyle = Mathf.Lerp(4f, 19 * 4f, cinematicAdditionalLightVolume.sharpnessRimMask3D_DynamicStyle.value);
            float finalCinematic3DSharpness_StableStyle = Mathf.Lerp(5f, 15f, cinematicAdditionalLightVolume.sharpnessRimMask3D_StableStyle.value);

            if(cinematicAdditionalLightVolume.enableStyleSafeGuard.value)
            {
                float maxStrengthFromAllStyle = 
                    Mathf.Max(finalCinematic3DStrength_ClassicStyle,
                        Mathf.Max(finalCinematic3DStrength_DynamicStyle,
                            Mathf.Max(finalCinematic3DStrength_StableStyle, 
                                finalCinematic2DStrength)));
                    
                if (maxStrengthFromAllStyle > 0)
                {
                    // [When any style is active, find the highest strength style, max it's strength to 1]

                    // when 2 styles has the same highest strength, we pick only one of them, the picking order is the order of if-else chain,
                    // the order is:
                    // 1->2D
                    // 2->3D (Dynamic)
                    // 3->3D (Classic)
                    // 4->3D (Stable)
                    if (Math.Abs(finalCinematic2DStrength - maxStrengthFromAllStyle) < 0.001f)
                    {
                        finalCinematic2DStrength = 1;
                    }
                    else if (Math.Abs(finalCinematic3DStrength_DynamicStyle - maxStrengthFromAllStyle) < 0.001f)
                    {
                        finalCinematic3DStrength_DynamicStyle = 1;
                    }
                    else if (Math.Abs(finalCinematic3DStrength_ClassicStyle - maxStrengthFromAllStyle) < 0.001f)
                    {
                        finalCinematic3DStrength_ClassicStyle = 1;
                    }
                    else if (Math.Abs(finalCinematic3DStrength_StableStyle - maxStrengthFromAllStyle) < 0.001f)
                    {
                        finalCinematic3DStrength_StableStyle = 1;
                    }
                }
                else
                {
                    // [When all styles are inactive, we hardcode pick '2D' as the default style since it is the safest option, max it's strength to 1]
                    finalCinematic2DStrength = 1;
                }
            }
                
            float finalCinematicEnabled = Mathf.Clamp01(finalCinematic3DStrength_ClassicStyle + finalCinematic3DStrength_DynamicStyle + finalCinematic3DStrength_StableStyle + finalCinematic2DStrength);
                
            cmd.SetGlobalFloat(_GlobalCinematic3DRimMaskEnabled_SID, finalCinematicEnabled);
            cmd.SetGlobalFloat(_GlobalCinematic3DRimMaskStrength_ClassicStyle_SID, finalCinematic3DStrength_ClassicStyle);
            cmd.SetGlobalFloat(_GlobalCinematic3DRimMaskStrength_DynamicStyle_SID, finalCinematic3DStrength_DynamicStyle);
            cmd.SetGlobalFloat(_GlobalCinematic3DRimMaskSharpness_DynamicStyle_SID, finalCinematic3DSharpness_DynamicStyle);
            cmd.SetGlobalFloat(_GlobalCinematic3DRimMaskStrength_StableStyle_SID, finalCinematic3DStrength_StableStyle);
            cmd.SetGlobalFloat(_GlobalCinematic3DRimMaskSharpness_StableStyle_SID, finalCinematic3DSharpness_StableStyle);
            cmd.SetGlobalFloat(_GlobalCinematic2DRimMaskStrength_SID, finalCinematic2DStrength);
            cmd.SetGlobalFloat(_GlobalCinematicRimTintBaseMap_SID, cinematicAdditionalLightVolume.tintBaseMap.value);

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // global volume (NiloToonAdditionalLightStyleVolume)
            //////////////////////////////////////////////////////////////////////////////////////////////////
            cmd.SetGlobalFloat(_GlobalAdditionalLightInjectIntoMainLightColor_Strength_SID,additionalLightStyleVolume.additionalLightInjectIntoMainLightColor_Strength.value);
            cmd.SetGlobalFloat(_GlobalAdditionalLightInjectIntoMainLightDirection_Strength_SID,additionalLightStyleVolume.additionalLightInjectIntoMainLightDirection_Strength.value);
            cmd.SetGlobalFloat(_GlobalAdditionalLightInjectIntoMainLightColor_AllowCloseLightOverBright_SID,additionalLightStyleVolume.additionalLightInjectIntoMainLightColor_AllowCloseLightOverBright.value);
            cmd.SetGlobalFloat(_GlobalAdditionalLightInjectIntoMainLightColor_Desaturate_SID, additionalLightStyleVolume.additionalLightInjectIntoMainLightColor_Desaturate.value);
            //////////////////////////////////////////////////////////////////////////////////////////////////
            // global volume (NiloToonEnvironmentControlVolume)
            //////////////////////////////////////////////////////////////////////////////////////////////////
            cmd.SetGlobalColor(_NiloToonGlobalEnviGITintColor_SID, environmentControlVolume.GlobalIlluminationTintColor.value);
            cmd.SetGlobalColor(_NiloToonGlobalEnviGIAddColor_SID, environmentControlVolume.GlobalIlluminationAddColor.value);
            cmd.SetGlobalColor(_NiloToonGlobalEnviGIOverride_SID, environmentControlVolume.GlobalIlluminationOverrideColor.value);
            cmd.SetGlobalColor(_NiloToonGlobalEnviAlbedoOverrideColor_SID, environmentControlVolume.GlobalAlbedoOverrideColor.value);
            cmd.SetGlobalFloat(_NiloToonGlobalEnviMinimumShader_SID, Setting.ForceMinimumEnviShader ? 1 : 0);
            cmd.SetGlobalColor(_NiloToonGlobalEnviShadowBorderTintColor_SID, environmentControlVolume.GlobalShadowBoaderTintColorOverrideColor.value);
            cmd.SetGlobalColor(_NiloToonGlobalEnviSurfaceColorResultOverrideColor_SID, environmentControlVolume.GlobalSurfaceColorResultOverrideColor.value);

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // per char script param to global array
            //////////////////////////////////////////////////////////////////////////////////////////////////
            SetGlobalPerCharacterDataArray(cmd, passData);

            //////////////////////////////////////////////////////////////////////////////////////////////////
            // Character Light controllers to global array
            //////////////////////////////////////////////////////////////////////////////////////////////////
            SetGlobalNiloToonCharacterLightControllerDataArray(cmd, passData);
                
            //////////////////////////////////////////////////////////////////////////////////////////////////
            // Light Modifier to global array
            //////////////////////////////////////////////////////////////////////////////////////////////////
            SetGlobalNiloToonPerUnityVisibleLightDataArray(cmd, passData);
            return cmd;
        }

        // Must match: NiloToonCharacter_Shared.hlsl's MAX_CHARACTER_COUNT values!
        private const int k_MaxCharacterCountMobileShaderLevelLessThan45 = 16;
        private const int k_MaxCharacterCountMobile = 32;
        private const int k_MaxCharacterCountNonMobile = 128;  // for most game type, 128 is a big enough number, but still not affecting performance
        // this property is similar to UniversalRenderPipeline.maxVisibleAdditionalLights, but for NiloToon's max character count
        public static int maxCharacterCount
        {
            get
            {
                // Must match: NiloToon character shader's MAX_CHARACTER_COUNT
                bool isMobile = GraphicsSettings.HasShaderDefine(BuiltinShaderDefine.SHADER_API_MOBILE);
                if (isMobile && (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && Graphics.minOpenGLESVersion <= OpenGLESVersion.OpenGLES30)))
                    return k_MaxCharacterCountMobileShaderLevelLessThan45;

                // GLES can be selected as platform on Windows (not a mobile platform) but uniform buffer size so we must use a low light count.
                return (isMobile || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLCore || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES2 || SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3)
                    ? k_MaxCharacterCountMobile : k_MaxCharacterCountNonMobile;
            }
        }
       
        Vector4[] perCharFaceForwardDirWSArray;
        Vector4[] perCharFaceUpwardDirWSArray;
        Vector4[] perCharBoundCenterPosWSArray;
        Vector4[] perCharPerspectiveRemovalCenterPosWSArray;
        void SetGlobalPerCharacterDataArray()
        {
            NiloToonAllInOneRendererFeature.CheckInit();
            int characterCount = Mathf.Min(maxCharacterCount, NiloToonAllInOneRendererFeature.characterList.Count);
            
            // init array if not yet init
            if (perCharFaceForwardDirWSArray == null)
                perCharFaceForwardDirWSArray = new Vector4[maxCharacterCount];
            if (perCharFaceUpwardDirWSArray == null)
                perCharFaceUpwardDirWSArray = new Vector4[maxCharacterCount];
            if (perCharBoundCenterPosWSArray == null)
                perCharBoundCenterPosWSArray = new Vector4[maxCharacterCount];
            if (perCharPerspectiveRemovalCenterPosWSArray == null)
                perCharPerspectiveRemovalCenterPosWSArray = new Vector4[maxCharacterCount];
            
            for (int i = 0; i < characterCount; i++)
            {
                NiloToonPerCharacterRenderController controller = NiloToonAllInOneRendererFeature.characterList[i];

                if (!controller) continue;
                
                // these values will change every frame,
                // if we upload these value via Material.SetVector (just like NiloToon 0.15.x or below),
                // it will force SRP to re upload the whole material constant buffer every frame to GPU since material has changed, the buffer is huge so it is very slow.
                // So for values that will change every frame, in NiloToon 0.16.x or later we use a global array for reading them in shader instead of assigning via Material.SetXXX API,
                // which is so much faster when the number of material is huge due to SRP Batcher using a fast path(since materials are not changing every frame now if user didn't do any special animation)
                // see https://docs.unity3d.com/Manual/SRPBatcher.html
                
                // "When Unity detects a new material(= material was edited) during the render loop, the CPU collects all properties and binds them to the GPU in constant buffers.
                // The number of GPU buffers depends on how the shader declares its constant buffers.
                // The SRP Batcher is a low-level render loop that makes material data persist in GPU memory.
                // If the material content doesn’t change, theSRP Batcher doesn’t make any render-state changes.
                // Instead, the SRP Batcher uses a dedicated code path to update the Unity Engine properties in a large GPU buffer"
                
                perCharFaceForwardDirWSArray[i] = controller.finalFaceDirectionWS_Forward;
                perCharFaceUpwardDirWSArray[i] = controller.finalFaceDirectionWS_Up;
                perCharBoundCenterPosWSArray[i] = controller.characterBoundCenter;
                perCharPerspectiveRemovalCenterPosWSArray[i] = controller.perspectiveRemovalCenter;
            }
        }
        static void SetGlobalPerCharacterDataArray(CommandBuffer cmd, PassData passData)
        {
            cmd.SetGlobalVectorArray(_NiloToonGlobalPerCharFaceForwardDirWSArray_SID, passData.perCharFaceForwardDirWSArray);
            cmd.SetGlobalVectorArray(_NiloToonGlobalPerCharFaceUpwardDirWSArray_SID,passData.perCharFaceUpwardDirWSArray);
            cmd.SetGlobalVectorArray(_NiloToonGlobalPerCharBoundCenterPosWSArray_SID,passData.perCharBoundCenterPosWSArray);
            cmd.SetGlobalVectorArray(_NiloToonGlobalPerCharHeadBonePosWSArray_SID,passData.perCharPerspectiveRemovalCenterPosWSArray);
        }
        
        Vector4[] finalPerCharMainDirectionalLightTintColorArray;
        Vector4[] finalPerCharMainDirectionalLightAddColorArray;
        void SetGlobalNiloToonCharacterLightControllerDataArray()
        {
            int MAX_CHARACTER_SLOT_COUNT = maxCharacterCount;
            int activeLightControllersCount = NiloToonCharacterLightController.AllNiloToonCharacterLightControllers.Count;

            // init array if not yet init or wrong length(due to switching platform)
            if (finalPerCharMainDirectionalLightTintColorArray == null || finalPerCharMainDirectionalLightTintColorArray.Length != MAX_CHARACTER_SLOT_COUNT)
                finalPerCharMainDirectionalLightTintColorArray = new Vector4[MAX_CHARACTER_SLOT_COUNT];
            if (finalPerCharMainDirectionalLightAddColorArray == null || finalPerCharMainDirectionalLightAddColorArray.Length != MAX_CHARACTER_SLOT_COUNT)
                finalPerCharMainDirectionalLightAddColorArray = new Vector4[MAX_CHARACTER_SLOT_COUNT];
            
            // reset array values
            for (int i = 0; i < MAX_CHARACTER_SLOT_COUNT; i++)
            {
                finalPerCharMainDirectionalLightTintColorArray[i] = Color.white;
                finalPerCharMainDirectionalLightAddColorArray[i] = Color.black;
            }
            
            // merge loop
            for (int i = 0; i < activeLightControllersCount; i++)
            {
                NiloToonCharacterLightController controller = NiloToonCharacterLightController.AllNiloToonCharacterLightControllers[i];

                if (!controller) continue;
                
                // loop all per character slot
                for (int characterID = 0; characterID < MAX_CHARACTER_SLOT_COUNT; characterID++)
                {
                    // early exit
                    if (!controller.AffectsCharacter(characterID)) continue;
                    
                    //--------------------------------------------------------
                    // Main light tint color 
                    {
                        Color finalTint = Color.white;
                    
                        if (controller.enableMainLightTintColor)
                        {
                            finalTint *= controller.mainLightTintColor;
                            finalTint *= controller.mainLightIntensityMultiplier;
                        }

                        if (controller.enableMainLightTintColorByLight && controller.mainLightTintColorByLight_Target)
                        {
                            Light light = controller.mainLightTintColorByLight_Target;
                            Color originalLightColor = light.color;
                            Color finalLightColor = Color.Lerp(originalLightColor, Color.white * originalLightColor.grayscale,controller.mainLightTintColorByLight_Desaturate);
                            Color finalTintColor = Color.Lerp(Color.white,light.intensity * finalLightColor, controller.mainLightTintColorByLight_Strength);
                            finalTint *= finalTintColor;
                        }
                        finalPerCharMainDirectionalLightTintColorArray[characterID] *= finalTint;
                    }
                    
                    //--------------------------------------------------------
                    // Main light add color
                    {
                        Color finalAdd = Color.black;
                    
                        if (controller.enableMainLightAddColor)
                        {
                            finalAdd += controller.mainLightAddColorIntensity * controller.mainLightAddColor;
                        }

                        if (controller.enableMainLightAddColorByLight && controller.mainLightAddColorByLight_Target)
                        {
                            Light light = controller.mainLightAddColorByLight_Target;
                            Color originalLightColor = light.color;
                            Color finalLightColor = Color.Lerp(originalLightColor, Color.white * originalLightColor.grayscale,controller.mainLightAddColorByLight_Desaturate);
                            Color finalAddColor = light.intensity * finalLightColor * controller.mainLightAddColorByLight_Strength;
                            finalAdd += finalAddColor;
                        }
                        finalPerCharMainDirectionalLightAddColorArray[characterID] += (Vector4)finalAdd;
                    }
                }
            }
        }
        static void SetGlobalNiloToonCharacterLightControllerDataArray(CommandBuffer cmd, PassData passData)
        {
            // TODO: + dirty detect? don't call if the array is the same as previous frame
            cmd.SetGlobalVectorArray(_NiloToonGlobalPerCharMainDirectionalLightTintColorArray_SID,passData.finalPerCharMainDirectionalLightTintColorArray);
            cmd.SetGlobalVectorArray(_NiloToonGlobalPerCharMainDirectionalLightAddColorArray_SID,passData.finalPerCharMainDirectionalLightAddColorArray);
        }

        private Vector4[] perUnityVisibleLightNiloToonDataArray;
        private Vector4[] perUnityVisibleLightNiloToonDataArray2;
        void SetGlobalNiloToonPerUnityVisibleLightDataArray(PassData passData)
        {
            int MAX_UNITY_VISIBLE_LIGHT_SLOT_COUNT = UniversalRenderPipeline.maxVisibleAdditionalLights;
            
            // init array if not yet init
            if (perUnityVisibleLightNiloToonDataArray == null)
            {
                perUnityVisibleLightNiloToonDataArray = new Vector4[MAX_UNITY_VISIBLE_LIGHT_SLOT_COUNT];
            }
            if (perUnityVisibleLightNiloToonDataArray2 == null)
            {
                perUnityVisibleLightNiloToonDataArray2 = new Vector4[MAX_UNITY_VISIBLE_LIGHT_SLOT_COUNT];
            }
            
            // reset array
            int visibleLightCount = passData.visibleLights.Length;
            for (int i = 0; i < visibleLightCount && i < MAX_UNITY_VISIBLE_LIGHT_SLOT_COUNT; i++)
            {
                // after reset, if no NiloToonLightSourceModifier affect this array, then this array's value will not affect rendering in shader
                
                // (1,1,0,1) is the no-op value in shader
                perUnityVisibleLightNiloToonDataArray[i] = new Vector4(1,1,0,1);
                // (0,0,_,_) is the no-op value in shader
                perUnityVisibleLightNiloToonDataArray2[i] = new Vector4(0, 0, 0, 0);
            }
            
            int activeLightModifierScriptCount = NiloToonLightSourceModifier.AllNiloToonLightModifiers.Count;
            for (int lightModifierIndex = 0; lightModifierIndex < activeLightModifierScriptCount; lightModifierIndex++)
            {
                NiloToonLightSourceModifier script = NiloToonLightSourceModifier.AllNiloToonLightModifiers[lightModifierIndex];

                if (!script) continue;
                
                // C# visibleLights array contains these lights in order:
                // 1. Main Light x1
                // 2. Additional Directional lights xN
                // 3. All point/spot lights x M
                  
                // you must follow URP's ForwardLights.cs -> SetupAdditionalLightConstants(...) for how to match light index in shader
                // the NiloToon code below is following URP12.1.12's code light loop structure for setting up GPU light data array
                var lights = passData.visibleLights;
                int maxAdditionalLightsCount = UniversalRenderPipeline.maxVisibleAdditionalLights;
      
                for (int i = 0, lightIter = 0; i < lights.Length && lightIter < maxAdditionalLightsCount; ++i)
                {
                    VisibleLight light = lights[i];
                    
                    if (passData.mainLightIndex != i)
                    {
                        if (script.AffectsLight(light.light))
                        {
                            Vector4 data = perUnityVisibleLightNiloToonDataArray[lightIter];
                            data.x *= script.contributeToMainLightColor;
                            data.y *= script.contributeToMainLightDirection;
                            data.z = Mathf.Lerp(data.z,1,script.applyDesaturateWhenContributeToMainLightColor);
                            data.w *= script.contributeToAdditiveOrRimLightIntensity;
                            perUnityVisibleLightNiloToonDataArray[lightIter] = data;
                            
                            Vector4 data2 = perUnityVisibleLightNiloToonDataArray2[lightIter];
                            data2.x = Mathf.Lerp(data2.x,1,script.backLightOcclusion2DWhenContributeToMainLightColor);
                            data2.y = Mathf.Lerp(data2.y,1,script.backLightOcclusion3DWhenContributeToMainLightColor);
                            perUnityVisibleLightNiloToonDataArray2[lightIter] = data2;
                        }

                        lightIter++;
                    }
                }
            }
        }
        static void SetGlobalNiloToonPerUnityVisibleLightDataArray(CommandBuffer cmd, PassData passData)
        {
            // TODO: + dirty detect? don't call if the array is the same as previous frame
            cmd.SetGlobalVectorArray(_NiloToonGlobalPerUnityLightDataArray_SID,passData.perUnityVisibleLightNiloToonDataArray);
            cmd.SetGlobalVectorArray(_NiloToonGlobalPerUnityLightDataArray2_SID,passData.perUnityVisibleLightNiloToonDataArray2);
        }
        
        /////////////////////////////////////////////////////////////////////
        // RG support
        /////////////////////////////////////////////////////////////////////
#if UNITY_6000_0_OR_NEWER    
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            using (var builder = renderGraph.AddUnsafePass<PassData>("NiloToonSetToonParam(RG)", out var passData))
            {
                // UniversalResourceData contains all the texture references used by URP,
                // including the active color and depth textures of the camera.
                UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
                
                UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
                UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
                UniversalLightData lightData = frameContext.Get<UniversalLightData>();
                
                //============================================
                // request URP depth texture
                //============================================
                // DANGER!!!!!!!!!! 
                // Calling ConfigureInput(...) inside this RecordRenderGraph() method will prevent unity android build (error produced) (https://discussions.unity.com/t/introduction-of-render-graph-in-the-universal-render-pipeline-urp/930355/226)
                // We now call ConfigureInput(...) in NiloToonAllInOneRenderereFeature's AddRenderPasses as an temp workaround.
                //var input = GetScriptableRenderPassInput(cameraData.renderer);
                //ConfigureInput(input); 
                
                //--------------------------------------
                // fill in passData
                //--------------------------------------
                passData.camera = cameraData.camera;
                passData.additionalLightsCount = lightData.additionalLightsCount;
                passData.mainLightIndex = lightData.mainLightIndex;
                passData.Setting = Setting;
                passData.visibleLights = lightData.visibleLights;
                passData.renderer = cameraData.renderer;
                passData.needCorrectDepthTextureDepthWrite = GetNeedCorrectDepthTextureDepthWrite(cameraData.renderer);
                
                FillInPassDataArray(passData);
                //--------------------------------------
                
                builder.AllowPassCulling(false);
                
                builder.SetRenderFunc((PassData data, UnsafeGraphContext context) => ExecutePass(data, context));
            }
        }
#endif
        private void FillInPassDataArray(PassData passData)
        {
            SetGlobalPerCharacterDataArray();
            passData.perCharFaceForwardDirWSArray = perCharFaceForwardDirWSArray;
            passData.perCharFaceUpwardDirWSArray = perCharFaceUpwardDirWSArray;
            passData.perCharBoundCenterPosWSArray = perCharBoundCenterPosWSArray;
            passData.perCharPerspectiveRemovalCenterPosWSArray = perCharPerspectiveRemovalCenterPosWSArray;
                
            SetGlobalNiloToonCharacterLightControllerDataArray();
            passData.finalPerCharMainDirectionalLightTintColorArray = finalPerCharMainDirectionalLightTintColorArray;
            passData.finalPerCharMainDirectionalLightAddColorArray = finalPerCharMainDirectionalLightAddColorArray;
                
            SetGlobalNiloToonPerUnityVisibleLightDataArray(passData);
            passData.perUnityVisibleLightNiloToonDataArray = perUnityVisibleLightNiloToonDataArray;
            passData.perUnityVisibleLightNiloToonDataArray2 = perUnityVisibleLightNiloToonDataArray2;
        }
    }
}