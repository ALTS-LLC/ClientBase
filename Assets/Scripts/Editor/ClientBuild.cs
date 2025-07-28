using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;
using System.Linq;
using UnityEditor.Build.Reporting;


#if UNITY_EDITOR
using UnityEditor;
public class ClientBuild : EditorWindow
{
	private static MotionClientBuild _motionClientBuild = null;
	private static PropClientBuild _propClientBuild= null;

	private static int _optionsIndex = 0;
	private static BehaviorType _currentBehaviorType = BehaviorType.MotionClient;
	private static BehaviorType? _lastBehaviorType = BehaviorType.MotionClient;

	[MenuItem("Assets/Alts_Tool/CharacterSetup")]
	private static void CreateWindow()
	{
		GetWindow<ClientBuild>("ClientBuild");
	}

	private void OnGUI()
	{
		SelectBehaviorType();

		switch (_currentBehaviorType)
		{
			case BehaviorType.MotionClient:
				if (_motionClientBuild == null)
				{
					_motionClientBuild = new MotionClientBuild();
				}
				if (SelectionObj() != null)
				{
					MotionClientBuild.Actor = SelectionObj();
				}
				_motionClientBuild.GUIWindow();
				break;
			case BehaviorType.PropClient:
				if (_propClientBuild == null)
				{
					_propClientBuild = new PropClientBuild();
				}
				if (SelectionObj() != null)
				{
					PropClientBuild.Prop= SelectionObj();
				}
				_propClientBuild.GUIWindow();
				break;
			default:
				break;
		}
	}

	private static void SelectBehaviorType()
	{
		Dictionary<int, BehaviorType> clientOptionPair = new Dictionary<int, BehaviorType>();
		List<UnityEngine.GUIContent> clientOptions = new List<GUIContent>();

		for (int i = 0; i < Enum.GetValues(typeof(BehaviorType)).Length; i++)
		{
			clientOptions.Add(new UnityEngine.GUIContent(Enum.GetValues(typeof(BehaviorType)).GetValue(i).ToString()));
			clientOptionPair.Add(i, (BehaviorType)Enum.GetValues(typeof(BehaviorType)).GetValue(i));
		}
		_optionsIndex = EditorGUILayout.Popup(label: new UnityEngine.GUIContent("Popup"), selectedIndex: _optionsIndex, displayedOptions: clientOptions.ToArray());
		_currentBehaviorType = clientOptionPair[_optionsIndex];
	}

	public static void Build(string buildPath)
	{
		BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions();
		buildPlayerOptions.scenes = EditorBuildSettings.scenes
									.Where(s => s.enabled)
									.Select(s => s.path)
									.ToArray();

		buildPlayerOptions.locationPathName =buildPath;
		buildPlayerOptions.target = BuildTarget.StandaloneWindows64;
		buildPlayerOptions.options = BuildOptions.None;

		Debug.Log("ビルドを開始します...");
		Debug.Log(buildPath);
		BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);

		if (report.summary.result == BuildResult.Succeeded)
		{
			Debug.Log($"ビルド成功! パス: {report.summary.outputPath}");
		}
		else if (report.summary.result == BuildResult.Failed)
		{
			Debug.LogError($"ビルド失敗! エラー数: {report.summary.totalErrors}");
		}
	}

	private static GameObject SelectionObj()
	{
		GameObject returnval = null;
		foreach (var instanceID in Selection.instanceIDs)
		{
			string path = AssetDatabase.GetAssetPath(instanceID);
			returnval= AssetDatabase.LoadAssetAtPath<GameObject>(path);
		}
		return returnval;
	}

	public static void AddSceneToBuildSettings(string scenePath, bool enabled = true)
	{
		List<EditorBuildSettingsScene> currentScenes = EditorBuildSettings.scenes.ToList();
		EditorBuildSettingsScene newScene = new EditorBuildSettingsScene(scenePath, enabled);
		currentScenes.Add(newScene);
		EditorBuildSettings.scenes = currentScenes.ToArray();
	}

	public static void RemoveSceneFromBuildSettings()
	{
		EditorBuildSettings.scenes = new EditorBuildSettingsScene[] { };
	}
}
#endif