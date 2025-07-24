// render a separated shadow map (Not related to URP's shadowmap), 
// only containing NiloToon characters, and shadow map orthographic box will fit/bound to NiloToon character only.
// this pass's shadow camera's orthographic view bound is very tight(because only NiloToon characters are included), 
// so a small size RT can still produce sharp shadow.

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
#endif

namespace NiloToon.NiloToonURP
{
    public class NiloToonCharSelfShadowMapRTPass : ScriptableRenderPass
    {
        // singleton
        public static NiloToonCharSelfShadowMapRTPass Instance => _instance;
        static NiloToonCharSelfShadowMapRTPass _instance;

        internal const float SHADOW_RANGE_MIN = 5f;
        internal const float SHADOW_RANGE_DEFAULT = 10f;
        internal const float SHADOW_RANGE_MAX = 100f;

        public enum SoftShadowQuality
        {
            Low = 1,
            Medium = 2,
            High = 3,
        }
        // settings
        [Serializable]
        public class Settings
        {
            [Header("Enable Nilo Char Shadow map")]
            [Tooltip(   "Enable to render NiloToon's close fit shadow map system for NiloToon characters only, this close fit shadow map system only consider NiloToon characters and ignore other objects, so shadow map utilization % will be higher(no shadow map space wasted on non-NiloToon characters).\n" +
                        "*this NiloToon shadow map system is not related to URP's shadow map system.\n\n" +
                        "- Best when only a single character is visible, it will render the best shadowmap possible for that visible character\n" +
                        "- Good for a group of visible characters that are very close to each other\n" +
                        "- Very bad for a group of visible characters that are far away from each other, shadow artifacts will appear easily\n\n" +
                        "Default: ON")]
            [OverrideDisplayName("Enable?")]
            public bool enableCharSelfShadow = true;

            [Header("> Style")]
            [Tooltip(   "- If OFF, will use the camera's forward direction(and apply with Vertical & Horizontal angle) as cast shadow direction.(imagine it is a shadow casting light attached on the camera)\n\n" +
                        "- If ON, will use scene main light's direction as cast shadow direction, just like the URP shadowmap's shadow casting direction.\n\n" +
                        "Turn it ON if you don't want this shadow affected by camera transform(rotation)\n\n" +
                        "*Default: ON, since many user expect it to act the same as URP's shadow casting direction by default")]
            [OverrideDisplayName("MainLight as Shadow Dir?")]
            public bool useMainLightAsCastShadowDirection = true;

            [Tooltip(   "Only useful if 'MainLight as Shadow Dir?' is OFF.\n\n" +
                        "Default: 30 (30 degrees pointing downward)")]
            [RangeOverrideDisplayName("     Vertical angle",-90, 90)]
            public float shadowAngle = 30f;

            [Tooltip(   "Only useful if 'MainLight as Shadow Dir?' is OFF.\n\n" +
                        "Default: 0 (0 degrees, no rotation to left or right by default")]
            [RangeOverrideDisplayName("     Horizontal angle", -90, 90)]
            public float shadowLRAngle = 0;

            [Header("> Quality")]
            [Tooltip(   "The higher the better(shadow quality), but larger shadow map size = GPU slower.\n" +
                        "*You will want to max it when making high quality editor recordings (e.g. using Recorder)\n\n" +
                        "Default: 4096")]
            [RangeOverrideDisplayName("Resolution",256, 16384)]
            public int shadowMapSize = 4096;

            [Tooltip(   "Enable to make shadow blurrier with nice AA, but adding more GPU cost\n\n" +
                        "Default: true")]
            [OverrideDisplayName("Soft Shadow?")]
            public bool useSoftShadow = true;
            [Tooltip(   "Blurriness of soft shadow, the higher the blurrier and slower in GPU\n\n" +
                        "Default: Low")]
            [OverrideDisplayName("    Quality")]
            public SoftShadowQuality softShadowQuality = SoftShadowQuality.Medium;
            [Tooltip(   "Enable to resharpen the result of soft shadow to produce a more cel-shade look\n\n" +
                        "Default: false")]
            [OverrideDisplayName("    Resharpen?")]
            public bool useSoftShadowResharpen = false;
            [Tooltip(   "Strength of the resharpen, the higher the sharper\n\n" +
                        "Default: 0.5")]
            [OverrideDisplayName("        Strength")]
            [Range(0, 1)]
            public float resharpenStregth = 0.5f;

            [Header("> Fix shadow artifacts options")]
            [Tooltip("The shorter the range, the higher the quality of shadow rendering, but characters outside the range will not render/receive shadows\n\n" +
                "Default: 5(meter), shadowRange starts from the first visible character, not from the camera.")]
            [Range(SHADOW_RANGE_MIN, SHADOW_RANGE_MAX)]
            public float shadowRange = SHADOW_RANGE_DEFAULT;

            [Tooltip(   "The higher the depthBias, the less artifact(shadow acne) will appear, but more Peter panning will appear\n\n" +
                        "Default: 1")]
            [Range(0, 10)]
            public float depthBias = 1f;

            [Tooltip(   "The inset amount of shadowcaster, you can try different value to see if it solved shadow acne artifact. Higher is not always the better.\n" +
                        "*but higher = more shadow caster model deform will appear (e.g. very thin finger in shadow map)\n" +
                        "*You can set it to 0 if this is producing more shadow acne artifact instead, usually it may happen on flat cloth double side surface.\n\n" +
                        "Default: 0.5")]
            [Range(0, 4)]
            public float normalBias = 0.5f;

            [Tooltip(   "The higher the receiverDepthBias, the less artifact(shadow acne) will appear, but more Peter panning will appear\n" +
                        "*This is the shadow receiver's shadow test position depth bias, it will not affect the shadow caster's bias.\n\n" +
                        "Default: 1")]
            [Range(0,10)]
            public float receiverDepthBias = 1f;
            [Tooltip(   "The inflate amount of shadow receiver's shadow test position. The higher the receiverNormalBias, the less artifact(shadow acne) will appear.\n" +
                        "Unlike the shadowcaster's normal bias, this will not change the shape of shadow caster, so you can use a much bigger value if shadow acne appears.\n\n" +
                        "Default: 1")]
            [Range(0,10)]
            public float receiverNormalBias = 1f;
            
            [Tooltip(   "Apply an additional local diffuse(dot(N,L)) cel shading to hide more shadowmap's artifact(shadow acne).\n\n" +
                        "Default: On")]
            public bool useNdotLFix = true;

            [Tooltip(   "Extra CPU culling to improve shadow correctness for making sure shadow caster that is not visible still render shadow map correctly, " +
                        "but Unity will crash if you use terrain and 'UnityCrash Safe Guard' is off.\n\n" +
                        "Disable this if you find that it affects other plugin's rendering.(e.g., Volumetric Light Beam's SRP Batcher Mode may not work if you enable this toggle.\n\n" +
                        "Default: On")]
            [OverrideDisplayName("High Quality Culling")]
            public bool perfectCullingForShadowCasters = true;

            [Header("> If Unity crash (terrain), enable it!")]
            [Tooltip(   "If having a terrain in your scene makes your unity crash, enable this toggle until URP/SRP fix it in the future.\n" +
                        "If you are sure that you don't use any terrain in this project, you can disable it.\n" +
                        "* enable this will alloc 32B GC per renderer per frame!\n\n" +
                        "If you use terrain, and don't want any GC alloc, turn off this and 'High Quality Culling' together\n\n" +
                        "Default: ON")]
            [OverrideDisplayName("UnityCrash Safe Guard")]
            public bool terrainCrashSafeGuard = true;
        }
        public Settings settings { get; }

        public static bool showShadowCameraDebugFrustum = false;

        static readonly string _NILOTOON_RECEIVE_SELF_SHADOW_Keyword = "_NILOTOON_RECEIVE_SELF_SHADOW";

        NiloToonRendererFeatureSettings allSettings;
        Plane[] cameraPlanes = new Plane[6];

#if UNITY_2022_2_OR_NEWER
        RTHandle shadowMapRTH;
#else
        RenderTargetHandle shadowMapRTH;
#endif

        List<NiloToonPerCharacterRenderController> validCharList = new List<NiloToonPerCharacterRenderController>();
        List<NiloToonPerCharacterRenderController> finalValidCharList = new List<NiloToonPerCharacterRenderController>();


        // Constructor(will not call on every frame)
        public NiloToonCharSelfShadowMapRTPass(NiloToonRendererFeatureSettings allSettings)
        {
            this.allSettings = allSettings;
            this.settings = allSettings.charSelfShadowSettings;
            _instance = this;

#if !UNITY_2022_2_OR_NEWER
            shadowMapRTH = new RenderTargetHandle();
            shadowMapRTH.Init("_NiloToonCharSelfShadowMapRT");
#endif

            base.profilingSampler = new ProfilingSampler(nameof(NiloToonCharSelfShadowMapRTPass));
        }

        bool shouldRender;

        // This method is called before executing the render pass.
        // It can be used to configure render targets and their clear state. Also to create temporary render target textures.
        // When empty this render pass will render to the active camera render target.
        // You should never call CommandBuffer.SetRenderTarget. Instead call <c>ConfigureTarget</c> and <c>ConfigureClear</c>.
        // The render pipeline will ensure target setup and clearing happens in a performant manner.
        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            var volumeEffect = VolumeManager.instance.stack.GetComponent<NiloToonShadowControlVolume>();

            int shadowMapSize = getShadowMapSize(volumeEffect);

            if (!getShouldRender(volumeEffect, renderingData.cameraData.camera))
            {
                shadowMapSize = 1;
                shouldRender = false;
            }
            else
            {
                shouldRender = true;
            }
            
            RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor(shadowMapSize, shadowMapSize, RenderTextureFormat.Shadowmap, 16);

#if UNITY_2022_2_OR_NEWER
            RenderingUtils.ReAllocateIfNeeded(ref shadowMapRTH, renderTextureDescriptor, FilterMode.Bilinear, wrapMode: TextureWrapMode.Clamp, isShadowMap: true, name: "_NiloToonCharSelfShadowMapRT");
            ConfigureTarget(shadowMapRTH);
#else
            cmd.GetTemporaryRT(shadowMapRTH.id, renderTextureDescriptor, FilterMode.Bilinear);
            ConfigureTarget(shadowMapRTH.Identifier());
#endif      
            ConfigureClear(ClearFlag.Depth, Color.black); // clearing color doesn't matter? since we will redraw character pixels with depth value (now default clear to far value [DX: near = 1, far = 0])
        }

        // Here you can implement the rendering logic.
        // Use <c>ScriptableRenderContext</c> to issue drawing commands or execute command buffers
        // https://docs.unity3d.com/ScriptReference/Rendering.ScriptableRenderContext.html
        // You don't have to call ScriptableRenderContext.submit, the render pipeline will call it at specific points in the pipeline.
        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            renderCharacterSelfShadowmapRT(context, renderingData);
        }

        // Cleanup any allocated resources that were created during the execution of this render pass.
        public override void OnCameraCleanup(CommandBuffer cmd)
        {
            // To Release a RTHandle, do it in ScriptableRendererFeature's Dispose(), don't do it in OnCameraCleanup(...)
            //https://www.cyanilux.com/tutorials/custom-renderer-features/#oncameracleanup

#if !UNITY_2022_2_OR_NEWER
            if (shouldRender)
            {
                cmd.ReleaseTemporaryRT(shadowMapRTH.id);
            }
#endif
        }

#if UNITY_2022_2_OR_NEWER
        public void Dispose()
        {
            shadowMapRTH?.Release();
        }
#endif

        bool getShouldRender(NiloToonShadowControlVolume volumeEffect, Camera camera)
        {
            // Let volume disable this pass if needed.
            // Due to performance cost of this pass,
            // usually user will disable settings.enableCharSelfShadow in lower quality settings(nilotoon all in one renderer feature),
            // so here we use &= to merge "settings.enableCharSelfShadow" and "volumeEffect.enableCharSelfShadow.value", instead of simple volume override
            //-------------------------------------------------------------------------------------------------------------------------------------------------------------
            // what is simple volume override? -> volumeEffect.enableCharSelfShadow.overrideState ? volumeEffect.enableCharSelfShadow.value : settings.enableCharSelfShadow;
            //-------------------------------------------------------------------------------------------------------------------------------------------------------------
            // *after NiloToon 0.10.18, when rendererfeature's enableCharSelfShadow is false, no matter what override value on volume is, this pass will not render.
            bool shouldRender = settings.enableCharSelfShadow;
            if (volumeEffect.enableCharSelfShadow.overrideState)
            {
                shouldRender &= volumeEffect.enableCharSelfShadow.value; // it is &= merge, not override
            }

            // only game and scene view will render this pass
            bool allowedCameraType = camera.cameraType == CameraType.Game;
            allowedCameraType |= camera.cameraType == CameraType.SceneView;

            shouldRender &= allowedCameraType;

            return shouldRender;
        }
        int getShadowMapSize(NiloToonShadowControlVolume volumeEffect)
        {
            int shadowMapSize = (int)(volumeEffect.shadowMapSize.overrideState ? volumeEffect.shadowMapSize.value : settings.shadowMapSize);
            
            // [TEMP fix, reduce shadow map size by 1 pixel]
            // if our shadowMapRTH's shadowMap format and size is the same as URP's shadow map RT(or any other URP's RT), URP's shadow will be buggy, not sure why.
            // we assume it is due to Unity/URP try to share RT that has the same RenderTextureDescriptor.
            // For now, we reduce shadow height by 1 pixel, to avoid our RT having the same RenderTextureDescriptor as URP's RT(which triggers the bug). 
            // This temp fix will exist until we find out what the problem is.
            if (Mathf.IsPowerOfTwo(shadowMapSize))
            {
                shadowMapSize += 1;
            }
            
            shadowMapSize = Mathf.Min(shadowMapSize, SystemInfo.maxTextureSize); // Most modern GPUs support a maximum texture size of 16384x16384 pixels
            shadowMapSize = Mathf.Max(shadowMapSize, 1);

            return shadowMapSize;
        }

        bool getUseMainLightCastShadowDirection(NiloToonShadowControlVolume volume)
        {
            return volume.useMainLightAsCastShadowDirection.overrideState ? volume.useMainLightAsCastShadowDirection.value : settings.useMainLightAsCastShadowDirection;
        }
        private void renderCharacterSelfShadowmapRT(ScriptableRenderContext context, RenderingData renderingData)
        {
            // NOTE: Do NOT mix ProfilingScope with named CommandBuffers i.e. CommandBufferPool.Get("name").
            // Currently there's an issue which results in mismatched markers.
            CommandBuffer cmd = CommandBufferPool.Get();
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

                var v = VolumeManager.instance.stack.GetComponent<NiloToonShadowControlVolume>();
                var charRenderingControlVolume = VolumeManager.instance.stack.GetComponent<NiloToonCharRenderingControlVolume>();

                if (shouldRender)
                {
                    // override settings if user override any of them in volume
                    // if user didn't override, we will get the value from renderer feature
                    bool useMainLightCastShadowDirection = getUseMainLightCastShadowDirection(v);
                    float shadowAngle = v.shadowAngle.overrideState ? v.shadowAngle.value : settings.shadowAngle;
                    float shadowLRAngle = v.shadowLRAngle.overrideState ? v.shadowLRAngle.value : settings.shadowLRAngle;
                    float shadowRange = Mathf.Clamp(v.shadowRange.overrideState ? v.shadowRange.value : settings.shadowRange, SHADOW_RANGE_MIN, SHADOW_RANGE_MAX);
                    float shadowMapSize = getShadowMapSize(v);
                    float depthBias = v.depthBias.overrideState ? v.depthBias.value : settings.depthBias;
                    float normalBias = v.normalBias.overrideState ? v.normalBias.value : settings.normalBias;
                    float receiverDepthBias = settings.receiverDepthBias;
                    float receiverNormalBias = settings.receiverNormalBias;

                    Camera camera = renderingData.cameraData.camera;

                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // find shadowCamViewMatrix
                    // (only want to find the rotation. Position is not important since it will be canceled by project matrix)
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    Matrix4x4 shadowCamViewMatrix = Matrix4x4.zero;

                    if (useMainLightCastShadowDirection)
                    {
                        //-------------------------------------------------------------
                        // try to sync with NiloToonSetToonParam as much as possible
                        //-------------------------------------------------------------

                        // [0.fill with URP's main light first]
                        int mainLightIndex = renderingData.lightData.mainLightIndex;
                        
                        // Note: when mainLight doesn't exist, mainLightIndex will be -1
                        bool isURPMainLightExist = mainLightIndex != -1;
                        if (isURPMainLightExist)
                        {
                            // first follow regular URP main light's shadow casting logic (same as URP's mainlight shadow map's light direction)
                            VisibleLight mainVisibleLight = renderingData.lightData.visibleLights[mainLightIndex];
                            Light mainLight = mainVisibleLight.light;
                            shadowCamViewMatrix = mainLight.transform.worldToLocalMatrix;
                            shadowCamViewMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)) * shadowCamViewMatrix;
                            
                            // [1.Light modifier]
                            // do nothing since no new direction data is provided
                        }

                        // [2.NiloToonCharacterMainLightOverrider override (before volume override]
                        {
                            NiloToonCharacterMainLightOverrider mainLightOverrider = 
                                NiloToonCharacterMainLightOverrider.GetHighestPriorityOverrider(NiloToonCharacterMainLightOverrider.OverrideTiming.BeforeVolumeOverride);
                            if (mainLightOverrider)
                            {
                                if(mainLightOverrider.overrideDirection)
                                {
                                    shadowCamViewMatrix = mainLightOverrider.transform.worldToLocalMatrix;
                                    shadowCamViewMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)) * shadowCamViewMatrix;
                                } 
                            }
                        }
                        
                        // [3.NiloToonCharRenderingControlVolume override]
                        Vector3 volumeOverrideRotationVectorVS = new Vector3(charRenderingControlVolume.overridedLightUpDownAngle.value, -charRenderingControlVolume.overridedLightLRAngle.value, 0);
                        // WS -> VS -> apply rotation in VS -> WS
                        Matrix4x4 volumeOverrideRotatedCamera_localToWorldMatrix = camera.cameraToWorldMatrix * Matrix4x4.Rotate(Quaternion.Euler(volumeOverrideRotationVectorVS)) * (camera.worldToCameraMatrix * camera.transform.localToWorldMatrix);
                        Matrix4x4 volumeOverrideRotatedShadowCamViewMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)) * volumeOverrideRotatedCamera_localToWorldMatrix.inverse; // inverse is worldToLocalMatrix
                        
                        float volumeOverrideLightDirStrength = charRenderingControlVolume.overrideLightDirectionIntensity.value;
                        if (shadowCamViewMatrix.ValidTRS())
                        {
                            shadowCamViewMatrix = MixTransforms(shadowCamViewMatrix, volumeOverrideRotatedShadowCamViewMatrix, volumeOverrideLightDirStrength);
                        }
                        
                        // [4.volume - desaturate, then add]
                        // do nothing since it is no related to direction
                        
                        // [5.NiloToonCharacterMainLightOverrider override (after volume override)]
                        {
                            NiloToonCharacterMainLightOverrider mainLightOverrider = 
                                NiloToonCharacterMainLightOverrider.GetHighestPriorityOverrider(NiloToonCharacterMainLightOverrider.OverrideTiming.AfterVolumeOverride);
                            if (mainLightOverrider)
                            {
                                if(mainLightOverrider.overrideDirection)
                                {
                                    shadowCamViewMatrix = mainLightOverrider.transform.worldToLocalMatrix;
                                    shadowCamViewMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)) * shadowCamViewMatrix;
                                } 
                            }
                        }
                        
                        // [7.NiloToonCharacterMainLightOverrider override (after everything)]
                        {
                            NiloToonCharacterMainLightOverrider mainLightOverrider = 
                                NiloToonCharacterMainLightOverrider.GetHighestPriorityOverrider(NiloToonCharacterMainLightOverrider.OverrideTiming.AfterEverything);
                            if (mainLightOverrider)
                            {
                                if(mainLightOverrider.overrideDirection)
                                {
                                    shadowCamViewMatrix = mainLightOverrider.transform.worldToLocalMatrix;
                                    shadowCamViewMatrix = Matrix4x4.Rotate(Quaternion.Euler(0, 180, 0)) * shadowCamViewMatrix;
                                } 
                            }
                        }
                    }

                    if (!useMainLightCastShadowDirection || (shadowCamViewMatrix == Matrix4x4.zero))
                    {
                        // if we go here, that means:
                        // - user explict only care the rotation of camera (useMainLightCastShadowDirection is false)
                        // or
                        // - no main light information exist(URP main light/NiloToonCharacterMainLightOverrider/NiloToonCharRenderingControlVolume) (shadowCamViewMatrix is zero)
                        // Then we will calculate shadow dir using "camera transform + rotation"
                        shadowCamViewMatrix = Matrix4x4.Rotate(Quaternion.Euler(new Vector3(shadowAngle, shadowLRAngle, 0))) * camera.worldToCameraMatrix;
                    }

                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // auto close-fit to find "character only" tight bound shadow map ortho projection matrix 
                    // (smallest orthographic box that includes all effective shadow caster characters)
                    // https://docs.microsoft.com/en-us/windows/win32/dxtecharts/common-techniques-to-improve-shadow-depth-maps
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

                    GeometryUtility.CalculateFrustumPlanes(camera, cameraPlanes);

                    validCharList.Clear();

                    // [1] filter list 
                    foreach (var targetChar in NiloToonAllInOneRendererFeature.characterList)
                    {
                        // if target is not valid, skip it
                        if (targetChar == null) continue;
                        if (!targetChar.isActiveAndEnabled) continue; // character GameObject not enabled(not rendering) but in list

                        // if character bounding sphere is completely not visible in game camera frustum, skip it
                        var boundRadius = targetChar.GetCharacterBoundRadius();
                        var centerPosWS = targetChar.GetCharacterBoundCenter();
                        // TODO: this section is not correct, which may incorrectly cull effective shadow caster that is OUTSIDE of main camera frustum
                        if (!GeometryUtility.TestPlanesAABB(cameraPlanes, new Bounds(centerPosWS, Vector3.one * boundRadius)))
                        {
                            continue;
                        }

                        // it is a valid visible char, add to list
                        validCharList.Add(targetChar);
                    }

                    // [2] find closest VS depth from all visible char
                    float closestShadowCasterNearSideRangeVS = float.MaxValue;
                    float closestShadowCasterFarSideRangeVS = float.MaxValue;
                    foreach (var targetChar in validCharList)
                    {
                        var boundRadius = targetChar.GetCharacterBoundRadius();
                        var centerPosWS = targetChar.GetCharacterBoundCenter();
                        var centerPosVS = camera.worldToCameraMatrix.MultiplyPoint(centerPosWS);

                        float currentCharVSNearSideDepth = -centerPosVS.z - boundRadius;
                        if (currentCharVSNearSideDepth < closestShadowCasterNearSideRangeVS)
                        {
                            closestShadowCasterNearSideRangeVS = currentCharVSNearSideDepth;
                            closestShadowCasterFarSideRangeVS = -centerPosVS.z + boundRadius;
                        }
                    }
                    float finalShadowAllowedEndRangeVS = Mathf.Max(0, closestShadowCasterFarSideRangeVS) + shadowRange;

                    // [3] remove "out of shadowrange" char, and find final shadow end range
                    finalValidCharList.Clear();
                    float farestShadowCasterEndRangeVS = float.MinValue;
                    foreach (var targetChar in validCharList)
                    {
                        var boundRadius = targetChar.GetCharacterBoundRadius();
                        var centerPosWS = targetChar.GetCharacterBoundCenter();
                        var centerPosVS = camera.worldToCameraMatrix.MultiplyPoint(centerPosWS);

                        float currentCharVSNearSideDepth = -centerPosVS.z - boundRadius;
                        if (currentCharVSNearSideDepth <= finalShadowAllowedEndRangeVS)
                        {
                            finalValidCharList.Add(targetChar);

                            float currentCharVSFarSideDepth = -centerPosVS.z + boundRadius;
                            if (currentCharVSFarSideDepth > farestShadowCasterEndRangeVS)
                            {
                                farestShadowCasterEndRangeVS = currentCharVSFarSideDepth;
                            }
                        }
                    }

                    // [4] if nothing to render, treat NiloToon self shadow as disabled, early exit
                    if (finalValidCharList.Count == 0)
                    {
                        CoreUtils.SetKeyword(cmd, _NILOTOON_RECEIVE_SELF_SHADOW_Keyword, false);
                        goto END;
                    }

                    // [5] in shadow camera's view space(not game camera), use smallest ortho box to capture all char in finalValidCharList
                    float minX = Mathf.Infinity;
                    float maxX = Mathf.NegativeInfinity;
                    float minY = Mathf.Infinity;
                    float maxY = Mathf.NegativeInfinity;
                    float minZ = Mathf.Infinity;
                    float maxZ = Mathf.NegativeInfinity;
                    foreach (var targetChar in finalValidCharList)
                    {
                        // prepare information of character's bounding sphere in world space(WS) and shadow camera's view space(VS)
                        var centerPosWS = targetChar.GetCharacterBoundCenter();
                        var centerPosShadowCamVS = (Matrix4x4.Scale(new Vector3(1, 1, -1)) * shadowCamViewMatrix).MultiplyPoint(centerPosWS);
                        var boundRadius = targetChar.GetCharacterBoundRadius();

                        // expand shadow camera's view space orthographic 3D box bound to include all char in finalValidCharList
                        minX = Mathf.Min(minX, centerPosShadowCamVS.x - boundRadius);
                        maxX = Mathf.Max(maxX, centerPosShadowCamVS.x + boundRadius);
                        minY = Mathf.Min(minY, centerPosShadowCamVS.y - boundRadius);
                        maxY = Mathf.Max(maxY, centerPosShadowCamVS.y + boundRadius);
                        minZ = Mathf.Min(minZ, centerPosShadowCamVS.z - boundRadius);
                        maxZ = Mathf.Max(maxZ, centerPosShadowCamVS.z + boundRadius);
                    }

                    // force ortho frustum become a rectangle in xy plane, although it wastes more shadowmap space, it works with shadowbias much better!
                    // (see URP's main light's shadow bias C# code)
                    float width = Mathf.Abs(maxX - minX);
                    float height = Mathf.Abs(maxY - minY);
                    float diff = Mathf.Abs(width - height);
                    if (width > height)
                    {
                        minY -= diff / 2f;
                        maxY += diff / 2f;
                    }
                    else
                    {
                        minX -= diff / 2f;
                        maxX += diff / 2f;
                    }

                    Matrix4x4 shadowCamProjectionMatrix = Matrix4x4.Ortho(minX, maxX, minY, maxY, minZ, maxZ);

#if UNITY_EDITOR
                    bool isGameViewCamera = renderingData.cameraData.cameraType == CameraType.Game && !renderingData.cameraData.isPreviewCamera;

                    // we only want to draw using game camera's transform data
                    // so we can render a stable white box in scene view
                    if (showShadowCameraDebugFrustum && isGameViewCamera)
                    {
                        Matrix4x4 I_V = (Matrix4x4.Scale(new Vector3(1, 1, -1)) * shadowCamViewMatrix).inverse;
                        Vector3 point1 = I_V.MultiplyPoint(new Vector3(minX, maxY, minZ));
                        Vector3 point2 = I_V.MultiplyPoint(new Vector3(maxX, maxY, minZ));
                        Vector3 point3 = I_V.MultiplyPoint(new Vector3(maxX, minY, minZ));
                        Vector3 point4 = I_V.MultiplyPoint(new Vector3(minX, minY, minZ));
                        Vector3 point5 = I_V.MultiplyPoint(new Vector3(minX, maxY, maxZ));
                        Vector3 point6 = I_V.MultiplyPoint(new Vector3(maxX, maxY, maxZ));
                        Vector3 point7 = I_V.MultiplyPoint(new Vector3(maxX, minY, maxZ));
                        Vector3 point8 = I_V.MultiplyPoint(new Vector3(minX, minY, maxZ));

                        // draw shadow camera visible ortho box
                        Debug.DrawLine(point1, point2, Color.red);
                        Debug.DrawLine(point2, point3, Color.red);
                        Debug.DrawLine(point3, point4, Color.red);
                        Debug.DrawLine(point4, point1, Color.red);

                        Debug.DrawLine(point5, point6, Color.white);
                        Debug.DrawLine(point6, point7, Color.white);
                        Debug.DrawLine(point7, point8, Color.white);
                        Debug.DrawLine(point8, point5, Color.white);

                        Debug.DrawLine(point1, point5, Color.white);
                        Debug.DrawLine(point2, point6, Color.white);
                        Debug.DrawLine(point3, point7, Color.white);
                        Debug.DrawLine(point4, point8, Color.white);

                        foreach (var character in finalValidCharList)
                        {
                            Color color = Color.yellow;
                            color.a = 0.05f;
                            DrawSphere(character.GetCharacterBoundCenter(), character.GetCharacterBoundRadius(), color);
                        }
                    }
#endif

                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // set culling for shadow camera -> do culling
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////               
                    camera.TryGetCullingParameters(out var cullingParameters);

                    // update culling matrix
                    cullingParameters.cullingMatrix = shadowCamProjectionMatrix * shadowCamViewMatrix;

                    // update culling planes
                    GeometryUtility.CalculateFrustumPlanes(cullingParameters.cullingMatrix, cameraPlanes);
                    for (int i = 0; i < cameraPlanes.Length; i++)
                    {
                        cullingParameters.SetCullingPlane(i, cameraPlanes[i]);
                    }

                    CullingResults cullResults;

                    bool terrainExist = false;
                    if (settings.terrainCrashSafeGuard)
                    {
                        terrainExist = Terrain.activeTerrains.Length != 0;
                    }
                    if (settings.perfectCullingForShadowCasters && !terrainExist)
                    {
                        // use the above new cullResults in DrawRenderers() below, 
                        // so even a renderer is not visible in the perspective of main camera,
                        // it can still render correctly in shadow camera's perspective due to this new culling

                        // (2021-07-14) unity will crash if code running this line and terrain exist in scene
                        // (2024-03-21) enable this will make VLB's SRP batcher mode flicker randomly, not sure why, should we do something to revert this culling line?
                        cullResults = context.Cull(ref cullingParameters); // original working code, but will crash if terrain exist
                    }
                    else
                    {
                        // (2021-07-14) a special temp fix to avoid terrain crashing unity, but will make shadow culling not always correctly if shadow caster is not existing on screen
                        cullResults = renderingData.cullResults;
                    }

                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // Set uniform (before context.DrawRenderers)
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    Matrix4x4 GPU_P = GL.GetGPUProjectionMatrix(shadowCamProjectionMatrix, true);

                    // [copied from URP16's ShadowUtils.cs -> GetShadowBias(...)]
                    // 
                    float frustumSize = 2.0f / shadowCamProjectionMatrix.m00;
                    // depth and normal bias scale is in shadowmap texel size in world space
                    float texelSize = frustumSize / shadowMapSize;

                    Matrix4x4 GPU_worldToClip = GPU_P * shadowCamViewMatrix;
                    cmd.SetGlobalMatrix("_NiloToonSelfShadowWorldToClip", GPU_worldToClip);
                    cmd.SetGlobalVector("_NiloToonSelfShadowParam", new Vector4(1f / shadowMapSize, 1f / shadowMapSize, shadowMapSize, shadowMapSize));
                    cmd.SetGlobalFloat("_NiloToonGlobalSelfShadowCasterDepthBias", -depthBias * texelSize);
                    cmd.SetGlobalFloat("_NiloToonGlobalSelfShadowCasterNormalBias", -normalBias * texelSize);
                    cmd.SetGlobalFloat("_NiloToonGlobalSelfShadowReceiverDepthBias", receiverDepthBias  * texelSize);
                    cmd.SetGlobalFloat("_NiloToonGlobalSelfShadowReceiverNormalBias", receiverNormalBias  * texelSize);
                    cmd.SetGlobalVector("_NiloToonSelfShadowLightDirection", shadowCamViewMatrix.inverse.MultiplyVector(Vector3.forward));
                    cmd.SetGlobalFloat("_NiloToonSelfShadowUseNdotLFix", settings.useNdotLFix ? 1 : 0);
                    cmd.SetGlobalFloat("_NiloToonSelfShadowRange", finalShadowAllowedEndRangeVS); // or farestShadowCasterEndRangeVS?
                    cmd.SetGlobalFloat("_GlobalReceiveNiloToonSelfShadowMap", v.charSelfShadowStrength.value);
                    
                    // Only for supporting NiloToon 0.13.8 shader (Warudo / asset bundle), not used in the latest shader
                    //--------------------------------------------------------------------------------
                    cmd.SetGlobalFloat("_NiloToonGlobalSelfShadowDepthBias", depthBias); 
                    cmd.SetGlobalFloat("_NiloToonGlobalSelfShadowNormalBias", normalBias);
                    //--------------------------------------------------------------------------------

                    float softShadowQualityID;
                    if (settings.useSoftShadow)
                    {
                        softShadowQualityID = (float)settings.softShadowQuality;
                    }
                    else
                    {
                        softShadowQualityID = 0;
                    }

                    float softShadowResharpWidth = Mathf.Lerp(0.5f,0.05f,settings.resharpenStregth);
                    cmd.SetGlobalVector("_NiloToonSelfShadowSoftShadowParam", new Vector4(softShadowQualityID,settings.useSoftShadowResharpen ? 1 : 0,softShadowResharpWidth,0));
                    
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    //set global RT
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
#if UNITY_2022_2_OR_NEWER
                    cmd.SetGlobalTexture(shadowMapRTH.name, shadowMapRTH);
#else
                    cmd.SetGlobalTexture(shadowMapRTH.id, new RenderTargetIdentifier(shadowMapRTH.id));
#endif                    
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    // Note: Since we are providing our own _NiloToonSelfShadowWorldToClip to shader for VP transform,
                    // this section is not needed anymore, it will trigger a bug in multi pass mode XR
                    /*
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // override view & Projection matrix for shadowmap draw
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    cmd.SetViewProjectionMatrices(shadowCamViewMatrix, shadowCamProjectionMatrix);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    */

                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // draw all char renderer using SRP batching (must set all uniforms and executed before draw!)
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    ShaderTagId shaderTagId = new ShaderTagId("NiloToonSelfShadowCaster");
                    var drawSetting = CreateDrawingSettings(shaderTagId, ref renderingData, SortingCriteria.CommonOpaque);
                    var filterSetting = new FilteringSettings(RenderQueueRange.opaque);
                    context.DrawRenderers(cullResults, ref drawSetting, ref filterSetting); // using custom cullResults from shadow camera's perspective, instead of main camera's cull result

                    // Note: Since we are providing our own _NiloToonSelfShadowWorldToClip to shader for VP transform,
                    // this section is not needed anymore, it will trigger a bug in multi pass mode XR
                    /*
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    // restore view & Projection matrix
                    ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                    cmd.SetViewProjectionMatrices(renderingData.cameraData.camera.worldToCameraMatrix, renderingData.cameraData.camera.projectionMatrix);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    */
                }

                CoreUtils.SetKeyword(cmd, _NILOTOON_RECEIVE_SELF_SHADOW_Keyword, shouldRender);
            }

        END:
            // must write these line after using{} finished, to ensure profiler and frame debugger display correctness
            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }

        Matrix4x4 MixTransforms(Matrix4x4 matrixA, Matrix4x4 matrixB, float t)
        {
            // Extract the position (translation) from each matrix
            Vector3 positionA = matrixA.GetColumn(3);
            Vector3 positionB = matrixB.GetColumn(3);

            // Interpolate the position
            Vector3 mixedPosition = Vector3.Lerp(positionA, positionB, t);

            // Extract the rotation from each matrix and convert to Quaternion
            Quaternion rotationA = matrixA.rotation;
            Quaternion rotationB = matrixB.rotation;

            // Interpolate the rotation
            Quaternion mixedRotation = Quaternion.Slerp(rotationA, rotationB, t);

            // Extract the scale from each matrix
            Vector3 scaleA = new Vector3(matrixA.GetColumn(0).magnitude, matrixA.GetColumn(1).magnitude, matrixA.GetColumn(2).magnitude);
            Vector3 scaleB = new Vector3(matrixB.GetColumn(0).magnitude, matrixB.GetColumn(1).magnitude, matrixB.GetColumn(2).magnitude);

            // Interpolate the scale
            Vector3 mixedScale = Vector3.Lerp(scaleA, scaleB, t);

            // Create a new matrix from the interpolated position, rotation, and scale
            Matrix4x4 mixedMatrix = Matrix4x4.TRS(mixedPosition, mixedRotation, mixedScale);

            return mixedMatrix;
        }

        public static void DrawSphere(Vector3 position, float radius, Color color, int resolution = 12)
        {
            float step = Mathf.PI * 2 / resolution;

            // Draw horizontal circles
            for (float phi = -Mathf.PI / 2; phi < Mathf.PI / 2; phi += step)
            {
                for (float theta = 0; theta <= Mathf.PI * 2; theta += step)
                {
                    Vector3 pos1 = position + new Vector3(radius * Mathf.Cos(theta) * Mathf.Cos(phi), radius * Mathf.Sin(phi), radius * Mathf.Sin(theta) * Mathf.Cos(phi));
                    Vector3 pos2 = position + new Vector3(radius * Mathf.Cos(theta + step) * Mathf.Cos(phi), radius * Mathf.Sin(phi), radius * Mathf.Sin(theta + step) * Mathf.Cos(phi));

                    Debug.DrawLine(pos1, pos2, color);
                }
            }

            // Draw vertical circles
            for (float theta = 0; theta <= Mathf.PI * 2; theta += step)
            {
                for (float phi = -Mathf.PI / 2; phi < Mathf.PI / 2; phi += step)
                {
                    Vector3 pos1 = position + new Vector3(radius * Mathf.Cos(theta) * Mathf.Cos(phi), radius * Mathf.Sin(phi), radius * Mathf.Sin(theta) * Mathf.Cos(phi));
                    Vector3 pos2 = position + new Vector3(radius * Mathf.Cos(theta) * Mathf.Cos(phi + step), radius * Mathf.Sin(phi + step), radius * Mathf.Sin(theta) * Mathf.Cos(phi + step));

                    Debug.DrawLine(pos1, pos2, color);
                }
            }
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
            public bool shouldRender;
        }
 
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameContext)
        {
            using (var builder = renderGraph.AddRasterRenderPass<PassData>("NiloToonCharSelfShadowMapRTPass(RG)", out var passData))
            {
                // Get the data needed to create the list of objects to draw
                UniversalCameraData cameraData = frameContext.Get<UniversalCameraData>();
                UniversalRenderingData renderingData = frameContext.Get<UniversalRenderingData>();
                UniversalLightData lightData = frameContext.Get<UniversalLightData>();
 
                SortingCriteria sortFlags = SortingCriteria.CommonOpaque; //cameraData.defaultOpaqueSortFlags;
                RenderQueueRange renderQueueRange = RenderQueueRange.opaque;
                FilteringSettings filterSettings = new FilteringSettings(renderQueueRange, ~0);

                // Redraw only objects that have their LightMode tag set to toonOutlineLightModeShaderTagId 
                ShaderTagId shadersToOverride = new ShaderTagId("NiloToonSelfShadowCaster");

                // Create drawing settings
                DrawingSettings drawSettings = RenderingUtils.CreateDrawingSettings(shadersToOverride, renderingData, cameraData, lightData, sortFlags);

                // Create the list of objects to draw
                var rendererListParameters = new RendererListParams(renderingData.cullResults, drawSettings, filterSettings);

               

                // create RT (temp)
                // Create texture properties that match the screen size
                var volumeEffect = VolumeManager.instance.stack.GetComponent<NiloToonShadowControlVolume>();

                int shadowMapSize = getShadowMapSize(volumeEffect);

                if (!getShouldRender(volumeEffect, cameraData.camera))
                {
                    shadowMapSize = 1;
                    shouldRender = false;
                }
                else
                {
                    shouldRender = true;
                }
                
                // Convert the list to a list handle that the render graph system can use
                passData.rendererListHandle = renderGraph.CreateRendererList(rendererListParameters);
                passData.shouldRender = shouldRender;
            
                RenderTextureDescriptor renderTextureDescriptor = new RenderTextureDescriptor(shadowMapSize, shadowMapSize, RenderTextureFormat.Shadowmap, 16);

                // Create a temporary texture
                TextureHandle shadowMapRT = UniversalRenderer.CreateRenderGraphTexture(renderGraph, renderTextureDescriptor, "_NiloToonCharSelfShadowMapRT", true);
                
                // Set the render target as the color and depth textures of the active camera texture
                UniversalResourceData resourceData = frameContext.Get<UniversalResourceData>();
                builder.UseRendererList(passData.rendererListHandle);
                //builder.SetRenderAttachment(resourceData.activeColorTexture, 0);
                builder.SetRenderAttachmentDepth(shadowMapRT, AccessFlags.Write);
                
                builder.AllowGlobalStateModification(true);

                builder.SetRenderFunc((PassData data, RasterGraphContext context) => ExecutePass(data, context));
            }
        }
        
        static void ExecutePass(PassData data, RasterGraphContext context)
        {
            var cmd = context.cmd;

            // temp WIP code: set the only required cmd
            ;
            cmd.SetKeyword(GlobalKeyword.Create(_NILOTOON_RECEIVE_SELF_SHADOW_Keyword), false);
            return;
            //@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@
            
            // early exit if no render needed
            if (!data.shouldRender)
            {
                // set the only required cmd
                cmd.SetKeyword(new GlobalKeyword(_NILOTOON_RECEIVE_SELF_SHADOW_Keyword), false);

                // no draw
                // (X)
                
                return;
            }

            // set cmd
            //...

            // Draw the objects in the list
            cmd.DrawRendererList(data.rendererListHandle);
        }
#endif
    }
}