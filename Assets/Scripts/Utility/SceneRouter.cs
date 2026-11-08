using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

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

    /* Only to test the Scene Router
    private void Update()
    {
        if (Keyboard.current.aKey.wasPressedThisFrame)
            HostStarts();
        if (Keyboard.current.bKey.wasPressedThisFrame)
            MapChosen(Enums.Maps.Grass);
        if (Keyboard.current.cKey.wasPressedThisFrame)
            GameEnds();
        if (Keyboard.current.dKey.wasPressedThisFrame)
            QuitMapScene();
    } */
    
    ////////////////////////////////
    //
    ////////////////////////////////

    public void HostStarts() => StartCoroutine(HostStartsLoad());
    public void QuitMapScene() => StartCoroutine(QuitMapSceneLoad());
    public void MapChosen(Enums.Maps mapChosen) => StartCoroutine(MapChosenLoad(mapChosen));
    public void GameEnds() => StartCoroutine(GameEndsLoad());

    // Coroutines
    private IEnumerator HostStartsLoad()
    {
        if (IsLoading) yield break;
        IsLoading = true;
        GameSceneReady = false;
        OnExitMenuScene?.Invoke();

        yield return SceneManager.LoadSceneAsync(loading_scene, LoadSceneMode.Single);
        AsyncOperation asyncLoad = 
            SceneManager.LoadSceneAsync(map_scene, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
#if UNITY_EDITOR
            Debug.Log($"Loading Map Scene: %{asyncLoad.progress}");
#endif
            yield return null;
        }
        
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(map_scene));
        yield return SceneManager.UnloadSceneAsync(loading_scene);
        IsLoading = false;
        OnStartMapScene?.Invoke();
        RPC_OnStartMapScene?.Invoke();
    }
    
    ////////////////////////////////
    //
    ////////////////////////////////
    
    private IEnumerator QuitMapSceneLoad()
    {
        if (IsLoading) yield break;
        IsLoading = true;
        GameSceneReady = false;
        OnExitMapScene?.Invoke();

        yield return SceneManager.LoadSceneAsync(loading_scene, LoadSceneMode.Single);
        AsyncOperation asyncLoad = 
            SceneManager.LoadSceneAsync(menu_scene, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
#if UNITY_EDITOR
            Debug.Log($"Loading Menu Scene: %{asyncLoad.progress}");
#endif
            yield return null;
        }
        
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(menu_scene));
        yield return SceneManager.UnloadSceneAsync(loading_scene);
        IsLoading = false;
        OnStartMenuScene?.Invoke();
        RPC_OnStartMenuScene?.Invoke();
    }
    
    
    ////////////////////////////////
    //
    ////////////////////////////////

    private IEnumerator MapChosenLoad(Enums.Maps mapChosen)
    {
        if (IsLoading) yield break;
        IsLoading = true;
        GameSceneReady = false;
        OnExitMapScene?.Invoke();

        yield return SceneManager.LoadSceneAsync(loading_scene, LoadSceneMode.Single);
        AsyncOperation asyncLoad = new AsyncOperation();
        
        // Multiplex between possible maps
        switch (mapChosen)
        {
            case Enums.Maps.Undersea:
                asyncLoad = SceneManager.LoadSceneAsync(undersea_scene, LoadSceneMode.Additive);
                break;
            case Enums.Maps.Ice:
                asyncLoad = SceneManager.LoadSceneAsync(ice_scene, LoadSceneMode.Additive);
                break;
            case Enums.Maps.Lava:
                asyncLoad = SceneManager.LoadSceneAsync(lava_scene, LoadSceneMode.Additive);
                break;
            case Enums.Maps.Space:
                asyncLoad = SceneManager.LoadSceneAsync(space_scene, LoadSceneMode.Additive);
                break;
            case Enums.Maps.Grass:
                asyncLoad = SceneManager.LoadSceneAsync(grass_scene, LoadSceneMode.Additive);
                break;
            default:
                Debug.LogError("Uh oh. Something seriously went wrong with the Scene Router rip. ");
                break;
        }
        
        while (!asyncLoad.isDone)
        {
#if UNITY_EDITOR
            Debug.Log($"Loading Menu Scene: %{asyncLoad.progress}");
#endif
            yield return null;
        }
        
        SceneManager.SetActiveScene(SceneManager.GetSceneAt(SceneManager.sceneCount - 1));
        yield return SceneManager.UnloadSceneAsync(loading_scene);
        IsLoading = false;
        GameSceneReady = true;
        OnStartGameScene?.Invoke();
        RPC_OnStartGameScene?.Invoke();        
    }

    ////////////////////////////////
    //
    ////////////////////////////////
    
    private IEnumerator GameEndsLoad()
    {
        if (IsLoading) yield break;
        IsLoading = true;
        GameSceneReady = false;
        OnExitGameScene?.Invoke();

        yield return SceneManager.LoadSceneAsync(loading_scene, LoadSceneMode.Single);
        AsyncOperation asyncLoad = 
            SceneManager.LoadSceneAsync(map_scene, LoadSceneMode.Additive);

        while (!asyncLoad.isDone)
        {
#if UNITY_EDITOR
            Debug.Log($"Loading Menu Scene: %{asyncLoad.progress}");
#endif
            yield return null;
        }
        
        SceneManager.SetActiveScene(SceneManager.GetSceneByName(map_scene));
        yield return SceneManager.UnloadSceneAsync(loading_scene);
        IsLoading = false;
        OnStartMapScene?.Invoke();
        RPC_OnStartMapScene?.Invoke();
    }
    
}
