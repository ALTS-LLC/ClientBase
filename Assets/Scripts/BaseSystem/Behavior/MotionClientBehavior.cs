using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MotionClientBehavior : IBehavior
{
	private string _jsonPath = null;
	public void OnStart()
	{
		_jsonPath = Application.dataPath + "/StreamingAssets/config.json";
		ManagerHub.Instance.DataManager.Config = ManagerHub.Instance.DataManager.JsonToSBParser<Config>(ManagerHub.Instance.DataManager.Config, _jsonPath);
	}
	public void OnQuit()
	{
		ManagerHub.Instance.DataManager.SBtoJsonParser<Config>(ManagerHub.Instance.DataManager.Config, _jsonPath);
	}
}