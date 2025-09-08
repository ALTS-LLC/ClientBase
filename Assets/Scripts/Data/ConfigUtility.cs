using System.Collections;
using System.Collections.Generic;
using System.IO;
using Unity.VisualScripting.FullSerializer;
using UnityEngine;

namespace ClientBaseUtility
{
    public static class ConfigUtility
    {
        public static Config Config = null;
        public static List<ScriptableObject> ConfigsGroup = new List<ScriptableObject>();


        public static void FactorialConfig<T>(T target)
        {
            ScriptableObject returnVal = null;
            foreach (System.Reflection.FieldInfo item in target.GetType().GetFields())
            {
                if (item.FieldType.BaseType == typeof(ScriptableObject))
                {
                    foreach (ScriptableObject scriptableObject_ in ConfigsGroup)
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

        public static ScriptableObject GetConfigObject<T>()
        {
            ScriptableObject scriptableObject = null;
            foreach (ScriptableObject scriptableObject_ in ConfigsGroup)
            {
                if (typeof(T) == scriptableObject_.GetType())
                {
                    scriptableObject = scriptableObject_;
                }
            }
            return scriptableObject;
        }


        public static T SBtoJsonParser<T>(T scriptableObject, string path)
        {
            string json = JsonUtility.ToJson(scriptableObject);
            File.WriteAllText(path, json);
            return scriptableObject;
        }

        public static T JsonToSBParser<T>(T scriptableObject, string jsonFilePath)
        {
            if (File.Exists(jsonFilePath))
            {
                string json = File.ReadAllText(jsonFilePath);
                JsonUtility.FromJsonOverwrite(json, scriptableObject);
            }
            FactorialConfig(Config);
            return scriptableObject;
        }


    }
}