using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.Rendering;

namespace NiloToon.NiloToonURP
{
    public class NiloToonEditorSelectAllMaterialsWithNiloToonShader
    {
#if UNITY_EDITOR
        [MenuItem("Window/NiloToonURP/Select all NiloToon_Character Materials in project", priority = 60)]
        public static void SelectAllMaterialsWithNiloToonShader()
        {
            // https://forum.unity.com/threads/setting-selection-with-multiple-objects.259130/
            Selection.objects = GetAllNiloToonCharacterShaderMaterials().ToArray();
        }

        public static List<Material> GetAllNiloToonCharacterShaderMaterials()
        {
            string[] guids = AssetDatabase.FindAssets("t:material");
            List<Material> mList = new List<Material>();
            foreach (string guid in guids)
            {
                Material m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                if (m.shader.name == ("Universal Render Pipeline/NiloToon/NiloToon_Character"))
                {
                    mList.Add(m);
                }
            }

            return mList;
        }
#endif
    }
}