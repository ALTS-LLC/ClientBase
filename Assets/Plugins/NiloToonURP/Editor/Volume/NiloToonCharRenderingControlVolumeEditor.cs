using System.Collections.Generic;
using NiloToon.NiloToonURP;
using UnityEditor;
using UnityEditor.Rendering;

namespace NiloToon.NiloToonURP
{
    [CanEditMultipleObjects]
#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(NiloToonCharRenderingControlVolume))]
#else
    [VolumeComponentEditor(typeof(NiloToonCharRenderingControlVolume))]
#endif
    public class NiloToonCharRenderingControlVolumeEditor : NiloToonVolumeComponentEditor<NiloToonCharRenderingControlVolume>
    {
        // Override GetHelpBoxContent to provide specific help box content
        protected override List<string> GetHelpBoxContent()
        {
            List<string> messages = new List<string>();
            
            messages.Add(
                "- Great for helping NiloToon characters blend into any environment\n" +
                "- Great for artist-controlled lighting and rim light on NiloToon characters");
            
            messages.Add(NonPostProcess_NotAffectPerformance_Message);
            
            return messages;
        }
    }
}