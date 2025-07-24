/* 
 * example code:
 *  [OverrideDisplayNameAttribute("new short name")] // make sure it is placed after other attributes
    public float _ExtremeLongUnreadableName = 1;
*/

using System;
using UnityEngine;

namespace NiloToon.NiloToonURP
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class OverrideDisplayNameAttribute : PropertyAttribute
    {
        public string newDisplayName;
        public OverrideDisplayNameAttribute(string newDisplayName)
        {
            this.newDisplayName = newDisplayName;
        }
    }
}