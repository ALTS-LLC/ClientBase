using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClientBaseUtility;

#if UNITY_EDITOR
using UnityEditor;
[InitializeOnLoad]
public class InitializeOnLoad
{
    static InitializeOnLoad()
    {
        ConfigUtility.Config = AssetDatabase.LoadAssetAtPath<Config>("Assets/ManagerAsset/Data/Config/Config.asset");
    }
}
#endif