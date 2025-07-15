#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace NiloToon.NiloToonURP
{
    [CustomEditor(typeof(NiloToonVolumePresetPicker))]
    public class NiloToonVolumePresetPickerEditor : Editor
    {
        SerializedProperty currentIndex;
        SerializedProperty weight;
        SerializedProperty mode;
        SerializedProperty priority;
        SerializedProperty volumeProfiles;

        private void OnEnable()
        {
            currentIndex = serializedObject.FindProperty("_currentIndex");
            weight = serializedObject.FindProperty("weight");
            mode = serializedObject.FindProperty("mode");
            priority = serializedObject.FindProperty("priority");
            volumeProfiles = serializedObject.FindProperty("volumeProfiles");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            NiloToonVolumePresetPicker volumeProfilePicker = (NiloToonVolumePresetPicker)target;

            EditorGUILayout.HelpBox(
                "1. Pick a Volume ID (001 & 002 are good start).\n" +
                "2. Clone the volume profile as your own asset.\n"+
                "3. After cloning, unpack completely this prefab and remove this script.\n" +
                "(Don't directly modify NiloToon's profile; changes are lost in future updates.)\n"+
                "(NiloToon's profile may change in future updates, if you don't want to be affected, always do step2+3)", MessageType.Info);

            EditorGUILayout.LabelField("Which volume?", EditorStyles.boldLabel);
            EditorGUI.BeginChangeCheck();  // Start tracking changes
            currentIndex.intValue = EditorGUILayout.IntSlider("Volume ID", currentIndex.intValue, 0, volumeProfilePicker.volumeProfiles.Count - 1);
            if (EditorGUI.EndChangeCheck()) {
                EditorUtility.SetDirty(target);  // Mark the object as dirty when changes are made
            }

            // Display the name of the child at the current index
            EditorGUILayout.LabelField(volumeProfilePicker.volumeProfiles[currentIndex.intValue].name);

            // Add the buttons here
            if (GUILayout.Button("Enable Next"))
            {
                volumeProfilePicker.EnableNext();
                EditorUtility.SetDirty(target);  // Mark the object as dirty
            }
            if (GUILayout.Button("Enable Previous"))
            {
                volumeProfilePicker.EnablePrevious();
                EditorUtility.SetDirty(target);  // Mark the object as dirty
            }

            EditorGUILayout.Space();  // Add some space between the sections

            EditorGUILayout.LabelField("Volume settings", EditorStyles.boldLabel);
            weight.floatValue = EditorGUILayout.Slider("Weight", weight.floatValue, 0, 1);
            mode.enumValueIndex = (int)(VolumeMode)EditorGUILayout.EnumPopup("Mode", (VolumeMode)mode.enumValueIndex);
            priority.intValue = EditorGUILayout.IntField("Priority", priority.intValue);
            
            EditorGUILayout.Space();  // Add some space between the sections
            
            EditorGUILayout.LabelField("Internal Volume profiles", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(volumeProfiles);
            
            serializedObject.ApplyModifiedProperties();
        }

        static void CreateNiloToonVolumePresetPicker(MenuCommand menuCommand)
        {
            // Make sure the prefab is located in a Resources folder
            string prefabPath = "NiloToonVolumePresetPicker";

            GameObject prefab = Resources.Load(prefabPath, typeof(GameObject)) as GameObject;
            if (prefab != null)
            {
                GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                if (instance != null)
                {
                    GameObjectUtility.SetParentAndAlign(instance, menuCommand.context as GameObject);
                    Undo.RegisterCreatedObjectUndo(instance, "Create " + instance.name);
                    Selection.activeObject = instance;
                }
                else
                {
                    Debug.LogError("Failed to instantiate prefab: " + prefabPath);
                }
            }
            else
            {
                Debug.LogError("Failed to load prefab at path: " + prefabPath);
            }
        }

        [MenuItem("GameObject/Volume/NiloToon/VolumePresetPicker", false, 10)]
        static void CreateNiloToonVolumePresetPicker_PathA(MenuCommand menuCommand)
        {
            CreateNiloToonVolumePresetPicker(menuCommand);
        }
        [MenuItem("GameObject/Create Other/NiloToon/VolumePresetPicker", false, 10)]
        static void CreateNiloToonVolumePresetPicker_PathB(MenuCommand menuCommand)
        {
            CreateNiloToonVolumePresetPicker(menuCommand);
        }
    }
}
#endif