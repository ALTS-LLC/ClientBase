using System.Collections.Generic;
using NiloToon.NiloToonURP;
using UnityEditor;
using UnityEditor.Rendering;

namespace NiloToon.NiloToonURP
{
    [CanEditMultipleObjects]
#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(NiloToonScreenSpaceOutlineV2ControlVolume))]
#else
    [VolumeComponentEditor(typeof(NiloToonScreenSpaceOutlineV2ControlVolume))]
#endif
    public class NiloToonScreenSpaceOutlineV2ControlVolumeEditor : NiloToonVolumeComponentEditor<NiloToonScreenSpaceOutlineV2ControlVolume>
    {
        // Override GetHelpBoxContent to provide specific help box content
        protected override List<string> GetHelpBoxContent()
        {
            List<string> messages = new List<string>();
            
            messages.Add(NonPostProcess_NotAffectPerformance_Message);
            
            return messages;
        }
    }
}