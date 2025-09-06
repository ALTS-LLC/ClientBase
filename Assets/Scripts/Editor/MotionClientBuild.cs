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
    private GUIStyle subHeaderStyle;
    private GUIStyle sectionStyle;
    private GUIStyle infoBoxStyle;
    private GUIStyle warningBoxStyle;
    private GUIStyle buttonStyle;
    private GUIStyle buildButtonStyle;
    private GUIStyle boldLabelStyle;
    private GUIStyle centeredLabelStyle;
    private bool isInitialized = false;

    // カラーパレット
    private static readonly Color primaryColor = new Color(0.2f, 0.6f, 1f, 1f);
    private static readonly Color successColor = new Color(0.3f, 0.8f, 0.3f, 1f);
    private static readonly Color warningColor = new Color(1f, 0.6f, 0.2f, 1f);
    private static readonly Color sectionBgColor = new Color(0.9f, 0.9f, 0.9f, 0.3f);

    public void GUIWindow()
    {
        InitializeStyles();

        // メインコンテナ
        EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

        // ヘッダーセクション
        DrawHeader();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        EditorGUILayout.Space(5);

        // キャプチャシステム設定セクション
        DrawCaptureSystemSection();

        EditorGUILayout.Space(10);

        // Actor選択セクション
        DrawActorSection();

        EditorGUILayout.Space(10);

        // ビルドパス選択セクション
        DrawBuildPathSection();

        EditorGUILayout.Space(10);

        // ビルドボタンセクション
        DrawBuildSection();

        EditorGUILayout.Space(20);

        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical();

        // タイトル
        EditorGUILayout.LabelField("🎬 Motion Client Builder", headerStyle);

        // 区切り線
        DrawSeparatorLine();

        EditorGUILayout.EndVertical();
    }

    private void DrawCaptureSystemSection()
    {
        EditorGUILayout.BeginVertical(sectionStyle);

        // セクションヘッダー
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("⚙️", GUILayout.Width(20));
        EditorGUILayout.LabelField("Capture System Settings", boldLabelStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // キャプチャシステム選択
        if (_captureSystemContent.Count == 0)
        {
            foreach (var item in Enum.GetValues(typeof(CaptureSystemType)))
            {
                _captureSystemContent.Add(new GUIContent(item.ToString()));
            }
        }

        EditorGUILayout.BeginVertical(infoBoxStyle);
        EditorGUILayout.LabelField("モーションキャプチャー機材", EditorStyles.boldLabel);
        _captureSystemTypeIndex = EditorGUILayout.Popup(_captureSystemTypeIndex, _captureSystemContent.ToArray(), GUILayout.Height(25));
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // キャプチャタイプ選択
        if (_captureTypeContent.Count == 0)
        {
            foreach (var item in Enum.GetValues(typeof(CaptureType)))
            {
                _captureTypeContent.Add(new GUIContent(item.ToString()));
            }
        }

        EditorGUILayout.BeginVertical(infoBoxStyle);
        EditorGUILayout.LabelField("Capture Type", EditorStyles.boldLabel);
        _captureTypeIndex = EditorGUILayout.Popup(_captureTypeIndex, _captureTypeContent.ToArray(), GUILayout.Height(25));
        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();
    }

    private void DrawActorSection()
    {
        EditorGUILayout.BeginVertical(sectionStyle);

        // セクションヘッダー
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🎭", GUILayout.Width(20));
        EditorGUILayout.LabelField("Actor Settings", boldLabelStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // Actor選択
        EditorGUILayout.BeginVertical(infoBoxStyle);
        EditorGUILayout.LabelField("Select Actor GameObject", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        GameObject newActor = EditorGUILayout.ObjectField(Target, typeof(GameObject), true, GUILayout.Height(25)) as GameObject;
        if (newActor != Target)
        {
            Target = newActor;
        }
        EditorGUILayout.EndVertical();

        // Actor情報表示
        if (Target != null)
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical(CreateInfoStyle(successColor));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("✅", GUILayout.Width(20));
            EditorGUILayout.LabelField("Actor Information", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField($"Name: {Target.name}", EditorStyles.label);
            EditorGUILayout.LabelField($"Path: {AssetDatabase.GetAssetPath(Target)}", EditorStyles.wordWrappedMiniLabel);

            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical(warningBoxStyle);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("⚠️", GUILayout.Width(20));
            EditorGUILayout.LabelField("Please select an Actor GameObject", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBuildPathSection()
    {
        EditorGUILayout.BeginVertical(sectionStyle);

        // セクションヘッダー
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("📁", GUILayout.Width(20));
        EditorGUILayout.LabelField("Build Settings", boldLabelStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // ビルドパス選択
        EditorGUILayout.BeginVertical(infoBoxStyle);
        EditorGUILayout.LabelField("Build Output Path", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        BuildPath = EditorGUILayout.TextField(BuildPath, GUILayout.Height(25));

        GUIStyle browseButtonStyle = new GUIStyle(GUI.skin.button);
        browseButtonStyle.fixedWidth = 80;
        browseButtonStyle.fixedHeight = 25;
        browseButtonStyle.fontStyle = FontStyle.Bold;

        if (GUILayout.Button("📂 Browse", browseButtonStyle))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Build Folder", "", "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                BuildPath = selectedPath + "/";
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        // パス情報表示
        if (!string.IsNullOrEmpty(BuildPath))
        {
            EditorGUILayout.Space(8);
            EditorGUILayout.BeginVertical(CreateInfoStyle(successColor));

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("📄", GUILayout.Width(20));
            EditorGUILayout.LabelField("Output File Information", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);
            if (Target != null)
            {
                string outputPath = $"{BuildPath}{Target.name}MotionClient.exe";
                EditorGUILayout.LabelField("Output:", EditorStyles.boldLabel);
                EditorGUILayout.SelectableLabel(outputPath, EditorStyles.wordWrappedLabel, GUILayout.Height(30));
            }
            else
            {
                EditorGUILayout.LabelField("Please select an Actor first", EditorStyles.centeredGreyMiniLabel);
            }

            EditorGUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawBuildSection()
    {
        EditorGUILayout.BeginVertical(sectionStyle);

        // セクションヘッダー
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("🚀", GUILayout.Width(20));
        EditorGUILayout.LabelField("Build Process", boldLabelStyle);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // ビルド条件チェック
        bool canBuild = Target != null && !string.IsNullOrEmpty(BuildPath);

        if (!canBuild)
        {
            EditorGUILayout.BeginVertical(warningBoxStyle);
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("⚠️", GUILayout.Width(20));
            EditorGUILayout.LabelField("Build Requirements", EditorStyles.boldLabel);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(3);

            if (Target == null)
                EditorGUILayout.LabelField("• Select an Actor GameObject", EditorStyles.label);
            if (string.IsNullOrEmpty(BuildPath))
                EditorGUILayout.LabelField("• Specify a build output path", EditorStyles.label);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(8);
        }

        // ビルドプロセス説明
        EditorGUILayout.BeginVertical(infoBoxStyle);
        EditorGUILayout.LabelField("Build Process Overview", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("1. Creates scene copy with MotionSender component", EditorStyles.label);
        EditorGUILayout.LabelField("2. Adds ModelGroundingAdjuster component", EditorStyles.label);
        EditorGUILayout.LabelField("3. Configures capture system settings", EditorStyles.label);
        EditorGUILayout.LabelField("4. Builds executable client", EditorStyles.label);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        // ビルドボタン
        GUI.enabled = canBuild;

        Color originalBgColor = GUI.backgroundColor;
        if (canBuild)
        {
            GUI.backgroundColor = successColor;
        }

        if (GUILayout.Button("🚀 Build Motion Client", buildButtonStyle))
        {
            BuildMotionClient();
            GUIUtility.ExitGUI();
        }

        GUI.backgroundColor = originalBgColor;
        GUI.enabled = true;

        EditorGUILayout.EndVertical();
    }

    private void DrawSeparatorLine()
    {
        EditorGUILayout.Space(5);
        Rect rect = EditorGUILayout.GetControlRect(false, 2);
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorGUILayout.Space(5);
    }

    private GUIStyle CreateInfoStyle(Color backgroundColor)
    {
        GUIStyle style = new GUIStyle(EditorStyles.helpBox);
        style.padding = new RectOffset(10, 10, 8, 8);
        style.margin = new RectOffset(0, 0, 0, 0);

        Texture2D bgTexture = new Texture2D(1, 1);
        bgTexture.SetPixel(0, 0, backgroundColor * 0.3f);
        bgTexture.Apply();
        style.normal.background = bgTexture;

        return style;
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
        ManagerHub.Instance.DataManager.ConfigObjectSerialize();
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
        headerStyle.fontSize = 20;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.normal.textColor = primaryColor;

        // サブヘッダースタイル
        subHeaderStyle = new GUIStyle(EditorStyles.centeredGreyMiniLabel);
        subHeaderStyle.fontSize = 11;
        subHeaderStyle.alignment = TextAnchor.MiddleCenter;

        // セクションスタイル
        sectionStyle = new GUIStyle();
        sectionStyle.padding = new RectOffset(15, 15, 12, 12);
        sectionStyle.margin = new RectOffset(5, 5, 5, 5);

        Texture2D sectionBg = new Texture2D(1, 1);
        sectionBg.SetPixel(0, 0, sectionBgColor);
        sectionBg.Apply();
        sectionStyle.normal.background = sectionBg;

        // 情報ボックススタイル
        infoBoxStyle = new GUIStyle(EditorStyles.helpBox);
        infoBoxStyle.padding = new RectOffset(10, 10, 8, 8);
        infoBoxStyle.margin = new RectOffset(0, 0, 0, 0);

        // 警告ボックススタイル
        warningBoxStyle = new GUIStyle(EditorStyles.helpBox);
        warningBoxStyle.padding = new RectOffset(10, 10, 8, 8);
        warningBoxStyle.margin = new RectOffset(0, 0, 0, 0);

        Texture2D warningBg = new Texture2D(1, 1);
        warningBg.SetPixel(0, 0, warningColor * 0.3f);
        warningBg.Apply();
        warningBoxStyle.normal.background = warningBg;

        // ボタンスタイル
        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fixedHeight = 30;
        buttonStyle.fontSize = 12;
        buttonStyle.fontStyle = FontStyle.Bold;

        // ビルドボタンスタイル
        buildButtonStyle = new GUIStyle(GUI.skin.button);
        buildButtonStyle.fixedHeight = 45;
        buildButtonStyle.fontSize = 16;
        buildButtonStyle.fontStyle = FontStyle.Bold;
        buildButtonStyle.normal.textColor = Color.white;

        // 太字ラベルスタイル
        boldLabelStyle = new GUIStyle(EditorStyles.boldLabel);
        boldLabelStyle.fontSize = 13;

        // 中央揃えラベルスタイル
        centeredLabelStyle = new GUIStyle(EditorStyles.label);
        centeredLabelStyle.alignment = TextAnchor.MiddleCenter;

        isInitialized = true;
    }

    private void OnEnable()
    {
        isInitialized = false;
    }
}
#endif