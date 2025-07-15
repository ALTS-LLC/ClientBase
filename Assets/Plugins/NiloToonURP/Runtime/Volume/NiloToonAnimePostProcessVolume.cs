using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NiloToon.NiloToonURP
{
    [System.Serializable, VolumeComponentMenu("NiloToon/Anime PostProcess (NiloToon)")]
    public class NiloToonAnimePostProcessVolume : VolumeComponent, IPostProcessComponent
    {
        [Header("Master control")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 1);
        [OverrideDisplayName("Rotate Angle")]
        public ClampedFloatParameter rotation = new ClampedFloatParameter(0, -720, 720);

        [Header("Top Light settings")]
        [Tooltip("Default: 0.5 (top half of the screen vertically)")]
        [OverrideDisplayName("Draw Height")]
        public ClampedFloatParameter topLightEffectDrawHeight = new ClampedFloatParameter(0.5f,0,1);
        [OverrideDisplayName("Intensity")]
        public MinFloatParameter topLightEffectIntensity = new MinFloatParameter(1, 0);
        [OverrideDisplayName("Tint by MainLight color?")]
        public ClampedFloatParameter topLightMultiplyLightColor = new ClampedFloatParameter(1, 0, 1);
        [OverrideDisplayName("Sun Tint Color")]
        public ColorParameter topLightSunTintColor = new ColorParameter(new Color(0.3f, 0.225f, 0.1125f));
        [OverrideDisplayName("Desaturate Light")]
        public ClampedFloatParameter topLightDesaturate = new ClampedFloatParameter(0, 0, 1);
        [OverrideDisplayName("Tint Color")]
        public ColorParameter topLightTintColor = new ColorParameter(Color.white, true, false, true);
        [OverrideDisplayName("Rotate angle")]
        public ClampedFloatParameter topLightExtraRotation = new ClampedFloatParameter(0, -720, 720);

        [Header("Bottom Darken settings")]
        [Tooltip("Default: 0.5 (bottom half of the screen vertically)")]
        [OverrideDisplayName("Draw Height")]
        public ClampedFloatParameter bottomDarkenEffectDrawHeight = new ClampedFloatParameter(0.5f, 0, 1);
        [OverrideDisplayName("Intensity")]
        public MinFloatParameter bottomDarkenEffectIntensity = new MinFloatParameter(1, 0);
        [OverrideDisplayName("Rotate Angle")]
        public ClampedFloatParameter bottomDarkenExtraRotation = new ClampedFloatParameter(0, -720, 720);

        [Header("Draw Timing")]
        [Tooltip(   "- If false, will draw after all rendering, but before UGUI.\n" +
                    "- If true, will draw before postprocess.\n\n" +
                    "Should turn ON if you are rendering to a custom RenderTexture.")]
        public BoolParameter drawBeforePostProcess = new BoolParameter(false);

        [Header("Other")] 
        public BoolParameter affectedByCameraPostprocessToggle = new BoolParameter(true);
        
        public bool IsActive() => intensity.value > 0f && (topLightEffectIntensity.value > 0 || bottomDarkenEffectIntensity.value > 0);

        public bool IsTileCompatible() => false;
    }
}

