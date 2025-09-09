using ClientBaseUtility;
using ModestTree;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting.FullSerializer;
using UnityEditor;
using UnityEngine;

[DefaultExecutionOrder(-100)]
public class DataManager : ManagerBase
{
    [SerializeField]
    private Config _config = null;
    public Config Config
    {
        get { return _config; }
        set
        {
            _config = value;
        }
    }
    [SerializeField]
    private List<ScriptableObject> _scriptableObjects = new List<ScriptableObject>();

    private void Awake()
    {
        RegisterManager();
    }

    public void FactorialConfig<T>(T target)
    {
        ScriptableObject returnVal = null;
        foreach (System.Reflection.FieldInfo item in target.GetType().GetFields())
        {
            if (item.FieldType.BaseType == typeof(ScriptableObject))
            {
                foreach (ScriptableObject scriptableObject_ in _scriptableObjects)
                {
                    if (item.FieldType == scriptableObject_.GetType())
                    {
                        item.SetValue(target, scriptableObject_);
                        FactorialConfig(item.GetValue(target));
                    }
                }
            }
        }

    }

    public ScriptableObject GetConfigObject<T>()
    {
        ScriptableObject scriptableObject = null;
        foreach (ScriptableObject scriptableObject_ in _scriptableObjects)
        {
            if (typeof(T) == scriptableObject_.GetType())
            {
                scriptableObject = scriptableObject_;
            }
        }
        return scriptableObject;
    }


    public T SBtoJsonParser<T>(T scriptableObject, string path)
    {
        string json = JsonUtility.ToJson(scriptableObject);
        File.WriteAllText(path, json);
        return scriptableObject;
    }

    public T JsonToSBParser<T>(T scriptableObject, string jsonFilePath)
    {
        if (File.Exists(jsonFilePath))
        {
            string json = File.ReadAllText(jsonFilePath);
            JsonUtility.FromJsonOverwrite(json, scriptableObject);
        }
        FactorialConfig(_config);
        return scriptableObject;
    }

    protected override void RegisterManager()
    {
        ManagerHub.Instance.DataManager = this;
    }
}
