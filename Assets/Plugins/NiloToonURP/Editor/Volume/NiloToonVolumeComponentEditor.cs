#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using System.Reflection;
using UnityEditor;
using UnityEngine.Rendering;

namespace NiloToon.NiloToonURP
{
    public abstract class NiloToonVolumeComponentEditor<T> : VolumeComponentEditor where T : VolumeComponent
    {
        protected const string NonPostProcess_NotAffectPerformance_Message = 
            "- This volume is NOT postprocess.\n" +
            "  Won't affected by Camera's 'Post Processing' toggle.\n" +
            "- Settings here won't affect performance(fps).";
        
        protected const string NonPostProcess_MayAffectPerformance_Message = 
            "- This volume is NOT postprocess.\n" +
            "  Won't affected by Camera's 'Post Processing' toggle.\n" +
            "- Settings here may affect performance(fps).";
        
        protected const string IsPostProcessMessage = 
            "- This volume IS postprocess.\n" +
            "  Affected by Camera's 'Post Processing' toggle.\n" +
            "- Rendering this will have performance(fps) cost.";
        
        private Dictionary<string, SerializedDataParameter> parameters = new();
        private Dictionary<string, string> displayNameOverride = new();

        // Define an abstract method for child class to provide the help box content
        protected abstract List<string> GetHelpBoxContent();
        
        private static string UnityFormatDisplayName(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;

            // Replace underscore with space
            input = input.Replace("_", " ");
    
            // Insert a space before each uppercase letter that is either
            // preceded by a lowercase letter or followed by a lowercase letter
            string result = System.Text.RegularExpressions.Regex.Replace(input, "(?<=[a-z])([A-Z])|([A-Z])(?=[a-z])", " $1$2");

            // Make the first letter of the result uppercase
            return char.ToUpper(result[0]) + result.Substring(1);
        }
        
        public override void OnEnable()
        {
            var fields = typeof(T).GetFields(
                BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance);

            foreach (var field in fields)
            {
                if (field.FieldType.IsSubclassOf(typeof(VolumeParameter)))
                {
                    string displayName = UnityFormatDisplayName(field.Name); // Default to the formatted field name

                    // Check for [OverrideDisplayName("NewName")] attribute in VolumeComponent
                    // if found, override the display name
                    var overrideNameAttr = field.GetCustomAttribute<OverrideDisplayNameAttribute>();
                    if (overrideNameAttr != null)
                    {
                        displayName = overrideNameAttr.newDisplayName;
                    }

                    parameters[field.Name] = Unpack(serializedObject.FindProperty(field.Name));
                    displayNameOverride[field.Name] = displayName;
                }
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawShowHelpBox();

            bool newShowOverrideOnly = DrawShowOverrideOnly();
            
            DrawHorizontalLine();

            DrawAllProperties(newShowOverrideOnly);

            serializedObject.ApplyModifiedProperties();
        }

        private static void DrawHorizontalLine()
        {
            EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        }

        private void DrawAllProperties(bool newShowOverrideOnly)
        {
            foreach (var parameter in parameters)
            {
                // If the "Show Override Only?" toggle is on and the parameter isn't overridden, skip it
                if (newShowOverrideOnly && !parameter.Value.overrideState.boolValue) continue;

                string key = parameter.Key;
                string overridenDisplayName = displayNameOverride[key];
                PropertyField(parameter.Value, new GUIContent(overridenDisplayName));
            }
        }

        private bool DrawShowOverrideOnly()
        {
            // Draw "Show Override Only?" toggle
            bool showOverrideOnly = EditorPrefs.GetBool(NiloToonVolumeComponentEditorConstants.ShowOverrideOnlyKey, false);
            bool newShowOverrideOnly = EditorGUILayout.Toggle("Show Override Only?", showOverrideOnly);
            if (showOverrideOnly != newShowOverrideOnly)
            {
                EditorPrefs.SetBool(NiloToonVolumeComponentEditorConstants.ShowOverrideOnlyKey, newShowOverrideOnly);
            }

            // Draw a warning box if "Show Override Only?" is true
            if (newShowOverrideOnly)
            {
                EditorGUILayout.HelpBox(
                    "Non-override properties are hidden. Only overridden properties are displayed. (due to 'Show Override Only?' is on)",
                    MessageType.Warning);
            }

            return newShowOverrideOnly;
        }

        private void DrawShowHelpBox()
        {
            // Draw a toggle to control the visibility of the HelpBox
            bool showHelpBox = EditorPrefs.GetBool(NiloToonVolumeComponentEditorConstants.ShowHelpBoxKey, true);
            bool newShowHelpBox = EditorGUILayout.Toggle("Show Help Box?", showHelpBox);
            if (showHelpBox != newShowHelpBox)
            {
                EditorPrefs.SetBool(NiloToonVolumeComponentEditorConstants.ShowHelpBoxKey, newShowHelpBox);
            }

            // Only draw the HelpBox if the toggle is on
            if (newShowHelpBox)
            {
                foreach (var message in GetHelpBoxContent())
                {
                    if (!string.IsNullOrEmpty(message))
                    {
                        EditorGUILayout.HelpBox(message, MessageType.Info);
                    }
                }
            }
        }
    }

    public class NiloToonEditorUtilities
    {
        [InitializeOnLoadMethod]
        static void ResetToggles()
        {
            // it is better to reset on Unity editor start, since it is what user expected (same as other volume)
            EditorPrefs.SetBool(NiloToonVolumeComponentEditorConstants.ShowOverrideOnlyKey, false);
            EditorPrefs.SetBool(NiloToonVolumeComponentEditorConstants.ShowHelpBoxKey, false);
        }
    }

    public static class NiloToonVolumeComponentEditorConstants
    {
        // Add a new EditorPrefs key for the HelpBox visibility toggle
        public const string ShowHelpBoxKey = "NiloToonVolume_ShowHelpBox";

        // Add a new EditorPrefs key for the ShowOverrideOnly visibility toggle
        public const string ShowOverrideOnlyKey = "NiloToonVolume_ShowOverrideOnly";
    }
}
#endif