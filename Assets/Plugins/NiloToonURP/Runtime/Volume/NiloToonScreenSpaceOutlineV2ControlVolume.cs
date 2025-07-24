using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace NiloToon.NiloToonURP
{
    [System.Serializable, VolumeComponentMenu("NiloToon/Hidden/Screen Space Outline V2 (NiloToon)")]
    public class NiloToonScreenSpaceOutlineV2ControlVolume : VolumeComponent, IPostProcessComponent
    {
        [Header("WIP code. It will not trigger rendering, don't use this volume now.")]
        [Header("--------------------------")]
        [Header("Require enable ScreenSpace outline in NiloToonAllInOne renderer feature in order for this volume to become effective.")]
        [Header("*In Camera, turn on Temporal Anti-aliasing(TAA) is critical for screen space outline to look stable when camera moves.")]
        [Header("--------------------------")]
        [Header("Intensity")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(0, 0, 1);
        public ClampedFloatParameter intensityForCharacter = new ClampedFloatParameter(1, 0, 1);
        public ClampedFloatParameter intensityForEnvironment = new ClampedFloatParameter(1, 0, 1);

        [Header("Width")]
        public ClampedFloatParameter widthMultiplier = new ClampedFloatParameter(1, 0, 10f);
        public ClampedFloatParameter widthMultiplierForCharacter = new ClampedFloatParameter(1, 0, 10f);
        public ClampedFloatParameter widthMultiplierForEnvironment = new ClampedFloatParameter(1, 0, 10f);
        public ClampedFloatParameter extraWidthMultiplierForXR = new ClampedFloatParameter(0.2f, 0, 10f);

        [Header("Tint Color")]
        public ColorParameter outlineTintColor = new ColorParameter(Color.white, false, false, true);
        public ColorParameter outlineTintColorForChar = new ColorParameter(Color.white, false, false, true);
        public ColorParameter outlineTintColorForEnvi = new ColorParameter(Color.white, false, false, true);
        
        [Header("Edge: Geometry Edge")] 
        public BoolParameter drawGeometryEdgeOutline = new BoolParameter(true);
        public BoolParameter drawGeometryEdgeOutlineForChar = new BoolParameter(true);
        public BoolParameter drawGeometryEdgeOutlineForEnvi = new BoolParameter(true);
        public ClampedFloatParameter geometryEdgeThreshold = new ClampedFloatParameter(1, 0, 100f);
        public ClampedFloatParameter geometryEdgeThresholdForCharacter = new ClampedFloatParameter(1, 0f, 100f);
        public ClampedFloatParameter geometryEdgeThresholdForEnvironment = new ClampedFloatParameter(1, 0f, 100f);

        [Header("Edge: Normal Angle")] 
        public BoolParameter drawNormalAngleOutline = new BoolParameter(true);
        public BoolParameter drawNormalAngleOutlineForChar = new BoolParameter(true);
        public BoolParameter drawNormalAngleOutlineForEnvi = new BoolParameter(true);
        public ClampedFloatParameter normalAngleMin = new ClampedFloatParameter(40, 0, 180);
        public ClampedFloatParameter normalAngleMinForCharacter = new ClampedFloatParameter(40, 0, 180f);
        public ClampedFloatParameter normalAngleMinForEnvironment = new ClampedFloatParameter(40, 0, 180f);
        public ClampedFloatParameter normalAngleMax = new ClampedFloatParameter(180, 0, 180);
        public ClampedFloatParameter normalAngleMaxForCharacter = new ClampedFloatParameter(180, 0, 180f);
        public ClampedFloatParameter normalAngleMaxForEnvironment = new ClampedFloatParameter(180, 0, 180f);
        
        [Header("Edge: Material ID Boundary (Character only)")]
        public BoolParameter drawMaterialBoundaryOutline = new BoolParameter(true);

        [Header("Edge: Custom ID Boundary (Character only)")]
        public BoolParameter drawCustomIDBoundaryOutline = new BoolParameter(true);
        
        [Header("Edge: Wireframe")]
        public BoolParameter drawWireframeOutline = new BoolParameter(false);
        
        public bool IsActive() => 
            (intensity.value > 0.0f && widthMultiplier.value > 0.0f) && 
            (drawGeometryEdgeOutline.value || drawNormalAngleOutline.value || drawMaterialBoundaryOutline.value || drawCustomIDBoundaryOutline.value || drawWireframeOutline.value);

        public bool IsTileCompatible() => false;
    }
}

