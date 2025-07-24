using System;
using UnityEngine;
using UnityEngine.Rendering;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace NiloToon.NiloToonURP
{
    public class NiloToonToonOutlinePass : ScriptableRenderPass
    {
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // do nothing
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            renderClassicOutline(context, renderingData);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // do nothing
        }

        static readonly ShaderTagId toonOutlineLightModeShaderTagId = new ShaderTagId("NiloToonOutline");

        static readonly int _GlobalShouldRenderOutline_SID = Shader.PropertyToID("_GlobalShouldRenderOutline");
        static readonly int _GlobalOutlineWidthMultiplier_SID = Shader.PropertyToID("_GlobalOutlineWidthMultiplier");
        static readonly int _GlobalOutlineTintColor_SID = Shader.PropertyToID("_GlobalOutlineTintColor");
        static readonly int _GlobalOutlineWidthAutoAdjustToCameraDistanceAndFOV_SID = Shader.PropertyToID("_GlobalOutlineWidthAutoAdjustToCameraDistanceAndFOV");

        [Serializable]
        public class Settings
        {
            [Header("Classic Outline")]

            [Tooltip(   "Enable to let characters render 'Classic Outline'.\n" +
                        "Can turn OFF to improve performance.\n\n" +
                        "Default: ON")]
            [OverrideDisplayName("Enable?")]
            public bool ShouldRenderOutline = true;

            [Tooltip("Optional 'Classic Outline' width multiplier for all Classic Outline.\n\n" +
                     "Default: 1")]
            [RangeOverrideDisplayName("     Width", 0, 4)]
            public float outlineWidthMultiplier = 1;

            [Tooltip("VR will apply an extra 'Classic Outline' width multiplier, due to high FOV(90)\n\n" +
                     "Default: 0.5")]
            [RangeOverrideDisplayName("     Width multiplier(XR)",0, 4)]
            public float outlineWidthExtraMultiplierForXR = 0.5f;

            [Tooltip("Optional outline color multiplier.\n\n" +
                     "Default: White")]
            [ColorUsageOverrideDisplayName("     Tint Color", false, true)]
            public Color outlineTintColor = Color.white;
            
            [Tooltip(   "Should 'Classic Outline' width auto adjust to camera distance & FOV?\n\n" +
                        "If set to 1:\n" +
                        "- When camera is closer to character or camera FOV is lower, outline width in world space will be smaller automatically\n" +
                        "- When camera is further away from character or camera FOV is higher, outline width in world space will be larger automatically\n\n" +
                        "If set to 0:\n" +
                        "- Outline width will be always constant in world space\n" +
                        "\n" +
                        "Default: 1 (apply 100% adjustment)")]
            [RangeOverrideDisplayName("     Auto width adjustment",0, 1)]
            public float outlineWidthAutoAdjustToCameraDistanceAndFOV = 1;

            [Tooltip(   "Usually it is recommended to disable 'Classic Outline' in planar reflection's rendering due to performance reason.\n\n" +
                        "If this toggle is disabled, NiloToon will stop rendering character's 'Classic Outline' when the camera GameObject's name contains any of these:\n" +
                        "   - Reflect\n" +
                        "   - Mirror\n" +
                        "   - Planar\n" +
                        "\n" + 
                        "Default: OFF")]
            [OverrideDisplayName("     Draw in planar reflection?")]
            public bool allowClassicOutlineInPlanarReflection = false;

            //-----------------------------------------------------------------------
            [Header("Screen Space Outline")]

            [Tooltip("Enable this will\n" +
                     "- enable URP's Depth and Normals Texture's rendering(can be slow)\n" +
                     "- allow Screen Space Outline's rendering in Game window, since Depth and Normal textures are now rendered.\n\n" +
                     "Default: OFF")]
            [OverrideDisplayName("Allow render?")]
            public bool AllowRenderScreenSpaceOutline = false;

            // TODO: when the minimum support version for NiloToon is Unity2021.3, we should move this to a global setting file, similar to URP12's global setting
            [Tooltip("Screen space outline may be very disturbing in scene view window(scene view window = high fov, small window, lowest resolution), this toggle allows you to turn it on/off.\n\n" +
                     "Default: OFF")]
            [OverrideDisplayName("     Allow in Scene View?")]
            public bool AllowRenderScreenSpaceOutlineInSceneView = false;

            //-----------------------------------------------------------------------
            [Header("Extra Thick Outline")]

            [Tooltip("Usually you only need to consider \"AfterRenderingTransparents\" or \"BeforeRenderingTransparents\".\n\n" +
                "If you set to AfterRenderingTransparents:\n" +
                "- extra thick outline will render on top of transparent material (= extra thick outline covering transparent material).\n" +
                "If you set to BeforeRenderingTransparents\n" +
                "- extra thick outline will NOT render on top of transparent material (= extra thick outline covered by transparent material)\n\n" +
                "*You can also control extra thick outline's ZWrite in each NiloToonPerCharacterRenderController.\n\n" +
                "Default: AfterRenderingTransparents")]
            [OverrideDisplayName("RenderPassEvent")]
            public RenderPassEvent extraThickOutlineRenderTiming = RenderPassEvent.AfterRenderingTransparents;
        }

        NiloToonRendererFeatureSettings allSettings;
        Settings settings;
        ProfilingSampler m_ProfilingSamplerClassicOutline;

        RenderQueueRange renderQueueRange;

        // constructor(will not construct on every frame)
        public NiloToonToonOutlinePass(NiloToonRendererFeatureSettings allSettings, RenderQueueRange renderQueueRange, string ProfilingSamplerName)
        {
            this.allSettings = allSettings;
            this.settings = allSettings.outlineSettings;
            m_ProfilingSamplerClassicOutline = new ProfilingSampler(ProfilingSamplerName);
            this.renderQueueRange = renderQueueRange;
        }

        // NOTE: [how to use ProfilingSampler to correctly]
        /*
            // [write as class member]
            ProfilingSampler m_ProfilingSampler;

            // [call once in constrcutor]
            m_ProfilingSampler = new ProfilingSampler("NiloToonToonOutlinePass");

            // [call in execute]
            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSampler))
            { 
                context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref _filteringSettings);
                cmd.SetGlobalTexture("_CameraNormalRT", _normalRT.Identifier());
            }
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd); 
        */

        private void renderClassicOutline(ScriptableRenderContext context, RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSamplerClassicOutline))
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
                
                ExecutePassShared(context, renderingData, cmd);
            }
            
            // must write these line after using{} finished, to ensure profiler and frame debugger display correctness
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        private bool GetShouldRender(Camera camera)
        {
            bool shouldRenderClassicOutline = settings.ShouldRenderOutline && !allSettings.MiscSettings.ForceNoOutline && !(allSettings.MiscSettings.ForceMinimumShader || NiloToonSetToonParamPass.ForceMinimumShader);

            if (NiloToonPlanarReflectionHelper.IsPlanarReflectionCamera(camera) && !settings.allowClassicOutlineInPlanarReflection)
            {
                shouldRenderClassicOutline = false;
            }

            return shouldRenderClassicOutline;
        }
        private void ExecutePassShared(ScriptableRenderContext context, RenderingData renderingData, CommandBuffer cmd)
        {
            // early exit if no render needed
            if (!GetShouldRender(renderingData.cameraData.camera))
            {
                // set the only required cmd
                cmd.SetGlobalFloat(_GlobalShouldRenderOutline_SID, 0);

                // no draw
                // (X)
                
                return;
            }

            
            var volumeEffect = VolumeManager.instance.stack.GetComponent<NiloToonCharRenderingControlVolume>();

            // set default value first because volume may not exist in scene
            float outlineWidthMultiplierResult = settings.outlineWidthMultiplier * volumeEffect.charOutlineWidthMultiplier.value;
            Color outlineTintColor = settings.outlineTintColor * volumeEffect.charOutlineMulColor.value;
            float outlineWidthAutoAdjustToCameraDistanceAndFOV = settings.outlineWidthAutoAdjustToCameraDistanceAndFOV * volumeEffect.charOutlineWidthAutoAdjustToCameraDistanceAndFOV.value;

            // extra outline control if XR
            if (XRSettings.isDeviceActive)
            {
                outlineWidthMultiplierResult *= volumeEffect.charOutlineWidthExtraMultiplierForXR.overrideState ? volumeEffect.charOutlineWidthExtraMultiplierForXR.value : settings.outlineWidthExtraMultiplierForXR;
            }

            // set
            cmd.SetGlobalFloat(_GlobalShouldRenderOutline_SID, 1);
            cmd.SetGlobalFloat(_GlobalOutlineWidthMultiplier_SID, outlineWidthMultiplierResult);
            cmd.SetGlobalColor(_GlobalOutlineTintColor_SID, outlineTintColor);
            cmd.SetGlobalFloat(_GlobalOutlineWidthAutoAdjustToCameraDistanceAndFOV_SID, outlineWidthAutoAdjustToCameraDistanceAndFOV);

            // execute cmd before DrawRenderers, to avoid 1 frame delay
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();

            // draw self classic outline
            DrawingSettings drawingSettings = CreateDrawingSettings(toonOutlineLightModeShaderTagId, ref renderingData, SortingCriteria.CommonOpaque);
            FilteringSettings filteringSettings = new FilteringSettings(renderQueueRange);
            context.DrawRenderers(renderingData.cullResults, ref drawingSettings, ref filteringSettings);
        }
        
        //======================================================================================================
        // RG support
        //======================================================================================================
#if UNITY_6000_0_OR_NEWER
        
        // copy and edit of https://docs.unity3d.com/6000.0/Documentation/Manual/urp/render-graph-draw-objects-in-a-pass.html
        private class PassData
        {
            // Create a field to store the list of objects to draw
            public RendererListHandle rendererListHandle;
            public Camera camera;
            public bool shouldRender;
            public float outlineWidthMultiplierResult;
            public Color outlineTintColor;
            public float outlineWidthAutoAdjustToCameraDistanceAndFOV;
        }
 
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("NiloToonClassicOutlinePass(RG)", out var passData))
            {
                // Get the data needed to create the list of objects to draw
                UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
                UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
                UniversalLightData lightData = frameContext.Get<UniversalLightData>();
 
                SortingCriteria sortFlags = SortingCriteria.CommonOpaque; //cameraData.defaultOpaqueSortFlags;
                RenderQueueRange renderQueueRange = this.renderQueueRange;
                FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, ~0);

                // Redraw only objects that have their LightMode tag set to toonOutlineLightModeShaderTagId 
                ShaderTagId shadersToOverride = toonOutlineLightModeShaderTagId;

                // Create drawing settings
                DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shadersToOverride, renderingData, cameraData, lightData, sortFlags);

                // Create the list of objects to draw
                var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

                // Convert the list to a list handle that the render graph system can use
                var volumeEffect = VolumeManager.instance.stack.GetComponent<NiloToonCharRenderingControlVolume>();
                passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);
                passData.camera = cameraData.camera;
                passData.shouldRender = GetShouldRender(cameraData.camera);
                passData.outlineWidthMultiplierResult = settings.outlineWidthMultiplier * volumeEffect.charOutlineWidthMultiplier.value;
                passData.outlineTintColor = settings.outlineTintColor * volumeEffect.charOutlineMulColor.value;
                passData.outlineWidthAutoAdjustToCameraDistanceAndFOV = settings.outlineWidthAutoAdjustToCameraDistanceAndFOV * volumeEffect.charOutlineWidthAutoAdjustToCameraDistanceAndFOV.value;
                
                // extra outline control if XR
                if (XRSettings.isDeviceActive)
                {
                    passData.outlineWidthMultiplierResult *= volumeEffect.charOutlineWidthExtraMultiplierForXR.overrideState ? volumeEffect.charOutlineWidthExtraMultiplierForXR.value : settings.outlineWidthExtraMultiplierForXR;
                }
                
                // Set the render target as the color and depth textures of the active camera texture
                UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
                builder.UseRendererList(passData.rendererListHandle);
                builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(resourceData.activeDepthTexture, AccessFlags.Write);
                
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
        
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            // run cmd similar to ExecutePassShared()
            var cmd = context.cmd;

            // early exit if no render needed
            if (!data.shouldRender)
            {
                // set the only required cmd
                cmd.SetGlobalFloat(_GlobalShouldRenderOutline_SID, 0);

                // no draw
                // (X)
                
                return;
            }

            // set cmd
            cmd.SetGlobalFloat(_GlobalShouldRenderOutline_SID, 1);
            cmd.SetGlobalFloat(_GlobalOutlineWidthMultiplier_SID, data.outlineWidthMultiplierResult);
            cmd.SetGlobalColor(_GlobalOutlineTintColor_SID, data.outlineTintColor);
            cmd.SetGlobalFloat(_GlobalOutlineWidthAutoAdjustToCameraDistanceAndFOV_SID, data.outlineWidthAutoAdjustToCameraDistanceAndFOV);

            // Draw the objects in the list
            cmd.DrawRendererList(data.rendererListHandle);
        }
#endif
    }
}