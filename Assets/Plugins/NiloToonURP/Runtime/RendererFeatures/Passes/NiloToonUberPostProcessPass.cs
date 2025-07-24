// It is impossible to do correct custom tonemapping for URP via renderer feature.
// - If we do it @ RenderPassEvent.BeforeRenderingPostProcessing, URP's {CHROMATIC_ABERRATION,BLOOM,Vignette} will have a clamped input
// - If we do it @ RenderPassEvent.AfterRenderingPostProcessing, our tonemapping's input is clamped, it is wrong

// URP' official bloom's logic note
/*
// RT note (all RT = B10G11R11_UFloatPack32)
_CameraColorTexture =	full resolution
_BloomMipDown0 = 	1/2 resolution
_BloomMipUp1 = 		1/4 resolution
_BloomMipDown1 = 	1/4 resolution
_BloomMipUp2 = 		1/8 resolution
_BloomMipDown2 = 	1/8 resolution
...

// pass note
Bloom.shader - Bloom Prefilter          = sample _CameraColorTexture(HQ = get more near samples, average) -> user clamp(min()) -> threshold -> EncodeHDR() -> return
Bloom.shader - Bloom Blur Horizontal    = sample 9 taps gaussin blur -> EncodeHDR() -> return
Bloom.shader - Bloom Blur Vertical      = sample 5 taps gaussin blur -> EncodeHDR() -> return
Bloom.shader - Bloom Upsample           = sample _SourceTex and _SourceTexLowMip, then combine them by lerp(a,b,t) -> return

// render step
1.RT _CameraColorTexture render done, resolve
----------------------------------------
2.render RT _BloomMipDown0	(set _CameraColorTexture as _SourceTex)(Bloom.shader - Bloom Prefilter)
----------------------------------------
3.render RT _BloomMipUp1	(set _BloomMipDown0 as _SourceTex)(Bloom.shader - Bloom Blur Horizontal)
4.render RT _BloomMipDown1	(set _BloomMipUp1 as _SourceTex)(Bloom.shader - Bloom Blur Vertical)
5.render RT _BloomMipUp2	(set _BloomMipDown1 as _SourceTex)(Bloom.shader - Bloom Blur Horizontal)
6.render RT _BloomMipDown2	(set _BloomMipUp2 as _SourceTex)(Bloom.shader - Bloom Blur Vertical)
....
7.render RT _BloomMipUp8	(set _BloomMipDown7 as _SourceTex)(Bloom.shader - Bloom Blur Horizontal)
8.render RT _BloomMipDown8	(set _BloomMipUp8 as _SourceTex)(Bloom.shader - Bloom Blur Vertical)
----------------------------------------
9.render RT _BloomMipUp7	(set _BloomMipDown7 as _SourceTex, _BloomMipDown8 as _SourceTexLowMip)(Bloom.shader - Bloom Upsample)
----------------------------------------
10.render RT _BloomMipUp6	(set _BloomMipDown6 as _SourceTex, _BloomMipUp7 as _SourceTexLowMip)(Bloom.shader - Bloom Upsample)
11.render RT _BloomMipUp5	(set _BloomMipDown5 as _SourceTex, _BloomMipUp6 as _SourceTexLowMip)(Bloom.shader - Bloom Upsample)
12.render RT _BloomMipUp4	(set _BloomMipDown4 as _SourceTex, _BloomMipUp5 as _SourceTexLowMip)(Bloom.shader - Bloom Upsample)
...
13.render RT _BloomMipUp0	(set _BloomMipDown0 as _SourceTex, _BloomMipUp1 as _SourceTexLowMip)(Bloom.shader - Bloom Upsample)
----------------------------------------
14.render to <No name>		(set _CameraColorTex as _SourceTex, _BloomMipUp0 as _BloomTexture, do LUT) (UberPost.shader - UberPost) (keyword = _BLOOM_LQ only)
(do ApplyColorGrading(_CameraColorTex + _BloomMipUp0 * BloomTint * BloomIntensity))
(so custom postprocess ping pong +bloom result, is correct)
*/

using System;
using System.Runtime.Serialization.Formatters;
using UnityEngine;
using UnityEngine.Experimental.Rendering;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;

namespace NiloToon.NiloToonURP
{
#if UNITY_2022_2_OR_NEWER
    public class NiloToonUberPostProcessPass : ScriptableRenderPass
    {
    #region Singleton
        public static NiloToonUberPostProcessPass Instance
        {
            get => _instance;
        }
        static NiloToonUberPostProcessPass _instance;
    #endregion

        [Serializable]
        public class Settings
        {
            [Header("Render Timing")]
            [Tooltip("The default value is BeforeRenderingPostProcess + 0, you can edit it to make NiloToon work with other renderer features")]
            [OverrideDisplayName("Renderer Feature Order Offset")]
            public int renderPassEventTimingOffset = 0;
            
            [Header("Bloom")]
            [Tooltip("Can turn off to prevent rendering NiloToonBloomVolume, which will improve performance for low quality graphics setting renderer")]
            [OverrideDisplayName("Allow render Bloom?")]
            public bool allowRenderNiloToonBloom = true;
        }
        public Settings settings { get; }

        NiloToonRendererFeatureSettings allSettings;

        ProfilingSampler niloToonBloomProfileSampler;
        ProfilingSampler niloToonUberProfileSampler;
        const string k_RenderPostProcessingTag = "Render NiloToon PostProcessing Effects"; // edited string value, original is "Render PostProcessing Effects"
        NiloToonBloomVolume m_Bloom; // edited type, original is Bloom
        ColorLookup m_ColorLookup;
        ColorAdjustments m_ColorAdjustments;
        NiloToonTonemappingVolume m_Tonemapping; // edited type, original is Tonemapping

        RenderTextureDescriptor m_Descriptor;
        //RTHandle m_Source; // disabled for "Not calling renderer.cameraColorTargetHandle in renderer feature"

        RTHandle[] m_BloomMipDown;
        RTHandle[] m_BloomMipUp;
        RTHandle NiloTempRTH;

        private static readonly ProfilingSampler m_ProfilingRenderPostProcessing = new ProfilingSampler(k_RenderPostProcessingTag);

        MaterialLibrary m_Materials;

        // Misc
        const int k_MaxPyramidSize = 16;
        readonly GraphicsFormat m_DefaultHDRFormat;
        bool m_UseRGBM;

        // this renderer feature/pass is NOT using swapbuffer system
        const bool m_UseSwapBuffer = true;

        public NiloToonUberPostProcessPass(NiloToonRendererFeatureSettings allSettings)
        {
            this.allSettings = allSettings;
            settings = allSettings.uberPostProcessSettings;
            _instance = this;

            // name displayed in Frame debugger
            niloToonBloomProfileSampler = new ProfilingSampler("Bloom");
            niloToonUberProfileSampler = new ProfilingSampler("UberPostProcess");

            Shader bloomShader = Shader.Find("Hidden/Universal Render Pipeline/NiloToonBloom");
            Shader uberShader = Shader.Find("Hidden/Universal Render Pipeline/NiloToonUberPost");
            Shader blitShader = Shader.Find("Hidden/Universal/CoreBlit");

            base.profilingSampler = new ProfilingSampler(nameof(NiloToonUberPostProcessPass)); // edited type, original is PostProcessPass
            m_Materials = new MaterialLibrary(bloomShader, uberShader, blitShader); // edited input param, original is a PostProcessData class instance        

            // Bloom pyramid shader ids - can't use a simple stackalloc in the bloom function as we
            // unfortunately need to allocate strings
            ShaderConstants._BloomMipUp = new int[k_MaxPyramidSize];
            ShaderConstants._BloomMipDown = new int[k_MaxPyramidSize];
            m_BloomMipUp = new RTHandle[k_MaxPyramidSize];
            m_BloomMipDown = new RTHandle[k_MaxPyramidSize];

            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                ShaderConstants._BloomMipUp[i] = Shader.PropertyToID("_NiloToonBloomMipUp" + i);
                ShaderConstants._BloomMipDown[i] = Shader.PropertyToID("_NiloToonBloomMipDown" + i);
                // Get name, will get Allocated with descriptor later
                m_BloomMipUp[i] = RTHandles.Alloc(ShaderConstants._BloomMipUp[i], name: "_NiloToonBloomMipUp" + i); // edited string name, original is _BloomMipUp
                m_BloomMipDown[i] = RTHandles.Alloc(ShaderConstants._BloomMipDown[i], name: "_NiloToonBloomMipDown" + i); // edited string name, original is _BloomMipDown
            }
            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                ShaderConstants._BloomMipUp[i] = Shader.PropertyToID("_NiloToonBloomMipUp" + i); 
                ShaderConstants._BloomMipDown[i] = Shader.PropertyToID("_NiloToonBloomMipDown" + i); 
            }

            // Texture format pre-lookup
            const FormatUsage usage = FormatUsage.Linear | FormatUsage.Render;
            if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, usage)) // HDR fallback
            {
                m_DefaultHDRFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                m_UseRGBM = false;
            }
            else
            {
                m_DefaultHDRFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? GraphicsFormat.R8G8B8A8_SRGB
                    : GraphicsFormat.R8G8B8A8_UNorm;
                m_UseRGBM = true;
            }
        }

        public void Cleanup() => m_Materials.Cleanup();

        public void Dispose()
        {
            foreach (var handle in m_BloomMipDown)
                handle?.Release();
            foreach (var handle in m_BloomMipUp)
                handle?.Release();

            NiloTempRTH?.Release();
        }
        public void Setup(in RenderTextureDescriptor baseDescriptor)
        {
            m_Descriptor = baseDescriptor;
            m_Descriptor.useMipMap = false;
            m_Descriptor.autoGenerateMips = false;
            //m_Source = source;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            //overrideCameraTarget = true;
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // [NiloToon added]
            // re-init m_Materials if needed, usually m_Materials's materials are null on project's first-time startup (right after rebuild library folder)
            if (m_Materials == null || 
                m_Materials.blit == null ||
                m_Materials.bloom == null ||
                m_Materials.uber == null)
            {
                Shader bloomShader = Shader.Find("Hidden/Universal Render Pipeline/NiloToonBloom");
                Shader uberShader = Shader.Find("Hidden/Universal Render Pipeline/NiloToonUberPost");
                Shader blitShader = Shader.Find("Hidden/Universal/CoreBlit");
                m_Materials = new MaterialLibrary(bloomShader, uberShader, blitShader);
            }
            
            // Start by pre-fetching all builtin effect settings we need
            var stack = VolumeManager.instance.stack;
            m_Bloom = stack.GetComponent<NiloToonBloomVolume>();
            m_ColorLookup = stack.GetComponent<ColorLookup>();
            m_ColorAdjustments = stack.GetComponent<ColorAdjustments>();
            m_Tonemapping = stack.GetComponent<NiloToonTonemappingVolume>(); // edited type, original is Tonemapping

            var cmd = CommandBufferPool.Get();
            // Regular render path (not on-tile) - we do everything in a single command buffer as it
            // makes it easier to manage temporary targets' lifetime
            using (new ProfilingScope(cmd, m_ProfilingRenderPostProcessing))
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

                Render(cmd, ref renderingData);
            }

            // must write these line after using{} finished, to ensure profiler and frame debugger display correctness
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        RenderTextureDescriptor GetCompatibleDescriptor()
            => GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_Descriptor.graphicsFormat);

        RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format, DepthBits depthBufferBits = DepthBits.None)
            => GetCompatibleDescriptor(m_Descriptor, width, height, format, depthBufferBits);

        internal static RenderTextureDescriptor GetCompatibleDescriptor(RenderTextureDescriptor desc, int width, int height, GraphicsFormat format, DepthBits depthBufferBits = DepthBits.None)
        {
            desc.depthBufferBits = (int)depthBufferBits;
            desc.msaaSamples = 1;
            desc.width = width;
            desc.height = height;
            desc.graphicsFormat = format;
            return desc;
        }

        void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            ref var cameraData = ref renderingData.cameraData;
            ref ScriptableRenderer renderer = ref cameraData.renderer;

            // Don't use these directly unless you have a good reason to, use GetSource() and
            // GetDestination() instead
            RTHandle source = renderer.cameraColorTargetHandle; // call renderer.cameraColorTargetHandle here directly, for "Not calling renderer.cameraColorTargetHandle in renderer feature"

            RTHandle GetSource() => source;

            // Setup projection matrix for cmd.DrawMesh()
            cmd.SetGlobalMatrix(ShaderConstants._FullscreenProjMat, GL.GetGPUProjectionMatrix(Matrix4x4.identity, true));

            // Combined post-processing stack
            using (new ProfilingScope(cmd, niloToonUberProfileSampler)) // NiloToon edit: original = ProfilingSampler.Get(URPProfileId.UberPostProcess)
            {
                // Reset uber keywords
                m_Materials.uber.shaderKeywords = null;

                // Bloom goes first
                bool bloomActive = m_Bloom.IsActive();
                bloomActive &= settings.allowRenderNiloToonBloom && cameraData.postProcessEnabled; // NiloToon edit: added && allowRenderNiloToonBloom && postProcessEnabled
                if (bloomActive)
                {
                    using (new ProfilingScope(cmd, niloToonBloomProfileSampler)) // NiloToon edit: ProfilingSampler.Get(URPProfileId.Bloom)
                        SetupBloom(cmd, GetSource(), m_Materials.uber);
                }

                // Setup other effects constants
                bool tonemappingActive = m_Tonemapping.IsActive() && cameraData.postProcessEnabled;
                
                // no need if check, always run
                {
                    SetupColorGrading(cmd, ref renderingData, m_Materials.uber);
                }

                bool uberNeeded = bloomActive || tonemappingActive;

                if (uberNeeded)
                {
                    RenderTextureDescriptor rtdTempRT = renderingData.cameraData.cameraTargetDescriptor;
                    rtdTempRT.depthBufferBits = 0;
                    rtdTempRT.depthStencilFormat = GraphicsFormat.None;
                    rtdTempRT.msaaSamples = 1; // no need MSAA for just a blit
                    RenderingUtils.ReAllocateIfNeeded(ref NiloTempRTH, rtdTempRT, name: "_NiloToonUberTempRT");

                    // 1.Blit direct copy _CameraColorTexture to _NiloToonUberTempRT
                    Blitter.BlitCameraTexture(cmd, GetSource(), NiloTempRTH, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Materials.blit, 0);
                    // 2.Blit _NiloToonUberTempRT to _CameraColorTexture, do uber postprocess
                    Blitter.BlitCameraTexture(cmd, NiloTempRTH, GetSource(), RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, m_Materials.uber, 0);
                }
            }
        }
        void SetupBloom(CommandBuffer cmd, RTHandle source, Material uberMaterial)
        {
            // [NiloToon edited]
            //===========================================================================================================
            // URP's official bloom will have very different bloom result depending on game window size(height), because blur is applied to constant count of pixels(4+1+4).
            // NiloToon's bloom can override to use a fixed size height instead of URP's "Start at half-res", to make all screen resolution's bloom result consistant.

            int th;
            int tw;
            if (m_Bloom.renderTextureOverridedToFixedHeight.overrideState)
            {
                // NiloToon's bloom resolution override code
                th = m_Bloom.renderTextureOverridedToFixedHeight.value;
                tw = (int)Mathf.Min(SystemInfo.maxTextureSize,th * ((float)m_Descriptor.width / (float)m_Descriptor.height));
            }
            else
            {
                // URP's official bloom code
                tw = m_Descriptor.width >> 1;
                th = m_Descriptor.height >> 1;
            }
            //===========================================================================================================

            // Determine the iteration count
            int maxSize = Mathf.Max(tw, th);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
            int mipCount = Mathf.Clamp(iterations, 1, m_Bloom.maxIterations.value);

            // Pre-filtering parameters
            float clamp = m_Bloom.clamp.value;
            float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
            float thresholdKnee = threshold * 0.5f; // Hardcoded soft knee

            // Material setup
            float scatter = Mathf.Lerp(0.05f, 0.95f, m_Bloom.scatter.value);
            var bloomMaterial = m_Materials.bloom;
            bloomMaterial.SetVector(ShaderConstants._Params, new Vector4(scatter, clamp, threshold, thresholdKnee));
            CoreUtils.SetKeyword(bloomMaterial, ShaderKeywordStrings.BloomHQ, m_Bloom.highQualityFiltering.value);
            //CoreUtils.SetKeyword(bloomMaterial, ShaderKeywordStrings.UseRGBM, m_UseRGBM); // disabled due to Unity6000.0.23f1 release on 2024/10/17

            // [NiloToon added]
            //====================================================================================
            // if not overridden, use generic threshold.
            // if overridden, use characterAreaOverridedThreshold.
            float finalThreshold = Mathf.GammaToLinearSpace(m_Bloom.characterAreaOverridedThreshold.overrideState ? m_Bloom.characterAreaOverridedThreshold.value : m_Bloom.threshold.value);
            float finalThresholdKnee = finalThreshold * 0.5f; // Hardcoded soft knee
            bloomMaterial.SetFloat("_NiloToonBloomCharacterAreaThreshold", finalThreshold);
            bloomMaterial.SetFloat("_NiloToonBloomCharacterAreaThresholdKnee", finalThresholdKnee);

            bloomMaterial.SetFloat("_NiloToonBloomCharacterAreaBloomEmitMultiplier", m_Bloom.characterAreaBloomEmitMultiplier.value);

            Vector4 HSV_applyToCharAreaOnly;
            HSV_applyToCharAreaOnly.x = m_Bloom.HueOffset.value;
            HSV_applyToCharAreaOnly.y = m_Bloom.SaturationBoost.value;
            HSV_applyToCharAreaOnly.z = m_Bloom.ValueMultiply.value;
            HSV_applyToCharAreaOnly.w = m_Bloom.ApplyHSVToCharAreaOnly.value;
            bloomMaterial.SetVector("_NiloToonBloomHSVModifier", HSV_applyToCharAreaOnly);
            //====================================================================================

            // Prefilter
            var desc = GetCompatibleDescriptor(tw, th, m_DefaultHDRFormat);
            for (int i = 0; i < mipCount; i++)
            {
                RenderingUtils.ReAllocateIfNeeded(ref m_BloomMipUp[i], desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: m_BloomMipUp[i].name);
                RenderingUtils.ReAllocateIfNeeded(ref m_BloomMipDown[i], desc, FilterMode.Bilinear, TextureWrapMode.Clamp, name: m_BloomMipDown[i].name);
                desc.width = Mathf.Max(1, desc.width >> 1);
                desc.height = Mathf.Max(1, desc.height >> 1);
            }

            Blitter.BlitCameraTexture(cmd, source, m_BloomMipDown[0], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterial, 0);

            // Downsample - gaussian pyramid
            var lastDown = m_BloomMipDown[0];
            for (int i = 1; i < mipCount; i++)
            {
                // Classic two pass gaussian blur - use mipUp as a temporary target
                //   First pass does 2x downsampling + 9-tap gaussian
                //   Second pass does 9-tap gaussian using a 5-tap filter + bilinear filtering
                Blitter.BlitCameraTexture(cmd, lastDown, m_BloomMipUp[i], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterial, 1);
                Blitter.BlitCameraTexture(cmd, m_BloomMipUp[i], m_BloomMipDown[i], RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterial, 2);

                lastDown = m_BloomMipDown[i];
            }

            // Upsample (bilinear by default, HQ filtering does bicubic instead
            for (int i = mipCount - 2; i >= 0; i--)
            {
                var lowMip = (i == mipCount - 2) ? m_BloomMipDown[i + 1] : m_BloomMipUp[i + 1];
                var highMip = m_BloomMipDown[i];
                var dst = m_BloomMipUp[i];

                cmd.SetGlobalTexture(ShaderConstants._SourceTexLowMip, lowMip);
                Blitter.BlitCameraTexture(cmd, highMip, dst, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store, bloomMaterial, 3);
            }

            // Setup bloom on uber
            var tint = m_Bloom.tint.value.linear;
            var luma = ColorUtils.Luminance(tint);
            tint = luma > 0f ? tint * (1f / luma) : Color.white;

            var bloomParams = new Vector4(m_Bloom.intensity.value, tint.r, tint.g, tint.b);
            uberMaterial.SetVector(ShaderConstants._Bloom_Params, bloomParams);
            uberMaterial.SetFloat(ShaderConstants._Bloom_RGBM, m_UseRGBM ? 1f : 0f);

            // [NiloToon added]
            //====================================================================================
            // if not overridden, use generic intensity.
            // if overridden, use characterAreaOverridedIntensity.
            uberMaterial.SetFloat("_NiloToonBloomCharacterAreaIntensity", m_Bloom.characterAreaOverridedIntensity.overrideState ? m_Bloom.characterAreaOverridedIntensity.value : m_Bloom.intensity.value);
            //====================================================================================

            cmd.SetGlobalTexture(ShaderConstants._Bloom_Texture, m_BloomMipUp[0]);

            // Setup lens dirtiness on uber
            // Keep the aspect ratio correct & center the dirt texture, we don't want it to be
            // stretched or squashed
            var dirtTexture = m_Bloom.dirtTexture.value == null ? Texture2D.blackTexture : m_Bloom.dirtTexture.value;
            float dirtRatio = dirtTexture.width / (float)dirtTexture.height;
            float screenRatio = m_Descriptor.width / (float)m_Descriptor.height;
            var dirtScaleOffset = new Vector4(1f, 1f, 0f, 0f);
            float dirtIntensity = m_Bloom.dirtIntensity.value;

            if (dirtRatio > screenRatio)
            {
                dirtScaleOffset.x = screenRatio / dirtRatio;
                dirtScaleOffset.z = (1f - dirtScaleOffset.x) * 0.5f;
            }
            else if (screenRatio > dirtRatio)
            {
                dirtScaleOffset.y = dirtRatio / screenRatio;
                dirtScaleOffset.w = (1f - dirtScaleOffset.y) * 0.5f;
            }

            uberMaterial.SetVector(ShaderConstants._LensDirt_Params, dirtScaleOffset);
            uberMaterial.SetFloat(ShaderConstants._LensDirt_Intensity, dirtIntensity);
            uberMaterial.SetTexture(ShaderConstants._LensDirt_Texture, dirtTexture);

            // Keyword setup - a bit convoluted as we're trying to save some variants in Uber...
            if (m_Bloom.highQualityFiltering.value)
                uberMaterial.EnableKeyword(dirtIntensity > 0f ? ShaderKeywordStrings.BloomHQDirt : ShaderKeywordStrings.BloomHQ);
            else
                uberMaterial.EnableKeyword(dirtIntensity > 0f ? ShaderKeywordStrings.BloomLQDirt : ShaderKeywordStrings.BloomLQ);
        }

        void SetupColorGrading(CommandBuffer cmd, ref RenderingData renderingData, Material material)
        {
            ref var postProcessingData = ref renderingData.postProcessingData;
            bool hdr = postProcessingData.gradingMode == ColorGradingMode.HighDynamicRange;
            int lutHeight = postProcessingData.lutSize;
            int lutWidth = lutHeight * lutHeight;

            // Source material setup
            float postExposureLinear = Mathf.Pow(2f, m_ColorAdjustments.postExposure.value);
            //cmd.SetGlobalTexture(ShaderConstants._InternalLut, m_InternalLut.nameID); // [NiloToon edit] temp disabled since we don't have m_InternalLut here
            material.SetVector(ShaderConstants._Lut_Params, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f, postExposureLinear));
            material.SetTexture(ShaderConstants._UserLut, m_ColorLookup.texture.value);
            material.SetVector(ShaderConstants._UserLut_Params, !m_ColorLookup.IsActive()
                ? Vector4.zero
                : new Vector4(1f / m_ColorLookup.texture.value.width,
                    1f / m_ColorLookup.texture.value.height,
                    m_ColorLookup.texture.value.height - 1f,
                    m_ColorLookup.contribution.value)
            );

            // [NiloToon added]
            //====================================================================================
            // NiloToon can't support HDR Grading Mode due to URP's design,
            // so we just enable the same NiloToon tonemapping code for both ldr and hdr Color Grading Mode 
            hdr = false;
            //====================================================================================
                
            if (hdr)
            {
                material.EnableKeyword(ShaderKeywordStrings.HDRGrading);
            }
            else
            {
                switch (m_Tonemapping.mode.value)
                {
                    case TonemappingMode.Neutral: material.EnableKeyword(ShaderKeywordStrings.TonemapNeutral); break;
                    case TonemappingMode.ACES: material.EnableKeyword(ShaderKeywordStrings.TonemapACES); break;

                    // [NiloToon added]
                    //====================================================================================
                    case TonemappingMode.GT: 
                        material.EnableKeyword("_TONEMAP_GRANTURISMO"); 
                        break;
                    case TonemappingMode.ACES_CustomSR:
                        // custom ACES function with ABCDE param from this repo, it has better saturation than other tonemap method
                        // https://github.com/stalomeow/StarRailNPRShader/blob/main/Shaders/Postprocessing/UberPost.shader
                        material.EnableKeyword("_TONEMAP_ACES_CUSTOM"); 
                        material.SetFloat("_NiloToon_CustomACESParamA", 2.8f);
                        material.SetFloat("_NiloToon_CustomACESParamB", 0.4f);
                        material.SetFloat("_NiloToon_CustomACESParamC", 2.1f);
                        material.SetFloat("_NiloToon_CustomACESParamD", 0.5f);
                        material.SetFloat("_NiloToon_CustomACESParamE", 1.5f);
                        break;
                    case TonemappingMode.KhronosPBRNeutral:
                        material.EnableKeyword("_TONEMAP_KHRONOS_PBR_NEUTRAL");
                        break;
                    case TonemappingMode.NiloNPRNeutralV1:
                        material.EnableKeyword("_TONEMAP_NILO_NPR_NEUTRAL_V1");
                        break;
                    case TonemappingMode.NiloHybirdACES:
                        material.EnableKeyword("_TONEMAP_NILO_HYBIRD_ACES");
                        break;
                    //====================================================================================

                    default: break; // None
                }
            }

            // [NiloToon added]
            //====================================================================================
            material.SetFloat("_NiloToonTonemappingCharacterAreaRemove", 1 - m_Tonemapping.ApplyToCharacter.value * m_Tonemapping.intensity.value);
            material.SetFloat("_NiloToonTonemappingCharacterAreaPreTonemapBrightnessMul", m_Tonemapping.BrightnessMulForCharacter.value * m_Bloom.characterPreTonemapBrightness.value);
            
            material.SetFloat("_NiloToonTonemappingNonCharacterAreaRemove", 1 - m_Tonemapping.ApplyToNonCharacter.value * m_Tonemapping.intensity.value);
            material.SetFloat("_NiloToonTonemappingNonCharacterAreaPreTonemapBrightnessMul", m_Tonemapping.BrightnessMulForNonCharacter.value);
            //====================================================================================
        }

        class MaterialLibrary
        {
            public readonly Material bloom;
            public readonly Material uber;
            public readonly Material blit;

            // NiloToon edited, instead of receiving a PostProcessData class, we pass 2 Shaders directly
            public MaterialLibrary(Shader bloomShader, Shader uberShader, Shader blitShader)
            {
                bloom = Load(bloomShader);
                uber = Load(uberShader);
                blit = Load(blitShader);
            }

            Material Load(Shader shader)
            {
                if (shader == null)
                {
                    //Debug.LogErrorFormat($"Missing shader. {GetType().DeclaringType.Name} render pass will not execute. Check for missing reference in the renderer resources."); // NiloToonURP: removed
                    return null;
                }
                else if (!shader.isSupported)
                {
                    return null;
                }

                return CoreUtils.CreateEngineMaterial(shader);
            }

            internal void Cleanup()
            {
                CoreUtils.Destroy(bloom);
                CoreUtils.Destroy(uber);
                CoreUtils.Destroy(blit);
            }
        }

        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        static class ShaderConstants
        {
            public static readonly int _TempTarget = Shader.PropertyToID("_TempTarget");
            public static readonly int _TempTarget2 = Shader.PropertyToID("_TempTarget2");

            public static readonly int _StencilRef = Shader.PropertyToID("_StencilRef");
            public static readonly int _StencilMask = Shader.PropertyToID("_StencilMask");

            public static readonly int _FullCoCTexture = Shader.PropertyToID("_FullCoCTexture");
            public static readonly int _HalfCoCTexture = Shader.PropertyToID("_HalfCoCTexture");
            public static readonly int _DofTexture = Shader.PropertyToID("_DofTexture");
            public static readonly int _CoCParams = Shader.PropertyToID("_CoCParams");
            public static readonly int _BokehKernel = Shader.PropertyToID("_BokehKernel");
            public static readonly int _BokehConstants = Shader.PropertyToID("_BokehConstants");
            public static readonly int _PongTexture = Shader.PropertyToID("_PongTexture");
            public static readonly int _PingTexture = Shader.PropertyToID("_PingTexture");

            public static readonly int _Metrics = Shader.PropertyToID("_Metrics");
            public static readonly int _AreaTexture = Shader.PropertyToID("_AreaTexture");
            public static readonly int _SearchTexture = Shader.PropertyToID("_SearchTexture");
            public static readonly int _EdgeTexture = Shader.PropertyToID("_EdgeTexture");
            public static readonly int _BlendTexture = Shader.PropertyToID("_BlendTexture");

            public static readonly int _ColorTexture = Shader.PropertyToID("_ColorTexture");
            public static readonly int _Params = Shader.PropertyToID("_Params");
            public static readonly int _SourceTexLowMip = Shader.PropertyToID("_SourceTexLowMip");
            public static readonly int _Bloom_Params = Shader.PropertyToID("_Bloom_Params");
            public static readonly int _Bloom_RGBM = Shader.PropertyToID("_Bloom_RGBM");
            public static readonly int _Bloom_Texture = Shader.PropertyToID("_Bloom_Texture");
            public static readonly int _LensDirt_Texture = Shader.PropertyToID("_LensDirt_Texture");
            public static readonly int _LensDirt_Params = Shader.PropertyToID("_LensDirt_Params");
            public static readonly int _LensDirt_Intensity = Shader.PropertyToID("_LensDirt_Intensity");
            public static readonly int _Distortion_Params1 = Shader.PropertyToID("_Distortion_Params1");
            public static readonly int _Distortion_Params2 = Shader.PropertyToID("_Distortion_Params2");
            public static readonly int _Chroma_Params = Shader.PropertyToID("_Chroma_Params");
            public static readonly int _Vignette_Params1 = Shader.PropertyToID("_Vignette_Params1");
            public static readonly int _Vignette_Params2 = Shader.PropertyToID("_Vignette_Params2");
            public static readonly int _Vignette_ParamsXR = Shader.PropertyToID("_Vignette_ParamsXR");
            public static readonly int _Lut_Params = Shader.PropertyToID("_Lut_Params");
            public static readonly int _UserLut_Params = Shader.PropertyToID("_UserLut_Params");
            public static readonly int _InternalLut = Shader.PropertyToID("_InternalLut");
            public static readonly int _UserLut = Shader.PropertyToID("_UserLut");
            public static readonly int _DownSampleScaleFactor = Shader.PropertyToID("_DownSampleScaleFactor");

            public static readonly int _FlareOcclusionRemapTex = Shader.PropertyToID("_FlareOcclusionRemapTex");
            public static readonly int _FlareOcclusionTex = Shader.PropertyToID("_FlareOcclusionTex");
            public static readonly int _FlareOcclusionIndex = Shader.PropertyToID("_FlareOcclusionIndex");
            public static readonly int _FlareTex = Shader.PropertyToID("_FlareTex");
            public static readonly int _FlareColorValue = Shader.PropertyToID("_FlareColorValue");
            public static readonly int _FlareData0 = Shader.PropertyToID("_FlareData0");
            public static readonly int _FlareData1 = Shader.PropertyToID("_FlareData1");
            public static readonly int _FlareData2 = Shader.PropertyToID("_FlareData2");
            public static readonly int _FlareData3 = Shader.PropertyToID("_FlareData3");
            public static readonly int _FlareData4 = Shader.PropertyToID("_FlareData4");
            public static readonly int _FlareData5 = Shader.PropertyToID("_FlareData5");

            public static readonly int _FullscreenProjMat = Shader.PropertyToID("_FullscreenProjMat");

            public static int[] _BloomMipUp;
            public static int[] _BloomMipDown;
        }
    }
#else

    public class NiloToonUberPostProcessPass : ScriptableRenderPass
    {
        #region Singleton
        public static NiloToonUberPostProcessPass Instance
        {
            get => _instance;
        }
        static NiloToonUberPostProcessPass _instance;
        #endregion

        [Serializable]
        public class Settings
        {
            [Header("Render Timing")]
            [Tooltip("The default value is BeforeRenderingPostProcess + 0, you can edit it to make NiloToon work with other renderer features")]
            [OverrideDisplayName("Renderer Feature Order Offset")]
            public int renderPassEventTimingOffset = 0;
            
            [Header("Bloom")]
            [Tooltip("Can turn off to prevent rendering NiloToonBloomVolume, which will improve performance for low quality graphics setting renderer")]
            [OverrideDisplayName("Allow render Bloom?")]
            public bool allowRenderNiloToonBloom = true;
        }
        public Settings settings { get; }

        NiloToonRendererFeatureSettings allSettings;

        ProfilingSampler niloToonBloomProfileSampler;
        ProfilingSampler niloToonUberProfileSampler;
        #region [Copy and edited, from URP10.5.0's PostProcessPass.cs's fields]
        const string k_RenderPostProcessingTag = "Render NiloToon PostProcessing Effects"; // edited string value, original is "Render PostProcessing Effects"
        NiloToonBloomVolume m_Bloom; // edited type, original is Bloom
        ColorLookup m_ColorLookup;
        ColorAdjustments m_ColorAdjustments;
        NiloToonTonemappingVolume m_Tonemapping; // edited type, original is Tonemapping
        #endregion

        #region [Direct copy and NO edit, from URP10.5.0's PostProcessPass.cs's fields]
        RenderTextureDescriptor m_Descriptor;
        RenderTargetHandle m_Source;

        private static readonly ProfilingSampler m_ProfilingRenderPostProcessing = new ProfilingSampler(k_RenderPostProcessingTag);

        MaterialLibrary m_Materials;

        // Misc
        const int k_MaxPyramidSize = 16;
        readonly GraphicsFormat m_DefaultHDRFormat;
        bool m_UseRGBM;

        // Option to use procedural draw instead of cmd.blit
        bool m_UseDrawProcedural;
        #endregion

        public NiloToonUberPostProcessPass(NiloToonRendererFeatureSettings allSettings)
        {
            this.allSettings = allSettings;
            settings = allSettings.uberPostProcessSettings;
            _instance = this;

            niloToonBloomProfileSampler = new ProfilingSampler("NiloToonBloomProfileSampler");
            niloToonUberProfileSampler = new ProfilingSampler("NiloToonUberProfileSampler");

            Shader bloomShader = Shader.Find("Hidden/Universal Render Pipeline/NiloToonBloom");
            Shader uberShader = Shader.Find("Hidden/Universal Render Pipeline/NiloToonUberPost");
            Shader blitShader = Shader.Find("Hidden/Universal Render Pipeline/Blit");

            #region [Copy and edited, from URP10.5.0's PostProcessPass.cs's constructor PostProcessPass(...)]
            base.profilingSampler = new ProfilingSampler(nameof(NiloToonUberPostProcessPass)); // edited type, original is PostProcessPass
            m_Materials = new MaterialLibrary(bloomShader, uberShader, blitShader); // edited input param, original is a PostProcessData class instance
            #endregion

            #region [Copy and remove unneeded, keep bloom only, from URP10.5.0's PostProcessPass.cs's constructor PostProcessPass(...)]
            // Texture format pre-lookup
            if (SystemInfo.IsFormatSupported(GraphicsFormat.B10G11R11_UFloatPack32, FormatUsage.Linear | FormatUsage.Render))
            {
                m_DefaultHDRFormat = GraphicsFormat.B10G11R11_UFloatPack32;
                m_UseRGBM = false;
            }
            else
            {
                m_DefaultHDRFormat = QualitySettings.activeColorSpace == ColorSpace.Linear
                    ? GraphicsFormat.R8G8B8A8_SRGB
                    : GraphicsFormat.R8G8B8A8_UNorm;
                m_UseRGBM = true;
            }

            // Bloom pyramid shader ids - can't use a simple stackalloc in the bloom function as we
            // unfortunately need to allocate strings
            ShaderConstants._BloomMipUp = new int[k_MaxPyramidSize];
            ShaderConstants._BloomMipDown = new int[k_MaxPyramidSize];
            #endregion

            #region [Copy and edited, from URP10.5.0's PostProcessPass.cs's constructor PostProcessPass(...)]
            for (int i = 0; i < k_MaxPyramidSize; i++)
            {
                ShaderConstants._BloomMipUp[i] = Shader.PropertyToID("_NiloToonBloomMipUp" + i); // edited string name, original is _BloomMipUp
                ShaderConstants._BloomMipDown[i] = Shader.PropertyToID("_NiloToonBloomMipDown" + i); // edited string name, original is _BloomMipDown
            }
            #endregion
        }

        #region [Copy and edited, from URP10.5.0's PostProcessPass.cs's Setup(...)]
        public void Setup(in RenderTextureDescriptor baseDescriptor, in RenderTargetHandle source)
        {
            m_Descriptor = baseDescriptor;
            m_Descriptor.useMipMap = false;
            m_Descriptor.autoGenerateMips = false;
            m_Source = source;

            // NiloToonURP removed
            /*
            m_Destination = destination;
            m_Depth = depth;
            m_InternalLut = internalLut;
            m_IsFinalPass = false;
            m_HasFinalPass = hasFinalPass;
            m_EnableSRGBConversionIfNeeded = enableSRGBConversion;
            */
        }
        #endregion

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // do nothing
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            // [NiloToon added]
            // re-init m_Materials if needed, usually m_Materials's materials are null on project's first-time startup (right after rebuild library folder)
            if (m_Materials == null || 
                m_Materials.blit == null ||
                m_Materials.bloom == null ||
                m_Materials.uber == null)
            {
                Shader bloomShader = Shader.Find("Hidden/Universal Render Pipeline/NiloToonBloom");
                Shader uberShader = Shader.Find("Hidden/Universal Render Pipeline/NiloToonUberPost");
                Shader blitShader = Shader.Find("Hidden/Universal Render Pipeline/Blit");
                m_Materials = new MaterialLibrary(bloomShader, uberShader, blitShader);
            }
            
            #region [Copy and edited, from URP10.5.0's PostProcessPass.cs's Execute(...)]
            // Start by pre-fetching all builtin effect settings we need
            // Some of the color-grading settings are only used in the color grading lut pass
            var stack = VolumeManager.instance.stack;
            m_Bloom = stack.GetComponent<NiloToonBloomVolume>(); // edited type, original is Bloom
            m_ColorLookup = stack.GetComponent<ColorLookup>();
            m_ColorAdjustments = stack.GetComponent<ColorAdjustments>();
            m_Tonemapping = stack.GetComponent<NiloToonTonemappingVolume>(); // edited type, original is Tonemapping

            m_UseDrawProcedural = renderingData.cameraData.xrRendering; // edited, original is renderingData.cameraData.xr
            m_UseDrawProcedural = false; // TEMP disabled to make Blit() works
            #endregion

            #region [Direct copy and no edit, from URP10.5.0's PostProcessPass.cs's Execute(...)]

            // Regular render path (not on-tile) - we do everything in a single command buffer as it
            // makes it easier to manage temporary targets' lifetime
            var cmd = CommandBufferPool.Get();
            using (new ProfilingScope(cmd, m_ProfilingRenderPostProcessing))
            {
                Render(cmd, ref renderingData);
            }

            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
            #endregion
        }

        void Render(CommandBuffer cmd, ref RenderingData renderingData)
        {
            #region [copy and edited, from URP10.5.0's PostProcessPass.cs's Render()]
            ref var cameraData = ref renderingData.cameraData;

            int source = m_Source.id;

            // Utilities to simplify intermediate target management
            int GetSource() => source;

            // Setup projection matrix for cmd.DrawMesh()
            cmd.SetGlobalMatrix(ShaderConstants._FullscreenProjMat, GL.GetGPUProjectionMatrix(Matrix4x4.identity, true));

            // Combined post-processing stack
            using (new ProfilingScope(cmd, niloToonUberProfileSampler)) // NiloToon edit: original = ProfilingSampler.Get(URPProfileId.UberPostProcess)
            {
                // Reset uber keywords
                m_Materials.uber.shaderKeywords = null;
                
                // Bloom goes first
                bool bloomActive = m_Bloom.IsActive() && settings.allowRenderNiloToonBloom && cameraData.postProcessEnabled; // NiloToon edit: added && allowRenderNiloToonBloom && postProcessEnabled
                if (bloomActive)
                {
                    using (new ProfilingScope(cmd, niloToonBloomProfileSampler)) // NiloToon edit: ProfilingSampler.Get(URPProfileId.Bloom)
                        SetupBloom(cmd, GetSource(), m_Materials.uber);
                }

                // Setup other effects constants
                bool tonemappingActive = m_Tonemapping.IsActive() && cameraData.postProcessEnabled;

                // no need if check, always run
                {
                    SetupColorGrading(cmd, ref renderingData, m_Materials.uber);
                }
                
                bool uberNeeded = bloomActive || tonemappingActive;
                if (uberNeeded)
                {
                    // 1.Blit direct copy _CameraColorTexture to _NiloToonUberTempRT
                    RenderTextureDescriptor rtdTempRT = renderingData.cameraData.cameraTargetDescriptor;
                    rtdTempRT.msaaSamples = 1; // no need MSAA for just a blit
                    cmd.GetTemporaryRT(Shader.PropertyToID("_NiloToonUberTempRT"), rtdTempRT, FilterMode.Point);
                    Blit(cmd, new RenderTargetIdentifier(GetSource()), new RenderTargetIdentifier("_NiloToonUberTempRT"), m_Materials.blit);

                    // 2.Blit _NiloToonUberTempRT to _CameraColorTexture, do uber postprocess
                    Blit(cmd, new RenderTargetIdentifier("_NiloToonUberTempRT"), new RenderTargetIdentifier(GetSource()), m_Materials.uber);

                    // 3.Cleanup
                    cmd.ReleaseTemporaryRT(Shader.PropertyToID("_NiloToonUberTempRT"));
                }

                // 3.Cleanup
                if (bloomActive)
                    cmd.ReleaseTemporaryRT(ShaderConstants._BloomMipUp[0]);
            }
            #endregion
        }
        #region [Copy and edited, from URP10.5.0's PostProcessPass.cs's SetupBloom()]
        // (all changes will be marked by a [NiloToon edited] tag)
        // (if there is no [NiloToon edited] tag, then it is a direct copy
        void SetupBloom(CommandBuffer cmd, int source, Material uberMaterial)
        {
            // [NiloToon edited]
            //===========================================================================================================
            // URP's official bloom will have very different bloom result depending on game window size(height), because blur is applied to constant count of pixels(4+1+4).
            // NiloToon's bloom can override to use a fixed size height instead of URP's "Start at half-res", to make all screen resolution's bloom result consistant.

            int th;
            int tw;
            if (m_Bloom.renderTextureOverridedToFixedHeight.overrideState)
            {
                // NiloToon's bloom code
                th = m_Bloom.renderTextureOverridedToFixedHeight.value;
                tw = (int)Mathf.Min(SystemInfo.maxTextureSize,th * ((float)m_Descriptor.width / (float)m_Descriptor.height));
            }
            else
            {
                // URP's official bloom code
                tw = m_Descriptor.width >> 1;
                th = m_Descriptor.height >> 1;
            }
            //===========================================================================================================

            // Determine the iteration count
            int maxSize = Mathf.Max(tw, th);
            int iterations = Mathf.FloorToInt(Mathf.Log(maxSize, 2f) - 1);
            iterations -= m_Bloom.skipIterations.value;
            int mipCount = Mathf.Clamp(iterations, 1, k_MaxPyramidSize);

            // Pre-filtering parameters
            float clamp = m_Bloom.clamp.value;
            float threshold = Mathf.GammaToLinearSpace(m_Bloom.threshold.value);
            float thresholdKnee = threshold * 0.5f; // Hardcoded soft knee

            // Material setup
            float scatter = Mathf.Lerp(0.05f, 0.95f, m_Bloom.scatter.value);
            var bloomMaterial = m_Materials.bloom;
            bloomMaterial.SetVector(ShaderConstants._Params, new Vector4(scatter, clamp, threshold, thresholdKnee));
            CoreUtils.SetKeyword(bloomMaterial, ShaderKeywordStrings.BloomHQ, m_Bloom.highQualityFiltering.value);
            CoreUtils.SetKeyword(bloomMaterial, ShaderKeywordStrings.UseRGBM, m_UseRGBM);

            // [NiloToon added]
            //====================================================================================
            // if not overridden, use generic threshold.
            // if overridden, use characterAreaOverridedThreshold.
            float finalThreshold = Mathf.GammaToLinearSpace(m_Bloom.characterAreaOverridedThreshold.overrideState ? m_Bloom.characterAreaOverridedThreshold.value : m_Bloom.threshold.value);
            float finalThresholdKnee = finalThreshold * 0.5f; // Hardcoded soft knee
            bloomMaterial.SetFloat("_NiloToonBloomCharacterAreaThreshold", finalThreshold);
            bloomMaterial.SetFloat("_NiloToonBloomCharacterAreaThresholdKnee", finalThresholdKnee);

            bloomMaterial.SetFloat("_NiloToonBloomCharacterAreaBloomEmitMultiplier", m_Bloom.characterAreaBloomEmitMultiplier.value);

            Vector4 HSV_applyToCharAreaOnly;
            HSV_applyToCharAreaOnly.x = m_Bloom.HueOffset.value;
            HSV_applyToCharAreaOnly.y = m_Bloom.SaturationBoost.value;
            HSV_applyToCharAreaOnly.z = m_Bloom.ValueMultiply.value;
            HSV_applyToCharAreaOnly.w = m_Bloom.ApplyHSVToCharAreaOnly.value;
            bloomMaterial.SetVector("_NiloToonBloomHSVModifier", HSV_applyToCharAreaOnly);
            //====================================================================================

            // Prefilter
            var desc = GetCompatibleDescriptor(tw, th, m_DefaultHDRFormat);
            cmd.GetTemporaryRT(ShaderConstants._BloomMipDown[0], desc, FilterMode.Bilinear);
            cmd.GetTemporaryRT(ShaderConstants._BloomMipUp[0], desc, FilterMode.Bilinear);
            Blit(cmd, source, ShaderConstants._BloomMipDown[0], bloomMaterial, 0);

            // Downsample - gaussian pyramid
            int lastDown = ShaderConstants._BloomMipDown[0];
            for (int i = 1; i < mipCount; i++)
            {
                tw = Mathf.Max(1, tw >> 1);
                th = Mathf.Max(1, th >> 1);
                int mipDown = ShaderConstants._BloomMipDown[i];
                int mipUp = ShaderConstants._BloomMipUp[i];

                desc.width = tw;
                desc.height = th;

                cmd.GetTemporaryRT(mipDown, desc, FilterMode.Bilinear);
                cmd.GetTemporaryRT(mipUp, desc, FilterMode.Bilinear);

                // Classic two pass gaussian blur - use mipUp as a temporary target
                //   First pass does 2x downsampling + 9-tap gaussian
                //   Second pass does 9-tap gaussian using a 5-tap filter + bilinear filtering
                Blit(cmd, lastDown, mipUp, bloomMaterial, 1);
                Blit(cmd, mipUp, mipDown, bloomMaterial, 2);

                lastDown = mipDown;
            }

            // Upsample (bilinear by default, HQ filtering does bicubic instead
            for (int i = mipCount - 2; i >= 0; i--)
            {
                int lowMip = (i == mipCount - 2) ? ShaderConstants._BloomMipDown[i + 1] : ShaderConstants._BloomMipUp[i + 1];
                int highMip = ShaderConstants._BloomMipDown[i];
                int dst = ShaderConstants._BloomMipUp[i];

                cmd.SetGlobalTexture(ShaderConstants._SourceTexLowMip, lowMip);
                Blit(cmd, highMip, BlitDstDiscardContent(cmd, dst), bloomMaterial, 3);
            }

            // Cleanup
            for (int i = 0; i < mipCount; i++)
            {
                cmd.ReleaseTemporaryRT(ShaderConstants._BloomMipDown[i]);
                if (i > 0) cmd.ReleaseTemporaryRT(ShaderConstants._BloomMipUp[i]);
            }

            // Setup bloom on uber
            var tint = m_Bloom.tint.value.linear;
            var luma = ColorUtils.Luminance(tint);
            tint = luma > 0f ? tint * (1f / luma) : Color.white;

            var bloomParams = new Vector4(m_Bloom.intensity.value, tint.r, tint.g, tint.b);
            uberMaterial.SetVector(ShaderConstants._Bloom_Params, bloomParams);
            uberMaterial.SetFloat(ShaderConstants._Bloom_RGBM, m_UseRGBM ? 1f : 0f);

            // [NiloToon added]
            //====================================================================================
            // if not overridden, use generic intensity.
            // if overridden, use characterAreaOverridedIntensity.
            uberMaterial.SetFloat("_NiloToonBloomCharacterAreaIntensity", m_Bloom.characterAreaOverridedIntensity.overrideState ? m_Bloom.characterAreaOverridedIntensity.value : m_Bloom.intensity.value);
            //====================================================================================

            cmd.SetGlobalTexture(ShaderConstants._Bloom_Texture, ShaderConstants._BloomMipUp[0]);

            // Setup lens dirtiness on uber
            // Keep the aspect ratio correct & center the dirt texture, we don't want it to be
            // stretched or squashed
            var dirtTexture = m_Bloom.dirtTexture.value == null ? Texture2D.blackTexture : m_Bloom.dirtTexture.value;
            float dirtRatio = dirtTexture.width / (float)dirtTexture.height;
            float screenRatio = m_Descriptor.width / (float)m_Descriptor.height;
            var dirtScaleOffset = new Vector4(1f, 1f, 0f, 0f);
            float dirtIntensity = m_Bloom.dirtIntensity.value;

            if (dirtRatio > screenRatio)
            {
                dirtScaleOffset.x = screenRatio / dirtRatio;
                dirtScaleOffset.z = (1f - dirtScaleOffset.x) * 0.5f;
            }
            else if (screenRatio > dirtRatio)
            {
                dirtScaleOffset.y = dirtRatio / screenRatio;
                dirtScaleOffset.w = (1f - dirtScaleOffset.y) * 0.5f;
            }

            uberMaterial.SetVector(ShaderConstants._LensDirt_Params, dirtScaleOffset);
            uberMaterial.SetFloat(ShaderConstants._LensDirt_Intensity, dirtIntensity);
            uberMaterial.SetTexture(ShaderConstants._LensDirt_Texture, dirtTexture);

            // Keyword setup - a bit convoluted as we're trying to save some variants in Uber...
            if (m_Bloom.highQualityFiltering.value)
                uberMaterial.EnableKeyword(dirtIntensity > 0f ? ShaderKeywordStrings.BloomHQDirt : ShaderKeywordStrings.BloomHQ);
            else
                uberMaterial.EnableKeyword(dirtIntensity > 0f ? ShaderKeywordStrings.BloomLQDirt : ShaderKeywordStrings.BloomLQ);
        }
        #endregion

        #region [Copy and edited, from URP12.1.12's PostProcessPass.cs's SetupColorGrading()]
        void SetupColorGrading(CommandBuffer cmd, ref RenderingData renderingData, Material material)
        {
            ref var postProcessingData = ref renderingData.postProcessingData;
            bool hdr = postProcessingData.gradingMode == ColorGradingMode.HighDynamicRange;
            int lutHeight = postProcessingData.lutSize;
            int lutWidth = lutHeight * lutHeight;

            // Source material setup
            float postExposureLinear = Mathf.Pow(2f, m_ColorAdjustments.postExposure.value);
            //cmd.SetGlobalTexture(ShaderConstants._InternalLut, m_InternalLut.Identifier()); // [NiloToon edit] temp disabled since we don't have m_InternalLut here
            material.SetVector(ShaderConstants._Lut_Params, new Vector4(1f / lutWidth, 1f / lutHeight, lutHeight - 1f, postExposureLinear));
            material.SetTexture(ShaderConstants._UserLut, m_ColorLookup.texture.value);
            material.SetVector(ShaderConstants._UserLut_Params, !m_ColorLookup.IsActive()
                ? Vector4.zero
                : new Vector4(1f / m_ColorLookup.texture.value.width,
                    1f / m_ColorLookup.texture.value.height,
                    m_ColorLookup.texture.value.height - 1f,
                    m_ColorLookup.contribution.value)
            );

            // [NiloToon added]
            //====================================================================================
            // NiloToon can't support HDR Grading Mode due to URP's design,
            // so we just enable the same NiloToon tonemapping code for both ldr and hdr Color Grading Mode 
            hdr = false;
            //====================================================================================

            if (hdr)
            {
                material.EnableKeyword(ShaderKeywordStrings.HDRGrading);
            }
            else
            {
                switch (m_Tonemapping.mode.value)
                {
                    case TonemappingMode.Neutral: material.EnableKeyword(ShaderKeywordStrings.TonemapNeutral); break;
                    case TonemappingMode.ACES: material.EnableKeyword(ShaderKeywordStrings.TonemapACES); break;
                    
                    // [NiloToon added]
                    //====================================================================================
                    case TonemappingMode.GT: 
                        material.EnableKeyword("_TONEMAP_GRANTURISMO"); 
                        break;
                    case TonemappingMode.ACES_CustomSR:
                        // custom ACES function with ABCDE param from this repo, it has better saturation than other tonemap method
                        // https://github.com/stalomeow/StarRailNPRShader/blob/main/Shaders/Postprocessing/UberPost.shader
                        material.EnableKeyword("_TONEMAP_ACES_CUSTOM"); 
                        material.SetFloat("_NiloToon_CustomACESParamA", 2.8f);
                        material.SetFloat("_NiloToon_CustomACESParamB", 0.4f);
                        material.SetFloat("_NiloToon_CustomACESParamC", 2.1f);
                        material.SetFloat("_NiloToon_CustomACESParamD", 0.5f);
                        material.SetFloat("_NiloToon_CustomACESParamE", 1.5f);
                        break;
                    case TonemappingMode.KhronosPBRNeutral:
                        material.EnableKeyword("_TONEMAP_KHRONOS_PBR_NEUTRAL");
                        break;
                    case TonemappingMode.NiloNPRNeutralV1:
                        material.EnableKeyword("_TONEMAP_NILO_NPR_NEUTRAL_V1");
                        break;
                    case TonemappingMode.NiloHybirdACES:
                        material.EnableKeyword("_TONEMAP_NILO_HYBIRD_ACES");
                        break;
                    //====================================================================================
                    
                    default: break; // None
                }
            }
            
            // [NiloToon added]
            //====================================================================================
            material.SetFloat("_NiloToonTonemappingCharacterAreaRemove", 1 - m_Tonemapping.ApplyToCharacter.value * m_Tonemapping.intensity.value);
            material.SetFloat("_NiloToonTonemappingCharacterAreaPreTonemapBrightnessMul", m_Tonemapping.BrightnessMulForCharacter.value * m_Bloom.characterPreTonemapBrightness.value);
            
            material.SetFloat("_NiloToonTonemappingNonCharacterAreaRemove", 1 - m_Tonemapping.ApplyToNonCharacter.value * m_Tonemapping.intensity.value);
            material.SetFloat("_NiloToonTonemappingNonCharacterAreaPreTonemapBrightnessMul", m_Tonemapping.BrightnessMulForNonCharacter.value);
            //====================================================================================
        }
        #endregion
        
        #region [Copy and remove unneeded and edited, keep bloom and uber only, add blit, delay material create, from URP10.5.0's PostProcessPass.cs's MaterialLibrary class]
        class MaterialLibrary
        {
            public readonly Material bloom;
            public readonly Material uber;
            public readonly Material blit;

            // NiloToon edited, instead of receiving a PostProcessData class, we pass 2 Shaders directly
            public MaterialLibrary(Shader bloomShader, Shader uberShader, Shader blitShader)
            {
                bloom = Load(bloomShader);
                uber = Load(uberShader);
                blit = Load(blitShader);
            }

            Material Load(Shader shader)
            {
                if (shader == null)
                {
                    //Debug.LogErrorFormat($"Missing shader. {GetType().DeclaringType.Name} render pass will not execute. Check for missing reference in the renderer resources."); // NiloToonURP: removed
                    return null;
                }
                else if (!shader.isSupported)
                {
                    return null;
                }

                return CoreUtils.CreateEngineMaterial(shader);
            }

            internal void Cleanup()
            {
                CoreUtils.Destroy(bloom);
                CoreUtils.Destroy(uber);
                CoreUtils.Destroy(blit);
            }
        }
        #endregion

        #region [Direct copy and no edit, from URP10.5.0's PostProcessPass.cs]
        RenderTextureDescriptor GetCompatibleDescriptor()
            => GetCompatibleDescriptor(m_Descriptor.width, m_Descriptor.height, m_Descriptor.graphicsFormat, m_Descriptor.depthBufferBits);

        RenderTextureDescriptor GetCompatibleDescriptor(int width, int height, GraphicsFormat format, int depthBufferBits = 0)
        {
            var desc = m_Descriptor;
            desc.depthBufferBits = depthBufferBits;
            desc.msaaSamples = 1;
            desc.width = width;
            desc.height = height;
            desc.graphicsFormat = format;
            return desc;
        }

        private BuiltinRenderTextureType BlitDstDiscardContent(CommandBuffer cmd, RenderTargetIdentifier rt)
        {
            // We set depth to DontCare because rt might be the source of PostProcessing used as a temporary target
            // Source typically comes with a depth buffer and right now we don't have a way to only bind the color attachment of a RenderTargetIdentifier
            cmd.SetRenderTarget(new RenderTargetIdentifier(rt, 0, CubemapFace.Unknown, -1),
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store,
                RenderBufferLoadAction.DontCare, RenderBufferStoreAction.DontCare);
            return BuiltinRenderTextureType.CurrentActive;
        }

        private new void Blit(CommandBuffer cmd, RenderTargetIdentifier source, RenderTargetIdentifier destination, Material material, int passIndex = 0)
        {
            cmd.SetGlobalTexture(ShaderPropertyId.sourceTex, source);
            if (m_UseDrawProcedural)
            {
                Vector4 scaleBias = new Vector4(1, 1, 0, 0);
                cmd.SetGlobalVector(ShaderPropertyId.scaleBias, scaleBias);

                cmd.SetRenderTarget(new RenderTargetIdentifier(destination, 0, CubemapFace.Unknown, -1),
                    RenderBufferLoadAction.Load, RenderBufferStoreAction.Store, RenderBufferLoadAction.Load, RenderBufferStoreAction.Store);
                cmd.DrawProcedural(Matrix4x4.identity, material, passIndex, MeshTopology.Quads, 4, 1, null);
            }
            else
            {
                cmd.Blit(source, destination, material, passIndex);
            }
        }


        // Precomputed shader ids to same some CPU cycles (mostly affects mobile)
        static class ShaderConstants
        {
            public static readonly int _TempTarget = Shader.PropertyToID("_TempTarget");
            public static readonly int _TempTarget2 = Shader.PropertyToID("_TempTarget2");

            public static readonly int _StencilRef = Shader.PropertyToID("_StencilRef");
            public static readonly int _StencilMask = Shader.PropertyToID("_StencilMask");

            public static readonly int _FullCoCTexture = Shader.PropertyToID("_FullCoCTexture");
            public static readonly int _HalfCoCTexture = Shader.PropertyToID("_HalfCoCTexture");
            public static readonly int _DofTexture = Shader.PropertyToID("_DofTexture");
            public static readonly int _CoCParams = Shader.PropertyToID("_CoCParams");
            public static readonly int _BokehKernel = Shader.PropertyToID("_BokehKernel");
            public static readonly int _PongTexture = Shader.PropertyToID("_PongTexture");
            public static readonly int _PingTexture = Shader.PropertyToID("_PingTexture");

            public static readonly int _Metrics = Shader.PropertyToID("_Metrics");
            public static readonly int _AreaTexture = Shader.PropertyToID("_AreaTexture");
            public static readonly int _SearchTexture = Shader.PropertyToID("_SearchTexture");
            public static readonly int _EdgeTexture = Shader.PropertyToID("_EdgeTexture");
            public static readonly int _BlendTexture = Shader.PropertyToID("_BlendTexture");

            public static readonly int _ColorTexture = Shader.PropertyToID("_ColorTexture");
            public static readonly int _Params = Shader.PropertyToID("_Params");
            public static readonly int _SourceTexLowMip = Shader.PropertyToID("_SourceTexLowMip");
            public static readonly int _Bloom_Params = Shader.PropertyToID("_Bloom_Params");
            public static readonly int _Bloom_RGBM = Shader.PropertyToID("_Bloom_RGBM");
            public static readonly int _Bloom_Texture = Shader.PropertyToID("_Bloom_Texture");
            public static readonly int _LensDirt_Texture = Shader.PropertyToID("_LensDirt_Texture");
            public static readonly int _LensDirt_Params = Shader.PropertyToID("_LensDirt_Params");
            public static readonly int _LensDirt_Intensity = Shader.PropertyToID("_LensDirt_Intensity");
            public static readonly int _Distortion_Params1 = Shader.PropertyToID("_Distortion_Params1");
            public static readonly int _Distortion_Params2 = Shader.PropertyToID("_Distortion_Params2");
            public static readonly int _Chroma_Params = Shader.PropertyToID("_Chroma_Params");
            public static readonly int _Vignette_Params1 = Shader.PropertyToID("_Vignette_Params1");
            public static readonly int _Vignette_Params2 = Shader.PropertyToID("_Vignette_Params2");
            public static readonly int _Lut_Params = Shader.PropertyToID("_Lut_Params");
            public static readonly int _UserLut_Params = Shader.PropertyToID("_UserLut_Params");
            public static readonly int _InternalLut = Shader.PropertyToID("_InternalLut");
            public static readonly int _UserLut = Shader.PropertyToID("_UserLut");
            public static readonly int _DownSampleScaleFactor = Shader.PropertyToID("_DownSampleScaleFactor");

            public static readonly int _FullscreenProjMat = Shader.PropertyToID("_FullscreenProjMat");

            public static int[] _BloomMipUp;
            public static int[] _BloomMipDown;
        }
        #endregion

        #region [Direct copy and no edit, from URP10.5.0's UniversalRenderPipelineCore.cs]
        internal static class ShaderPropertyId
        {
            public static readonly int glossyEnvironmentColor = Shader.PropertyToID("_GlossyEnvironmentColor");
            public static readonly int subtractiveShadowColor = Shader.PropertyToID("_SubtractiveShadowColor");

            public static readonly int ambientSkyColor = Shader.PropertyToID("unity_AmbientSky");
            public static readonly int ambientEquatorColor = Shader.PropertyToID("unity_AmbientEquator");
            public static readonly int ambientGroundColor = Shader.PropertyToID("unity_AmbientGround");

            public static readonly int time = Shader.PropertyToID("_Time");
            public static readonly int sinTime = Shader.PropertyToID("_SinTime");
            public static readonly int cosTime = Shader.PropertyToID("_CosTime");
            public static readonly int deltaTime = Shader.PropertyToID("unity_DeltaTime");
            public static readonly int timeParameters = Shader.PropertyToID("_TimeParameters");

            public static readonly int scaledScreenParams = Shader.PropertyToID("_ScaledScreenParams");
            public static readonly int worldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");
            public static readonly int screenParams = Shader.PropertyToID("_ScreenParams");
            public static readonly int projectionParams = Shader.PropertyToID("_ProjectionParams");
            public static readonly int zBufferParams = Shader.PropertyToID("_ZBufferParams");
            public static readonly int orthoParams = Shader.PropertyToID("unity_OrthoParams");

            public static readonly int viewMatrix = Shader.PropertyToID("unity_MatrixV");
            public static readonly int projectionMatrix = Shader.PropertyToID("glstate_matrix_projection");
            public static readonly int viewAndProjectionMatrix = Shader.PropertyToID("unity_MatrixVP");

            public static readonly int inverseViewMatrix = Shader.PropertyToID("unity_MatrixInvV");
            public static readonly int inverseProjectionMatrix = Shader.PropertyToID("unity_MatrixInvP");
            public static readonly int inverseViewAndProjectionMatrix = Shader.PropertyToID("unity_MatrixInvVP");

            public static readonly int cameraProjectionMatrix = Shader.PropertyToID("unity_CameraProjection");
            public static readonly int inverseCameraProjectionMatrix = Shader.PropertyToID("unity_CameraInvProjection");
            public static readonly int worldToCameraMatrix = Shader.PropertyToID("unity_WorldToCamera");
            public static readonly int cameraToWorldMatrix = Shader.PropertyToID("unity_CameraToWorld");

            public static readonly int sourceTex = Shader.PropertyToID("_SourceTex");
            public static readonly int scaleBias = Shader.PropertyToID("_ScaleBias");
            public static readonly int scaleBiasRt = Shader.PropertyToID("_ScaleBiasRt");

            // Required for 2D Unlit Shadergraph master node as it doesn't currently support hidden properties.
            public static readonly int rendererColor = Shader.PropertyToID("_RendererColor");
        }
        #endregion
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // do nothing
        }
    }
#endif
}