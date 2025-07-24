using UnityEngine;
using UnityEditor;

namespace NiloToon.NiloToonURP
{
    [CustomPropertyDrawer(typeof(DisableIfAttribute))]
    public class DisableIfDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            DisableIfAttribute disableIf = (DisableIfAttribute)attribute;

            SerializedProperty conditionField = property.serializedObject.FindProperty(disableIf.ConditionFieldName);
            if (conditionField != null && conditionField.boolValue == disableIf.Condition)
            {
                EditorGUI.BeginDisabledGroup(true);
            }

            EditorGUI.PropertyField(position, property, label, true);

            if (conditionField != null && conditionField.boolValue == disableIf.Condition)
            {
                EditorGUI.EndDisabledGroup();
            }
        }
    }
}