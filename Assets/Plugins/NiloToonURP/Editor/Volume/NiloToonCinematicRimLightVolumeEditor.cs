using System.Collections.Generic;
using NiloToon.NiloToonURP;
using UnityEditor;
using UnityEditor.Rendering;

namespace NiloToon.NiloToonURP
{
    [CanEditMultipleObjects]
#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(NiloToonCinematicRimLightVolume))]
#else
    [VolumeComponentEditor(typeof(NiloToonCinematicRimLightVolume))]
#endif
    public class NiloToonCinematicRimLightVolumeEditor : NiloToonVolumeComponentEditor<NiloToonCinematicRimLightVolume>
    {
        // Override GetHelpBoxContent to provide specific help box content
        protected override List<string> GetHelpBoxContent()
        {
            List<string> messages = new List<string>();

            messages.Add(
                "Make NiloToon characters convert all received additional light into rim light.\n" +
                "Useful if you want characters to receive many bright additional lights,\n" +
                "yet maintain a pleasing lighting effect, without becoming overly bright or unappealing.\n" +
                "(e.g., ideal for concert stage live performances or cinematic cut scenes)"
            );
            
            messages.Add(NonPostProcess_NotAffectPerformance_Message);
            
            return messages;
        }
    }
}