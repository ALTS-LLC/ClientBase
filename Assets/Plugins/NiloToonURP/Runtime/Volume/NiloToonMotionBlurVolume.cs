using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NiloToon.NiloToonURP
{
    [System.Serializable, VolumeComponentMenu("NiloToon/Motion Blur (NiloToon)")]
    public class NiloToonMotionBlurVolume : VolumeComponent, IPostProcessComponent
    {
        [Header("Master control")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 2);
        
        public bool IsActive() => intensity.value > 0f;

        public bool IsTileCompatible() => false;
    }
}

