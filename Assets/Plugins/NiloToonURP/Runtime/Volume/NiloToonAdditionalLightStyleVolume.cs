using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace NiloToon.NiloToonURP
{
    [System.Serializable, VolumeComponentMenu("NiloToon/Additional Light Style (NiloToon)")]
    public class NiloToonAdditionalLightStyleVolume : VolumeComponent, IPostProcessComponent
    {
        [Header("'Inject into Main Light' - Amount")]
        [OverrideDisplayName("Color")]
        // should keep at 1 by default, inorder to make multiple directional lights sum works no matter which one is the mainlight
        public ClampedFloatParameter additionalLightInjectIntoMainLightColor_Strength = new ClampedFloatParameter(1f,0,1); 
        [OverrideDisplayName("Direction")]
        public ClampedFloatParameter additionalLightInjectIntoMainLightDirection_Strength = new ClampedFloatParameter(1f,0,1);
        
        [Header("'Inject into Main Light' - Style")]
        [OverrideDisplayName("Desaturate")]
        // should keep at 0 by default, inorder to make multiple directional lights sum works no matter which one is the mainlight
        public ClampedFloatParameter additionalLightInjectIntoMainLightColor_Desaturate = new ClampedFloatParameter(0.0f, 0, 1);
        [OverrideDisplayName("Close light as white?")]
        [Tooltip("This option allows you to clamp additional light's distanceAttenuation when merging them into main light")]
        public ClampedFloatParameter additionalLightInjectIntoMainLightColor_AllowCloseLightOverBright = new ClampedFloatParameter(1,0,1);
        public bool IsActive() => true;

        public bool IsTileCompatible() => false;
    }
}
