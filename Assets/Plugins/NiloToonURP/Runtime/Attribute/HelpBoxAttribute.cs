using System;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace NiloToon.NiloToonURP
{
    [AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
    public class HelpBoxAttribute : PropertyAttribute
    {
        public string Text;
        
        // This field should only be used within the Unity Editor.
#if UNITY_EDITOR
        public MessageType Type;
#endif

        public HelpBoxAttribute(string text
#if UNITY_EDITOR
            , MessageType type = MessageType.Info
#endif
        )
        {
            Text = text;
#if UNITY_EDITOR
            Type = type;
#endif
        }
    }
}