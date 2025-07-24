using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NiloToon.NiloToonURP
{
    public enum TonemappingMode
    {
        None,
        Neutral,    // Neutral tonemapper
        ACES,       // ACES Filmic reference tonemapper (custom approximation)
        GT,// [NiloToon added] GranTurismo tonemap (a.k.a GT Tonemap)
        
        [InspectorName("ACES (Custom SR)")]
        ACES_CustomSR, // [NiloToon added]
        
        [InspectorName("Khronos PBR Neutral")]
        KhronosPBRNeutral,
        
        [InspectorName("Nilo NPR Neutral V1 (experimental)")]
        NiloNPRNeutralV1,
        
        [InspectorName("Nilo Hybird ACES (experimental)")]
        NiloHybirdACES
    }

    [Serializable, VolumeComponentMenuForRenderPipeline("NiloToon/Tonemapping (NiloToon)", typeof(UniversalRenderPipeline))]
    public class NiloToonTonemappingVolume : VolumeComponent, IPostProcessComponent
    {
        [Tooltip("Select a tonemapping algorithm to use for the color grading process.\n" +
                 "*You must disable URP's Tonemapping if you use this tonemapping!")]
        public TonemappingModeParameter mode = new TonemappingModeParameter(TonemappingMode.None);
        
        [Header("Intensity (Tonemap)")]
        [Tooltip("Apply tonemap?")]
        [OverrideDisplayName("Master")]
        public ClampedFloatParameter intensity = new ClampedFloatParameter(1, 0, 1);
        
        [Tooltip("Apply tonemap to character pixels?\n\n" +
                 "In most cases apply tonemapping to character pixels will produce dull and gray color, which is undesired, so the default tonemapping intensity for character area is 0.\n" +
                 "If character is too bright when compared to environment, use 'Pre-Tonemap brightness' for adjustment instead, it will produce much nicer character color.")]
        [OverrideDisplayName("-   Chara")]
        public ClampedFloatParameter ApplyToCharacter = new ClampedFloatParameter(0, 0, 1);
        [Tooltip("Apply tonemap to non-character pixels?")]
        [OverrideDisplayName("-   Non Chara")]
        public ClampedFloatParameter ApplyToNonCharacter = new ClampedFloatParameter(1, 0, 1);
        
        [Header("Pre-Tonemap brightness")]
        [Tooltip("Character area brightness multiplier (just before applying tonemapping)")]
        [OverrideDisplayName("-   Chara")]
        public ClampedFloatParameter BrightnessMulForCharacter = new ClampedFloatParameter(1, 0, 4);
        [Tooltip("Non-Character area brightness multiplier (just before applying tonemapping)")]
        [OverrideDisplayName("-   Non Chara")]
        public ClampedFloatParameter BrightnessMulForNonCharacter = new ClampedFloatParameter(1, 0, 4);
        
        public bool IsActive() => (mode.value != TonemappingMode.None && intensity.value > 0) ||
                                  (BrightnessMulForCharacter.value != 1) ||
                                  (BrightnessMulForNonCharacter.value != 1);

        public bool IsTileCompatible() => true;
    }

    [Serializable]
    public sealed class TonemappingModeParameter : VolumeParameter<TonemappingMode> { public TonemappingModeParameter(TonemappingMode value, bool overrideState = false) : base(value, overrideState) { } }
}