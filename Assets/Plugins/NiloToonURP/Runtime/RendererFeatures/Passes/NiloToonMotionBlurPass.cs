// Already existing Unity Motion Blur solutions (NiloToon's motion blur develop based on KinoMotion):
// https://github.com/keijiro/KinoMotion/tree/master
// https://github.com/keijiro/MotionBlurTest

// Important Reference:
// https://casual-effects.com/research/McGuire2012Blur/McGuire12Blur.pdf

// Other reference:
// https://github.com/raysjoshua/UnrealEngine/blob/master/Engine/Shaders/PostProcessMotionBlur.usf
// https://github.com/EpicGames/UnrealEngine/tree/release/Engine/Shaders/Private/MotionBlur
// https://john-chapman-graphics.blogspot.com/2013/01/per-object-motion-blur.html
// https://youtu.be/b0S6WMAfi0o?si=8KSxKDHz9Z95VbVt (A Reconstruction Filter for Plausible Motion Blur (I3D 12))
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NiloToon.NiloToonURP
{
    public class NiloToonMotionBlurPass : ScriptableRenderPass
    {
        [System.Serializable]
        public class Settings
        {
            public bool allowRender = true;
        }
#if UNITY_2022_3_OR_NEWER        
        private Material blurMaterial;
        private RTHandle source;
        private RTHandle tempRT0;
        private RTHandle tempRT1;
        private RTHandle copyRT;

        static class Uniforms
        {
            internal static readonly int _VelocityScale     = Shader.PropertyToID("_VelocityScale");
            internal static readonly int _MaxBlurRadius     = Shader.PropertyToID("_MaxBlurRadius");
            internal static readonly int _RcpMaxBlurRadius  = Shader.PropertyToID("_RcpMaxBlurRadius");
            internal static readonly int _VelocityTex       = Shader.PropertyToID("_VelocityTex");
            internal static readonly int _MainTex           = Shader.PropertyToID("_MainTex");
            internal static readonly int _Tile2RT           = Shader.PropertyToID("_Tile2RT");
            internal static readonly int _Tile4RT           = Shader.PropertyToID("_Tile4RT");
            internal static readonly int _Tile8RT           = Shader.PropertyToID("_Tile8RT");
            internal static readonly int _TileMaxOffs       = Shader.PropertyToID("_TileMaxOffs");
            internal static readonly int _TileMaxLoop       = Shader.PropertyToID("_TileMaxLoop");
            internal static readonly int _TileVRT           = Shader.PropertyToID("_TileVRT");
            internal static readonly int _NeighborMaxTex    = Shader.PropertyToID("_NeighborMaxTex");
            internal static readonly int _LoopCount         = Shader.PropertyToID("_LoopCount");
        }

        enum Pass
        {
            VelocitySetup,
            TileMax1,
            TileMax2,
            TileMaxV,
            NeighborMax,
            Reconstruction,
        }

        private Settings settings;
        private string profilerTag;
        private float lastValidDeltaTime;
        private Vector3 lastValidCameraPosWS;
        private Matrix4x4 lastValidCameraViewProjMatrix;
        private bool lastFrameIsSafeToApplyMotionBlur;

        public NiloToonMotionBlurPass(NiloToonRendererFeatureSettings settings)
        {
            this.settings = settings.motionBlurSettings;
            renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;
            profilerTag = "NiloToon Motion Blur";

            ConfigureInput(ScriptableRenderPassInput.Motion | ScriptableRenderPassInput.Depth);

            if (blurMaterial == null)
            {
                blurMaterial = CoreUtils.CreateEngineMaterial("Hidden/NiloToon/NiloToonKinoMotionBlur");
            }
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            source = renderingData.cameraData.renderer.cameraColorTargetHandle;

            // Allocate temporary RTHandles
            var descriptor = renderingData.cameraData.cameraTargetDescriptor;
            descriptor.depthBufferBits = 0;
            descriptor.colorFormat = RenderTextureFormat.RGHalf; // Use RGHalf for higher precision if needed

            float rt_TargetLength = GetRTTargetLength(descriptor);
            float tileSize = GetTileSize(rt_TargetLength);
            
            int tileWidth = Mathf.CeilToInt(descriptor.width / tileSize);
            int tileHeight = Mathf.CeilToInt(descriptor.height / tileSize);

            var tileDesc = descriptor;
            tileDesc.width = tileWidth;
            tileDesc.height = tileHeight;
            
            RenderingUtils.ReAllocateIfNeeded(ref tempRT0, tileDesc, name: "_TempRT0");
            RenderingUtils.ReAllocateIfNeeded(ref tempRT1, tileDesc, name: "_TempRT1");

            var copyRTDesc = renderingData.cameraData.cameraTargetDescriptor;
            copyRTDesc.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref copyRT, copyRTDesc, name: "_CopyTex");
        }

        public float GetRTTargetLength(RenderTextureDescriptor descriptor)
        {
            return Mathf.Min(descriptor.width, descriptor.height);
        }

        public float GetIntensity()
        {
            float deltaTime = Time.deltaTime;
            
            // handle editor pause, where Time.deltaTime is 0
            if (deltaTime <= 0f)
            {
                deltaTime = lastValidDeltaTime; 
            }
            else
            {
                lastValidDeltaTime = deltaTime;
            }

            if (deltaTime <= 0) return 0; // should not happen except the first frame
            
            var motionBlurPP = VolumeManager.instance.stack.GetComponent<NiloToonMotionBlurVolume>();

            float intensity = motionBlurPP.intensity.value; // user custom multiplier control
            
            // make it fps independent (24fps 180 shutter angle as base, it is the perfect cinematic motion blur)
            intensity /= deltaTime / (1f/24f);  

            // * 0.5, so that when intensity in volume is 1, the result is matching ground truth 24fps 180 shutter angle from NiloToonMotionBlurVideoBaker)
            intensity *= 0.5f;
            return intensity;
        }
        public int GetTileSize(float rt_TargetLength)
        {
            // the ideal 1/48s shutter speed tileSize for:
            // 4320p = 30
            // 2160p = 15
            // 1080p = 7.5 (=8)
            int tileSize = Mathf.CeilToInt(rt_TargetLength / 4320f * 30f);
            
            // allow more blur bleed out
            tileSize *= 4;
            
            return tileSize;
        }

        bool IsCameraSafeToApplyMotionBlur(
            Matrix4x4 prevViewProj, 
            Matrix4x4 currentViewProj,
            Vector3 prevCamPos,
            Vector3 currentCamPos)
        {
            // Default thresholds
            const float maxPositionDiff = 2.0f;     // units in world space
            const float maxAngleDegrees = 15.0f;    // degrees
            const float maxSizeRatio = 2.0f;        // maximum 2x screen size change (100% change)

            // 1. Position Check
            float positionDiff = Vector3.Distance(currentCamPos, prevCamPos);
            if (positionDiff > maxPositionDiff)
                return false;

            // 2. Camera Direction Check
            Vector3 prevForward = -new Vector3(prevViewProj.m20, prevViewProj.m21, prevViewProj.m22).normalized;
            Vector3 currentForward = -new Vector3(currentViewProj.m20, currentViewProj.m21, currentViewProj.m22).normalized;
            float dotProduct = Vector3.Dot(prevForward, currentForward);
            float angleDiff = Mathf.Acos(Mathf.Clamp(dotProduct, -1.0f, 1.0f)) * Mathf.Rad2Deg;

            if (angleDiff > maxAngleDegrees)
                return false;

            // 3. FOV Check - using screen size ratio directly
            float prevTanHalfFOV = 1.0f / prevViewProj.m11;
            float currentTanHalfFOV = 1.0f / currentViewProj.m11;
            float screenSizeRatio = currentTanHalfFOV / prevTanHalfFOV;

            if (screenSizeRatio > maxSizeRatio || screenSizeRatio < 1.0f/maxSizeRatio)
                return false;

            return true;
        }
        
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (blurMaterial == null || source == null)
            {
                Debug.LogError("Motion Blur: Missing required resources");
                return;
            }

            var motionBlurPP = VolumeManager.instance.stack.GetComponent<NiloToonMotionBlurVolume>();

            if(!motionBlurPP.IsActive() || !settings.allowRender) return;

            CommandBuffer cmd = CommandBufferPool.Get(profilerTag);

            using (new ProfilingScope(cmd, new ProfilingSampler(profilerTag)))
            {
                var descriptor = renderingData.cameraData.cameraTargetDescriptor;

                float rt_TargetLength = GetRTTargetLength(descriptor);

                float intensity = GetIntensity();

                Camera currentCam = renderingData.cameraData.camera;
                bool thisFrameIsSafeToApplyMotionBlur = IsCameraSafeToApplyMotionBlur(
                    lastValidCameraViewProjMatrix,
                    currentCam.projectionMatrix * currentCam.worldToCameraMatrix,
                    lastValidCameraPosWS,
                    currentCam.transform.position);

                lastValidCameraPosWS = currentCam.transform.position;
                lastValidCameraViewProjMatrix = currentCam.projectionMatrix * currentCam.worldToCameraMatrix;
                
                // debug
                /*
                if (lastFrameIsSafeToApplyMotionBlur == false && thisFrameIsSafeToApplyMotionBlur == false)
                {
                    Debug.LogWarning("NiloToonMotionBlur 2 frames not safe to draw, is the auto detection too harsh?");
                }
                */
                
                lastFrameIsSafeToApplyMotionBlur = thisFrameIsSafeToApplyMotionBlur;
                

                if (intensity > 0 && thisFrameIsSafeToApplyMotionBlur)
                {
                    // [kino motion blur execute]

                    // setup var (this section added by Nilo)
                    //-----------------------------------------
                    CommandBuffer cb = cmd;
                    const float kMaxBlurRadius = 5f; // max blur is 5% of screen
                    Material material = blurMaterial;
                    
                    // Texture format for storing 2D vectors.
                    RenderTextureFormat m_VectorRTFormat = RenderTextureFormat.RGHalf;

                    // Texture format for storing packed velocity/depth.
                    RenderTextureFormat m_PackedRTFormat = RenderTextureFormat.ARGB2101010;

                    float shutterAngle = 180f * intensity;
                    int sampleCount = 32;

                    RTHandle destination =renderingData.cameraData.renderer.cameraColorTargetHandle;
                    //-----------------------------------------

                    // Calculate the maximum blur radius in pixels.
                    int maxBlurPixels = (int)(kMaxBlurRadius * rt_TargetLength / 100);

                    // Calculate the TileMax size.
                    // It should be a multiple of 8 and larger than maxBlur.
                    int tileSize = ((maxBlurPixels - 1) / 8 + 1) * 8;

                    // Pass 1 - Velocity/depth packing
                    var velocityScale = shutterAngle / 360f;
                    cb.SetGlobalFloat(Uniforms._VelocityScale, velocityScale);
                    cb.SetGlobalFloat(Uniforms._MaxBlurRadius, maxBlurPixels);
                    cb.SetGlobalFloat(Uniforms._RcpMaxBlurRadius, 1f / maxBlurPixels);

                    int vbuffer = Uniforms._VelocityTex;
                    cb.GetTemporaryRT(vbuffer, descriptor.width, descriptor.height, 0, FilterMode.Point, m_PackedRTFormat, RenderTextureReadWrite.Linear);
                    cb.Blit((Texture)null, vbuffer, material, (int)Pass.VelocitySetup);

                    // Pass 2 - First TileMax filter (1/2 downsize)
                    int tile2 = Uniforms._Tile2RT;
                    cb.GetTemporaryRT(tile2, descriptor.width / 2, descriptor.height / 2, 0, FilterMode.Point, m_VectorRTFormat, RenderTextureReadWrite.Linear);
                    cb.SetGlobalTexture(Uniforms._MainTex, vbuffer);
                    cb.Blit(vbuffer, tile2, material, (int)Pass.TileMax1);

                    // Pass 3 - Second TileMax filter (1/2 downsize)
                    int tile4 = Uniforms._Tile4RT;
                    cb.GetTemporaryRT(tile4, descriptor.width / 4, descriptor.height / 4, 0, FilterMode.Point, m_VectorRTFormat, RenderTextureReadWrite.Linear);
                    cb.SetGlobalTexture(Uniforms._MainTex, tile2);
                    cb.Blit(tile2, tile4, material, (int)Pass.TileMax2);
                    cb.ReleaseTemporaryRT(tile2);

                    // Pass 4 - Third TileMax filter (1/2 downsize)
                    int tile8 = Uniforms._Tile8RT;
                    cb.GetTemporaryRT(tile8, descriptor.width / 8, descriptor.height / 8, 0, FilterMode.Point, m_VectorRTFormat, RenderTextureReadWrite.Linear);
                    cb.SetGlobalTexture(Uniforms._MainTex, tile4);
                    cb.Blit(tile4, tile8, material, (int)Pass.TileMax2);
                    cb.ReleaseTemporaryRT(tile4);

                    // Pass 5 - Fourth TileMax filter (reduce to tileSize)
                    var tileMaxOffs = Vector2.one * (tileSize / 8f - 1f) * -0.5f;
                    cb.SetGlobalVector(Uniforms._TileMaxOffs, tileMaxOffs);
                    cb.SetGlobalFloat(Uniforms._TileMaxLoop, (int)(tileSize / 8f));

                    int tile = Uniforms._TileVRT;
                    cb.GetTemporaryRT(tile, descriptor.width / tileSize, descriptor.height / tileSize, 0, FilterMode.Point, m_VectorRTFormat, RenderTextureReadWrite.Linear);
                    cb.SetGlobalTexture(Uniforms._MainTex, tile8);
                    cb.Blit(tile8, tile, material, (int)Pass.TileMaxV);
                    cb.ReleaseTemporaryRT(tile8);

                    // Pass 6 - NeighborMax filter
                    int neighborMax = Uniforms._NeighborMaxTex;
                    int neighborMaxWidth = descriptor.width / tileSize;
                    int neighborMaxHeight = descriptor.height / tileSize;
                    cb.GetTemporaryRT(neighborMax, neighborMaxWidth, neighborMaxHeight, 0, FilterMode.Point, m_VectorRTFormat, RenderTextureReadWrite.Linear);
                    cb.SetGlobalTexture(Uniforms._MainTex, tile);
                    cb.Blit(tile, neighborMax, material, (int)Pass.NeighborMax);
                    cb.ReleaseTemporaryRT(tile);

                    // Copy the color buffer to use for sampling during the blur pass
                    Blitter.BlitCameraTexture(cmd, source, copyRT);
                    
                    // Pass 7 - Reconstruction pass
                    cb.SetGlobalFloat(Uniforms._LoopCount, Mathf.Clamp(sampleCount / 2, 1, 64));
                    cb.SetGlobalTexture(Uniforms._MainTex, copyRT);

                    cb.Blit(copyRT, destination, material, (int)Pass.Reconstruction);
                    
                    cb.ReleaseTemporaryRT(vbuffer);
                    cb.ReleaseTemporaryRT(neighborMax);
                }
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // do not write anything here
        }

        public void Dispose()
        {
            tempRT0?.Release();
            tempRT1?.Release();
            copyRT?.Release();
            
            CoreUtils.Destroy(blurMaterial);
        }
#else
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // do nothing in Unity2021.3
        }
#endif
    }
}
