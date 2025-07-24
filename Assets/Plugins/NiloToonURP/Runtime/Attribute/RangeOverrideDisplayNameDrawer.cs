/* 
 * example code:
 *  [RangeOverrideDisplayNameAttribute("new short name", 0, 1)] // make sure it is placed after other attributes
    public float _ExtremeLongUnreadableName = 1;
*/

using System;
using UnityEngine;

namespace NiloToon.NiloToonURP
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class RangeOverrideDisplayNameAttribute : PropertyAttribute
    {
        public string newDisplayName;
        public float min;
        public float max;

        public RangeOverrideDisplayNameAttribute(string newDisplayName, float min, float max)
        {
            this.newDisplayName = newDisplayName;
            this.min = min;
            this.max = max;
        }
    }
}
