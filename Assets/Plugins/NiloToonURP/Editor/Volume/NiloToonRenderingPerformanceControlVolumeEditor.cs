using System.Collections.Generic;
using NiloToon.NiloToonURP;
using UnityEditor;
using UnityEditor.Rendering;

namespace NiloToon.NiloToonURP
{
    [CanEditMultipleObjects]
#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(NiloToonRenderingPerformanceControlVolume))]
#else
    [VolumeComponentEditor(typeof(NiloToonRenderingPerformanceControlVolume))]
#endif
    public class NiloToonRenderingPerformanceControlVolumeEditor : NiloToonVolumeComponentEditor<NiloToonRenderingPerformanceControlVolume>
    {
        // Override GetHelpBoxContent to provide specific help box content
        protected override List<string> GetHelpBoxContent()
        {
            List<string> messages = new List<string>();
            
            messages.Add(
            "Use for control rendering performance in different scene.\n" +
                "e.g., for 2D rim light&shadow,\n" +
            "- override to 'disable' in multi-character scene to boost performance\n" +
            "- override to 'enable' in single character scene to ensure quality");
            
            return messages;
        }
    }
}