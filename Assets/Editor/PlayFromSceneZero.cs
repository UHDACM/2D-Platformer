using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

[InitializeOnLoad]
public static class PlayFromSceneZero
{
    static PlayFromSceneZero()
    {
        // Ensure this logic runs automatically when the Editor loads
        EditorApplication.delayCall += SetPlayModeStartScene;
    }

    private static void SetPlayModeStartScene()
    {
        // Get the first scene listed in your Build Settings (Scene 0)
        EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;

        if (scenes.Length > 0 && !string.IsNullOrEmpty(scenes[0].path))
        {
            // Load the scene asset from its file path
            SceneAsset startScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(scenes[0].path);

            if (startScene != null)
            {
                // Assign it as the mandatory entry scene whenever you press Play
                EditorSceneManager.playModeStartScene = startScene;
                return;
            }
        }

        Debug.LogWarning("PlayFromSceneZero: No scenes found in Build Settings to set as start scene.");
    }
}
