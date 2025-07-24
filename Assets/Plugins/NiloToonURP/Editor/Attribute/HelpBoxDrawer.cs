using UnityEditor;
using UnityEngine;

namespace NiloToon.NiloToonURP
{
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(HelpBoxAttribute))]
    public class HelpBoxDrawer : DecoratorDrawer
    {
        private float cachedCurrentViewWidth = -1;

        public override void OnGUI(Rect position)
        {
            // Cache the current view width when OnGUI is called
            cachedCurrentViewWidth = EditorGUIUtility.currentViewWidth;

            HelpBoxAttribute helpBoxAttr = attribute as HelpBoxAttribute;
            if (helpBoxAttr != null)
            {
                position.height = GetHelpBoxHeight(helpBoxAttr.Text, position.width);
                EditorGUI.HelpBox(position, helpBoxAttr.Text, helpBoxAttr.Type);
            }
        }

        public override float GetHeight()
        {
            HelpBoxAttribute helpBoxAttr = attribute as HelpBoxAttribute;
            if (helpBoxAttr != null)
            {
                // Use the cached current view width instead of accessing it directly
                float width = cachedCurrentViewWidth;
                return GetHelpBoxHeight(helpBoxAttr.Text, width) + 5; // Extra padding
            }
            return base.GetHeight();
        }

        private static float GetHelpBoxHeight(string text, float width)
        {
            GUIStyle style = "helpbox";
            return style.CalcHeight(new GUIContent(text), width);
        }
    }
#endif
}