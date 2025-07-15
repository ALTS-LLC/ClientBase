using System.Collections.Generic;
using NiloToon.NiloToonURP;
using UnityEditor;
using UnityEditor.Rendering;

namespace NiloToon.NiloToonURP
{
    [CanEditMultipleObjects]
#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(NiloToonTonemappingVolume))]
#else
    [VolumeComponentEditor(typeof(NiloToonTonemappingVolume))]
#endif
    public class NiloToonTonemappingVolumeEditor : NiloToonVolumeComponentEditor<NiloToonTonemappingVolume>
    {
        // Override GetHelpBoxContent to provide specific help box content
        protected override List<string> GetHelpBoxContent()
        {
            List<string> messages = new List<string>();
            
            messages.Add(
                "Use this if you want to apply tonemap without affecting character pixels, as it can often cause character colors to appear dull and gray.\n" +
                    "* If used, you should disable URP's tonemapping.\n" +
                "* If used, recommend using NiloToonBloom instead of URP's Bloom for Tonemap order correctness.");
            
            messages.Add(IsPostProcessMessage);
            
            return messages;
        }
    }
}