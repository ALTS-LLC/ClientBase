using NiloToon.NiloToonURP;
using UnityEditor;
using UnityEngine;

namespace NiloToonURP.Editor
{
    public static class NiloToonEditorCreateObjectMenu
    {
        [MenuItem("GameObject/Light/NiloToon/MainLight Overrider", false, 50)]
        static void CreateMainLightOverriderFromLightMenu(MenuCommand menuCommand) =>
            CreateMainLightOverrider(menuCommand);

        [MenuItem("GameObject/Create Other/NiloToon/MainLight Overrider", false, 10)]
        static void CreateMainLightOverriderFromCreateOtherMenu(MenuCommand menuCommand) =>
            CreateMainLightOverrider(menuCommand);
        
        [MenuItem("GameObject/Light/NiloToon/Light Controller", false, 50)]
        static void CreateLightControllerFromLightMenu(MenuCommand menuCommand) =>
            CreateLightController(menuCommand);

        [MenuItem("GameObject/Create Other/NiloToon/Light Controller", false, 10)]
        static void CreateLightControllerFromCreateOtherMenu(MenuCommand menuCommand) =>
            CreateLightController(menuCommand);
        
        [MenuItem("GameObject/Light/NiloToon/Light Source Modifier", false, 50)]
        static void CreateLightSourceModifierFromLightMenu(MenuCommand menuCommand) =>
            CreateLightSourceModifier(menuCommand);

        [MenuItem("GameObject/Create Other/NiloToon/Light Source Modifier", false, 10)]
        static void CreateLightSourceModifierFromCreateOtherMenu(MenuCommand menuCommand) =>
            CreateLightSourceModifier(menuCommand);
        
        [MenuItem("GameObject/Light/NiloToon/Directional Light(+Light Source Modifier)", false, 50)]
        static void CreateDirectionalLightWithLightSourceModifierFromLightMenu(MenuCommand menuCommand) =>
            CreateLightWithLightSourceModifier(menuCommand, LightType.Directional);
        
        [MenuItem("GameObject/Create Other/NiloToon/Directional Light(+Light Source Modifier)", false, 50)]
        static void CreateDirectionalLightWithLightSourceModifierFromCreateOtherMenu(MenuCommand menuCommand) =>
            CreateLightWithLightSourceModifier(menuCommand, LightType.Directional);
        
        [MenuItem("GameObject/Light/NiloToon/Point Light(+Light Source Modifier)", false, 50)]
        static void CreatePointLightWithLightSourceModifierFromLightMenu(MenuCommand menuCommand) =>
            CreateLightWithLightSourceModifier(menuCommand, LightType.Point);
        
        [MenuItem("GameObject/Create Other/NiloToon/Point Light(+Light Source Modifier)", false, 50)]
        static void CreatePointLightWithLightSourceModifierFromCreateOtherMenu(MenuCommand menuCommand) =>
            CreateLightWithLightSourceModifier(menuCommand, LightType.Point);
        
        [MenuItem("GameObject/Light/NiloToon/Spot Light(+Light Source Modifier)", false, 50)]
        static void CreateSpotLightWithLightSourceModifierFromLightMenu(MenuCommand menuCommand) =>
            CreateLightWithLightSourceModifier(menuCommand, LightType.Spot);
        
        [MenuItem("GameObject/Create Other/NiloToon/Spot Light(+Light Source Modifier)", false, 50)]
        static void CreateSpotLightWithLightSourceModifierFromCreateOtherMenu(MenuCommand menuCommand) =>
            CreateLightWithLightSourceModifier(menuCommand, LightType.Spot);
        
        


        static void CreateMainLightOverrider(MenuCommand menuCommand)
        {
            // Create a new GameObject
            GameObject go = new GameObject("NiloToonCharacterMainLightOverrider");
            go.AddComponent<NiloToonCharacterMainLightOverrider>();
            go.transform.forward = Vector3.down;

            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

            // Finally, select it
            Selection.activeObject = go;
        }
        
        static void CreateLightController(MenuCommand menuCommand)
        {
            // Create a new GameObject
            GameObject go = new GameObject("NiloToonCharacterLightController");
            go.AddComponent<NiloToonCharacterLightController>();

            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

            // Finally, select it
            Selection.activeObject = go;
        }
        
        static void CreateLightSourceModifier(MenuCommand menuCommand)
        {
            // Create a new GameObject
            GameObject go = new GameObject("NiloToonLightSourceModifier");
            go.AddComponent<NiloToonLightSourceModifier>();

            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

            // Finally, select it
            Selection.activeObject = go;
        }
        
        static void CreateLightWithLightSourceModifier(MenuCommand menuCommand, LightType lightType)
        {
            // Create a new GameObject
            GameObject go = new GameObject($"{lightType.ToString()} Light (+NiloToonLightSourceModifier)");
            Light light = go.AddComponent<Light>();
            light.type = lightType;
            NiloToonLightSourceModifier modifier = go.AddComponent<NiloToonLightSourceModifier>();
            if(!modifier.targetLightsMask.Contains(light))
                modifier.targetLightsMask.Add(light);

            // Ensure it gets reparented if this was a context click (otherwise does nothing)
            GameObjectUtility.SetParentAndAlign(go, menuCommand.context as GameObject);

            // Register the creation in the undo system
            Undo.RegisterCreatedObjectUndo(go, "Create " + go.name);

            // Finally, select it
            Selection.activeObject = go;
        }
    }
}