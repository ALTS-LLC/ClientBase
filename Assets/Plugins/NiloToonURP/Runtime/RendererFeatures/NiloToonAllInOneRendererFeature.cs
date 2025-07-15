// An all in one high-level RendererFeature that contains all NiloToon related passes, user only need to add this RendererFeature in their renderer
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NiloToon.NiloToonURP
{
    [Serializable]
    public class NiloToonRendererFeatureSettings
    {
        [Header("Outline settings")]
        public NiloToonToonOutlinePass.Settings outlineSettings = new NiloToonToonOutlinePass.Settings();
        [Header("Misc settings")]
        public NiloToonSetToonParamPass.Settings MiscSettings = new NiloToonSetToonParamPass.Settings();
        [Header("Average URP shadow map")]
        public NiloToonAverageShadowTestRTPass.Settings sphereShadowTestSettings = new NiloToonAverageShadowTestRTPass.Settings();
        [Header("Anime PostProcess")]
        public NiloToonAnimePostProcessPass.Settings animePostProcessSettings = new NiloToonAnimePostProcessPass.Settings();
        [Header("Bloom & Tonemap")]
        public NiloToonUberPostProcessPass.Settings uberPostProcessSettings = new NiloToonUberPostProcessPass.Settings();

        [Header("Char SelfShadow")]
        public NiloToonCharSelfShadowMapRTPass.Settings charSelfShadowSettings = new NiloToonCharSelfShadowMapRTPass.Settings();

        [Header("Motion Blur")]
        public NiloToonMotionBlurPass.Settings motionBlurSettings = new NiloToonMotionBlurPass.Settings();

        [Header("Override Shader stripping")]
        [OverrideDisplayName("Shader Stripping Settings")]
        [Tooltip("This slot is useful when you are in the following situation:\n" +
                 "- Want to reduce build time/build size/runtime shader memory usage\n" +
                 "- Found that a feature is missing in build, and want to include that feature in build\n\n" +
                 "If you haven't configured this slot, NiloToon will default to the platform-specific stripping setting. This may not provide the optimal configuration for your project." +
                 "\n\n" +
                 "To create a custom stripping setting for enhanced shader stripping control, follow these steps:\n" +
                 "1.Right click in the project window\n" +
                 "2.Navigate to 'Create > NiloToon > NiloToonShaderStrippingSettingSO', click it to create a custom stripping setting\n" +
                 "3.Assign that setting to this slot.\n\n" +
                 "Please note:\n" +
                 "* You only need one instance of NiloToonShaderStrippingSettingSO in your project.\n" +
                 "* Ensure that all NiloToon renderer features share the same NiloToonShaderStrippingSettingSO for consistency.")]
        public NiloToonShaderStrippingSettingSO shaderStrippingSettingSO;
    }

    [DisallowMultipleRendererFeature]
    public class NiloToonAllInOneRendererFeature : ScriptableRendererFeature
    {
        public NiloToonRendererFeatureSettings settings = new NiloToonRendererFeatureSettings();

        NiloToonSetToonParamPass SetToonParamPass;
        NiloToonAverageShadowTestRTPass SphereShadowTestRTPass;
        NiloToonCharSelfShadowMapRTPass CharSelfShadowMapRTRenderPass;
        DrawSkyboxPass SkyboxRedrawBeforeOpaquePass;
        NiloToonToonOutlinePass ToonOutlinePass;
        NiloToonToonOutlinePass ToonOutlinePass_RightAfterTransparent;
        NiloToonScreenSpaceOutlinePass ScreenSpaceOutlinePass;
        NiloToonExtraThickOutlinePass ExtraThickOutlinePass;
        NiloToonAnimePostProcessPass AnimePostProcessPass;
        NiloToonPrepassBufferRTPass PrepassBufferRTPass;
        NiloToonUberPostProcessPass UberPostProcessPass;
#if UNITY_2022_3_OR_NEWER
        NiloToonMotionBlurPass MotionBlurPass;
#endif

        // This method is used to initialize the ScriptableRenderPass and any required resources.
        // Unity calls this method in OnEnable/OnValidate - 
        // so every time the project loads, enter/exit play mode, scripts recompile, or serialisation changes.
        // You should create all passes and set their renderPassEvent inside Create()
        public override void Create()
        {
            // this can only ensure the last used(Created) NiloToonAllInOneRendererFeature can be accessed by NiloToonAllInOneRendererFeature.Instance
            // (safe to use when    :1 Universal Renderer asset per 1 quality setting)
            // (unsafe to use when  :N Universal Renderer asset per 1 quality setting)
            // TODO: we should remove Singleton of ScriptableRendererFeature/ScriptableRenderPass completely, since it is not a safe design for ScriptableRendererFeature/ScriptableRenderPass due to URP's multi-renderer per URP asset design
            // TODO: how about we turn Instance(Singleton) to Instances?  where "Instances" is a list containing all Created and not yet deleted instances?
            _instance = this;

            ReInitPassesIfNeeded();
        }

        private void ReInitPassesIfNeeded()
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Create all passes
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            if (SetToonParamPass == null)
                SetToonParamPass = new NiloToonSetToonParamPass(settings);
            if (SphereShadowTestRTPass == null)
                SphereShadowTestRTPass = new NiloToonAverageShadowTestRTPass(settings);
            if (CharSelfShadowMapRTRenderPass == null)
                CharSelfShadowMapRTRenderPass = new NiloToonCharSelfShadowMapRTPass(settings);
            if (SkyboxRedrawBeforeOpaquePass == null)
                SkyboxRedrawBeforeOpaquePass = new DrawSkyboxPass(RenderPassEvent.BeforeRenderingOpaques);

            // RenderQueueRange.opaque = render queue(0-2500) materials
            if (ToonOutlinePass == null)
                ToonOutlinePass = new NiloToonToonOutlinePass(settings, RenderQueueRange.opaque, "NiloToonToonOutlinePass(Classic outline - OpaqueQueue)");
            // RenderQueueRange.transparent = render queue(2501-5000) materials
            if (ToonOutlinePass_RightAfterTransparent == null)
                ToonOutlinePass_RightAfterTransparent = new NiloToonToonOutlinePass(settings, RenderQueueRange.transparent, "NiloToonToonOutlinePass(Classic outline - TransparentQueue)");

            if (ScreenSpaceOutlinePass == null)
                ScreenSpaceOutlinePass = new NiloToonScreenSpaceOutlinePass(settings);
            if (ExtraThickOutlinePass == null)
                ExtraThickOutlinePass = new NiloToonExtraThickOutlinePass(settings);
            if (AnimePostProcessPass == null)
                AnimePostProcessPass = new NiloToonAnimePostProcessPass(settings);
            if (PrepassBufferRTPass == null)
                PrepassBufferRTPass = new NiloToonPrepassBufferRTPass(settings);
            if (UberPostProcessPass == null)
                UberPostProcessPass = new NiloToonUberPostProcessPass(settings);
#if UNITY_2022_3_OR_NEWER
            if (MotionBlurPass == null)
                MotionBlurPass = new NiloToonMotionBlurPass(settings);
#endif

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Configures where the render pass should be injected.
            // sorted by RenderPassEvent order here
            // (*Be aware that camera matrices and stereo rendering is not set up until the BeforeRenderingPrePasses event (value of 150))
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // this pass must be in front of all other NiloToon passes
            // DO NOT set this at AfterRenderingPrePasses or later!
            SetToonParamPass.renderPassEvent = RenderPassEvent.BeforeRenderingPrePasses;

            // require after _CameraDepthTexture is ready, since character shader's NiloToonPrepassBuffer pass needs _CameraDepthTexture. (AfterRenderingPrePasses is still too early because _CameraDepthTexture is not ready at this timing)
            PrepassBufferRTPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques - 2;

            SphereShadowTestRTPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques - 1;
            CharSelfShadowMapRTRenderPass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques - 1;
            ScreenSpaceOutlinePass.renderPassEvent = RenderPassEvent.BeforeRenderingOpaques - 1; // use BeforeRenderingOpaques because we need depth and normal texture, URP's SSAO is BeforeRenderingOpaques also

            // SkyboxRedrawBeforeOpaquePass's renderPassEvent is BeforeRenderingOpaques - 0 (defined when calling the pass's constructor, so we didn't write it here)
            // ...(X)

            // [The chart below shows all NiloToon's skybox and outline draw timing & order]
            //---------------------------------------
            // BeforeRenderingOpaques (= Before RenderQueue 0) -> NiloToon's SkyboxRedrawBeforeOpaquePass
            // URP RenderingOpaques, draw RenderQueue 0~2500's color pass "UniversalForwardOnly"
            // AfterRenderingOpaque (= After RenderQueue 2500) -> (X)
            //---------------------------------------
            // BeforeRenderingSkybox (= Before Skybox) -> (X)
            // URP draw skybox
            // (ToonOutlinePass) AfterRenderingSkybox (= After Skybox)
            //---------------------------------------
            // (X) Before Transparent (= before RenderQueue 2501)
            // URP draw RenderQueue 2501~5000's color pass "UniversalForwardOnly"
            // (ToonOutlinePass_RightAfterTransparent) After Transparent (= After RenderQueue 5000)
            //---------------------------------------
            ToonOutlinePass.renderPassEvent = RenderPassEvent.AfterRenderingSkybox; // use AfterRenderingSkybox instead of BeforeRenderingSkybox, to make "semi-transparent(ZWrite) + outline" blend with skybox correctly
            ToonOutlinePass_RightAfterTransparent.renderPassEvent = RenderPassEvent.BeforeRenderingTransparents + 1; // right after transparent materials finish drawing, draw this outline pass

            ExtraThickOutlinePass.renderPassEvent = settings.outlineSettings.extraThickOutlineRenderTiming; // default use AfterRenderingTransparents, because we want this outline not being blocked by transparent effects

            // AnimePostProcessPass's renderPassEvent will be decided by the pass itself

            UberPostProcessPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing + UberPostProcessPass.settings.renderPassEventTimingOffset;
        }

        // Here you can inject one or multiple render passes in the renderer.
        // This method is called when setting up the renderer once per-camera.
        // *be aware this is called every frame/update, so avoid creating/instantiating anything in here (can use the Create method for that).
        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            // a fix to any possible missing/null pass
            ReInitPassesIfNeeded();
            
            //----------------------------------------------------------------
            // temp fix for supporting SetToonParamPass+RenderGraph
            // since we can't call ConfigureInput() inside RecordRenderGraph()
            //----------------------------------------------------------------
            SetToonParamPass.ConfigureInputs(renderer);
                
            // Also note that by default it would enqueue the pass for all cameras - 
            // including ones used by the Unity Editor. 
            // In order to avoid this, we can test the camera type and return before enqueueing 
            // (or can check later during Execute to prevent that function running, if you prefer).
            // Example code:
            //if (renderingData.cameraData.isPreviewCamera) return;
            //if (renderingData.cameraData.isSceneViewCamera) return;

#if !UNITY_2022_2_OR_NEWER
            RenderTextureDescriptor cameraTargetDescriptor = renderingData.cameraData.cameraTargetDescriptor;
            RenderTargetHandle cameraTarget = new RenderTargetHandle();
            cameraTarget.Init("_CameraColorAttachmentA");
            UberPostProcessPass.Setup(cameraTargetDescriptor, cameraTarget);
#endif

            // the order of Enqueue matters if they have the same renderPassEvent
            renderer.EnqueuePass(SetToonParamPass);
            renderer.EnqueuePass(SphereShadowTestRTPass);
            renderer.EnqueuePass(CharSelfShadowMapRTRenderPass);
            renderer.EnqueuePass(ToonOutlinePass);
            renderer.EnqueuePass(ToonOutlinePass_RightAfterTransparent);
            renderer.EnqueuePass(ScreenSpaceOutlinePass);
            renderer.EnqueuePass(ExtraThickOutlinePass);
            renderer.EnqueuePass(AnimePostProcessPass);

            if (settings.MiscSettings.EnableSkyboxDrawBeforeOpaque && renderingData.cameraData.camera.clearFlags == CameraClearFlags.Skybox)
                renderer.EnqueuePass(SkyboxRedrawBeforeOpaquePass);

            var tonemappingEffect = VolumeManager.instance.stack.GetComponent<NiloToonTonemappingVolume>();
            var bloomEffect = VolumeManager.instance.stack.GetComponent<NiloToonBloomVolume>();
            
            bool isAnyNiloPostEnabled = false;
            isAnyNiloPostEnabled |= tonemappingEffect.IsActive();
            isAnyNiloPostEnabled |= bloomEffect.IsActive() && settings.uberPostProcessSettings.allowRenderNiloToonBloom;

            // optimization when no character is using Color Fill feature
            bool isAnyNiloToonPerCharacterScriptRequiresPrepass = false;
            foreach (var characterRenderController in characterList)
            {
                if (!characterRenderController) continue;
                
                if (characterRenderController.isActiveAndEnabled && 
                    characterRenderController.gameObject.activeInHierarchy && 
                    characterRenderController.shouldRenderCharacterAreaColorFill)
                {
                    isAnyNiloToonPerCharacterScriptRequiresPrepass = true;
                    break;
                }
            }

            bool shouldDrawPrepass = renderingData.cameraData.postProcessEnabled && isAnyNiloPostEnabled;
            shouldDrawPrepass |= isAnyNiloToonPerCharacterScriptRequiresPrepass;
            if (shouldDrawPrepass)
            {
                renderer.EnqueuePass(PrepassBufferRTPass);
            }

            renderer.EnqueuePass(UberPostProcessPass);
#if UNITY_2022_3_OR_NEWER
            if (renderingData.cameraData.postProcessEnabled &&
                renderingData.cameraData.cameraType == CameraType.Game) // only apply to game window
            {
                renderer.EnqueuePass(MotionBlurPass);
            }
#endif
        }

#if UNITY_2022_2_OR_NEWER
        // If you want to pass cameraColorTargetHandle or cameraDepthTargetHandle RTHandle into a pass
        // You must do it here instead of AddRenderPasses(...), since these RTHandle are not yet init in AddRenderPasses(...)!
        // see https://docs.unity3d.com/Packages/com.unity.render-pipelines.universal@14.0/manual/upgrade-guide-2022-2.html
        public override void SetupRenderPasses(ScriptableRenderer renderer, in RenderingData renderingData)
        {
            UberPostProcessPass.Setup(renderingData.cameraData.cameraTargetDescriptor);
        }
#endif

#if UNITY_2022_2_OR_NEWER
        // The method can be useful for releasing any resources that have been allocated.
        // In editor, the method is called when removing features, recompiling scripts, entering/exiting play mode. 
        // (Not too sure when it gets called in builds, probably when changing scenes?)
        // You should call Dispose() of every pass here to avoid memory leak
        // Usually each pass's Dispose() will call:
        // - RTHandle?.Release();
        // - Destroy materials/textures created 
        protected override void Dispose(bool disposing)
        {
            // call Dispose to passes that alloc RT
            PrepassBufferRTPass?.Dispose();
            CharSelfShadowMapRTRenderPass?.Dispose();
            UberPostProcessPass?.Dispose();
            SphereShadowTestRTPass?.Dispose();
            MotionBlurPass?.Dispose();
            
            // null all passes
            SetToonParamPass = null;
            SphereShadowTestRTPass = null;
            CharSelfShadowMapRTRenderPass = null;
            SkyboxRedrawBeforeOpaquePass = null;
            ToonOutlinePass = null;
            ToonOutlinePass_RightAfterTransparent = null;
            ScreenSpaceOutlinePass = null;
            ExtraThickOutlinePass = null;
            AnimePostProcessPass = null;
            PrepassBufferRTPass = null;
            UberPostProcessPass = null;
            MotionBlurPass = null;
        }
#endif

        #region [Singleton to store list of active char (no matter visible by camera or not)]
        public NiloToonAllInOneRendererFeature()
        {
            CheckInit();
        }
        // TODO: should we enable this?
        //[Obsolete("We may remove Singleton(.Instance) API from all NiloToon ScriptableRendererFeature/ScriptableRenderPass in the future, since multiple Universal Renderer(s) can be added to 1 URP asset, Singleton is not possible.", false)]
        public static NiloToonAllInOneRendererFeature Instance
        {
            get => _instance;
        }
        static NiloToonAllInOneRendererFeature _instance;

        public static List<NiloToonPerCharacterRenderController> characterList;
        public static HashSet<NiloToonPerCharacterRenderController> characterHashSet;

        public static void AddCharIfNotExist(NiloToonPerCharacterRenderController controller)
        {
            CheckInit();

            // optimize .Contains() call, now use HashSet instead of List: https://stackoverflow.com/questions/823860/listt-contains-is-very-slow
            if (!characterHashSet.Contains(controller))
            {
                characterHashSet.Add(controller);
                characterList.Add(controller);
                UpdateCharacterControllerIndex();
            }
        }
        public static void Remove(NiloToonPerCharacterRenderController controller)
        {
            CheckInit();

            // optimize .Contains() call, now use HashSet instead of List: https://stackoverflow.com/questions/823860/listt-contains-is-very-slow
            if (characterHashSet.Contains(controller))
            {
                characterHashSet.Remove(controller);
                characterList.Remove(controller);
                UpdateCharacterControllerIndex();
            }
        }
        static void UpdateCharacterControllerIndex()
        {
            for (int i = 0; i < characterList.Count; i++)
            {
                var c = characterList[i];
                if (!c)
                    continue;

                c.characterID = i;
            }
        }
        internal static void CheckInit()
        {
            if (characterList == null)
                characterList = new List<NiloToonPerCharacterRenderController>(); // for UpdateCharacterControllerIndex()
            if (characterHashSet == null)
                characterHashSet = new HashSet<NiloToonPerCharacterRenderController>(); // for .Contains() checks
        }
#endregion
    }
}

