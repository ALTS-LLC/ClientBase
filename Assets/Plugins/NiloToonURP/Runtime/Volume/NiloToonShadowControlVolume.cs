using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NiloToon.NiloToonURP
{
    [System.Serializable, VolumeComponentMenu("NiloToon/Shadow Control (NiloToon)")]
    public class NiloToonShadowControlVolume : VolumeComponent, IPostProcessComponent
    {
        [Header("Overall shadow")]
        public ColorParameter characterOverallShadowTintColor = new ColorParameter(Color.white, true, false, true);
        public ClampedFloatParameter characterOverallShadowStrength = new ClampedFloatParameter(1, 0, 2);

        [Header("Character NiloToon Average shadow map")]
        public BoolParameter enableCharAverageShadow = new BoolParameter(true);
        public ClampedFloatParameter charAverageShadowStrength = new ClampedFloatParameter(1, 0, 1);

        [Header("Character NiloToon self shadow map")]
        // all default values copy from NiloToonCharSelfShadowMapRTPass.Settings
        public BoolParameter enableCharSelfShadow = new BoolParameter(true);
        public ClampedFloatParameter charSelfShadowStrength = new ClampedFloatParameter(1, 0, 1);

        [Tooltip(   "- If overridden to false, will use camera's forward(with the shadowAngle & shadowLRAngle applied) as cast shadow direction.\n" +
                    "- If overridden to true, will use main light's forward as cast shadow direction, same as any regular shadowmapping.\n" +
                    "Keep it ON if you don't want shadow affected by camera rotation")]
        public BoolParameter useMainLightAsCastShadowDirection = new BoolParameter(true); // default is true since 0.12.1, since most user expect it to be true by default

        [Tooltip("Useful only when useMainLightAsCastShadowDirection is false")]
        public ClampedFloatParameter shadowAngle = new ClampedFloatParameter(30f, -45f, 45f);
        [Tooltip("Useful only when useMainLightAsCastShadowDirection is false")]
        public ClampedFloatParameter shadowLRAngle = new ClampedFloatParameter(0f, -45f, 45f);

        [Tooltip("Maximum Shadow rendering distance. It starts from the closest visible character(not start from camera).\n" +
            "- Increase this will make more characters render self shadow even when they are far away from camera, but the overall shadow quality will be lower.\n" +
            "- Decrease this will make less characters render self shadow even when they are close to camera, but the overall shadow quality will be higher.\n" +
            "\n" +
            "*When only 1 nilo character is visible, this value doesn't matter, since it is the extend distance after the closest character(not distance from camera).")]
        public ClampedFloatParameter shadowRange = new ClampedFloatParameter(NiloToonCharSelfShadowMapRTPass.SHADOW_RANGE_DEFAULT, NiloToonCharSelfShadowMapRTPass.SHADOW_RANGE_MIN, NiloToonCharSelfShadowMapRTPass.SHADOW_RANGE_MAX);
        [Tooltip("Higher resolution will look much better, but it requires more GPU resources.")]
        public ClampedFloatParameter shadowMapSize = new ClampedFloatParameter(4096, 512, 16384);

        [Tooltip("In most cases, you don't need to edit it.\n" +
            "Increase it to hide Shadow Acne, but Peter Panning will appear")]
        public ClampedFloatParameter depthBias = new ClampedFloatParameter(1, 0, 4);
        [Tooltip("In most cases, you don't need to edit it.\n" +
            "In some situation you may want to reduce it to keep the shadow shape closer to the character(e.g. finger shape).")]
        public ClampedFloatParameter normalBias = new ClampedFloatParameter(1, 0, 4);

        [Header("Character receiving URP's shadow map")]
        public BoolParameter receiveURPShadow = new BoolParameter(false);
        public ClampedFloatParameter URPShadowIntensity = new ClampedFloatParameter(1, 0, 1);
        [Tooltip("Drag to 0 to produce regular URP shadow result (block all direct light)")]
        public ClampedFloatParameter URPShadowAsDirectLightMultiplier = new ClampedFloatParameter(1, 0, 1);
        public ColorParameter URPShadowAsDirectLightTintColor = new ColorParameter(Color.white,false,false,true);
        public ClampedFloatParameter URPShadowAsDirectLightTintIgnoreMaterialURPUsageSetting = new ClampedFloatParameter(1, 0, 1);
        public ClampedFloatParameter URPShadowblurriness = new ClampedFloatParameter(1, 0, 1);
        public bool IsActive() => true;

        public bool IsTileCompatible() => false;
    }
}

