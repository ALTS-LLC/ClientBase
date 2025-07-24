using System.Collections.Generic;
using NiloToon.NiloToonURP;
using UnityEditor;
using UnityEditor.Rendering;

namespace NiloToon.NiloToonURP
{
    [CanEditMultipleObjects]
#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(NiloToonAnimePostProcessVolume))]
#else
    [VolumeComponentEditor(typeof(NiloToonAnimePostProcessVolume))]
#endif
    public class NiloToonAnimePostProcessVolumeEditor : NiloToonVolumeComponentEditor<NiloToonAnimePostProcessVolume>
    {
        // Override GetHelpBoxContent to provide specific help box content
        protected override List<string> GetHelpBoxContent()
        {
            List<string> messages = new List<string>();
            
            messages.Add(
                "- Draw a brightened gradient ('Screen' blendmode) on the top side of the screen\n" +
                    "- Draw a darkened gradient ('Multiply' blendmode) on the bottom side of the screen");
            messages.Add(IsPostProcessMessage);
            
            return messages;
        }
    }
}