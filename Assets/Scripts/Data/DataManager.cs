using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

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

	private void Awake()
	{
		RegisterManager();
	}

	public T SBtoJsonParser<T>(T scriptableObject,string path)
	{
		string json = JsonUtility.ToJson(scriptableObject);
		File.WriteAllText(path, json);
		return scriptableObject;
	}

	public T JsonToSBParser<T>(T scriptableObject , string jsonFilePath)
	{
		if (File.Exists(jsonFilePath))
		{
			string json = File.ReadAllText(jsonFilePath);
			JsonUtility.FromJsonOverwrite(json, scriptableObject);
		}
		return scriptableObject;
	}

	protected override void RegisterManager()
	{
		ManagerHub.Instance.DataManager = this;
	}
}
