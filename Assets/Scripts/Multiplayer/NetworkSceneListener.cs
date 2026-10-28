using System;
using UnityEngine;
using Fusion;

public class NetworkSceneListener : MonoBehaviourPlus
{
    public override string prettyName => "NetworkSceneListener";
    public override string description => "Listens for Scene index changes\n Talks to the SceneRouter directly";
    
    [Header("Assigned at run time")]
    [SerializeField]
    public GlobalGameSettings globalGameSettings;      // were tracking the host's game settings
    
    private void Update()
    {
        if (globalGameSettings == null) return;
        
        // assigned by Network Simulation Manager
        
        foreach (string change in globalGameSettings.changeDetector.DetectChanges(globalGameSettings))
        {
            switch ( change)
            {
                case nameof(globalGameSettings.sceneIndex):
                    Debug.Log($"scene index changed to: {globalGameSettings.sceneIndex}");
                    SceneChanged(globalGameSettings.sceneIndex);
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
