using System.Collections.Generic;
using NiloToon.NiloToonURP;
using UnityEditor;
using UnityEditor.Rendering;

namespace NiloToon.NiloToonURP
{
    [CanEditMultipleObjects]
#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(NiloToonShadowControlVolume))]
#else
    [VolumeComponentEditor(typeof(NiloToonShadowControlVolume))]
#endif
    public class NiloToonShadowControlVolumeEditor : NiloToonVolumeComponentEditor<NiloToonShadowControlVolume>
    {
        // Override GetHelpBoxContent to provide specific help box content
        protected override List<string> GetHelpBoxContent()
        {
            List<string> messages = new List<string>();
            
            messages.Add(
                "- If overridden, will use settings here instead of NiloToonAllInOneRendererFeature.\n" +
                    "- If not overridden, will use NiloToonAllInOneRendererFeature's settings.");
            
            messages.Add(NonPostProcess_MayAffectPerformance_Message);
            
            return messages;
        }
    }
}