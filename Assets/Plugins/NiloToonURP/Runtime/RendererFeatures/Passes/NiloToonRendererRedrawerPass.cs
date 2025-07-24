// This script/class is not used anymore.

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace NiloToon.NiloToonURP
{
    public class NiloToonRendererRedrawerPass : ScriptableRenderPass
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
            /*
            // Never draw in Preview
            Camera camera = renderingData.cameraData.camera;
            if (camera.cameraType == CameraType.Preview)
                return;

            render(context, renderingData);
            */
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // do nothing
        }

        ProfilingSampler m_ProfilingSamplerNiloToonRendererRedrawer;
        NiloToonRendererFeatureSettings allSettings;

        // constructor(will not construct on every frame)
        public NiloToonRendererRedrawerPass(NiloToonRendererFeatureSettings allSettings)
        {
            this.allSettings = allSettings;
            m_ProfilingSamplerNiloToonRendererRedrawer = new ProfilingSampler("NiloToonRendererRedrawerPass");
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

        private void render(ScriptableRenderContext context, RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSamplerNiloToonRendererRedrawer))
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
                
                /*
                // draw every enabled NiloToonRendererRedrawer inside this renderer pass
                var list = NiloToonRendererRedrawer.instances;
                foreach (var instance in list)
                {
                    foreach (var draw in instance.DrawList)
                    {
                        Renderer renderer = instance.Renderer;

                        // safe check
                        if (!draw.enabled) continue;
                        if (!renderer) continue;
                        if (!draw.material) continue;

                        cmd.DrawRenderer(instance.Renderer, draw.material, draw.subMeshIndex, draw.passIndex);
                    }
                }
                */
            }

            // must write these line after using{} finished, to ensure profiler and frame debugger display correctness
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }
}