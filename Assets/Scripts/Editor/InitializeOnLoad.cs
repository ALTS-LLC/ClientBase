using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClientBaseUtility;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
[InitializeOnLoad]
public class InitializeOnLoad
{
    static InitializeOnLoad()
    {
        Debug.Log("ClientBase Load");
        ConfigUtility.Config = AssetDatabase.LoadAssetAtPath<Config>(PathUtility.ConfigSBPath);

        foreach (string sb in Directory.GetFiles(PathUtility.ConfigsSBDirectory,PathUtility.ExcludeExtensions.AssetFile))
        {
            var a = AssetDatabase.LoadAssetAtPath<ScriptableObject>(sb.Replace(Application.dataPath, "").Replace("\\", "/"));
            ConfigUtility.ConfigsGroup.Add(a);
        }
    }
}
#endif