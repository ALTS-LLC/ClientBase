// copy and edit of "URP adding a RenderFeature from script"
// https://forum.unity.com/threads/urp-adding-a-renderfeature-from-script.1117060/#post-9009526

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace NiloToon.NiloToonURP
{
    public static class SetupRenderFeatures
    {
        const string NiloToon_AutoTriggerAutoInstallTriggeredAlready_Key = "NiloToon_AutoTriggerAutoInstallTriggeredAlready_Key";

        [InitializeOnLoadMethod]
        static void InitOnScriptReloadOrEditorRestart()
        {
            if (!SessionState.GetBool(NiloToon_AutoTriggerAutoInstallTriggeredAlready_Key, false))
            {
                SessionState.SetBool(NiloToon_AutoTriggerAutoInstallTriggeredAlready_Key, true);
                EditorApplication.delayCall += SetupWithoutLog;
            }
        }

        [MenuItem("Window/NiloToonURP/Auto Install RendererFeatures", priority = 40)]
        static void RunAutoInstallOnce()
        {
            EditorApplication.delayCall += Setup;
        }

        static void SetupWithoutLog() => Setup(false);
        static void Setup() => Setup(true);
        static void Setup(bool showLog)
        {
            var renderersThatDontHaveNiloToonRendererFeature =
                GetRenderersThatDontHaveTargetRendererFeature<NiloToonAllInOneRendererFeature>();
            if (renderersThatDontHaveNiloToonRendererFeature.Count == 0)
            {
                if (showLog)
                {
                    if (EditorUtility.DisplayDialog("NiloToon Auto-Installation",
                            "All required NiloToon renderer features have already been added to all active renderers. No auto-installation is needed.",
                            "Ok"))
                    {
                        // User clicked "Ok"
                    }
                }
                
                return;
            }

            string message =
                "NiloToonURP is about to automatically add renderer features to the following renderers, would you like to proceed?\n\n";
            foreach (var renderer in renderersThatDontHaveNiloToonRendererFeature)
            {
                message += $"- {AssetDatabase.GetAssetPath(renderer)}\n";
            }
            if (EditorUtility.DisplayDialog("NiloToonURP Auto Install", message, "Yes (recommended)", "NO"))
            {
                AddRendererFeature<NiloToonAllInOneRendererFeature>();
            }
        }

        private static List<ScriptableRendererData> GetRenderersThatDontHaveTargetRendererFeature<T>()
        {
            void ProcessRPAsset(RenderPipelineAsset inputAsset, List<ScriptableRendererData> handledDataObjects, List<ScriptableRendererData> missingFeatureData)
            {
                var asset = inputAsset as UniversalRenderPipelineAsset;
                
                // if asset is BIRP/HDRP, skip it
                if (!asset) return;

                var data = getDefaultRenderer(asset);

                // This is needed in case multiple renderers share the same renderer data object.
                // If they do then we only handle it once.
                if (handledDataObjects.Contains(data))
                {
                    return;
                }

                handledDataObjects.Add(data);

                // Check if the feature already exists
                bool found = false;
                foreach (var feature in data.rendererFeatures)
                {
                    if (feature is T)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    // If the feature not found in this renderer, add it to the list
                    missingFeatureData.Add(data);
                }
            }

            var handledDataObjects = new List<ScriptableRendererData>();
            var missingFeatureData = new List<ScriptableRendererData>();
    
            // find renderers from "Quality" tab
            int levels = QualitySettings.names.Length;
            for (int level = 0; level < levels; level++)
            {
                ProcessRPAsset(QualitySettings.GetRenderPipelineAssetAt(level), handledDataObjects, missingFeatureData);
            }
            
            // find renderer from "Graphics" tab
            ProcessRPAsset(GraphicsSettings.defaultRenderPipeline, handledDataObjects, missingFeatureData);
            
            // Return the list of renderers missing the NiloToon feature
            return missingFeatureData;
        }

        static void AddRendererFeature<T>() where T : ScriptableRendererFeature
        {
            // search
            var renderersThatDontHaveNiloToonRendererFeature =
                GetRenderersThatDontHaveTargetRendererFeature<NiloToonAllInOneRendererFeature>();

            // add NiloToon to that renderer
            foreach (var rendererData in renderersThatDontHaveNiloToonRendererFeature)
            {
                // Create the feature
                var feature = ScriptableObject.CreateInstance<T>();
                feature.name = typeof(T).Name;

                // Add it to the renderer data.
                addRenderFeature(rendererData, feature);

                // Log the path of the renderer data asset
                string assetPath = AssetDatabase.GetAssetPath(rendererData);
                Debug.Log("[NiloToon] Auto added renderer feature '" + feature.name + "' to " + rendererData.name + ". Asset path: " + assetPath, rendererData);
            }
        }
 
        /// <summary>
        /// Gets the default renderer index.
        /// Thanks to: https://forum.unity.com/threads/urp-adding-a-renderfeature-from-script.1117060/#post-7184455
        /// </summary>
        /// <param name="asset"></param>
        /// <returns></returns>
        static int getDefaultRendererIndex(UniversalRenderPipelineAsset asset)
        {
            return (int)typeof(UniversalRenderPipelineAsset).GetField("m_DefaultRendererIndex", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(asset);
        }
 
        /// <summary>
        /// Gets the renderer from the current pipeline asset that's marked as default.
        /// Thanks to: https://forum.unity.com/threads/urp-adding-a-renderfeature-from-script.1117060/#post-7184455
        /// </summary>
        /// <returns></returns>
        static ScriptableRendererData getDefaultRenderer(UniversalRenderPipelineAsset asset)
        {
            if (asset)
            {
                ScriptableRendererData[] rendererDataList = (ScriptableRendererData[])typeof(UniversalRenderPipelineAsset)
                        .GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance)
                        .GetValue(asset);
                int defaultRendererIndex = getDefaultRendererIndex(asset);
 
                return rendererDataList[defaultRendererIndex];
            }
            else
            {
                Debug.LogError("No Universal Render Pipeline is currently active.");
                return null;
            }
        }
 
        /// <summary>
        /// Based on Unity add feature code.
        /// See: AddComponent() in https://github.com/Unity-Technologies/Graphics/blob/d0473769091ff202422ad13b7b764c7b6a7ef0be/com.unity.render-pipelines.universal/Editor/ScriptableRendererDataEditor.cs#180
        /// </summary>
        /// <param name="data"></param>
        /// <param name="feature"></param>
        static void addRenderFeature(ScriptableRendererData data, ScriptableRendererFeature feature)
        {
            // Let's mirror what Unity does.
            var serializedObject = new SerializedObject(data);
 
            var renderFeaturesProp = serializedObject.FindProperty("m_RendererFeatures"); // Let's hope they don't change these.
            var renderFeaturesMapProp = serializedObject.FindProperty("m_RendererFeatureMap");
 
            serializedObject.Update();
 
            // Store this new effect as a sub-asset so we can reference it safely afterwards.
            // Only when we're not dealing with an instantiated asset
            if (EditorUtility.IsPersistent(data))
                AssetDatabase.AddObjectToAsset(feature, data);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out var guid, out long localId);
 
            // Grow the list first, then add - that's how serialized lists work in Unity
            renderFeaturesProp.arraySize++;
            var componentProp = renderFeaturesProp.GetArrayElementAtIndex(renderFeaturesProp.arraySize - 1);
            componentProp.objectReferenceValue = feature;
 
            // Update GUID Map
            renderFeaturesMapProp.arraySize++;
            var guidProp = renderFeaturesMapProp.GetArrayElementAtIndex(renderFeaturesMapProp.arraySize - 1);
            guidProp.longValue = localId;
 
            // Force save / refresh
            if (EditorUtility.IsPersistent(data))
            {
                AssetDatabase.SaveAssetIfDirty(data);
            }
 
            serializedObject.ApplyModifiedProperties();
        }
    }
}
 