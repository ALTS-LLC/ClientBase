using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;


#if UNITY_EDITOR
using UnityEditor;

public class MotionClientBuild : EditorWindow, IBuildable
{
    public static GameObject Actor = null;
    private string BuildPath = "";
    private readonly string _clientScenePath = "Assets/Scenes/ClientMain.unity";
    private readonly string _clientSceneName = "ClientMain";

    // UI用の変数
    private Vector2 scrollPosition = Vector2.zero;
    private GUIStyle headerStyle;
    private GUIStyle sectionStyle;
    private GUIStyle buttonStyle;
    private bool isInitialized = false;

    public void GUIWindow()
    {
        InitializeStyles();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        // ヘッダー
        EditorGUILayout.LabelField("Motion Client Builder", headerStyle);
        EditorGUILayout.Space(10);

        // Actor選択セクション
        DrawActorSection();

        EditorGUILayout.Space(10);

        // ビルドパス選択セクション
        DrawBuildPathSection();

        EditorGUILayout.Space(15);

        // ビルドボタンセクション
        DrawBuildSection();

        EditorGUILayout.EndScrollView();
    }

    private void DrawActorSection()
    {
        EditorGUILayout.BeginVertical(sectionStyle);

        EditorGUILayout.LabelField("Actor Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Select Actor GameObject:", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        GameObject newActor = EditorGUILayout.ObjectField("Actor", Actor, typeof(GameObject), true) as GameObject;
        if (newActor != Actor)
        {
            Actor = newActor;
        }

        // Actor情報表示
        if (Actor != null)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Actor Info:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Name: {Actor.name}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Path: {AssetDatabase.GetAssetPath(Actor)}", EditorStyles.miniLabel);

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBuildPathSection()
    {
        EditorGUILayout.BeginVertical(sectionStyle);

        EditorGUILayout.LabelField("Build Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("Build Output Path:", EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();

        BuildPath = EditorGUILayout.TextField("Path", BuildPath);

        GUIStyle selectButtonStyle = new GUIStyle(GUI.skin.button);
        selectButtonStyle.fixedWidth = 70;
        if (GUILayout.Button("Browse", selectButtonStyle))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Build Folder", "", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                BuildPath = selectedPath + "/";
            }
        }

        EditorGUILayout.EndHorizontal();

        // パス情報表示
        if (!string.IsNullOrEmpty(BuildPath))
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Output File:", EditorStyles.miniLabel);
            if (Actor != null)
            {
                EditorGUILayout.LabelField($"{BuildPath}{Actor.name}MotionClient.exe", EditorStyles.miniLabel);
            }
            else
            {
                EditorGUILayout.LabelField("Please select an Actor first", EditorStyles.miniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBuildSection()
    {
        EditorGUILayout.BeginVertical(sectionStyle);

        // ビルド条件チェック
        bool canBuild = Actor != null && !string.IsNullOrEmpty(BuildPath);

        if (!canBuild)
        {
            EditorGUILayout.HelpBox("Please select an Actor and specify a build path before building.", MessageType.Warning);
            EditorGUILayout.Space(5);
        }

        // ビルドボタン
        GUI.enabled = canBuild;

        GUIStyle buildButtonStyle = new GUIStyle(GUI.skin.button);
        buildButtonStyle.fixedHeight = 40;
        buildButtonStyle.fontSize = 14;
        buildButtonStyle.fontStyle = FontStyle.Bold;

        if (canBuild)
        {
            buildButtonStyle.normal.textColor = Color.white;
        }

        if (GUILayout.Button("🚀 Build Motion Client", buildButtonStyle))
        {
            BuildMotionClient();
            GUIUtility.ExitGUI();
        }

        GUI.enabled = true;

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Build Process:", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Creates scene copy with MotionSender component", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Adds ModelGroundingAdjuster component", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Builds executable client", EditorStyles.miniLabel);

        EditorGUILayout.EndVertical();
    }

    private void BuildMotionClient()
    {
        try
        {
            if (SceneManager.GetActiveScene().name != _clientSceneName)
            {
                EditorSceneManager.OpenScene(_clientScenePath, OpenSceneMode.Single);
            }

            var app = GameObject.Find("AppInstaller").GetComponent<AppInstaller>();
            app.SelectedBehaviorType = BehaviorType.MotionClient;
            Undo.RegisterCreatedObjectUndo(app.gameObject, "ChangeBehaviorType");
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

            EditorUtility.DisplayProgressBar("Building Motion Client", "Preparing build...", 0f);

            string path = AssetDatabase.GetAssetPath(Actor);
            PlayerPrefs.SetString("MotionClientPath", BuildPath);

            EditorUtility.DisplayProgressBar("Building Motion Client", "Removing scene from build settings...", 0.2f);
            ClientBuild.RemoveSceneFromBuildSettings();

            var scenePath = path.Replace(Path.GetFileName(path), "") + Actor.name + ".unity";

            EditorUtility.DisplayProgressBar("Building Motion Client", "Creating scene copy...", 0.4f);
            AssetDatabase.CopyAsset(_clientScenePath, scenePath);

            EditorUtility.DisplayProgressBar("Building Motion Client", "Setting up actor...", 0.6f);
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            //ここ
            var opti = PrefabUtility.LoadPrefabContents(Application.dataPath + "/ManagerAsset/App/MotionCapture/MotionClient/Client - OptiTrack.prefab");
            var vicon = PrefabUtility.LoadPrefabContents(Application.dataPath + "/ManagerAsset/App/MotionCapture/MotionClient/ViconDataStreamPrefab.prefab");
            var optitrackStreamingClient = Instantiate(opti).GetComponent<OptitrackStreamingClient>();
            Instantiate(vicon);

            var actor = Instantiate(Actor).gameObject.AddComponent<MotionSender>();
            var optitrackSA= actor.AddComponent<OptitrackSkeletonAnimator>();
            optitrackSA.StreamingClient = optitrackStreamingClient;
            foreach (var item in actor.gameObject.GetComponentsInChildren<Transform>())
            {
                if (item.gameObject.TryGetComponent<Animator>(out Animator animator))
                {
                    optitrackSA.DestinationAvatar = animator.avatar;
                }
            }
            optitrackSA.SkeletonAssetName = ManagerHub.Instance.DataManager.Config.TagName;


            actor.gameObject.AddComponent<ModelGroundingAdjuster>();
            Undo.RegisterCreatedObjectUndo(actor.gameObject, "CreateActor");

            EditorSceneManager.SaveOpenScenes();

            EditorUtility.DisplayProgressBar("Building Motion Client", "Adding scene to build settings...", 0.8f);
            ClientBuild.AddSceneToBuildSettings(scenePath);

            EditorUtility.DisplayProgressBar("Building Motion Client", "Building executable...", 0.9f);
            ClientBuild.Build(BuildPath + Actor.name + "MotionClient.exe");

            EditorSceneManager.OpenScene(_clientScenePath, OpenSceneMode.Single);

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Build Complete", $"Motion Client built successfully!\nOutput: {BuildPath}{Actor.name}MotionClient.exe", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Build Error", $"Build failed with error:\n{e.Message}", "OK");
            Debug.LogError($"Motion Client Build Error: {e}");
        }
    }

    private void InitializeStyles()
    {
        if (isInitialized) return;

        // ヘッダースタイル
        headerStyle = new GUIStyle(EditorStyles.largeLabel);
        headerStyle.fontSize = 18;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;

        // セクションスタイル
        sectionStyle = new GUIStyle(EditorStyles.helpBox);
        sectionStyle.padding = new RectOffset(10, 10, 10, 10);
        sectionStyle.margin = new RectOffset(5, 5, 5, 5);

        // ボタンスタイル
        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fixedHeight = 30;
        buttonStyle.fontSize = 12;

        isInitialized = true;
    }

    private void OnEnable()
    {
        isInitialized = false;
    }
}
#endif