// the main purpose of this pass is to:
// - request URP's depth & normal texture at a correct RenderPassEvent(must be BeforeOpaque or earlier(same as URP's SSAO = BeforeOpaque)), not any timing after that, else depth & normal texture may flicker)
// - set screen space outline's global param
// this pass doesn't "render"/"draw" anything

using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace NiloToon.NiloToonURP
{
    public class NiloToonScreenSpaceOutlinePass : ScriptableRenderPass
    {
        NiloToonRendererFeatureSettings allSettings;
        NiloToonToonOutlinePass.Settings settings;
        ProfilingSampler m_ProfilingSamplerScreenSpaceOutline;

        static readonly int _GlobalScreenSpaceOutlineIntensityForChar_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineIntensityForChar");
        static readonly int _GlobalScreenSpaceOutlineIntensityForEnvi_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineIntensityForEnvi");

        static readonly int _GlobalScreenSpaceOutlineWidthMultiplierForChar_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineWidthMultiplierForChar");
        static readonly int _GlobalScreenSpaceOutlineWidthMultiplierForEnvi_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineWidthMultiplierForEnvi");

        static readonly int _GlobalScreenSpaceOutlineNormalsSensitivityOffsetForChar_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineNormalsSensitivityOffsetForChar");
        static readonly int _GlobalScreenSpaceOutlineNormalsSensitivityOffsetForEnvi_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineNormalsSensitivityOffsetForEnvi");

        static readonly int _GlobalScreenSpaceOutlineDepthSensitivityOffsetForChar_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineDepthSensitivityOffsetForChar");
        static readonly int _GlobalScreenSpaceOutlineDepthSensitivityOffsetForEnvi_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineDepthSensitivityOffsetForEnvi");

        static readonly int _GlobalScreenSpaceOutlineDepthSensitivityDistanceFadeoutStrengthForChar_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineDepthSensitivityDistanceFadeoutStrengthForChar");
        static readonly int _GlobalScreenSpaceOutlineDepthSensitivityDistanceFadeoutStrengthForEnvi_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineDepthSensitivityDistanceFadeoutStrengthForEnvi");

        static readonly int _GlobalScreenSpaceOutlineTintColorForChar_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineTintColorForChar");
        static readonly int _GlobalScreenSpaceOutlineTintColorForEnvi_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineTintColorForEnvi");
        
        //----------------------[V2]------------------------------------
        static readonly int _GlobalScreenSpaceOutlineV2IntensityForChar_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineV2IntensityForChar");
        static readonly int _GlobalScreenSpaceOutlineV2IntensityForEnvi_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineV2IntensityForEnvi");
        
        static readonly int _GlobalScreenSpaceOutlineV2WidthMultiplierForChar_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineV2WidthMultiplierForChar");
        static readonly int _GlobalScreenSpaceOutlineV2WidthMultiplierForEnvi_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineV2WidthMultiplierForEnvi");

        static readonly int _GlobalScreenSpaceOutlineV2EnableGeometryEdgeForChar_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineV2EnableGeometryEdgeForChar");
        static readonly int _GlobalScreenSpaceOutlineV2EnableGeometryEdgeForEnvi_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineV2EnableGeometryEdgeForEnvi");
        static readonly int _GlobalScreenSpaceOutlineV2GeometryEdgeThresholdForChar_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineV2GeometryEdgeThresholdForChar");
        static readonly int _GlobalScreenSpaceOutlineV2GeometryEdgeThresholdForEnvi_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineV2GeometryEdgeThresholdForEnvi");
        
        static readonly int _GlobalScreenSpaceOutlineV2EnableNormalAngleEdgeForChar_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineV2EnableNormalAngleEdgeForChar");
        static readonly int _GlobalScreenSpaceOutlineV2EnableNormalAngleEdgeForEnvi_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineV2EnableNormalAngleEdgeForEnvi");
        static readonly int _GlobalScreenSpaceOutlineV2NormalAngleCosThetaThresholdForChar_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineV2NormalAngleCosThetaThresholdForChar");
        static readonly int _GlobalScreenSpaceOutlineV2NormalAngleCosThetaThresholdForEnvi_SID = Shader.PropertyToID("_GlobalScreenSpaceOutlineV2NormalAngleCosThetaThresholdForEnvi");

        // constructor(will not construct on every frame)
        public NiloToonScreenSpaceOutlinePass(NiloToonRendererFeatureSettings allSettings)
        {
            this.allSettings = allSettings;
            this.settings = allSettings.outlineSettings;
            m_ProfilingSamplerScreenSpaceOutline = new ProfilingSampler("NiloToonToonOutlinePass(Screen space outline)");
        }

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ConfigureInput(renderingData);
        }

        private void ConfigureInput(RenderingData renderingData)
        {
            // Nilotoon automatically turn on _CameraDepthTexture and _CameraNormalsTexture if needed,
            // so user don't need to turn on _CameraDepthTexture in their Universal Render Pipeline Asset manually.
            ScriptableRenderPassInput input = ScriptableRenderPassInput.None;
            if (ShouldRenderScreenSpaceOutline(renderingData, false))
            {
                input |= ScriptableRenderPassInput.Depth; // screen space outline requires URP's _CameraDepthTexture
                input |= ScriptableRenderPassInput.Normal; // screen space outline requires URP's _CameraNormalsTexture
            }
            ConfigureInput(input);
        }

        private bool ShouldRenderScreenSpaceOutline(RenderingData renderingData, bool limitedToAllowedCameraTypeOnly)
        {
            bool shouldRenderScreenSpaceOutline = settings.AllowRenderScreenSpaceOutline;

            // when requesting URP to draw _CameraDepthTexture / _CameraNormalTexture via ConfigureInput(ScriptableRenderPassInput) (called inside OnCameraSetup()),
            // it is better to include all CameraType, else _CameraDepthTexture / _CameraNormalTexture maybe wrong when mouse is moving across different CameraType windows
            // for example, _CameraDepthTexture / _CameraNormalTexture maybe flickering or using texture from another window

            // In preview window, screenSpaceOutlineVolumeEffect.IsActive() will return false even volume exists in scene and is active,
            // which will produce screen space outline flicker problem,
            // so in preview window, we skip volume check
            if (renderingData.cameraData.cameraType != CameraType.Preview)
            {
                var screenSpaceOutlineVolume = VolumeManager.instance.stack.GetComponent<NiloToonScreenSpaceOutlineControlVolume>();
                var screenSpaceOutlineV2Volume = VolumeManager.instance.stack.GetComponent<NiloToonScreenSpaceOutlineV2ControlVolume>();
                shouldRenderScreenSpaceOutline &= screenSpaceOutlineVolume.IsActive() || screenSpaceOutlineV2Volume.IsActive();
            }

            // but when control drawing screen space outline or not, we want to limited to CameraTypes that we desired,
            // so for control drawing (called inside Execute()), we will turn on "limitedToAllowedCameraTypeOnly".
            if (limitedToAllowedCameraTypeOnly)
            {
                // CameraType.Game is always needed
                bool isAllowedCameraType = renderingData.cameraData.cameraType == CameraType.Game;

                // we still provided option to On/Off screen space outline's drawing in SceneView, default is off since it maybe disturbing due to low resolution
                if (settings.AllowRenderScreenSpaceOutlineInSceneView)
                {
                    isAllowedCameraType |= renderingData.cameraData.cameraType == CameraType.SceneView;
                }

                shouldRenderScreenSpaceOutline &= isAllowedCameraType;
            }
            
            return shouldRenderScreenSpaceOutline;
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            renderScreenSpaceOutline(context, renderingData);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // do nothing
        }

        private void renderScreenSpaceOutline(ScriptableRenderContext context, RenderingData renderingData)
        {
            CommandBuffer cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingSamplerScreenSpaceOutline))
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
                
                bool shouldRenderScreenSpaceOutline = ShouldRenderScreenSpaceOutline(renderingData, true);
                bool isScreenSpaceOutlineV1Active = VolumeManager.instance.stack.GetComponent<NiloToonScreenSpaceOutlineControlVolume>().IsActive();
                bool isScreenSpaceOutlineV2Active = VolumeManager.instance.stack.GetComponent<NiloToonScreenSpaceOutlineV2ControlVolume>().IsActive();

                // V1 will be override by V2
                bool shouldRenderScreenSpaceOutlineV2 = shouldRenderScreenSpaceOutline && isScreenSpaceOutlineV2Active;
                bool shouldRenderScreenSpaceOutlineV1 = (shouldRenderScreenSpaceOutline && isScreenSpaceOutlineV1Active) && !shouldRenderScreenSpaceOutlineV2;

                // WIP temp code
                // disable SS outline V2, prevent user using it
                shouldRenderScreenSpaceOutlineV2 = false;
                
                if (shouldRenderScreenSpaceOutlineV1)
                {
                    var v = VolumeManager.instance.stack.GetComponent<NiloToonScreenSpaceOutlineControlVolume>();

                    float screenSpaceOutlineIntensityForChar = v.intensity.value * v.intensityForCharacter.value;
                    float screenSpaceOutlineIntensityForEnvi = v.intensity.value * v.intensityForEnvironment.value;

                    float screenSpaceOutlineWidthMultiplierForChar = v.widthMultiplier.value * v.widthMultiplierForCharacter.value;
                    float screenSpaceOutlineWidthMultiplierForEnvi = v.widthMultiplier.value * v.widthMultiplierForEnvironment.value;

                    // extra outline control if XR
                    if (XRSettings.isDeviceActive)
                    {
                        screenSpaceOutlineWidthMultiplierForChar *= v.extraWidthMultiplierForXR.value;
                        screenSpaceOutlineWidthMultiplierForEnvi *= v.extraWidthMultiplierForXR.value;
                    }

                    float screenSpaceOutlineDepthSensitivityGlobalOffsetForChar = v.depthSensitivityOffset.value + v.depthSensitivityOffsetForCharacter.value;
                    float screenSpaceOutlineDepthSensitivityGlobalOffsetForEnvi = v.depthSensitivityOffset.value + v.depthSensitivityOffsetForEnvironment.value;

                    float screenSpaceOutlineNormalsSensitivityGlobalOffsetForChar = v.normalsSensitivityOffset.value + v.normalsSensitivityOffsetForCharacter.value;
                    float screenSpaceOutlineNormalsSensitivityGlobalOffsetForEnvi = v.normalsSensitivityOffset.value + v.normalsSensitivityOffsetForEnvironment.value;

                    float screenSpaceOutlineDepthSensitivityDistanceFadeoutStrengthForChar = v.depthSensitivityDistanceFadeoutStrength.value * v.depthSensitivityDistanceFadeoutStrengthForCharacter.value;
                    float screenSpaceOutlineDepthSensitivityDistanceFadeoutStrengthForEnvi = v.depthSensitivityDistanceFadeoutStrength.value * v.depthSensitivityDistanceFadeoutStrengthForEnvironment.value;

                    Color screenSpaceOutlineTintColorForChar = v.outlineTintColor.value * v.outlineTintColorForChar.value;
                    Color screenSpaceOutlineTintColorForEnvi = v.outlineTintColor.value * v.outlineTintColorForEnvi.value;

                    // values that send to shader is defined here, adjustment is applyied in order to make volume UI's default value always = 0 or 1 (easier for user)
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineIntensityForChar_SID, screenSpaceOutlineIntensityForChar);
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineIntensityForEnvi_SID, screenSpaceOutlineIntensityForEnvi);

                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineWidthMultiplierForChar_SID, screenSpaceOutlineWidthMultiplierForChar * 1.875f);
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineWidthMultiplierForEnvi_SID, screenSpaceOutlineWidthMultiplierForEnvi * 1.875f);

                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineDepthSensitivityOffsetForChar_SID, screenSpaceOutlineDepthSensitivityGlobalOffsetForChar);
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineDepthSensitivityOffsetForEnvi_SID, screenSpaceOutlineDepthSensitivityGlobalOffsetForEnvi);

                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineNormalsSensitivityOffsetForChar_SID, screenSpaceOutlineNormalsSensitivityGlobalOffsetForChar);
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineNormalsSensitivityOffsetForEnvi_SID, screenSpaceOutlineNormalsSensitivityGlobalOffsetForEnvi + 0.25f);

                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineDepthSensitivityDistanceFadeoutStrengthForChar_SID, screenSpaceOutlineDepthSensitivityDistanceFadeoutStrengthForChar);
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineDepthSensitivityDistanceFadeoutStrengthForEnvi_SID, screenSpaceOutlineDepthSensitivityDistanceFadeoutStrengthForEnvi);

                    cmd.SetGlobalColor(_GlobalScreenSpaceOutlineTintColorForChar_SID, screenSpaceOutlineTintColorForChar);
                    cmd.SetGlobalColor(_GlobalScreenSpaceOutlineTintColorForEnvi_SID, screenSpaceOutlineTintColorForEnvi);
                }

                CoreUtils.SetKeyword(cmd, "_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE", shouldRenderScreenSpaceOutlineV1);
                
                //---------------------------------------------------------------------------------------------------------------------------------------
                // V2
                //---------------------------------------------------------------------------------------------------------------------------------------
                if (shouldRenderScreenSpaceOutlineV2)
                {
                    var v = VolumeManager.instance.stack.GetComponent<NiloToonScreenSpaceOutlineV2ControlVolume>();

                    //----------------------------------------------------------------------------------------------
                    // Intensity
                    //----------------------------------------------------------------------------------------------
                    float intensityForChar = v.intensity.value * v.intensityForCharacter.value;
                    float intensityForEnvi = v.intensity.value * v.intensityForEnvironment.value;
                    
                    //----------------------------------------------------------------------------------------------
                    // Width
                    //----------------------------------------------------------------------------------------------
                    float widthForChar = v.widthMultiplier.value * v.widthMultiplierForCharacter.value;
                    float widthForEnvi = v.widthMultiplier.value * v.widthMultiplierForEnvironment.value;

                    //----------------------------------------------------------------------------------------------
                    // Geometry Edge
                    //----------------------------------------------------------------------------------------------
                    bool enableGeometryEdgeForChar = v.drawGeometryEdgeOutlineForChar.value && v.drawGeometryEdgeOutline.value;
                    bool enableGeometryEdgeForEnvi = v.drawGeometryEdgeOutlineForEnvi.value && v.drawGeometryEdgeOutline.value;
                    float geometryEdgeThresholdForChar = v.geometryEdgeThresholdForCharacter.overrideState ? v.geometryEdgeThresholdForCharacter.value : v.geometryEdgeThreshold.value;
                    float geometryEdgeThresholdForEnvi = v.geometryEdgeThresholdForEnvironment.overrideState ? v.geometryEdgeThresholdForEnvironment.value : v.geometryEdgeThreshold.value;
                    // 0.001 threshold value in volume = 0.0003cm, it is the smallest threshold that shows most of the wireframe but just before precision noise artifact comes out
                    const float safeMinGeometryEdgeThreshold = 0.001f;
                    geometryEdgeThresholdForChar = Mathf.Max(safeMinGeometryEdgeThreshold, geometryEdgeThresholdForChar);
                    geometryEdgeThresholdForEnvi = Mathf.Max(safeMinGeometryEdgeThreshold, geometryEdgeThresholdForEnvi);
                    // threshold used in shader is in meter unit, = 0.3cm when threshold value in volume is 1
                    geometryEdgeThresholdForChar *= 0.003f; 
                    geometryEdgeThresholdForEnvi *= 0.003f;

                    //----------------------------------------------------------------------------------------------
                    // Normal Angle
                    //----------------------------------------------------------------------------------------------
                    bool enableNormalAngleEdgeForChar = v.drawNormalAngleOutlineForChar.value && v.drawNormalAngleOutline.value;
                    bool enableNormalAngleEdgeForEnvi = v.drawNormalAngleOutlineForEnvi.value && v.drawNormalAngleOutline.value;
                    float normalAngleThresholdForChar = v.normalAngleMinForCharacter.overrideState ? v.normalAngleMinForCharacter.value : v.normalAngleMin.value;
                    float normalAngleThresholdForEnvi = v.normalAngleMinForEnvironment.overrideState ? v.normalAngleMinForEnvironment.value : v.normalAngleMin.value;
                    float normalAngleCosThetaThresholdForChar = Mathf.Cos(Mathf.Deg2Rad * normalAngleThresholdForChar);
                    float normalAngleCosThetaThresholdForEnvi = Mathf.Cos(Mathf.Deg2Rad * normalAngleThresholdForEnvi);
                    
                    //----------------------------------------------------------------------------------------------
                    // Set Global param for shader
                    //----------------------------------------------------------------------------------------------
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineV2IntensityForChar_SID, intensityForChar);
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineV2IntensityForEnvi_SID, intensityForEnvi);
                    
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineV2WidthMultiplierForChar_SID, widthForChar);
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineV2WidthMultiplierForEnvi_SID, widthForEnvi);
                    
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineV2EnableGeometryEdgeForChar_SID, enableGeometryEdgeForChar ? 1 : 0);
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineV2EnableGeometryEdgeForEnvi_SID, enableGeometryEdgeForEnvi ? 1 : 0);
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineV2GeometryEdgeThresholdForChar_SID, geometryEdgeThresholdForChar);
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineV2GeometryEdgeThresholdForEnvi_SID,geometryEdgeThresholdForEnvi);
                    
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineV2EnableNormalAngleEdgeForChar_SID, enableNormalAngleEdgeForChar ? 1 : 0);
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineV2EnableNormalAngleEdgeForEnvi_SID, enableNormalAngleEdgeForEnvi ? 1 : 0);
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineV2NormalAngleCosThetaThresholdForChar_SID, normalAngleCosThetaThresholdForChar);
                    cmd.SetGlobalFloat(_GlobalScreenSpaceOutlineV2NormalAngleCosThetaThresholdForEnvi_SID,normalAngleCosThetaThresholdForEnvi);
                }
                CoreUtils.SetKeyword(cmd, "_NILOTOON_GLOBAL_ENABLE_SCREENSPACE_OUTLINE_V2", shouldRenderScreenSpaceOutlineV2);
            }
            
            // must write these line after using{} finished, to ensure profiler and frame debugger display correctness
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }
}