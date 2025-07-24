using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NiloToon.NiloToonURP
{
    [System.Serializable, VolumeComponentMenu("NiloToon/Bloom (NiloToon)")]
    public class NiloToonBloomVolume : VolumeComponent, IPostProcessComponent
    {
        [Header("======== Same as URP's Bloom ======================================================================")]

        [Header("Bloom")]
        [Tooltip("Filters out pixels under this level of brightness. Value is in gamma-space.")]
        public MinFloatParameter threshold = new MinFloatParameter(0.9f, 0f);

        [Tooltip("Strength of the bloom filter.")]
        public MinFloatParameter intensity = new MinFloatParameter(0f, 0f);

        [Tooltip("Changes the extent of veiling effects.")]
        public ClampedFloatParameter scatter = new ClampedFloatParameter(0.7f, 0f, 1f);

        [Tooltip("Global tint of the bloom filter.")]
        public ColorParameter tint = new ColorParameter(Color.white, false, false, true);

        [Tooltip("Clamps pixels to control the bloom amount.")]
        public MinFloatParameter clamp = new MinFloatParameter(65472f, 0f);

        [Tooltip("Use bicubic sampling instead of bilinear sampling for the upsampling passes. This is slightly more expensive but helps getting smoother visuals.")]
        public BoolParameter highQualityFiltering = new BoolParameter(false);

#if UNITY_2022_2_OR_NEWER
        /// <summary>
        /// Controls the starting resolution that this effect begins processing.
        /// </summary>
        [Tooltip("The starting resolution that this effect begins processing."), AdditionalProperty]
        public DownscaleParameter downscale = new DownscaleParameter(BloomDownscaleMode.Half);
#endif

#if !UNITY_2022_2_OR_NEWER
        [Tooltip("The number of final iterations to skip in the effect processing sequence.")]
        public ClampedIntParameter skipIterations = new ClampedIntParameter(1, 0, 16);
#endif

#if UNITY_2022_2_OR_NEWER
        /// <summary>
        /// Controls the maximum number of iterations in the effect processing sequence.
        /// </summary>
        [Tooltip("The maximum number of iterations in the effect processing sequence."), AdditionalProperty]
        public ClampedIntParameter maxIterations = new ClampedIntParameter(8, 2, 8); // default 8 instead of 6 to keep the same result as Unity2021.3(URP12's original design)
#endif
        [Header("Lens Dirt")]

        [Tooltip("Dirtiness texture to add smudges or dust to the bloom effect.")]
        public TextureParameter dirtTexture = new TextureParameter(null);

        [Tooltip("Amount of dirtiness.")]
        public MinFloatParameter dirtIntensity = new MinFloatParameter(0f, 0f);

        [Header("======== NiloToon added ==========================================================================")]

        [Header("Char Brightness")]
        [OverrideDisplayName("Mat BaseMap")]
        public MinFloatParameter characterBaseColorBrightness = new MinFloatParameter(1, 0);
        [OverrideDisplayName("Mat Result")]
        public MinFloatParameter characterBrightness = new MinFloatParameter(1, 0);
        [OverrideDisplayName("Post-Bloom Result")]
        public MinFloatParameter characterPreTonemapBrightness = new MinFloatParameter(1, 0);

        [Header("Char Area Bloom Emit")]
        [Tooltip(
            "The pre-blur bloom intensity(character area intensity/color that goes into the input of bloom blur-chain), the purpose is very similar to 'Threshold' but this is much more intuitive to use.\n" +
            "\n" +
            "- Set to a value < 1 = Make NiloToon character emit less bloom.\n" +
            "- Set to a value > 1 = Make NiloToon character emit more bloom.\n" +
            "\n" +
            "* Bloom from the environment will still bleed into character area.")]
        [OverrideDisplayName("Intensity")]
        public MinFloatParameter characterAreaBloomEmitMultiplier = new MinFloatParameter(1, 0);
        
        [Header("Char Area Bloom Style Override")]
        [Tooltip(
            "Same as the 'Threshold' in the above URP section, but this is affecting character area only.\n" +
            "(When enabled, character area filters out pixels under this overridden level of brightness. Value is in gamma-space.)\n" +
            "\n" +
            "- Override to a value higher than the 'Threshold' in the above URP section = makes NiloToon character harder to emit bloom.\n" +
            "- Override to a value lower than the 'Threshold' in the above URP section = makes NiloToon character easier to emit bloom.")]
        [OverrideDisplayName("Threshold")]
        public MinFloatParameter characterAreaOverridedThreshold = new MinFloatParameter(0.9f, 0f); // same default value as threshold
        [Tooltip(
            "Same as the 'Intensity' in the above URP section, but it is affecting character area only.\n" +
            "\n" +
            "This is the final bloom display intensity on character area.\n" +
            "Reduce this when you need to reduce/remove the additive bloom effect on character pixels in the final image.\n" +
            "* When set to 0, Character area will not display any bloom, even bloom emitted from the environment that bleeded into character area will not be displayed also.")]
        [OverrideDisplayName("Intensity")]
        public MinFloatParameter characterAreaOverridedIntensity = new MinFloatParameter(0,0); // same default value as intensity
        
        [Header("Bloom HSV")]
        [OverrideDisplayName("(H) Hue Offset")]
        public ClampedFloatParameter HueOffset = new ClampedFloatParameter(0, -1, 1);
        [OverrideDisplayName("(S) Saturation Boost")]
        public ClampedFloatParameter SaturationBoost = new ClampedFloatParameter(0, 0, 1);
        [OverrideDisplayName("(V) Value Multiply")]
        public ClampedFloatParameter ValueMultiply = new ClampedFloatParameter(1, 0, 1);
        [OverrideDisplayName("Apply to Char only?")]
        public ClampedFloatParameter ApplyHSVToCharAreaOnly = new ClampedFloatParameter(0, 0, 1);

        [Header("Resolution-Independent Bloom")]
        [Tooltip(
                    "Enable to use a constant bloom resolution instead of URP's dynamic size bloom resolution\n\n" +
                    "A constant bloom resolution will make the bloom result(e.g. bloom blur size) not affected by Game resolution/RenderScale.\n\n" +
                    "We add this setting because URP's Bloom is always affected by Game resolution/RenderScale, which is nearly unusable when you need a very consistent and stable bloom blur size(a.k.a scatter size) to produce a specific art style bloom(e.g. a fixed screen space blur size(scatter) anime soft diffusion bloom).")]
        [OverrideDisplayName("Lock resolution")]
        public ClampedIntParameter renderTextureOverridedToFixedHeight = new ClampedIntParameter(1080, 135, 2160);

        public bool IsActive() => intensity.value > 0f || (characterAreaOverridedIntensity.overrideState && characterAreaOverridedIntensity.value > 0f);

        public bool IsTileCompatible() => false;
    }
}
