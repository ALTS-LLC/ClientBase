using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor.SceneManagement;
using System.Linq;


#if UNITY_EDITOR
using UnityEditor;
public class PropClientBuild : EditorWindow, IBuildable
{
	public static GameObject Prop = null;
	private string BuildPath = "";
	private readonly string _clientScenePath = "Assets/Scenes/ClientMain.unity";


	public void GUIWindow()
	{
		Prop = EditorGUILayout.ObjectField(Prop, typeof(UnityEngine.Object), true) as GameObject;

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
			string path = AssetDatabase.GetAssetPath(Prop);

			PlayerPrefs.SetString("PropClientPath", BuildPath);
			ClientBuild.RemoveSceneFromBuildSettings();
			var scenePath = path.Replace(Path.GetFileName(path), "") + Prop.name + ".unity";
			AssetDatabase.CopyAsset(_clientScenePath, scenePath);
			EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

			var actor = Instantiate(Prop).gameObject.AddComponent<PropSender>();
			Undo.RegisterCreatedObjectUndo(actor.gameObject, "CreateProp");
			EditorSceneManager.SaveOpenScenes();

			ClientBuild.AddSceneToBuildSettings(scenePath);
			ClientBuild.Build(BuildPath + Prop.name + "PropClient.exe");

			EditorSceneManager.OpenScene(_clientScenePath, OpenSceneMode.Single);
		}
	}
}
#endif