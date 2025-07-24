using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NiloToon.NiloToonURP
{
    [System.Serializable, VolumeComponentMenu("NiloToon/Cinematic Rim Light (NiloToon)")]
    public class NiloToonCinematicRimLightVolume : VolumeComponent, IPostProcessComponent
    {
        [Header("Rim (2D Style)(= Default style if all style's strength are 0)")]
        [OverrideDisplayName("Strength")]
        public ClampedFloatParameter strengthRimMask2D = new ClampedFloatParameter(0, 0, 1);

        [Header("Rim (3D Classic Style)")]
        [OverrideDisplayName("Strength")]
        public ClampedFloatParameter strengthRimMask3D_ClassicStyle = new ClampedFloatParameter(0, 0, 1);

        [Header("Rim (3D Dynamic Style)")]
        [OverrideDisplayName("Strength")]
        public ClampedFloatParameter strengthRimMask3D_DynmaicStyle = new ClampedFloatParameter(0, 0, 1);
        [OverrideDisplayName("    Rim Sharpness")]
        public ClampedFloatParameter sharpnessRimMask3D_DynamicStyle = new ClampedFloatParameter(0.375f, 0, 1);

        [Header("Rim (3D BackLight only Style) (Experimental)")]
        [OverrideDisplayName("Strength")]
        public ClampedFloatParameter strengthRimMask3D_StableStyle = new ClampedFloatParameter(0, 0, 1);
        [OverrideDisplayName("    Rim Sharpness")]
        public ClampedFloatParameter sharpnessRimMask3D_StableStyle = new ClampedFloatParameter(0.5f, 0, 1);

        [Header("--------------------------------------------------------------------------------------------------------------------------------------")]
        [Header("Rim Light Intensity & color style")]
        [OverrideDisplayName("Intensity")]
        public MinFloatParameter lightIntensityMultiplier = new MinFloatParameter(1f, 0);
        [OverrideDisplayName("Tint BaseMap?")]
        public ClampedFloatParameter tintBaseMap = new ClampedFloatParameter(0.5f, 0, 1);
        
        [Header("--------------------------------------------------------------------------------------------------------------------------------------")]
        [Header("Style Safe guard")]
        [OverrideDisplayName("Auto fix unsafe Style?")]
        public BoolParameter enableStyleSafeGuard = new (true, false);

        public bool IsActive() => true;

        public bool IsTileCompatible() => false;
    }
}

