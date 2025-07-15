using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

using UnityEditor;
using UnityEditor.Rendering;
using UnityEditor.Rendering.Universal;
using System.Linq;

namespace NiloToon.NiloToonURP
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(NiloToonPerCharacterRenderController))]

    public class NiloToonEditorPerCharacterRenderControllerCustomEditor : FoldoutEditor
    {
        public override void OnInspectorGUI()
        {
            var t = (target as NiloToonPerCharacterRenderController);

            // ForceUpdateOnce() is extremely slow, so we only call ForceUpdateOnce() when required instead of calling ForceUpdateOnce() every frame
            if (t.requireUpdateCount > 0)
            {
                ForceUpdateOnce();
                t.requireUpdateCount--;
            }

            GUILayout.Space(5);
            DrawAutoSetupButton(t);
            GUILayout.Space(5);
            DiscardAndRefillAllRenderersList(t);
            GUILayout.Space(5);
            DrawSelectAllNiloToonCharacterMaterialsButton(t);
            GUILayout.Space(5);

            RenderMessage(t);

            ///////////////////////////////////////////////////
            // draw original
            ///////////////////////////////////////////////////
            base.OnInspectorGUI();
        }

        void ForceUpdateOnce()
        {
            var t = (target as NiloToonPerCharacterRenderController);

            ReimportSingleCharIfNeeded(t);

            // No longer force update all active char since NiloToon 0.16.0, it is just too slow in a scene that with many characters
            /*
            // brute force: force all active character in scene to update
            // (API FindObjectsByType only exist in 2022.3 or higher)
#if UNITY_2022_3_OR_NEWER
            foreach (var script in FindObjectsByType<NiloToonPerCharacterRenderController>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))          
#else
            foreach (var script in FindObjectsOfType<NiloToonPerCharacterRenderController>())
#endif
            {
                ReimportSingleCharIfNeeded(script);
            }
            */
            

            NiloToonEditor_ReimportAllAssetFilteredByLabel.DeleteAllTempMeshAssetCloneWithCanDeletePrefix(); // auto clean up project

            //Debug.Log($"[NiloToonURP] {t.gameObject.name} updated");
        }

        void ReimportSingleCharIfNeeded(NiloToonPerCharacterRenderController t)
        {
            AutoAssignLabelToAllMeshsOfSingleChar(t);
            AutoReimportIfMeshDontHaveUV8(t);
        }

        static void AutoReimportIfMeshDontHaveUV8(NiloToonPerCharacterRenderController perCharScript)
        {
            if (perCharScript.reimportDone) return;

            foreach (var renderer in perCharScript.allRenderers)
            {
                if (renderer == null) continue;

                Mesh mesh = null;
                switch (renderer)
                {
                    case MeshRenderer mr:
                        MeshFilter mf = mr.GetComponent<MeshFilter>();
                        if(!mf) continue;
                        mesh = mf.sharedMesh; 
                        break;
                    case SkinnedMeshRenderer smr: mesh = smr.sharedMesh; ; break;
                    default:
                        break; // do nothing if not a supported renderer(e.g. particle system's renderer)
                }

                if (mesh == null) continue;
                if (mesh.vertexCount == 0) continue; // it is possible to have valid mesh with 0 vertex, if we don't handle it, we will enter dead loop since uv8.Length == 0 
                
                // if uv8 is not the correct smooth normal data, AddAssetLabelAndReimport
                if (mesh.uv8 == null || mesh.uv8.Length == 0)
                {
                    NiloToonEditor_AssetLabelAssetPostProcessor.AddAssetLabelAndReimport(new UnityEngine.Object[] { mesh }, false); // use false = always reimport
                    perCharScript.reimportDone = true;
                }
            }
        }

        public static void AutoAssignLabelToAllMeshsOfSingleChar(NiloToonPerCharacterRenderController perCharScript, bool shouldSkipIfLabelExist = true)
        {
            foreach (var renderer in perCharScript.allRenderers)
            {
                if(!renderer) continue;
                
                Mesh mesh = null;
                switch (renderer)
                {
                    case MeshRenderer mr:
                        MeshFilter mf = mr.GetComponent<MeshFilter>();
                        if(!mf) continue;
                        mesh = mf.sharedMesh; 
                        break;
                    case SkinnedMeshRenderer smr: mesh = smr.sharedMesh; break;
                    default:
                        break; // do nothing if not a supported renderer(e.g. particle system's renderer)
                }
                
                if(!mesh) continue;
                
                NiloToonEditor_AssetLabelAssetPostProcessor.AddAssetLabelAndReimport(new UnityEngine.Object[] { mesh }, shouldSkipIfLabelExist);
            }
        }

        private void DrawAutoSetupButton(NiloToonPerCharacterRenderController perCharScript)
        {
            if (GUILayout.Button("Auto setup this character"))
            {
                var allNiloScripts = GetAllSelectedNiloToonPerCharacterRenderController();
                
                // also with the script that user click on if not exist
                if (!allNiloScripts.Contains(perCharScript))
                {
                    allNiloScripts.Add(perCharScript);
                }
                
                foreach (var niloScript in allNiloScripts)
                {
                    AutoSetupCharacterGameObject(niloScript);
                }
            }
        }

        private static List<NiloToonPerCharacterRenderController> GetAllSelectedNiloToonPerCharacterRenderController()
        {
            List<NiloToonPerCharacterRenderController> allNiloControllers = new List<NiloToonPerCharacterRenderController>();

            foreach (var obj in Selection.objects)
            {
                NiloToonPerCharacterRenderController script = (obj as GameObject)?.GetComponent<NiloToonPerCharacterRenderController>();
                if (script != null)
                {
                    allNiloControllers.Add(script);
                }
            }

            return allNiloControllers;
        }

        public static void AutoSetupCharacterGameObject(GameObject gameobject)
        {
            var charScript = gameobject.GetComponent<NiloToonPerCharacterRenderController>();
            if (!charScript)
            {
                charScript = gameobject.AddComponent<NiloToonPerCharacterRenderController>();
            }

            AutoSetupCharacterGameObject(charScript);
        }
        public static void AutoSetupCharacterGameObject(NiloToonPerCharacterRenderController perCharScript)
        {
            if (!perCharScript)
            {
                Debug.Log($"[NiloToon]Auto setup character rejected, " +
                          $"because the game object({perCharScript.gameObject.name}) is a generated prefab(e.g. fbx prefab) and can only be modified through an AssetPostprocessor. " +
                          "You can try drag it into a scene and try the auto setup again.");
                return;
            }
            perCharScript.RefillAllRenderers();

            bool shouldEditMaterial = EditorUtility.DisplayDialog(
                $"Auto setup character - {perCharScript.gameObject.name}",
                "Set up NiloToon's script on this character completed.\n\n" +
                "Do you want to convert the materials to NiloToon also?\n" +
                "(convert from lilToon/MToon/UniUnlit/URP Lit/URP Unlit to NiloToon_Character)",
                "Yes, convert the materials to NiloToon.",
                "No, DO NOT edit any materials."
            );

            if (shouldEditMaterial)
            {
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // collect all unique materials used by this character (don't care what the material's shader is)
                ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
                // use a HashSet to store only the unique materials of the character
                HashSet<Material> allTargetMaterialsHashSet = new HashSet<Material>();
                foreach (var renderer in perCharScript.allRenderers)
                {
                    if (renderer)
                    {
                        foreach (var mat in renderer.sharedMaterials)
                        {
                            if (mat)
                            {
                                allTargetMaterialsHashSet.Add(mat);
                            }
                            else
                            {
                                // it is ok to have null materials, so not log a warning now
                                //Debug.LogWarning($"NiloToon: null material slot found in renderer ({renderer.gameObject.name}).", renderer.gameObject);
                            }
                        } 
                    }
                    else
                    {
                        Debug.LogWarning($"NiloToon: null renderer found in NiloToonPerCharacterRenderController ({perCharScript.gameObject.name}).", perCharScript.gameObject);
                    }
                }

                // convert HashSet to a list to maintain consistent material order
                List<Material> allTargetMaterials = new List<Material>(allTargetMaterialsHashSet);

                NiloToonMaterialConvertor.AutoConvertMaterialsToNiloToon(allTargetMaterials);
            }
            AssetDatabase.SaveAssets();

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // NiloToonPerCharacterRenderController script settings
            // (Won't ask user Yes/No, since it is always better to apply this change)
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////       
            perCharScript.AutoFillInMissingProperties();
            
            // after headbone set, try auto set FaceForwardDirection and FaceUpDirection
            perCharScript.AutoFillInFaceForwardDirAndFaceUpDir();

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Set up completed, exit
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

            // let unity knows our edit
            EditorUtility.SetDirty(perCharScript);

            // force update twice (1 is not enough, need 2, not sure why)
            perCharScript.requireUpdateCount = 2;

            //Debug.Log($"NiloToon: Auto setup {perCharScript.gameObject.name} completed.");
        }
        
        static void DiscardAndRefillAllRenderersList(NiloToonPerCharacterRenderController t)
        {
            GUIContent buttonContent = new GUIContent(
                "Auto refill AllRenderers list",
                "Clicking this button will discard allRenderers list, and auto refill it correctly.\n" +
                "(each child NiloToonPerCharacterRenderController will do this also)\n\n" +
                "Useful when you face some problem related to allRenderers list.\n" +
                "(e.g. you have a complex structure that has multiple child and parent NiloToonPerCharacterRenderController(s),\n" +
                "and you are not sure if all allRenderers are correct or not,\n" +
                "then you can click this button to let NiloToon reset all allRenderers lists correctly.)\n\n" +
                "*Click this button is the same as calling API -> RefillAllRenderers()");
            if (GUILayout.Button(buttonContent))
            {
                t.RefillAllRenderers();

                // let unity knows our edit
                EditorUtility.SetDirty(t);

                Debug.Log($"NiloToon: Auto refill AllRenderers list -> {t.gameObject.name} completed.");
            }
        }
        static void DrawSelectAllNiloToonCharacterMaterialsButton(NiloToonPerCharacterRenderController t)
        {
            GUIContent buttonContent = new GUIContent(
                "Select all Nilo materials of this char",
                "Clicking this button will loop through:\n" +
                "- AllRenderers\n" +
                "- AttachmentRendererList\n" +
                "- NilotoonRendererRedrawerList\n" +
                "of this script, and select every material that are using NiloToon_Character shader.\n\n" +
                "Useful when you need to select all materials that are using NiloToon_Character shader for editing/checking together.\n" +
                "(e.g. changing outline or rimlight width / view modified properties of all nilo materials together)");

            if (GUILayout.Button(buttonContent))
            {
                HashSet<Material> selectionMaterialList = new HashSet<Material>();

                Shader niloToonCharacterShader = Shader.Find("Universal Render Pipeline/NiloToon/NiloToon_Character");

                List<NiloToonPerCharacterRenderController> allNiloControllers = GetAllSelectedNiloToonPerCharacterRenderController();
                
                // also with the script that user click on
                allNiloControllers.Add(t);

                foreach (var niloScript in allNiloControllers)
                {
                    var targetRenderers = niloScript.allRenderers.Concat(niloScript.attachmentRendererList);
                    foreach (var renderer in targetRenderers)
                    {
                        if (!renderer) continue;

                        foreach (var material in renderer.sharedMaterials)
                        {
                            if (!material) continue;

                            if (material.shader == niloToonCharacterShader)
                            {
                                selectionMaterialList.Add(material);
                            }
                        }
                    }
                    foreach (var redrawScript in niloScript.nilotoonRendererRedrawerList)
                    {
                        if (!redrawScript) continue;
                        if (redrawScript.DrawList == null) continue;

                        foreach (var drawData in redrawScript.DrawList)
                        {
                            if (drawData == null) continue;

                            Material material = drawData.sharedMaterial;
                            if (!material) continue;

                            if (material.shader == niloToonCharacterShader)
                            {
                                selectionMaterialList.Add(material);
                            }
                        }
                    }
                }
                
                Selection.objects = selectionMaterialList.ToArray();
            }
        }

        static void RenderMessage(NiloToonPerCharacterRenderController t)
        {
            if (Selection.gameObjects.Length == 1)
            {
                bool isSetupCorrect = true;

                if (!t.headBoneTransform)
                {
                    EditorGUILayout.HelpBox("'Head' is missing!\n\n" +
                        "For character, inside 'Head & Face' section, you should assign 'Head' with a head bone transform.\n" +
                        "Doing this can let nilo shader process 'Face Normal fix' and 'Perspective Removal' correctly.\n\n" +
                        "*You can ignore this warning if this is not a character(e.g. weapon, microphone...)", MessageType.Warning);
                    isSetupCorrect = false;
                }

                if (t.useCustomCharacterBoundCenter && !t.customCharacterBoundCenter)
                {
                    EditorGUILayout.HelpBox("'Custom Bound Center' is missing!\n\n" +
                        "Inside 'Bounding Sphere' section, you should assign 'Custom Bound Center' with a hip / pelvis bone.\n" +
                        "Doing this can make CPU much faster, else this script will rebuild realtime character bound center every frame to find character's center position, which is slower.", MessageType.Warning);
                    isSetupCorrect = false;
                }

                if (isSetupCorrect)
                {
                    GUIStyle style = new GUIStyle();
                    style.normal.textColor = Color.green;
                    EditorGUILayout.LabelField("Status: All setup correct", style);
                }
            }
            else
            {
                EditorGUILayout.HelpBox("Selecting multi objects, message hidden now", MessageType.None);
            }

            EditorGUILayout.HelpBox(
                "If you see (PlayMode) after a feature's name,\n" +
                "it means it can only affect PlayMode,\n" +
                "and requires meeting the follow conditions:\n" +
                "- NiloToon's debug window's \"Keep PlayMode mat edit?\" = off\n" +
                "- This script's MaterialInstanceMode = PlayModeOnly"
                , MessageType.Info);
        }
    }

    public class NiloToonEditorAutoSetupCharacter
    {
#if UNITY_EDITOR
        [MenuItem("Window/NiloToonURP/[GameObject] Convert Selected GameObjects to NiloToon", priority = 1)]
        [MenuItem("Assets/NiloToon/[GameObject] Convert Selected GameObjects to NiloToon", priority = 1100 + 1)]
        public static void SetupSelectedCharacterGameObject()
        {
            var allselectedObjects = Selection.objects;
            foreach (var selectedObject in allselectedObjects)
            {
                if (selectedObject is GameObject gameobject)
                {
                    NiloToonEditorPerCharacterRenderControllerCustomEditor.AutoSetupCharacterGameObject((GameObject)selectedObject);
                }
            }
        }

        [MenuItem("Window/NiloToonURP/[GameObject] Convert Selected GameObjects to NiloToon", priority = 1, validate = true)]
        [MenuItem("Assets/NiloToon/[GameObject] Convert Selected GameObjects to NiloToon", priority = 1100 + 1, validate = true)]
        public static bool ValidateSetupSelectedCharacterGameObject()
        {
            var allselectedObjects = Selection.objects;
            return allselectedObjects.All(selectedObject => selectedObject is GameObject);
        }
#endif
    }
}


