using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;

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
	private CaptureSystemConfig _captureSystemConfig = null;
	[SerializeField]
	private OptiConfig _optiConfig = null;
	[SerializeField]
	private ViconConfig _viconConfig = null;

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

	public void ConfigObjectSerialize()
	{
        _config.CaptureSystemConfig = _captureSystemConfig;
		_config.CaptureSystemConfig.OptiConfig = _optiConfig;
		_config.CaptureSystemConfig.ViconConfig = _viconConfig;
		_config.SetDirty();
	}

	protected override void RegisterManager()
	{
		ManagerHub.Instance.DataManager = this;
	}
}
