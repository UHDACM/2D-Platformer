using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneRouter : MonoBehaviourPlus
{
    public override string prettyName => "Scene Router";
    public override string description => "Controlls the many Scenes in the game. Lets us go from Menu->Map->Game and back";

    ////////////////////////////////
    //
    ////////////////////////////////
    /// 
    public bool IsLoading { get; private set; }
    public bool GameSceneReady { get; private set; }

    public Action RPC_OnStartMenuScene;
    public Action RPC_OnStartMapScene;
    public Action RPC_OnStartGameScene;
    
    public event Action OnStartMenuScene;
    public event Action OnStartMapScene;
    public event Action OnStartGameScene;
    
    public event Action OnExitMenuScene;
    public event Action OnExitMapScene;
    public event Action OnExitGameScene;
    
    // Scene names
    [Header("System Scenes")]
    public string menu_scene;
    public string map_scene;
    public string loading_scene;

    [Header("Map Scenes")]
    public string undersea_scene;
    public string ice_scene;
    public string lava_scene;
    public string space_scene;
    public string grass_scene;

    
    // static instance, only 1 per build.
    public static SceneRouter Instance  { get; private set; }
    
    
    ////////////////////////////////
    //
    ////////////////////////////////
    
    // Once the Game starts
    private void Awake()
    {
        // Delete duplicates
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        Instance = this;
        DontDestroyOnLoad(this.gameObject);

    }

    public void HostStarts() => StartCoroutine(Transition(map_scene, OnExitMenuScene, OnStartMapScene, RPC_OnStartMapScene));
    public void QuitMapScene() => StartCoroutine(Transition(menu_scene, OnExitMapScene, OnStartMenuScene, RPC_OnStartMenuScene));
    public void GameEnds() => StartCoroutine(Transition(map_scene, OnExitGameScene, OnStartMapScene, RPC_OnStartMapScene));

    public void MapChosen(Enums.Maps mapChosen)
    {
        string destination;
        switch (mapChosen)
        {
            case Enums.Maps.Undersea: destination = undersea_scene; break;
            case Enums.Maps.Ice: destination = ice_scene; break;
            case Enums.Maps.Lava: destination = lava_scene; break;
            case Enums.Maps.Space: destination = space_scene; break;
            case Enums.Maps.Grass: destination = grass_scene; break;
            default:
                Debug.LogError($"SceneRouter: unknown map '{mapChosen}'.", this);
                return;
        }
        StartCoroutine(Transition(destination, OnExitMapScene, OnStartGameScene, RPC_OnStartGameScene, true));
    }

    // Inspector references can be full asset paths or short scene names.
    // Never fall back to a same-named scene when an explicit path was supplied.
    public static Scene FindLoadedScene(string sceneReference)
    {
        if (string.IsNullOrWhiteSpace(sceneReference)) return default;
        string reference = sceneReference.Replace('\\', '/');
        if (reference.Contains("/"))
        {
            if (!reference.EndsWith(".unity", StringComparison.OrdinalIgnoreCase)) reference += ".unity";
            return SceneManager.GetSceneByPath(reference);
        }
        if (reference.EndsWith(".unity", StringComparison.OrdinalIgnoreCase))
            reference = reference.Substring(0, reference.Length - ".unity".Length);
        return SceneManager.GetSceneByName(reference);
    }

    bool CanLoad(string sceneReference)
    {
        if (!string.IsNullOrWhiteSpace(sceneReference) && Application.CanStreamedLevelBeLoaded(sceneReference)) return true;
        Debug.LogError($"SceneRouter: scene '{sceneReference}' is missing or disabled in the active Build Profile scene list. " +
            "Use TestMultiplayer or include the menu, loading, map-selection, and gameplay scenes.", this);
        return false;
    }

    IEnumerator Transition(string destination, Action leaving, Action arrived, Action rpc, bool gameScene = false)
    {
        if (IsLoading) yield break;
        // Validate before unloading the current scene or despawning its network controller.
        if (!CanLoad(loading_scene) || !CanLoad(destination)) yield break;
        IsLoading = true;
        GameSceneReady = false;
        try
        {
            leaving?.Invoke();
            yield return SceneManager.LoadSceneAsync(loading_scene, LoadSceneMode.Single);
            yield return SceneManager.LoadSceneAsync(destination, LoadSceneMode.Additive);

            Scene loaded = FindLoadedScene(destination);
            if (!loaded.IsValid() || !loaded.isLoaded || !SceneManager.SetActiveScene(loaded))
            {
                Debug.LogError($"SceneRouter: loaded destination '{destination}' could not be activated.", this);
                yield break;
            }
            Scene loading = FindLoadedScene(loading_scene);
            if (loading.IsValid() && loading.isLoaded && loading != loaded)
                yield return SceneManager.UnloadSceneAsync(loading);
            GameSceneReady = gameScene;
        }
        finally
        {
            // A failed load must not permanently block future transitions.
            IsLoading = false;
        }
        arrived?.Invoke();
        rpc?.Invoke();
    }
}
