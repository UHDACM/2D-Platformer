using System;
using UnityEngine;
using Fusion;

public class NetworkSceneListener : MonoBehaviourPlus
{
    public override string prettyName => "NetworkSceneListener";
    public override string description => "Listens for Scene index changes\n Talks to the SceneRouter directly";

    public static NetworkSceneListener Instance;    
    
    // Create singelton
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }
    
    private void Update()
    {
        GlobalGameSettings gs = GlobalGameSettings.Instance;    
        if (gs == null) return;
        
        
        // assigned by Network Simulation Manager
        foreach (string change in gs.changeDetector.DetectChanges(gs))
        {
            switch (change)
            {
                case nameof(gs.sceneIndex):
                    Debug.Log($"scene index changed to: {gs.sceneIndex}");
                    SceneChanged(gs.sceneIndex);
                    break;
            }
        }
    }

    public void SceneChanged(int index)
    {
        if (index == (int)Enums.Scenes.Menu)
            SceneRouter.Instance.QuitMapScene();
        
        if (index == (int)Enums.Scenes.Map)
            SceneRouter.Instance.HostStarts();
        
        if (index == (int)Enums.Scenes.Ice)
            SceneRouter.Instance.MapChosen(Enums.Maps.Ice);
    }
    
}
