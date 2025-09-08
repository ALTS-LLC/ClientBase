using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClientBaseUtility;

#if UNITY_EDITOR
using UnityEditor;
public class ClientBaseSetting : EditorWindow
{
    private Config _config = null;
    private Config _refConfig
    {
        get
        {
            return _config;  
        }
        set
        {
            _config = value;
            ConfigUtility.Config = value;
        }
    }

    [MenuItem("Alts_Tool/Client Base Setting")]
    private static void CreateWindow()
    {
        GetWindow<ClientBaseSetting>("Client Base Setting");
    }
    private void OnGUI()
    {
        if (ConfigUtility.Config != null)
        {
            _refConfig = ConfigUtility.Config;
        }
        _refConfig = EditorGUILayout.ObjectField(_config, typeof(Config)) as Config;
    }
}
#endif