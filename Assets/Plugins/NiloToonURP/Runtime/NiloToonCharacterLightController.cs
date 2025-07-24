using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Serialization;

namespace NiloToon.NiloToonURP
{
    [ExecuteAlways]
    public class NiloToonCharacterLightController : MonoBehaviour
    {
        internal static List<NiloToonCharacterLightController> AllNiloToonCharacterLightControllers = new();

        //-----------------------------------------------------------------------------
        [Header("Main Light - Tint Color")] 
        
        [OverrideDisplayName("Enable?")]
        public bool enableMainLightTintColor = false;
        
        [DisableIf("enableMainLightTintColor",false)]
        [OverrideDisplayName("    Color")]
        public Color mainLightTintColor = Color.white;
        
        [DisableIf("enableMainLightTintColor",false)]
        [OverrideDisplayName("    Intensity")]
        public float mainLightIntensityMultiplier = 1;
        
        //-----------------------------------------------------------------------------
        [Header("Main Light - Tint Color (by Light)")] 
        
        [OverrideDisplayName("Enable?")]
        public bool enableMainLightTintColorByLight = false;
        
        [DisableIf("enableMainLightTintColorByLight",false)]
        [OverrideDisplayName("    Strength")]
        [Range(0,1)]
        public float mainLightTintColorByLight_Strength = 1;
        
        [DisableIf("enableMainLightTintColorByLight",false)]
        [OverrideDisplayName("    From Light")]
        public Light mainLightTintColorByLight_Target = null;

        [DisableIf("enableMainLightTintColorByLight",false)]
        [OverrideDisplayName("        Desaturate")]
        [Range(0,1)]
        public float mainLightTintColorByLight_Desaturate = 0;
        
        //-----------------------------------------------------------------------------
        [Header("Main Light - Add Color")] 
        
        [OverrideDisplayName("Enable?")]
        public bool enableMainLightAddColor = false;
        
        [DisableIf("enableMainLightAddColor",false)]
        [OverrideDisplayName("    Color")]
        public Color mainLightAddColor = Color.black;
        
        [DisableIf("enableMainLightAddColor",false)]
        [OverrideDisplayName("    Intensity")]
        public float mainLightAddColorIntensity = 1;
        
        //-----------------------------------------------------------------------------
        [Header("Main Light - Add Color (by Light)")] 
        
        [OverrideDisplayName("Enable?")]
        public bool enableMainLightAddColorByLight = false;
        
        [DisableIf("enableMainLightAddColorByLight",false)]
        [OverrideDisplayName("    Strength")]
        [Range(0,1)]
        public float mainLightAddColorByLight_Strength = 1;
        
        [DisableIf("enableMainLightAddColorByLight",false)]
        [OverrideDisplayName("    From Light")]
        public Light mainLightAddColorByLight_Target = null;

        [DisableIf("enableMainLightAddColorByLight",false)]
        [OverrideDisplayName("        Desaturate")]
        [Range(0,1)]
        public float mainLightAddColorByLight_Desaturate = 0;

        //-----------------------------------------------------------------------------
        [Header("Character Mask")]
        [HelpBox("- When List is empty, this script will apply to all characters.\n" +
                 "- When List is not empty, this script will only apply to target characters in the list.")]
        [OverrideDisplayName("Enabled?")]
        public bool enableTargetCharacterMask = true;
        
        [OverrideDisplayName("    Mask")]
        public List<NiloToonPerCharacterRenderController> targetCharactersMask = new();
        
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
            mainLightIntensityMultiplier = Mathf.Max(0, mainLightIntensityMultiplier);
            mainLightAddColorIntensity = Mathf.Max(0, mainLightAddColorIntensity);
        }
        
        private void AddToGlobalList()
        {
            // Lazy init
            if (AllNiloToonCharacterLightControllers == null)
                AllNiloToonCharacterLightControllers = new List<NiloToonCharacterLightController>();

            AllNiloToonCharacterLightControllers.Add(this);
        }

        private void RemoveFromGlobalList()
        {
            AllNiloToonCharacterLightControllers?.Remove(this);
        }

        internal bool AffectsCharacter(int characterID)
        {
            if (!enableTargetCharacterMask || targetCharactersMask == null || targetCharactersMask.Count == 0) return true;

            foreach (var character in targetCharactersMask)
            {
                if(!character) continue;
                
                if (character.characterID == characterID) return true;
            }

            return false;
        }
    }
    
#if UNITY_EDITOR
    [CustomEditor(typeof(NiloToonCharacterLightController))]
    [CanEditMultipleObjects]
    public class NiloToonCharacterLightControllerUI : Editor
    {
        public override void OnInspectorGUI()
        {
            EditorGUILayout.HelpBox(
    "This script controls how NiloToon character(s) receive various kinds of lights.\n\n" +
            "All active NiloToonCharacterLightController(s) will be combined together behind the scene, then apply the final setting to each NiloToon character defining how they should receive lights.", MessageType.Info);
            
            DrawDefaultInspector();
        }
    }
#endif  
}