using UnityEngine;
using UnityEditor;

using System;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace NiloToon.NiloToonURP
{
    [CustomPropertyDrawer(typeof(OverrideDisplayNameAttribute))]
    public class OverrideDisplayNameAttributeDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            OverrideDisplayNameAttribute a = attribute as OverrideDisplayNameAttribute;
            label.text = a.newDisplayName;

            EditorGUI.PropertyField(position, property, label, true);
        }
    }

    // copy and edit of : https://github.com/Unity-Technologies/UnityCsReference/blob/master/Editor/Mono/ScriptAttributeGUI/Implementations/PropertyDrawers.cs
    [CustomPropertyDrawer(typeof(ColorUsageOverrideDisplayNameAttribute))]
    internal sealed class ColorUsageOverrideDisplayNameDrawer : PropertyDrawer
    {
        private static string s_InvalidTypeMessage = L10n.Tr("Use ColorUsageDrawer with color.");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var colorUsage = (ColorUsageOverrideDisplayNameAttribute)attribute;
            label.text = colorUsage.newDisplayName; // <-------------------------------------- ADDED

            if (property.propertyType == SerializedPropertyType.Color)
            {
                label = EditorGUI.BeginProperty(position, label, property);
                EditorGUI.BeginChangeCheck();
                Color newColor = EditorGUI.ColorField(position, label, property.colorValue, true, colorUsage.showAlpha, colorUsage.hdr);
                if (EditorGUI.EndChangeCheck())
                {
                    property.colorValue = newColor;
                }
                EditorGUI.EndProperty();
            }
            else
            {
                EditorGUI.ColorField(position, label, property.colorValue, true, colorUsage.showAlpha, colorUsage.hdr);
            }
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            if (property.propertyType == SerializedPropertyType.Color)
            {
                var colorUsage = (ColorUsageOverrideDisplayNameAttribute)attribute;
                var field = new ColorField(property.displayName);
                field.showAlpha = colorUsage.showAlpha;
                field.hdr = colorUsage.hdr;
                field.bindingPath = property.propertyPath;
                field.AddToClassList("ColorUsageOverrideDisplayNameDrawer"); // <-------------------------------------- EDITED
                return field;
            }

            return new Label(s_InvalidTypeMessage);
        }
    }


    [CustomPropertyDrawer(typeof(RangeOverrideDisplayNameAttribute))]
    internal sealed class RangeOverrideDisplayNameDrawer : PropertyDrawer
    {
        private static string s_InvalidTypeMessage = L10n.Tr("Use Range with float or int.");

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var range = (RangeOverrideDisplayNameAttribute)attribute;
            label.text = range.newDisplayName; // <-------------------------------------- ADDED

            if (property.propertyType == SerializedPropertyType.Float)
                EditorGUI.Slider(position, property, range.min, range.max, label);
            else if (property.propertyType == SerializedPropertyType.Integer)
                EditorGUI.IntSlider(position, property, (int)range.min, (int)range.max, label);
            else
                EditorGUI.LabelField(position, label.text, s_InvalidTypeMessage);
        }

        public override VisualElement CreatePropertyGUI(SerializedProperty property)
        {
            var range = (RangeOverrideDisplayNameAttribute)attribute;

            if (property.propertyType == SerializedPropertyType.Float)
            {
                var slider = new Slider(property.displayName, range.min, range.max);
                slider.AddToClassList("RangeOverrideDisplayNameDrawer"); // <-------------------------------------- EDITED
                slider.bindingPath = property.propertyPath;
                slider.showInputField = true;
                return slider;
            }
            else if (property.propertyType == SerializedPropertyType.Integer)
            {
                var intSlider = new SliderInt(property.displayName, (int)range.min, (int)range.max);
                intSlider.AddToClassList("RangeOverrideDisplayNameDrawer"); // <-------------------------------------- EDITED
                intSlider.bindingPath = property.propertyPath;
                intSlider.showInputField = true;
                return intSlider;
            }

            return new Label(s_InvalidTypeMessage);
        }
    }
}