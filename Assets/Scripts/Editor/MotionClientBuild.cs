using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEditor.SceneManagement;
using System.Linq;
using UnityEngine.SceneManagement;
using Unity.VisualScripting;
using ClientBase_MotionCapture;
using System;
using UnityVicon;




#if UNITY_EDITOR
using UnityEditor;

public class MotionClientBuild : EditorWindow, IBuildable
{
    public static GameObject Target = null;
    private string BuildPath = "";

    private CaptureSystemType _captureSystemType = CaptureSystemType.OptiTrack;
    private List<GUIContent> _captureSystemContent = new();
    private int _captureSystemTypeIndex = 0;


    private CaptureType _captureType = CaptureType.Motion;
    private List<GUIContent> _captureTypeContent = new();
    private int _captureTypeIndex = 0;

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

        if (_captureSystemContent.Count == 0)
        {
            foreach (var item in Enum.GetValues(typeof(CaptureSystemType)))
            {
                _captureSystemContent.Add(new GUIContent(item.ToString()));
            }
        }
        _captureSystemTypeIndex = EditorGUILayout.Popup(label: new GUIContent("使用するモーションキャプチャー機材"), selectedIndex: _captureSystemTypeIndex, displayedOptions: _captureSystemContent.ToArray());

        if (_captureTypeContent.Count == 0)
        {
            foreach (var item in Enum.GetValues(typeof(CaptureType)))
            {
                _captureTypeContent.Add(new GUIContent(item.ToString()));
            }
        }
        _captureTypeIndex = EditorGUILayout.Popup(label: new GUIContent("Capture Type"), selectedIndex: _captureTypeIndex, displayedOptions: _captureTypeContent.ToArray());


        if (GUILayout.Button("aaa"))
        {
            SetUPCaptureSystem();
        }

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

        GameObject newActor = EditorGUILayout.ObjectField("Actor", Target, typeof(GameObject), true) as GameObject;
        if (newActor != Target)
        {
            Target = newActor;
        }

        // Actor情報表示
        if (Target != null)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Actor Info:", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Name: {Target.name}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"Path: {AssetDatabase.GetAssetPath(Target)}", EditorStyles.miniLabel);

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
            if (Target != null)
            {
                EditorGUILayout.LabelField($"{BuildPath}{Target.name}MotionClient.exe", EditorStyles.miniLabel);
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
        bool canBuild = Target != null && !string.IsNullOrEmpty(BuildPath);

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
        if (SceneManager.GetActiveScene().name != _clientSceneName)
        {
            EditorSceneManager.OpenScene(_clientScenePath, OpenSceneMode.Single);
        }

        var app = GameObject.Find("AppInstaller").GetComponent<AppInstaller>();
        app.SelectedBehaviorType = BehaviorType.MotionClient;
        Undo.RegisterCreatedObjectUndo(app.gameObject, "ChangeBehaviorType");
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene());

        EditorUtility.DisplayProgressBar("Building Motion Client", "Preparing build...", 0f);

        string path = AssetDatabase.GetAssetPath(Target);
        PlayerPrefs.SetString("MotionClientPath", BuildPath);

        EditorUtility.DisplayProgressBar("Building Motion Client", "Removing scene from build settings...", 0.2f);
        ClientBuild.RemoveSceneFromBuildSettings();

        var scenePath = path.Replace(Path.GetFileName(path), "") + Target.name + ".unity";

        EditorUtility.DisplayProgressBar("Building Motion Client", "Creating scene copy...", 0.4f);
        AssetDatabase.CopyAsset(_clientScenePath, scenePath);

        EditorUtility.DisplayProgressBar("Building Motion Client", "Setting up actor...", 0.6f);
        EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        //ここ
        SetUPCaptureSystem();

        EditorSceneManager.SaveOpenScenes();

        EditorUtility.DisplayProgressBar("Building Motion Client", "Adding scene to build settings...", 0.8f);
        ClientBuild.AddSceneToBuildSettings(scenePath);

        EditorUtility.DisplayProgressBar("Building Motion Client", "Building executable...", 0.9f);
        ClientBuild.Build(BuildPath + Target.name + "MotionClient.exe");

        EditorSceneManager.OpenScene(_clientScenePath, OpenSceneMode.Single);

        EditorUtility.ClearProgressBar();
        EditorUtility.DisplayDialog("Build Complete", $"Motion Client built successfully!\nOutput: {BuildPath}{Target.name}MotionClient.exe", "OK");
        try
        {

        }
        catch (System.Exception e)
        {
            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("Build Error", $"Build failed with error:\n{e.Message}", "OK");
            Debug.LogError($"Motion Client Build Error: {e}");
        }








    }

    private void SetUPCaptureSystem()
    {
        CaptureSystemType captureSystemType = CaptureSystemType.OptiTrack;
        CaptureType captureType = CaptureType.Motion;

        string configJsonPath = Application.dataPath + "/StreamingAssets/Config_json/config.json";
        string captureSystemConfigJsonPath = Application.dataPath + "/StreamingAssets/Config_json/capture_system_config.json";
        string optiConfigJsonPath = Application.dataPath + "/StreamingAssets/Config_json/opti_config.json";
        string viconConfigJsonPath = Application.dataPath + "/StreamingAssets/Config_json/vicon_config.json";

        ManagerHub.Instance.DataManager.Config = ManagerHub.Instance.DataManager.JsonToSBParser<Config>(ManagerHub.Instance.DataManager.Config, configJsonPath);
        ManagerHub.Instance.DataManager.Config.CaptureSystemConfig = ManagerHub.Instance.DataManager.JsonToSBParser<CaptureSystemConfig>(ManagerHub.Instance.DataManager.Config.CaptureSystemConfig, captureSystemConfigJsonPath);
        ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.OptiConfig = ManagerHub.Instance.DataManager.JsonToSBParser<OptiConfig>(ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.OptiConfig, optiConfigJsonPath);
        ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig = ManagerHub.Instance.DataManager.JsonToSBParser<ViconConfig>(ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig, viconConfigJsonPath);


        foreach (var item in Enum.GetValues(typeof(CaptureSystemType)))
        {
            if (item.ToString() == _captureSystemContent[_captureSystemTypeIndex].text)
            {
                captureSystemType = (CaptureSystemType)item;
                ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.CaputureSystemType = item.ToString();
            }
        }

        foreach (var item in Enum.GetValues(typeof(CaptureType)))
        {
            if (item.ToString() == _captureTypeContent[_captureTypeIndex].text)
            {
                captureType = (CaptureType)item;
                ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.CaputureType = item.ToString();
            }
        }
        ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.TagName = "Skeleton 001";


        ManagerHub.Instance.DataManager.SBtoJsonParser<Config>(ManagerHub.Instance.DataManager.Config, configJsonPath);
        ManagerHub.Instance.DataManager.Config.CaptureSystemConfig = ManagerHub.Instance.DataManager.SBtoJsonParser<CaptureSystemConfig>(ManagerHub.Instance.DataManager.Config.CaptureSystemConfig, captureSystemConfigJsonPath);
        ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.OptiConfig = ManagerHub.Instance.DataManager.SBtoJsonParser<OptiConfig>(ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.OptiConfig, optiConfigJsonPath);
        ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig = ManagerHub.Instance.DataManager.SBtoJsonParser<ViconConfig>(ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.ViconConfig, viconConfigJsonPath);

        switch (captureSystemType)
        {
            case CaptureSystemType.OptiTrack:
                SettingOptiCaptureType(captureType);
                break;
            case CaptureSystemType.Vicon1_12:
                SettingViconCaptureType(captureType);
                break;
            default:
                break;
        }
    }

    private void SettingOptiCaptureType(CaptureType captureType)
    {
        var opti = PrefabUtility.LoadPrefabContents(Application.dataPath + "/ManagerAsset/App/MotionCapture/MotionClient/Client - OptiTrack.prefab");
        var optitrackStreamingClient = Instantiate(opti).GetComponent<OptitrackStreamingClient>();

        switch (captureType)
        {
            case CaptureType.Motion:
                var actor = Instantiate(Target).gameObject.AddComponent<MotionSender>();
                var optitrackSA = actor.AddComponent<OptitrackSkeletonAnimator>();
                optitrackSA.StreamingClient = optitrackStreamingClient;
                foreach (var item in actor.gameObject.GetComponentsInChildren<Transform>())
                {
                    if (item.gameObject.TryGetComponent<Animator>(out Animator animator))
                    {
                        optitrackSA.DestinationAvatar = animator.avatar;
                    }
                }
                optitrackSA.SkeletonAssetName = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.TagName;


                actor.gameObject.AddComponent<ModelGroundingAdjuster>();
                Undo.RegisterCreatedObjectUndo(actor.gameObject, "CreateActor");

                break;
            case CaptureType.Prop:
                var prop = Instantiate(Target).gameObject.AddComponent<PropSender>();
                var optitrackRB = prop.AddComponent<OptitrackRigidBody>();
                optitrackRB.StreamingClient = optitrackStreamingClient;
                optitrackRB.RigidBodyId = 0;

                Undo.RegisterCreatedObjectUndo(prop.gameObject, "CreateProp");
                break;
            default:
                break;
        }
    }
    private void SettingViconCaptureType(CaptureType captureType)
    {
        var vicon = PrefabUtility.LoadPrefabContents(Application.dataPath + "/ManagerAsset/App/MotionCapture/MotionClient/ViconDataStreamPrefab.prefab");
        var viconDataStreamClient = Instantiate(vicon).GetComponent<ViconDataStreamClient>();
        switch (captureType)
        {
            case CaptureType.Motion:


                var actor = Instantiate(Target).gameObject.AddComponent<MotionSender>();
                var viconActor = PrefabUtility.LoadPrefabContents(Application.dataPath + "/ManagerAsset/App/MotionCapture/MotionClient/ViconActor.prefab");

                var referenceActor = Instantiate(viconActor).GetComponent<SubjectScript_for12>();
                referenceActor.Client = MotionCaptureStream.ViconDataStreamClient;
                referenceActor.SubjectName = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.TagName;
                referenceActor.Client = viconDataStreamClient;
                var boneTracer = actor.gameObject.AddComponent<BoneTracer>();
                boneTracer.TargetAnimator = referenceActor.gameObject.GetComponent<Animator>();


                actor.gameObject.AddComponent<ModelGroundingAdjuster>();
                Undo.RegisterCreatedObjectUndo(actor.gameObject, "CreateActor");

                break;
            case CaptureType.Prop:
                var prop = Instantiate(Target).gameObject.AddComponent<PropSender>();
                var rbScript_For12 = prop.AddComponent<RBScript_for12>();
                rbScript_For12.Client = viconDataStreamClient;
                rbScript_For12.ObjectName = ManagerHub.Instance.DataManager.Config.CaptureSystemConfig.TagName;
                Undo.RegisterCreatedObjectUndo(prop.gameObject, "CreateProp");
                break;
            default:
                break;
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