using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneRouterChecks
{
    [MenuItem("Tools/Multiplayer/Run Scene Router Checks")]
    public static void Run()
    {
        if (Application.isPlaying) throw new InvalidOperationException("Run scene lookup checks in Edit Mode.");
        Scene originalActive = SceneManager.GetActiveScene();
        var created = new List<Scene>();
        string folder = "__SceneRouterChecks_" + Guid.NewGuid().ToString("N");
        string root = "Assets/" + folder;
        int checks = 0;
        void Check(bool condition, string message)
        {
            if (!condition) throw new Exception(message);
            checks++;
        }
        Scene MakeScene(string path)
        {
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            created.Add(scene);
            if (!EditorSceneManager.SaveScene(scene, path)) throw new Exception("Could not save fixture: " + path);
            return scene;
        }
        AssetDatabase.CreateFolder("Assets", folder);
        try
        {
            AssetDatabase.CreateFolder(root, "Maps");
            AssetDatabase.CreateFolder(root, "Other");
            Scene menu = MakeScene(root + "/Main Menu.unity");
            Scene map = MakeScene(root + "/Map.unity");
            Scene game = MakeScene(root + "/Maps/Grass.unity");
            Scene otherMap = MakeScene(root + "/Other/Map.unity");
            foreach (Scene scene in new[] { menu, map, game, otherMap })
            {
                Check(SceneRouter.FindLoadedScene(scene.path) == scene, "Full asset path did not identify the loaded scene");
                Check(SceneRouter.FindLoadedScene(scene.path.Replace('/', '\\')) == scene, "Windows path failed");
                Check(SceneRouter.FindLoadedScene(scene.path.Substring(0, scene.path.Length - 6)) == scene, "Extensionless path failed");
            }
            Check(SceneRouter.FindLoadedScene("Main Menu") == menu, "Short name with spaces failed");
            Check(SceneRouter.FindLoadedScene("Main Menu.unity") == menu, "Short name with extension failed");
            Check(!SceneRouter.FindLoadedScene(root + "/Missing/Map.unity").IsValid(), "Missing path fell back to a different Map scene");
            Check(!SceneRouter.FindLoadedScene(null).IsValid(), "Null reference returned a scene");
            Check(!SceneRouter.FindLoadedScene(" ").IsValid(), "Blank reference returned a scene");
            // Exercise the same activation used by menu -> map -> gameplay -> map -> menu.
            foreach (Scene destination in new[] { map, game, map, menu })
            {
                Check(SceneManager.SetActiveScene(SceneRouter.FindLoadedScene(destination.path)), "Destination activation failed");
                Check(SceneManager.GetActiveScene() == destination, "Wrong scene became active");
            }
            Debug.Log($"Scene router checks passed: {checks}");
        }
        finally
        {
            if (originalActive.IsValid() && originalActive.isLoaded) SceneManager.SetActiveScene(originalActive);
            for (int i = created.Count - 1; i >= 0; i--)
                if (created[i].IsValid() && created[i].isLoaded) EditorSceneManager.CloseScene(created[i], true);
            AssetDatabase.DeleteAsset(root);
        }
    }
}
