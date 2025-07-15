using System.Collections.Generic;
using NiloToon.NiloToonURP;
using UnityEditor;
using UnityEditor.Rendering;

namespace NiloToon.NiloToonURP
{
    [CanEditMultipleObjects]
#if UNITY_2022_2_OR_NEWER
    [CustomEditor(typeof(NiloToonAdditionalLightStyleVolume))]
#else
    [VolumeComponentEditor(typeof(NiloToonAdditionalLightStyleVolume))]
#endif
    public class NiloToonAdditionalLightStyleVolumeEditor : NiloToonVolumeComponentEditor<NiloToonAdditionalLightStyleVolume>
    {
        // Override GetHelpBoxContent to provide specific help box content
        protected override List<string> GetHelpBoxContent()
        {
            List<string> messages = new List<string>();
            
            messages.Add(
            "Default values means using a new style of additional light introduced since NiloToon 0.16.0.\n" +
                "The new style will merge color & direction from all additional lights into main light, instead of using the old style(additive blending of shading color from each additional light),\n" +
                "The new style is best when you want additional light to lit the character same as main directional light.\n" +
                "- If you like the new default additional light style, you can disable/remove this volume and tune settings in NiloToonCinematicRimLight volume.\n" +
                "- If you want to produce a result similar to the old style(NiloToon 0.15.x or below) to avoid major breaking change, you can:\n" +
                "1.enable this volume and set color & direction to 0.\n" +
                "2.set 'auto fix unsafe style' to false in NiloToonCinematicRimLight volume\n" +
                "* If additional light on character is over bright, you should tune settings in NiloToonCinematicRimLight volume");
            
            return messages;
        }
    }
}