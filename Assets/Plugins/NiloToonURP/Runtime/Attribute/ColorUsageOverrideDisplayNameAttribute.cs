/* 
 * example code:
 *  [ColorUsageOverrideDisplayNameAttribute("new short name", false, true)] // make sure it is placed after other attributes
    public float _ExtremeLongUnreadableName = 1;
*/

using System;
using UnityEngine;

namespace NiloToon.NiloToonURP
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class ColorUsageOverrideDisplayNameAttribute : PropertyAttribute
    {
        public string newDisplayName;
        public bool showAlpha;
        public bool hdr;

        public ColorUsageOverrideDisplayNameAttribute(string newDisplayName, bool showAlpha = true, bool hdr = false)
        {
            this.newDisplayName = newDisplayName;
            this.showAlpha = showAlpha;
            this.hdr = hdr;
        }
    }
}
