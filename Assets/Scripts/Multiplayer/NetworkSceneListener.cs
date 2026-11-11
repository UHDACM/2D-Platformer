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
    
    int observedIndex = -1;
    GlobalGameSettings observedSettings;
    bool wasInGame;
    private void Update()
    {
        var settings = GlobalGameSettings.Instance;
        var router = SceneRouter.Instance;
        if (settings == null || settings.Object == null || !settings.Object.IsValid || router == null || router.IsLoading) return;
        if (observedSettings != settings)
        {
            observedSettings = settings;
            observedIndex = -1;
            wasInGame = false;
        }
        int index = settings.sceneIndex;
        if (index == observedIndex || index == (int)Enums.Scenes.Loading) return;
        bool firstSnapshot = observedIndex < 0;
        observedIndex = index;
        // Apply late joiners' current scene even when there is no change event.
        if (firstSnapshot && index == (int)Enums.Scenes.Menu) return;
        SceneChanged(index);
    }

    public void SceneChanged(int index)
    {
        if (index == (int)Enums.Scenes.Menu)
            SceneRouter.Instance.QuitMapScene();
        
        if (index == (int)Enums.Scenes.Map)
        {
            if (wasInGame) SceneRouter.Instance.GameEnds();
            else SceneRouter.Instance.HostStarts();
        }
        
        if (index == (int)Enums.Scenes.Grass)
            SceneRouter.Instance.MapChosen(Enums.Maps.Grass);
        
        if (index == (int)Enums.Scenes.Ice)
            SceneRouter.Instance.MapChosen(Enums.Maps.Ice);
        
        if (index == (int)Enums.Scenes.Lava)
            SceneRouter.Instance.MapChosen(Enums.Maps.Lava);
        
        if (index == (int)Enums.Scenes.Space)
            SceneRouter.Instance.MapChosen(Enums.Maps.Space);
        
        if (index == (int)Enums.Scenes.Undersea)
            SceneRouter.Instance.MapChosen(Enums.Maps.Undersea);

        wasInGame = index >= (int)Enums.Scenes.Grass && index <= (int)Enums.Scenes.Undersea;
    }
    
}
