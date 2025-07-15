using System;
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Object = UnityEngine.Object;

namespace NiloToon.NiloToonURP
{
    public class NiloToonEditor_AutoGeneratePrefabVariantAndAssets : Editor
    {
        [MenuItem("Window/NiloToonURP/[Prefab] Create NiloToon Prefab Variant and Materials", priority = 0)]
        [MenuItem("Assets/NiloToon/[Prefab] Create NiloToon Prefab Variant and Materials", priority = 1100 + 0)]
        static void CreatePrefabVariantAndCloneMaterials()
        {
            // Get the selected prefab
            UnityEngine.Object selectedObject = Selection.activeObject;
            if (selectedObject == null)
            {
                EditorUtility.DisplayDialog("Error", "Please select a prefab.", "OK");
                return;
            }

            string assetPath = AssetDatabase.GetAssetPath(selectedObject);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("Error", "Selected object is not a prefab.", "OK");
                return;
            }

            // Ask the user where to save the prefab variant
            string selectedPath = AssetDatabase.GetAssetPath(selectedObject);
            string directory = Path.GetDirectoryName(selectedPath);
            string prefabName = prefab.name;
            string variantPath = EditorUtility.SaveFilePanelInProject(
                "Save Prefab Variant",
                prefabName + "(NiloToon)",
                "prefab",
                "Please enter a file name to save the prefab variant to",
                directory);

            if (string.IsNullOrEmpty(variantPath))
            {
                // User canceled
                return;
            }

            // Ensure the directory for the variant path exists
            string variantFolderPath = Path.GetDirectoryName(variantPath);
            if (string.IsNullOrEmpty(variantFolderPath))
            {
                Debug.LogError("Variant folder path is empty or invalid.");
                return;
            }

            variantFolderPath = variantFolderPath.Replace("\\", "/"); // Ensure forward slashes

            Debug.Log($"Variant folder path: {variantFolderPath}");

            // Create parent folders only, exclude the last folder
            if (!AssetDatabase.IsValidFolder(variantFolderPath))
            {
                CreateParentFolders(variantFolderPath);
            }

            // Collect all materials used by the prefab
            HashSet<Material> materialsSet = new HashSet<Material>();
            Renderer[] renderers = prefab.GetComponentsInChildren<Renderer>(true); // Include inactive objects

            foreach (Renderer renderer in renderers)
            {
                foreach (Material mat in renderer.sharedMaterials)
                {
                    if (mat != null)
                    {
                        materialsSet.Add(mat);
                    }
                }
            }

            // Clone materials into a new folder
            string materialsFolderName = prefabName + "(NiloToon)(Materials)";
            string materialsFolderPath = variantFolderPath + "/" + materialsFolderName;

            // Ensure materialsFolderPath starts with "Assets/"
            if (!materialsFolderPath.StartsWith("Assets/"))
            {
                Debug.LogError("Materials folder path must start with 'Assets/': " + materialsFolderPath);
                return;
            }

            // Create the materials folder and refresh the AssetDatabase
            if (!AssetDatabase.IsValidFolder(materialsFolderPath))
            {
                CreateFolderRecursively(materialsFolderPath);
                AssetDatabase.Refresh();
                Debug.Log("Created Materials Folder: " + materialsFolderPath);
            }

            // Mapping from original materials to cloned materials
            Dictionary<Material, Material> materialMap = new Dictionary<Material, Material>();

            foreach (Material mat in materialsSet)
            {
                Material matClone = Object.Instantiate(mat);
                string matName = mat.name; // Use the original material name

                string newMatPath = materialsFolderPath + "/" + matName + ".mat";

                // Ensure unique path
                newMatPath = AssetDatabase.GenerateUniqueAssetPath(newMatPath);

                try
                {
                    // Create the cloned material asset
                    AssetDatabase.CreateAsset(matClone, newMatPath);
                    Debug.Log("Created Material Asset: " + newMatPath);
                    materialMap[mat] = matClone;
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to create material asset at path: {newMatPath}");
                    Debug.LogException(e);
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Instantiate the selected prefab
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);

            Debug.Log($"Attempting to save prefab variant at path: {variantPath}");

            try
            {
                // Save the instance as a new prefab asset to create a prefab variant
                PrefabUtility.SaveAsPrefabAssetAndConnect(instance, variantPath, InteractionMode.UserAction);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to save prefab variant at path: {variantPath}");
                Debug.LogException(e);
                GameObject.DestroyImmediate(instance);
                return;
            }

            // Destroy the instance from the scene
            GameObject.DestroyImmediate(instance);

            // Load prefab variant contents for editing
            GameObject prefabVariantContents = PrefabUtility.LoadPrefabContents(variantPath);

            // Update materials in the prefab variant
            Renderer[] variantRenderers = prefabVariantContents.GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in variantRenderers)
            {
                Material[] mats = renderer.sharedMaterials;
                bool materialsChanged = false;
                for (int i = 0; i < mats.Length; i++)
                {
                    Material originalMat = mats[i];
                    if (originalMat != null && materialMap.ContainsKey(originalMat))
                    {
                        mats[i] = materialMap[originalMat];
                        materialsChanged = true;
                    }
                }

                if (materialsChanged)
                {
                    renderer.sharedMaterials = mats;
                }
            }

            // Run auto setup for the prefab variant
            NiloToonEditorPerCharacterRenderControllerCustomEditor.AutoSetupCharacterGameObject(prefabVariantContents);

            // Save and unload prefab variant
            PrefabUtility.SaveAsPrefabAsset(prefabVariantContents, variantPath);

            PrefabUtility.UnloadPrefabContents(prefabVariantContents);

            EditorUtility.DisplayDialog("Success", "Prefab variant created with cloned materials.", "OK");
        }

        [MenuItem("Window/NiloToonURP/[Prefab] Create NiloToon Prefab Variant and Materials", priority = 0, validate = true)]
        [MenuItem("Assets/NiloToon/[Prefab] Create NiloToon Prefab Variant and Materials", priority = 1100 + 0, validate = true)]
        public static bool ValidateCreatePrefabVariantAndCloneMaterials()
        {
            var allselectedObjects = Selection.objects;
            return allselectedObjects.All(selectedObject => selectedObject is GameObject);
        }

        // Helper method to create parent folders only
        static void CreateParentFolders(string fullPath)
        {
            fullPath = fullPath.Replace("\\", "/");

            if (!fullPath.StartsWith("Assets/"))
            {
                Debug.LogError("Path must start with 'Assets/': " + fullPath);
                return;
            }

            // Get the parent folder path
            string parentFolderPath = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrEmpty(parentFolderPath))
            {
                Debug.LogError("Parent folder path is empty or invalid.");
                return;
            }

            // Create parent folders recursively
            CreateFolderRecursively(parentFolderPath);
        }

        // Helper method to create folders recursively
        static void CreateFolderRecursively(string fullPath)
        {
            fullPath = fullPath.Replace("\\", "/");

            if (!fullPath.StartsWith("Assets/"))
            {
                Debug.LogError("Path must start with 'Assets/': " + fullPath);
                return;
            }

            string[] folders = fullPath.Split('/');
            string currentPath = "Assets";
            for (int i = 1; i < folders.Length; i++)
            {
                string folder = folders[i];
                if (string.IsNullOrEmpty(folder))
                {
                    continue;
                }

                string newPath = currentPath + "/" + folder;
                if (!AssetDatabase.IsValidFolder(newPath))
                {
                    // Check if a folder with this name already exists
                    string[] existingFolders = AssetDatabase.GetSubFolders(currentPath);
                    string existingFolder = existingFolders.FirstOrDefault(f => Path.GetFileName(f).Equals(folder, StringComparison.OrdinalIgnoreCase));

                    if (existingFolder != null)
                    {
                        // Use the existing folder
                        newPath = existingFolder;
                        Debug.Log("Using existing folder: " + newPath);
                    }
                    else
                    {
                        // Create a new folder
                        string guid = AssetDatabase.CreateFolder(currentPath, folder);
                        if (string.IsNullOrEmpty(guid))
                        {
                            Debug.LogError($"Failed to create folder: {newPath}");
                            return;
                        }
                        Debug.Log("Created folder: " + newPath);
                    }
                }

                currentPath = newPath;
            }

            AssetDatabase.Refresh();
        }
    }
}