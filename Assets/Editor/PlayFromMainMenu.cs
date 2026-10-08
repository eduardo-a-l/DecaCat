using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PlayFromMainMenu
{
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";

    static PlayFromMainMenu()
    {
        SceneAsset scene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuPath);

        if (scene != null)
            EditorSceneManager.playModeStartScene = scene;
    }
}
