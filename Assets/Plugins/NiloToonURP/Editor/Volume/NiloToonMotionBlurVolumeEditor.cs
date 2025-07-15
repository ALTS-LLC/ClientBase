using System.Collections.Generic;
using NiloToon.NiloToonURP;
using UnityEditor;
using UnityEditor.Rendering;

namespace NiloToon.NiloToonURP
{
    [CanEditMultipleObjects]
#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(NiloToonMotionBlurVolume))]
#else
    [VolumeComponentEditor(typeof(NiloToonMotionBlurVolume))]
#endif
    public class NiloToonMotionBlurVolumeEditor : NiloToonVolumeComponentEditor<NiloToonMotionBlurVolume>
    {
        // Override GetHelpBoxContent to provide specific help box content
        protected override List<string> GetHelpBoxContent()
        {
            List<string> messages = new List<string>();
            
            messages.Add(
                "[Experimental feature, requires Unity2022.3 or above]\n" +
                    "- Add a cinematic object motion blur, usually used for character dance animations in music videos (MVs). It is better than URP's object motion blur but comes with a much higher GPU cost.\n(Recommended for PC only)");
            messages.Add(IsPostProcessMessage);
            
            return messages;
        }
    }
}