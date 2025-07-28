using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor.SceneManagement;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
public class MotionClientBuild : EditorWindow, IBuildable
{
	public static GameObject Actor = null;
	private string BuildPath = "";
	private readonly string _clientScenePath = "Assets/Scenes/ClientMain.unity";


	public void GUIWindow()
	{
		Actor = EditorGUILayout.ObjectField(Actor, typeof(UnityEngine.Object), true) as GameObject;

		EditorGUILayout.BeginHorizontal();

		EditorGUILayout.TextField(BuildPath);
		if (GUILayout.Button("Select"))
		{
			BuildPath = EditorUtility.OpenFolderPanel("Select Folder", "", "");
		}

		EditorGUILayout.EndHorizontal();

		GUIStyle buttonStyle = new GUIStyle(GUI.skin.button);
		buttonStyle.fixedHeight = 30;
		buttonStyle.fontSize = 12;

		if (GUILayout.Button("Build",buttonStyle))
		{
			string path = AssetDatabase.GetAssetPath(Actor);

			PlayerPrefs.SetString("MotionClientPath", BuildPath);
			ClientBuild.RemoveSceneFromBuildSettings();
			var scenePath = path.Replace(Path.GetFileName(path), "") + Actor.name + ".unity";
			AssetDatabase.CopyAsset(_clientScenePath,scenePath );
			EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

			var actor =Instantiate(Actor).gameObject.AddComponent<MotionSender>();
			actor.gameObject.AddComponent<ModelGroundingAdjuster>();
			Undo.RegisterCreatedObjectUndo(actor.gameObject,"CreateActor");
			EditorSceneManager.SaveOpenScenes();

			ClientBuild.AddSceneToBuildSettings(scenePath);
			ClientBuild.Build(BuildPath + Actor.name + "MotionClient.exe");

			EditorSceneManager.OpenScene(_clientScenePath, OpenSceneMode.Single);
		}
	}
}
#endif