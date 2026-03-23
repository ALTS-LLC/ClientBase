using UnityEditor;
using UnityEditor.SceneManagement;

public static class AutoOpenScene
{
    public static void Open()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/ClientMain.unity");
    }
}