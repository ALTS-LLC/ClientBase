using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NiloToon.NiloToonURP
{
    [System.Serializable, VolumeComponentMenu("NiloToon/Char Rendering Control (NiloToon)")]
    public class NiloToonCharRenderingControlVolume : VolumeComponent, IPostProcessComponent
    {
        [Header("Base Color")]
        [Tooltip("You can lower the value to keep character not over bright. (won't affect rim light & specular)")]
        [OverrideDisplayName("Brightness")]
        public MinFloatParameter charBaseColorMultiply = new MinFloatParameter(1, 0);
        [Tooltip("You can tint a color to keep character not over bright. (won't affect rim light & specular)")]
        [OverrideDisplayName("Tint Color")]
        public ColorParameter charBaseColorTintColor = new ColorParameter(Color.white, true, false, true);

        [Header("Result Color")]
        [Tooltip("You can lower the value to keep character not over bright. (affects rim light & specular)")]
        [OverrideDisplayName("Brightness")]
        public MinFloatParameter charMulColorIntensity = new MinFloatParameter(1, 0);
        [Tooltip("You can tint a color to keep character not over bright. (affects rim light & specular)")]
        [OverrideDisplayName("Tint Color")]
        public ColorParameter charMulColor = new ColorParameter(Color.white, true, false, true);

        [Header("Replace to Color (fake fog)")]
        [OverrideDisplayName("Strength")]
        public ClampedFloatParameter charLerpColorUsage = new ClampedFloatParameter(0, 0, 1);
        [OverrideDisplayName("    Color")]
        public ColorParameter charLerpColor = new ColorParameter(new Color(1, 1, 1, 0), true, false, true);

        [Header("Occlusion Map")]
        [OverrideDisplayName("Strength")]
        public ClampedFloatParameter charOcclusionUsage = new ClampedFloatParameter(1, 0, 1);

        [Header("Indirect Light")]
        [Tooltip("Intensity multiplier of scene's light probe for character. If you don't want the character be affected by any light probe (e.g., you baked a very bright light probe in scene), set to a lower value or even 0.")]
        [OverrideDisplayName("Intensity")]
        public MinFloatParameter charIndirectLightMultiplier = new MinFloatParameter(1, 0);
        [Tooltip("A minimum indirect light color when no active light in scene, to prevent character rendering becomes completely black. You can change to a brighter color if light probe doesn't exist in scene.")]
        [OverrideDisplayName("Min")]
        public ColorParameter charIndirectLightMinColor = new ColorParameter(new Color(0.01f, 0.01f, 0.01f, 0), false, false, true);
        [Tooltip("The final maximum indirect light color on character allowed, you can use it for clamping indirect light's final color for character.")] 
        [OverrideDisplayName("Max")]
        public ColorParameter charIndirectLightMaxColor = new ColorParameter(Color.gray, true, false, true);

        [Header("Main Directional Light")]
        [Tooltip("Multiplier of scene's main light intensity for character.")]
        [OverrideDisplayName("Intensity")]
        public MinFloatParameter mainDirectionalLightIntensityMultiplier = new MinFloatParameter(1f, 0);
        [OverrideDisplayName("Tint Color")]
        public ColorParameter mainDirectionalLightIntensityMultiplierColor = new ColorParameter(Color.white, true, false, true);
        [OverrideDisplayName("Add Color")]
        public ColorParameter addMainLightColor = new ColorParameter(Color.black, true, false, true);
        [Tooltip("Set it lower can avoid character over exposure")]
        [OverrideDisplayName("Intensity Clamp")]
        public MinFloatParameter mainDirectionalLightMaxContribution = new MinFloatParameter(1f, 0);
        [OverrideDisplayName("Color Clamp")]
        public ColorParameter mainDirectionalLightMaxContributionColor = new ColorParameter(Color.white, true, false, true);

        [Header("Override Main Light Direction")]
        [OverrideDisplayName("Strength")]
        public ClampedFloatParameter overrideLightDirectionIntensity = new ClampedFloatParameter(0, 0, 1);
        [OverrideDisplayName("    UpDownAngle")]
        public ClampedFloatParameter overridedLightUpDownAngle = new ClampedFloatParameter(-45f, -180f, 180f);
        [OverrideDisplayName("    LRAngle")]
        public ClampedFloatParameter overridedLightLRAngle = new ClampedFloatParameter(0f, -180f, 180f);

        [Header("Override Main Light Color")]
        [OverrideDisplayName("Strength")]
        public ClampedFloatParameter lightColorOverrideStrength = new ClampedFloatParameter(0, 0, 1);
        [OverrideDisplayName("    Color")]
        public ColorParameter overridedLightColor = new ColorParameter(Color.white, true, false, true);
        [OverrideDisplayName("    Desaturate Light?")]
        public ClampedFloatParameter desaturateLightColor = new ClampedFloatParameter(0, 0, 1);

        [Header("Additional Light - Intensity")]
        [OverrideDisplayName("Intensity")]
        public MinFloatParameter additionalLightIntensityMultiplier = new MinFloatParameter(1f, 0);
        [OverrideDisplayName("Tint Color")]
        public ColorParameter additionalLightIntensityMultiplierColor = new ColorParameter(Color.white, true, false, true);
        [OverrideDisplayName("Intensity (Face)")]
        public MinFloatParameter additionalLightIntensityMultiplierForFaceArea = new MinFloatParameter(1f, 0);
        [OverrideDisplayName("Tint Color (Face)")]
        public ColorParameter additionalLightIntensityMultiplierColorForFaceArea = new ColorParameter(Color.white, true, false, true);
        [OverrideDisplayName("Intensity (Outline)")]
        public MinFloatParameter additionalLightIntensityMultiplierForOutlineArea = new MinFloatParameter(1f, 0);
        [OverrideDisplayName("Tint Color (Outline)")]
        public ColorParameter additionalLightIntensityMultiplierColorForOutlineArea = new ColorParameter(Color.white, true, false, true);

        [Header("Additional Light - Rim mask")]
        [OverrideDisplayName("Strength")]
        public ClampedFloatParameter additionalLightApplyRimMask = new ClampedFloatParameter(0, 0, 1);
        [OverrideDisplayName("    Fresnel Power")]
        public MinFloatParameter additionalLightRimMaskPower = new MinFloatParameter(1f, 0.1f);
        [OverrideDisplayName("    Softness")]
        public ClampedFloatParameter additionalLightRimMaskSoftness = new ClampedFloatParameter(1, 0, 1);

        [Header("Additional Light - Clamp")]
        [Tooltip("Set it lower can avoid character over exposure")]
        [OverrideDisplayName("Intensity Clamp")]
        public MinFloatParameter additionalLightMaxContribution = new MinFloatParameter(1f, 0);
        [OverrideDisplayName("Color Clamp")]
        public ColorParameter additionalLightMaxContributionColor = new ColorParameter(Color.white, true, false, true);

        [Header("Specular")]
        [OverrideDisplayName("Intensity")]
        public MinFloatParameter specularIntensityMultiplier = new MinFloatParameter(1f, 0);
        [OverrideDisplayName("Tint Color")]
        public ColorParameter specularTintColor = new ColorParameter(new Color(1, 1, 1, 0), true, false, true);
        [OverrideDisplayName("Min intensity in shadow")]
        public MinFloatParameter specularInShadowMinIntensity = new MinFloatParameter(0.25f, 0);
        [OverrideDisplayName("React to light dir")]
        [Tooltip("you can set it to false to produce a specular similar to matcap specular\n" +
                 "\n" +
                 "Default: true")]
        public BoolParameter specularReactToLightDirectionChange = new BoolParameter(true);

        [Header("2D Rim Light + Shadow")]
        [OverrideDisplayName("Width")]
        public MinFloatParameter depthTextureRimLightAndShadowWidthMultiplier = new MinFloatParameter(1f, 0);

        [Header("2D Rim Light")]
        [OverrideDisplayName("Intensity")]
        public MinFloatParameter charRimLightMultiplier = new MinFloatParameter(1, 0);
        [OverrideDisplayName("Tint Color")]
        public ColorParameter charRimLightTintColor = new ColorParameter(new Color(1, 1, 1, 0), true, false, true);
        [OverrideDisplayName("Intensity (Outline)")]
        public MinFloatParameter charRimLightMultiplierForOutlineArea = new MinFloatParameter(0, 0);
        [OverrideDisplayName("Tint Color (Outline)")]
        public ColorParameter charRimLightTintColorForOutlineArea = new ColorParameter(new Color(1, 1, 1, 0), true, false, true);
        [Tooltip("- A positive bias will make 2D rim light occlude by nearby objects easier (= removing inner 2D rim light), great for producing a clean silhouette-only rim light\n" +
                 "- A negative bias will produce more inner 2D rim light, which may not be desired\n" +
                 "\n" +
                 "Default: 0")]
        [OverrideDisplayName("Occlusion bias")]
        public FloatParameter depthTexRimLightDepthDiffThresholdOffset = new FloatParameter(0);
        [OverrideDisplayName("Fade start range")]
        [Tooltip("Defines the distance (in meters) from the camera at which the 2D rim light begins to fade out.\n" +
                 "\n" +
                 "Default: 1000(meter)")]
        public MinFloatParameter charRimLightCameraDistanceFadeoutStartDistance = new MinFloatParameter(1000, 0);
        [OverrideDisplayName("Fade end range")]
        [Tooltip("Defines the distance (in meters) from the camera at which the 2D rim light completes fading out\n" +
                 "\n" +
                 "Default: 2000(meter)")]
        public MinFloatParameter charRimLightCameraDistanceFadeoutEndDistance = new MinFloatParameter(2000, 0);

        [Header("Shadow")]
        [OverrideDisplayName("Strength")]
        public ClampedFloatParameter characterOverallShadowStrength = new ClampedFloatParameter(1, 0, 2);
        [OverrideDisplayName("Tint Color")]
        public ColorParameter characterOverallShadowTintColor = new ColorParameter(new Color(1, 1, 1, 0), true, false, true);
        
        [Header("Classic Outline")]
        [OverrideDisplayName("Width")]
        public MinFloatParameter charOutlineWidthMultiplier = new MinFloatParameter(1, 0);
        [OverrideDisplayName("Width (XR)")]
        public MinFloatParameter charOutlineWidthExtraMultiplierForXR = new MinFloatParameter(0.5f, 0); // VR default smaller outline, due to high FOV
        [OverrideDisplayName("Auto Width?")]
        [Tooltip("Should outline width auto adjust to camera distance and FOV? If you want outline width to be constant in world space, override this to 0.")]
        public ClampedFloatParameter charOutlineWidthAutoAdjustToCameraDistanceAndFOV = new ClampedFloatParameter(1, 0, 1);
        [OverrideDisplayName("Tint Color")]
        public ColorParameter charOutlineMulColor = new ColorParameter(Color.white, true, false, true);

        public bool IsActive()
        {
            return true;
        }

        public bool IsTileCompatible()
        {
            return false;
        }
    }
}

