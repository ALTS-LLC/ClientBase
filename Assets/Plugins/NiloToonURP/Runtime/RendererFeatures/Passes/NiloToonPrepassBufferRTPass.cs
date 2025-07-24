// very similar to URP's DepthNormalOnlyPass.cs, but only draw NiloToon character shader's "LightMode" = "NiloToonPrepassBuffer" pass.("LightMode"= ShaderTagId in C#)
// this pass's on/off is handled by NiloToonAllInOneRendererFeature.cs's AddRenderPasses() function

// *see DBufferRenderPass.cs to learn how to "ReAllocateIfNeeded" & "Dispose" RTHandle correctly

/*
_NiloToonPrepassBufferRT is storing the following data
-r: unused
-g: character visible area (for NiloToon Bloom)
-b: unused
-a: unused

* We may draw IsFace,IsSkin,RimArea into this RT in future versions
*/
using System;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NiloToon.NiloToonURP
{
    public class NiloToonPrepassBufferRTPass : ScriptableRenderPass
    {
        NiloToonRendererFeatureSettings allSettings;

#if UNITY_2022_2_OR_NEWER
        RTHandle prepassBufferRTHColor;
        RTHandle prepassBufferRTHDepth;
#else
        RenderTargetHandle prepassBufferRTH;
#endif

        // Constants
        static readonly ShaderTagId shaderTagId = new ShaderTagId("NiloToonPrepassBuffer");

        // Constructor(will not call every frame)
        public NiloToonPrepassBufferRTPass(NiloToonRendererFeatureSettings allSettings)
        {
            this.allSettings = allSettings;

#if !UNITY_2022_2_OR_NEWER
            prepassBufferRTH.Init("_NiloToonPrepassBufferRT"); // this is RT's name, different to the texture name used in shader
#endif

            base.profilingSampler = new ProfilingSampler(nameof(NiloToonPrepassBufferRTPass));
        }
        
        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // When doing prepass rendering, the RT's depth format(depth bit/depthStencilFormat/MSAA) need to be exactly matching formats from _CameraDepthTexture, else when rejecting blocked character pixels(using _CameraDepthTexture) will fail
            RenderTextureDescriptor renderTextureDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            var cameraData = renderingData.cameraData;

            // In URP12's DepthNormalOnlyPass.cs->Setup(...)
            // or
            // In URP14's UniversalRenderer.cs->Setup(...)
            // depthHandle's msaaSamples is forcibly set to 1(No MSAA),
            // so we need to follow it to make both _NiloToonPrepassBufferTex and _CameraDepthTexture's depthStencilFormat and MSAA 100% match,
            // so that the depth comparision in prepass buffer shader is 100% correct
            // TODO: if we use DBufferRenderPass as reference, we should "ReAllocateIfNeeded" all RTHandle inside a SetUp() method, and let NiloToonAllInOneRendererFeature's SetupRenderPasses() to call it 
#if UNITY_2022_2_OR_NEWER
            // see URP14 DBufferRenderPass.cs->Setup(...) for reference about how to set up color and depth Descriptor and RenderingUtils.ReAllocateIfNeeded() RTHandle

            // [color RTHandle's Descriptor]
            var colorDesc = cameraData.cameraTargetDescriptor;
            colorDesc.graphicsFormat = GraphicsFormat.R8G8B8A8_UNorm; // we don't need HDR color, since this RT's color is just a mask, so we change colorFormat to R8G8B8A8_UNorm
            colorDesc.depthBufferBits = 0;
            colorDesc.msaaSamples = 1;

            // [depth RTHandle's Descriptor]
            var depthDesc = cameraData.cameraTargetDescriptor;
            depthDesc.graphicsFormat = GraphicsFormat.None; //Depth only rendering
            depthDesc.depthStencilFormat = cameraData.cameraTargetDescriptor.depthStencilFormat;
            depthDesc.msaaSamples = 1;
#else
            renderTextureDescriptor.colorFormat = RenderTextureFormat.ARGB32;
            renderTextureDescriptor.depthStencilFormat = cameraData.cameraTargetDescriptor.depthStencilFormat;
            renderTextureDescriptor.msaaSamples = 1;
#endif

#if UNITY_2022_2_OR_NEWER
            RenderingUtils.ReAllocateIfNeeded(ref prepassBufferRTHColor, colorDesc, name: "_NiloToonPrepassBufferColor");
            RenderingUtils.ReAllocateIfNeeded(ref prepassBufferRTHDepth, depthDesc, name: "_NiloToonPrepassBufferDepth");

            //set global RT
            cmd.SetGlobalTexture("_NiloToonPrepassBufferTex", prepassBufferRTHColor);
            cmd.SetGlobalTexture("_NiloToonPrepassBufferDepthTex", prepassBufferRTHDepth);
#else
            cmd.GetTemporaryRT(prepassBufferRTH.id, renderTextureDescriptor);

            //set global RT
            cmd.SetGlobalTexture("_NiloToonPrepassBufferTex", prepassBufferRTH.Identifier());
#endif


#if UNITY_2022_2_OR_NEWER
            ConfigureTarget(prepassBufferRTHColor,prepassBufferRTHDepth);
#else
            ConfigureTarget(new RenderTargetIdentifier(prepassBufferRTH.Identifier(),0, CubemapFace.Unknown, -1));
#endif

            ConfigureClear(ClearFlag.All, Color.black);
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // since this pass is only enqueued when conditions are met in NiloToonAllInOneRendererFeature's AddRenderPasses method.
            // we don't need to do any "Should render?" check in this pass

            // Never draw in Preview
            Camera camera = renderingData.cameraData.camera;
            if (camera.cameraType == CameraType.Preview)
                return;

            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, base.profilingSampler))
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

                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // draw all nilotoon character renderer's "NiloToonPrepassBuffer" pass using SRP batching
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                var filterSetting = new FilteringSettings(RenderQueueRange.opaque); //TODO: should include transparent also?        

                var sortFlags = renderingData.cameraData.defaultOpaqueSortFlags;
                var drawSettings = CreateDrawingSettings(shaderTagId, ref renderingData, sortFlags);
                drawSettings.perObjectData = PerObjectData.None;
                context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filterSetting);
            }
            
            // must write these line after using{} finished, to ensure profiler and frame debugger display correctness
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // To Release a RTHandle, do it in ScriptableRendererFeature's Dispose(), don't do it in OnCameraCleanup(...)
            //https://www.cyanilux.com/tutorials/custom-renderer-features/#oncameracleanup

#if !UNITY_2022_2_OR_NEWER
            cmd.ReleaseTemporaryRT(prepassBufferRTH.id);
#endif
        }

#if UNITY_2022_2_OR_NEWER
        public void Dispose()
        {
            prepassBufferRTHColor?.Release();
            prepassBufferRTHDepth?.Release();
        }
#endif
    }
}