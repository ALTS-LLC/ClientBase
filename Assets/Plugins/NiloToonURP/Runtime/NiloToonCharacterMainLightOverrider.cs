using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace NiloToon.NiloToonURP
{
    [DisallowMultipleComponent]
    [ExecuteAlways]
    public class NiloToonCharacterMainLightOverrider : MonoBehaviour
    {
        private static List<NiloToonCharacterMainLightOverrider> allNiloToonCharacterMainLightOverriders;

        public enum OverrideTiming
        {
            BeforeVolumeOverride = 0,
            AfterVolumeOverride = 1,
            AfterEverything = 100,
        }

        [Header("OverrideTiming")]
        [OverrideDisplayName("Timing")]
        public OverrideTiming overrideTiming = OverrideTiming.BeforeVolumeOverride;
        
        [Header("Direction")]
        [OverrideDisplayName("Override?")]
        public bool overrideDirection = true;

        [Header("Color & Intensity")]
        [OverrideDisplayName("Override?")]
        public bool overrideColorAndIntensity = true;
        [DisableIf("overrideColorAndIntensity")]
        [OverrideDisplayName("    Color")]
        public Color color = Color.white;
        [DisableIf("overrideColorAndIntensity")]
        [OverrideDisplayName("    Intensity")]
        public float intensity = 1;

        [Header("Priority")]
        /// <summary>
        /// The Overrider priority. A higher value means higher priority. This supports negative values.
        /// </summary>
        [Tooltip("When multiple active overriders with the same 'Timing' exist in scene, NiloToon uses this value to determine which overrider to use. The overrider with the highest Priority value will be used, other overriders will be ignored.")]
        public float priority = 0;

        /// <summary>
        /// A color that combined color with intensity, it is the final Light.color for shader use. Similar to VisibleLight's finalColor
        /// </summary>
        public Color finalColor => color * intensity;

        /// <summary>
        /// A direction that look to the light, it is the Light.direction for shader use
        /// </summary>
        public Vector3 shaderLightDirectionWS => -transform.forward;

        /// <summary>
        /// return NiloToon's highest priority main light overrider that matches input 'timing', can return null if not exist
        /// </summary>
        public static NiloToonCharacterMainLightOverrider GetHighestPriorityOverrider(OverrideTiming timing)
        {
            NiloToonCharacterMainLightOverrider mainLightOverrider = null;
            if(allNiloToonCharacterMainLightOverriders != null)
            {
                foreach (var currentOverrider in allNiloToonCharacterMainLightOverriders)
                {
                    if(!currentOverrider) continue;
                    if(currentOverrider.overrideTiming != timing) continue;
                    
                    // when we don't have any overrider found yet, the first one enter here is the first overrider, so no need to check priority
                    if (!mainLightOverrider)
                    {
                        mainLightOverrider = currentOverrider;
                        continue;
                    }

                    if (currentOverrider.priority >= mainLightOverrider.priority)
                    {
                        mainLightOverrider = currentOverrider;
                    }
                }
            }
            return mainLightOverrider;
        }

        private void OnEnable()
        {
            // https://docs.unity3d.com/ScriptReference/ExecuteAlways.html
            // If a MonoBehaviour runs Play logic in Play Mode and fails to check if its GameObject is part of the playing world,
            // a Prefab being edited in Prefab Mode may incorrectly trigger logic intended only to be run as part of the game.
            if (!Application.isPlaying || Application.IsPlaying(gameObject))
            {
                AddToGlobalList();
            }
        }

        private void OnDisable()
        {
            RemoveFromGlobalList();
        }

        private void OnDestroy()
        {
            RemoveFromGlobalList();
        }

        private void OnValidate()
        {
            intensity = Mathf.Max(0, intensity);
        }

#if UNITY_EDITOR
        void OnDrawGizmosSelected()
        {
            // [Draw a Gizmo similar to Unity's directional light]

            // gizmo color
            float H, S, V;
            Color.RGBToHSV(color, out H, out S, out V);
            V = 1; // set V to 1
            Gizmos.color = Color.HSVToRGB(H, S, V);

            // Scale the gizmo size on its size on screen is constant
            float scaler = HandleUtility.GetHandleSize(transform.position);
            float scaledRadius = 0.2f * scaler;
            float scaledLength = 1f * scaler;

            const int circleSegments = 64;
            Vector3 prevPos = transform.position + transform.right * scaledRadius;

            for (int i = 1; i <= circleSegments; i++)
            {
                float angle = Mathf.PI * 2.0f * i / circleSegments;
                Vector3 newPos = transform.position + transform.right * Mathf.Cos(angle) * scaledRadius - transform.up * Mathf.Sin(angle) * scaledRadius;
                Gizmos.DrawLine(prevPos, newPos);
                if (i % 8 == 0)
                {
                    Gizmos.DrawLine(newPos, newPos + transform.forward * scaledLength);
                }
                prevPos = newPos;
            }
        }
#endif

        private void AddToGlobalList()
        {
            // Lazy init
            if (allNiloToonCharacterMainLightOverriders == null) 
                allNiloToonCharacterMainLightOverriders = new List<NiloToonCharacterMainLightOverrider>();

            allNiloToonCharacterMainLightOverriders.Add(this);
        }

        private void RemoveFromGlobalList()
        {
            allNiloToonCharacterMainLightOverriders?.Remove(this);
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(NiloToonCharacterMainLightOverrider))]
    [CanEditMultipleObjects]
    public class NiloToonCharacterMainLightOverriderUI : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
    "This script can override main directional light's 'direction & color' only for NiloToon character shader.\n\n" +
            "Use it when:\n" +
            "The regular URP main directional light is used for lighting the environment,\n" +
            "and you want control the directional light's 'direction & color' just for NiloToon character shader.\n\n" +
            "In this situation, simply treat this script as a directional light for NiloToon character shader.\n" +
            "*You only need 1 regular directional light and 1 NiloToonCharacterMainLightOverrider; you don't need any extra directional lights, Light layers, or Rendering layers.", MessageType.Info);
            
            EditorGUILayout.HelpBox(
    "You can create this script in any of the following ways:\n" +
            "- [menu]GameObject>Light>NiloToon\n" +
            "- [menu]GameObject>Create Other>NiloToon\n" +
            "- manually add this script to any GameObject", MessageType.Info);
            
            EditorGUILayout.HelpBox(
    "To keep URP rendering any main directional light,\n"+
            "you must enable your scene's regular main directional light and keep it's intensity larger than 0 !", MessageType.Info);
            
            DrawDefaultInspector();
        }
    }
#endif  
    
}