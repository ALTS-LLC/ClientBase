using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NiloToon.NiloToonURP
{
    [System.Serializable, VolumeComponentMenu("NiloToon/Rendering Performance Control (NiloToon)")]
    public class NiloToonRenderingPerformanceControlVolume : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Turn Off will improve rendering performance, but rim light result will be different")]
        public BoolParameter overrideEnableDepthTextureRimLigthAndShadow = new BoolParameter(true);

        public bool IsActive() => true;

        public bool IsTileCompatible() => false;
    }
}
