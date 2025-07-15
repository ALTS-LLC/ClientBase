// how to create EditorWindow editor script:
// https://docs.unity3d.com/Manual/editor-EditorWindows.html
// https://docs.unity3d.com/ScriptReference/EditorWindow.GetWindow.html

using UnityEngine;
using UnityEditor;
using System.Collections;

namespace NiloToon.NiloToonURP
{
    class NiloToonEditorDebugWindow : EditorWindow
    {
        [MenuItem("Window/NiloToonURP/Debug Window", priority = 20)]
        public static void ShowWindow()
        {
            GetWindow(typeof(NiloToonEditorDebugWindow), false, "NiloToon Debug Window");
        }

        // auto repaint param
        bool EnableShadingDebug_Cache;
        NiloToonSetToonParamPass.ShadingDebugCase shadingDebugCase_Cache;
        bool showShadowCameraFrustum_Cache;

        // user force repaint param
        bool shouldRepaintAllViews = false; // default is false because it is slow

        // The actual window code goes here
        void OnGUI()
        {
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Shading Debug 
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            EditorGUILayout.LabelField("Editor - Shading Debug ", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- In build, it is default OFF (NiloToonShaderStrippingSettingSO).");
            EditorGUILayout.LabelField("- Will reset to false if you enter PlayMode(Reload Domain = ON), or restart Unity");
            // no need to use PlayerPrefs in editor to hold this bool between edit mode and play mode, because we actually want to reset
            NiloToonSetToonParamPass.EnableShadingDebug = EditorGUILayout.Toggle("Enable Shading Debug?", NiloToonSetToonParamPass.EnableShadingDebug);
            NiloToonSetToonParamPass.shadingDebugCase = (NiloToonSetToonParamPass.ShadingDebugCase)EditorGUILayout.EnumPopup("      Shading Debug Case", NiloToonSetToonParamPass.shadingDebugCase);
            NiloToonCharSelfShadowMapRTPass.showShadowCameraDebugFrustum = EditorGUILayout.Toggle("Show SelfShadow frustum?", NiloToonCharSelfShadowMapRTPass.showShadowCameraDebugFrustum);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // Preserve PlayMode Material Change
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            EditorGUILayout.LabelField("Editor - Need Preserve PlayMode Material Change?", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- This option only works in editor. In build, it is always OFF, which allows SRP batching = better performance");
            EditorGUILayout.LabelField("- In editor, turn this ON  can preserve material change in editor playmode, but breaks  SRP batching and some per character feature. Good for material editing in PlayMode.");
            EditorGUILayout.LabelField("- In editor, turn this OFF can't preserve material change in editor playmode, but enables SRP batching and all  per character feature. Good for profiling / check final result.");
            // use PlayerPrefs in editor to hold this bool between edit mode and play mode
            bool preservePlayModeMaterialChange = EditorGUILayout.Toggle("Keep PlayMode mat edit?", NiloToonPerCharacterRenderController.GetNiloToonNeedPreserveEditorPlayModeMaterialChange_EditorOnly());
            NiloToonPerCharacterRenderController.SetNiloToonNeedPreserveEditorPlayModeMaterialChange_EditorOnly(preservePlayModeMaterialChange);

            EditorGUILayout.Space();
            EditorGUILayout.Space();

            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            // User Force Repaint all views
            ////////////////////////////////////////////////////////////////////////////////////////////////////////////
            bool finalShouldRepaintAllViews = false; // default DONT repaint since it is slow

            EditorGUILayout.LabelField("Editor - Force all windows update in edit mode?", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("- Enable this option to keep Scene & Game view always update (but editor will be much slower due to repaint)");
            EditorGUILayout.LabelField("- This option only works in editor. In build, it doesn't exist");
            EditorGUILayout.LabelField("- This option only works when this Debug Window is opened");

            shouldRepaintAllViews = EditorGUILayout.Toggle("Force repaint all windows?", shouldRepaintAllViews);
            finalShouldRepaintAllViews |= shouldRepaintAllViews;

            //////////////////////////////////////////////////////
            // Auto repaint all views when user edit debug shading values
            //////////////////////////////////////////////////////
            if (EnableShadingDebug_Cache != NiloToonSetToonParamPass.EnableShadingDebug)
            {
                EnableShadingDebug_Cache = NiloToonSetToonParamPass.EnableShadingDebug;
                finalShouldRepaintAllViews = true;
            }
            if (shadingDebugCase_Cache != NiloToonSetToonParamPass.shadingDebugCase)
            {
                shadingDebugCase_Cache = NiloToonSetToonParamPass.shadingDebugCase;
                finalShouldRepaintAllViews = true;
            }
            if (showShadowCameraFrustum_Cache != NiloToonCharSelfShadowMapRTPass.showShadowCameraDebugFrustum)
            {
                showShadowCameraFrustum_Cache = NiloToonCharSelfShadowMapRTPass.showShadowCameraDebugFrustum;
                finalShouldRepaintAllViews = true;
            }

            //////////////////////////////////////////////////////
            // Final Do repaint all views
            //////////////////////////////////////////////////////
            if (finalShouldRepaintAllViews)
            {
                if (!Application.isPlaying)
                {
                    // be careful about if this bug exist or not: NiloToon Debug window always wrongly focus project/scene window due to force repaint
                    //UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
                    SceneView.RepaintAll();
                    EditorApplication.QueuePlayerLoopUpdate();
                }
            }
        }
    }
}
