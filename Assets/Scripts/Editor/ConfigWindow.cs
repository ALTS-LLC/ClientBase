using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ClientBaseUtility;
using Unity.Collections;
using Unity.VisualScripting.FullSerializer;


#if UNITY_EDITOR
using UnityEditor;
public class ConfigWindow : EditorWindow
{
    // メニュー項目を作成
    [MenuItem("Alts_Tool/Config Window")]
    private static void CreateWindow()
    {
        GetWindow<ConfigWindow>("Config Window");
    }

    private void OnGUI()
    {
        // 全体のレイアウトを垂直方向に整列
        GUILayout.BeginVertical("box");

        // ユーザーが編集できないようにグループ全体を無効化
        EditorGUI.BeginDisabledGroup(true);

        // --- Config の表示 ---
        GUILayout.Label("Main Config", EditorStyles.boldLabel); // 太字のラベルでセクションを明確に
        EditorGUILayout.ObjectField("Config", ConfigUtility.Config, typeof(Config), true);

        EditorGUILayout.Space(10); // 項目間に少しスペースを入れる

        // --- Config Group の表示 ---
        GUILayout.Label("Config Group", EditorStyles.boldLabel); // 太字のラベルでセクションを明確に

        // リストの各要素をInspectorのリスト表示のようにする
        for (int i = 0; i < ConfigUtility.ConfigsGroup.Count; i++)
        {
            GUILayout.BeginHorizontal();

            // インデントを加えて階層を表現
            EditorGUI.indentLevel++;

            // リストのインデックスをラベルとして表示
            EditorGUILayout.ObjectField($"Config {i}", ConfigUtility.ConfigsGroup[i], typeof(ScriptableObject), false);

            EditorGUI.indentLevel--;
            GUILayout.EndHorizontal();
        }

        EditorGUI.EndDisabledGroup();
        GUILayout.EndVertical();
    }
}
#endif